import {AppLayout, AppLayoutTitleBarType} from "./AppLayout.tsx";
import React, {CSSProperties, MutableRefObject, useEffect, useRef, useState} from "react";
import "./Reservations.scss";
import {SpiralUpper} from "../../components/reservation_areas/SpiralUpper.tsx";
import {useStore} from "../../store.tsx";
import {PieChart} from "../../components/PieChart.tsx";
import {Avatar} from "../../components/Avatar.tsx";
import {Skeleton} from "@mui/material";
import {Link} from "react-router-dom";
import MoveableMap from "../../components/MoveableMap.tsx";
import {toast} from "react-toastify";
import {create} from "zustand";
import {Button} from "../../components/buttons/Button.tsx";
import {ButtonType} from "../../components/buttons/ButtonProps.ts";
import {AccountGender, AppSettings, LoggedUser} from "../../interfaces.ts";
import {formatTime, getAppSettings} from "../../utils.ts";
import {ITHub} from "../../components/reservation_areas/ITHub.tsx";
import {areas, RoomMap} from "./Map.tsx";


// global store
const useReservationsStore = create((set: any) => ({
    selectedReservation: null as SelectedReservation | null,
    setSelectedReservation: (reservation: SelectedReservation | null) => set({ selectedReservation: reservation }),

    socket: { current: null } as MutableRefObject<WebSocket | null>,

    selectedReservationLoadingButton: false as boolean,
    setSelectedReservationLoadingButton: (loading: boolean) => set({ selectedReservationLoadingButton: loading }),

    selectedReservationButtonCooldown: false as boolean,
    setSelectedReservationButtonCooldown: (cooldown: boolean) => set({ selectedReservationButtonCooldown: cooldown }),
}));




// interni datovy typy / interface
enum ReservationSocketStatus {
    DEFAULT, // pred prvnim pripojeni (pri prvnim renderu)
    CONNECTED,
    DISCONNECTED,
}

interface Computer {
    id: string,
    image: string,
    isTeachersPC: boolean,
    available?: boolean | null,
}

interface Room {
    id: string,
    label: string,
    image: string,
    limitOfSeats: number,
    reservedSpaces?: number | null,
    available?: boolean | null,
}

interface Reservation {
    id: string,
    computer?: Computer | null,
    room?: Room | null,
    user: {
        id: number,
        displayName: string,
        class: string,
        avatar: string | null,
        banner: string | null,
    } | "unknown", // kdyz uzivatel neni prihlasen, dropne to "unknown"
    createdAt: Date,
    updatedAt: Date,
}

interface SelectedReservation extends Room, Reservation {
    reservations: Reservation[],
    type?: string | null,
    element: HTMLElement,
}








// popup s aktualne zobrazenou rezervaci
const SelectedReservation = () => {
    const loggedUser:LoggedUser | null = useStore((state) => state.loggedUser);
    const selectedReservation = useReservationsStore((state) => state.selectedReservation);
    const setSelectedReservation = useReservationsStore((state) => state.setSelectedReservation);
    const socket = useReservationsStore((state) => state.socket);
    const appSettings: AppSettings = useStore((state) => state.appSettings);
    const buttonLoading = useReservationsStore((state) => state.selectedReservationLoadingButton);
    const setButtonLoading = useReservationsStore((state) => state.setSelectedReservationLoadingButton);
    const buttonCooldown = useReservationsStore((state) => state.selectedReservationButtonCooldown);
    const setButtonCooldown = useReservationsStore((state) => state.setSelectedReservationButtonCooldown);

    const reserve = async (room: string | null, computer: string | null) => {
        if(!appSettings.reservationsEnabledRightNow) {
            toast.error("Změny v rezervacích nejsou povoleny.");
            return;
        }

        if(!socket.current) {
            toast.error("Rezervace není dostupná, zkuste to prosím později.");
            return;
        }

        if(buttonCooldown) {
            return;
        }

        setButtonLoading(true);
        setButtonCooldown(true);
        setTimeout(() => setButtonCooldown(false), 1000);
        
        socket.current.send(JSON.stringify({
            action: "reserve",
            room: room,
            computer: computer,
        }));
    }

    const deleteReservation = async () => {
        if(!appSettings.reservationsEnabledRightNow) {
            toast.error("Změny v rezervacích nejsou povoleny.");
            return;
        }

        if(!socket.current) return;

        if(buttonCooldown) {
            return;
        }

        setButtonLoading(true);
        setButtonCooldown(true);
        setTimeout(() => setButtonCooldown(false), 1000);
        
        socket.current.send(JSON.stringify({ action: "deleteReservation" }));
    }



    // region render button + pomocne komponenty
    const Divider = () => <div className="divider"></div>;

    const UserNotLoggedInfo = () => (
        <>
            <Divider />
            <p>
                Pro rezervování{" "}
                <Link to="/login" style={{ color: "var(--accent-color)" }}>
                    se přihlaš
                </Link>.
            </p>
        </>
    );

    const NoPermissionText = (gender: any) => (
        <>
            <Divider />
            <div className="buttons">
                <p style={{ width: 205, color: "var(--accent-color)", textAlign: "justify" }}>Nejsi oprávněn{gender === "FEMALE" ? "a" : ""} k rezervaci.</p>
            </div>
        </>
    );

    const ReservationButton = (text: string, icon: string, onClick: () => void, type: ButtonType) => (
        <>
            <Divider />
            <div className="buttons">
                <Button type={type} text={text} icon={icon} onClick={onClick} loading={buttonLoading} disabled={buttonCooldown} />
            </div>
        </>
    );

    function renderButton(): React.JSX.Element | null {
        if (!appSettings.reservationsEnabledRightNow) return null;
        if (!selectedReservation) return null;

        const isLoggedIn = loggedUser !== null;
        const isComputer = selectedReservation.type === "computer";
        const isRoom = selectedReservation.type === "room";
        const isMyReservation = selectedReservation.reservations.some(r => r.user !== "unknown" && r.user?.id === loggedUser?.id);
        const hasPermission = loggedUser?.enableReservation;
        const hasFreeSeats = isComputer
            ? selectedReservation.reservations.length === 0
            : selectedReservation.reservations.length < (selectedReservation.limitOfSeats || 0);

        // nepřihlášený a místo je volné
        if (!isLoggedIn && hasFreeSeats) return <UserNotLoggedInfo />;

        // přihlášený
        if (isLoggedIn) {
            if (!hasPermission && hasFreeSeats) return NoPermissionText(loggedUser?.gender);

            if (isMyReservation) {
                return ReservationButton("Zrušit rezervaci", "/images/icons/cancel.svg", deleteReservation, ButtonType.SECONDARY);
            }

            if (hasFreeSeats) {
                if (isComputer) {
                    return ReservationButton("Rezervovat", "/images/icons/computer.svg", () => reserve(null, selectedReservation.id), ButtonType.PRIMARY);
                }
                if (isRoom) {
                    return ReservationButton("Rezervovat", "/images/icons/door.svg", () => reserve(selectedReservation.id, null), ButtonType.PRIMARY);
                }
            }
        }

        return null;
    }

    // endregion



    // region render middle info + komponenty
    const Status = ({ taken, limit }: { taken: number; limit?: number }) => (
        <div className="status">
            <h2>Rezervace</h2>
            <p>
                {taken} / {limit ?? 0}
            </p>
        </div>
    );

    const ReservationsList = ({reservations, currentUserId}: { reservations: any[]; currentUserId?: string | number | null; }) => (
        <div className="reservations-parent">
            {reservations.map((reservation: any, index: number) => {
                const you = reservation?.user?.id === currentUserId;
                return (
                    <div
                        key={index}
                        className={"reservation" + (you ? " you" : "")}
                    >
                        <Avatar
                            size="24px"
                            src={reservation?.user?.avatar}
                            name={reservation?.user?.displayName}
                        />
                        <div className="texts">
                            <div className="name">
                                <p title={reservation?.user?.displayName}>
                                    {reservation?.user?.displayName}{" "}
                                </p>
                                <span>{reservation?.user?.class}</span>
                            </div>
                            {/* poznamka: pokud budes chtit cas, odkomentuj a dopln createdAt
                        <p className="date">
                            {new Date(reservation.createdAt).toLocaleString("cs-CZ")}
                        </p>
                        */}
                        </div>
                    </div>
                );
            })}
        </div>
    );

    const ComputerReservedBy = ({ user }: { user: any }) => (
        <div className="reservedby">
            <div className="nameandavatar">
                <Avatar
                    size="24px"
                    src={user?.avatar}
                    name={user?.displayName}
                />
                <p title={user?.displayName}>{user?.displayName}</p>
            </div>
            <p className="class">{user?.class}</p>
        </div>
    );

    function renderMiddleInfo(): React.JSX.Element {
        // guard – bez vybrane rezervace se nic nezobrazi
        if (!selectedReservation) return <></>;

        // predvypocet stavu
        const isLoggedIn = loggedUser !== null;
        const isComputer = selectedReservation.type === "computer";
        const isRoom = selectedReservation.type === "room";
        const reservations = selectedReservation.reservations ?? [];
        const hasReservations = reservations.length > 0;
        const limitOfSeats = selectedReservation.limitOfSeats ?? 0;
        const hasFreeSeats = reservations.length < limitOfSeats;

        // pc rezervace
        if (isComputer) {
            // pokud je obsazeno
            if (hasReservations) {
                const first = reservations[0];
                const knownUser = first?.user !== "unknown" && !!first?.user;

                // prihlaseny + znamy uzivatel -> detail "kym je rezervovano"
                if (isLoggedIn && knownUser) {
                    return <ComputerReservedBy user={first.user} />;
                }

                // neprihlaseny nebo neznamy uzivatel -> jen "obsazeno"
                return (
                    <div className="reservedby">
                        <p>Obsazeno</p>
                    </div>
                );
            }

            // neni obsazeno -> nic se nezobrazuje
            return <></>;
        }

        // mistnost
        if (isRoom) {
            // prihlaseny uzivatel -> status + seznam rezervaci
            if (isLoggedIn) {
                return (
                    <>
                        <Status taken={reservations.length} limit={limitOfSeats} />
                        <ReservationsList
                            reservations={reservations}
                            currentUserId={loggedUser?.id}
                        />
                    </>
                );
            }

            // neprihlaseny a stale je misto -> status (prihlaseni je v buttons componente)
            if (!isLoggedIn && hasFreeSeats) {
                return (
                    <>
                        <Status taken={reservations.length} limit={limitOfSeats} />
                    </>
                );
            }

            // neprihlaseny a neni misto -> jen status
            return <Status taken={reservations.length} limit={limitOfSeats} />;
        }

        // fallback – pokud by pribyl jiny typ, ukaz jen status (pokud existuje koncept kapacity)
        return <Status taken={reservations.length} limit={limitOfSeats} />;
    }

    // endregion





    if(!selectedReservation) return null;
    
    return (
        <div className="reservation-popover">
            <div className="closebutton" onClick={() => setSelectedReservation(null) }></div>

            {selectedReservation?.image ? (
                <div
                    className="top"
                    style={{
                        backgroundImage: `url(/images/room_images/${selectedReservation?.image})`,
                    }}
                ></div>
            ) : null}

            <div className="bottom">
                <div className="first">
                    <h1>{selectedReservation?.label ?? selectedReservation?.id}</h1>
                    <p className="status"></p>
                </div>

                {
                    /* prostřední info */
                    renderMiddleInfo()
                }


                {
                    /* Tlačítka */
                    renderButton()
                }
            </div>
        </div>
    )
}















// main
export const Reservations = () => {
    const [selectedArea, setSelectedArea] = useState<string>(areas[0].id);
    const [computers, setComputers] = useState<Computer[]>([]);
    const [reservations, setReservations] = useState<Reservation[] | null>(null);
    const [rooms, setRooms] = useState<Room[]>([]);
    const [roomsCapacity, setRoomsCapacity] = useState<number>(0);
    const [occupiedPercent, setOccupiedPercent] = useState(0);
    const [clientsCount, setClientsCount] = useState<string>("?");
    const socket = useReservationsStore((state) => state.socket);
    const [socketStatus, setSocketStatus] = useState<ReservationSocketStatus>(ReservationSocketStatus.DEFAULT);
    const [statsCollapsed, setStatsCollapsed] = useState<boolean>(false);
    const [enableReservationsStats, setEnableReservationsStats] = useState<boolean>(true);
    const loggedUser = useStore((state) => state.loggedUser);
    const selectedReservation = useReservationsStore((state) => state.selectedReservation);
    const setSelectedReservation = useReservationsStore((state) => state.setSelectedReservation);
    const userAuthed = useStore((state) => state.userAuthed);
    const mapRef = useRef<HTMLDivElement>(null);
    const isFirstRender = useRef(true);
    const appSettings: AppSettings = useStore((state) => state.appSettings);
    const setAppSettings = useStore((state) => state.setAppSettings);
    const [countdownText, setCountdownText] = useState<string | null>(null);
    const [countdownText2, setCountdownText2] = useState<string | null>(null);
    const setSelectedReservationLoadingButton = useReservationsStore((state) => state.setSelectedReservationLoadingButton);


    // region ostatní funkce
    const receiveSocketMessage = (message: string) => {
        const object = JSON.parse(message);

        switch (object.action) {
            case "fetchAll": {
                setComputers(object.computers as any[])
                setReservations(object.reservations as any[]);
                setRooms(object.rooms as any[]);

                let roomsCapac = 0;
                for (let room of object.rooms) {
                    room = room as any;
                    roomsCapac += room.limitOfSeats;
                }

                setSelectedReservationLoadingButton(false);
                setRoomsCapacity(roomsCapac);
                setSocketStatus(ReservationSocketStatus.CONNECTED);
            } break;
            
            case "status": {
                setSocketStatus(ReservationSocketStatus.CONNECTED);
                setClientsCount(object.connectedUsers.length.toString());
            } break;
            
            case"error": {
                toast.error(object.message);

                setSelectedReservationLoadingButton(false);
            } break;
        }
    }
    

    const setCirclesStyle = () => {
        // reset room.reservedSpaces
        for(let room of rooms) {
            room = room as any;
            room.reservedSpaces = 0;
        }

        // compy
        for(let computer of computers) {
            computer = computer as any;
            const id = String(computer.id).toUpperCase();
            const element = document.getElementById(id);
            if (!element) continue;

            element.classList.add("available");
        }

        // roomky
        for(let room of rooms) {
            room = room as any;
            const id = String(room.id).toUpperCase();
            const element = document.getElementById("ROOM_" + id);
            if (!element) continue;

            element.classList.add("available");
        }

        // rezervace
        for(let reservation of (reservations ?? [])) {
            //reservation = reservation as unknown as Reservation[];

            // v pripade ze si uzivatel rezerovoval pc
            if (reservation.computer !== null) {
                const id = String(reservation.computer?.id).toUpperCase();
                const element = document.getElementById(id);
                if (!element) continue;

                element.classList.remove("available");
                if (loggedUser?.id && reservation.user !== "unknown" && reservation.user.id === loggedUser?.id) {
                    element.classList.add("taken-by-you");
                } else {
                    element.classList.add("unavailable");
                }
            }

            // v pripade ze si uzivatel rezervoval mistnost
            else if (reservation.room !== null) {
                const room = rooms.find((room) => room.id === reservation.room?.id);
                if (!room) continue;
                room.reservedSpaces ??= 0;
                room.reservedSpaces++;

                const id = String(room.id).toUpperCase();
                const element = document.getElementById("ROOM_" + id);
                if (!element) continue;



                // prirazeni classy
                element.classList.remove("available", "taken-by-you", "unavailable");
                if (loggedUser?.id && (reservations?.filter(r => r.room?.id === room.id && r.user !== "unknown" && r.user.id === loggedUser?.id).length ?? 0) > 0) {
                    element.classList.add("taken-by-you");
                }

                else if(room.reservedSpaces < room.limitOfSeats) {
                    element.classList.add("available");
                }

                else {
                    element.classList.add("unavailable");
                }

            }

            // v pripade ze si uzivatel rezervoval mistnost
        }
    }

    const unloadAllReservations = () => {
        setReservations([]);
        setStatsCollapsed(true);

        setTimeout(() => {
            setEnableReservationsStats(false);
        }, 300)
    };

    const selectReservation = (element: HTMLElement) => {
        const id = element.id;
        let type: string | null = null;

        if(!computers || !rooms || !reservations) return;

        const r = computers.find((r: any) => {
            type = "computer";
            return r.id === id;
        }) ?? rooms.find((r: any) => {
            type = "room";
            return r.id === id.replace("ROOM_", "");
        }) as Room | Computer | undefined;

        if(!r) return;

        const selectedReservation = r as SelectedReservation;
        selectedReservation.reservations = reservations.filter((res: any) => {
            return res.computer?.id === r.id || res.room?.id === r.id;
        });

        selectedReservation.type = type;
        selectedReservation.element = element;
        //console.log(r);
        setSelectedReservation(selectedReservation);
    }

    const connectToSocket = () => {
        if(socket.current) {
            socket.current.close();
            socket.current = null;
            setSocketStatus(ReservationSocketStatus.DEFAULT);
        }

        const ws = new WebSocket(
            `${location.protocol === 'https:' ? 'wss' : 'ws'}://${window.location.host}/ws/reservations`
        );

        ws.onopen = () => {
            //console.log("connected");
            setEnableReservationsStats(true);
        };

        ws.onmessage = (e) => {
            receiveSocketMessage(e.data);
        };

        ws.onclose = () => {
            //console.log("disconnected");
            setSocketStatus(ReservationSocketStatus.DISCONNECTED);
            unloadAllReservations();
        };

        socket.current = ws;
    }
    // endregion



    // připojení k websocketu
    useEffect(() => {
        connectToSocket();

        return () => {
            socket.current?.close();
            setSocketStatus(ReservationSocketStatus.DEFAULT);
        };
    }, []);

    // odpojení/připojení socketu při opuštění/návratu na stránku
    useEffect(() => {
        const handleVisibilityChange = () => {
            if (document.hidden) {
                // Stránka je skrytá (minimalizovaná nebo uživatel přešel na jinou záložku)
                if (socket.current && socket.current.readyState === WebSocket.OPEN) {
                    socket.current.close();
                }
            } else {
                // Stránka je zase viditelná
                if (!socket.current || socket.current.readyState !== WebSocket.OPEN) {
                    connectToSocket();
                }
            }
        };

        document.addEventListener('visibilitychange', handleVisibilityChange);

        return () => {
            document.removeEventListener('visibilitychange', handleVisibilityChange);
        };
    }, []);

    // efekt pro nastaveni veci po tom co se změní selectReservation
    useEffect(() => {
        if (computers.length > 0 && rooms.length > 0) {
            setCirclesStyle();

            let roomsAllSeats = 0;
            for(let room of rooms) {
                room = room as any;
                roomsAllSeats += room.limitOfSeats;
            }

            setOccupiedPercent(Math.round((reservations?.length ?? 0) / (computers.length + roomsAllSeats) * 100));

            if(selectedReservation !== null) {
                selectReservation(selectedReservation.element);
            }
        }
    }, [computers, rooms, reservations, selectedArea]);

    // efekt co se zmeni kdyz se odloaduji rezervace
    useEffect(() => {
        if(reservations?.length === 0 && socketStatus === ReservationSocketStatus.DISCONNECTED) {
            // compy
            for(let computer of computers) {
                computer = computer as any;
                const id = String(computer.id).toUpperCase();
                const element = document.getElementById(id);
                if (!element) continue;

                element.classList.remove("available", "unavailable", "taken-by-you");
            }

            // roomky
            for(let room of rooms) {
                room = room as any;
                const id = String(room.id).toUpperCase();
                const element = document.getElementById("ROOM_" + id);
                if (!element) continue;

                element.classList.remove("available", "unavailable", "taken-by-you");
            }

            setRoomsCapacity(0);
            setOccupiedPercent(0);
            setClientsCount("?");
            setSocketStatus(ReservationSocketStatus.DISCONNECTED);
            setSelectedReservation(null);
        }
    }, [reservations])

    // effekt kdyz se zmeni loggeduser
    useEffect(() => {
        if(!userAuthed) return;

        if (isFirstRender.current) {
            isFirstRender.current = false;
            return;
        }

        //console.log("logged user changed, reconnecting to socket");

        socket.current?.close();

        setTimeout(() => {
            connectToSocket();
        }, 750);
    }, [loggedUser, userAuthed]);

    // effekt pro časovač když jsou rezervace s časovačem
    useEffect(() => {
        if (appSettings.reservationsStatus !== "USE_TIMER") {
            setCountdownText(null);
            setCountdownText2(null);
            return;
        }

        const updateCountdown = () => {
            const now = Date.now();
            const from = new Date(appSettings.reservationsEnabledFrom).getTime();
            const to = new Date(appSettings.reservationsEnabledTo).getTime();

            if (now < from) {
                const diff = from - now;
                setCountdownText(`Rezervace se otevírají za`);
                setCountdownText2(formatTime(diff));
            } else if (now < to) {
                const diff = to - now;
                setCountdownText(`Rezervace se uzavírají za`);
                setCountdownText2(formatTime(diff));
            } else {
                setCountdownText("Rezervace jsou uzavřeny");
                setCountdownText2(null);
            }

            // v pripade ze se odpocet odpocita, tak se znovu nacte appsettings
            let dateDiff = Math.min(Math.abs(now - from), Math.abs(now - to));

            if(dateDiff < 1000) getAppSettings(setAppSettings);
        }

        updateCountdown();
        const interval = setInterval(updateCountdown, 1000);

        return () => clearInterval(interval);
    }, [appSettings]);



    return (
        <AppLayout className="reservations" titleBarText="Rezervace" titleBarType={AppLayoutTitleBarType.STATIC}>
            <div className="area-selector">
                {areas.map((area) => (
                    <p
                        onClick={() => setSelectedArea(area.id)}
                        key={area.id}
                        className={selectedArea === area.id ? "active" : ""}
                    >
                        {area.name}
                    </p>
                ))}
            </div>

            <div className="map-wrapper">
                <div className="map" ref={mapRef}>

                    <div className="reservations-status">
                        {appSettings.reservationsStatus === "CLOSED" ? (
                            <p>Rezervace jsou uzavřeny</p>
                        ) : (
                            <>
                                <p>{countdownText}</p>
                                <h1>{countdownText2}</h1>
                            </>
                        )}
                    </div>

                    <div className="legend">
                        <h3>Legenda mapy:</h3>
                        <div className="legend-item">
                            <div style={{ backgroundColor: "var(--room-available)" }}></div>
                            <p>Volná místnost pro vlastní setup</p>
                        </div>
                        <div className="legend-item">
                            <div style={{ backgroundColor: "var(--pc-available)" }}></div>
                            <p>Volný počítač</p>
                        </div>
                        <div className="legend-item">
                            <div style={{ backgroundColor: "var(--pc-unavailable)" }}></div>
                            <p>Obsazeno / Nedostupné</p>
                        </div>
                        <div className="legend-item">
                            <div style={{ backgroundColor: "var(--pc-taken-by-you)" }}></div>
                            <p>Tvá rezervace</p>
                        </div>
                    </div>

                    <div className="chart">
                        <PieChart value={occupiedPercent} width={100} height={100} />
                        <div className="texts">
                            <h1>{ occupiedPercent }%</h1>
                            <p>Naplněné kapacity</p>
                        </div>
                    </div>

                    <div className="rightbottom">
                        <div className={
                            socketStatus === ReservationSocketStatus.CONNECTED
                                ? "serverstatus connected"
                                : socketStatus === ReservationSocketStatus.DISCONNECTED
                                    ? "serverstatus disconnected"
                                    : "serverstatus"
                        }>
                            <div className="icon"></div>
                            {
                                socketStatus === null ? (
                                    <p className="text">Připojování k serveru...</p>
                                ) : socketStatus === ReservationSocketStatus.CONNECTED ? (
                                    <p className="text">Připojeno k serveru</p>
                                ) : socketStatus === ReservationSocketStatus.DISCONNECTED ? (
                                    <p className="text">Chyba připojení k serveru, restartuj stránku</p>
                                ) : null
                            }
                        </div>

                        <div className="viewers" title="Připojení uživatelé">
                            <p>{ clientsCount }</p>
                            <div className={"logo"}></div>
                        </div>
                    </div>

                    {/* Popup pri hoveru na nejakou rezervaci */}
                    <SelectedReservation />

                    { /* mapka */ }
                    <MoveableMap displayControls={true}>
                        <RoomMap selectedArea={selectedArea} selectReservation={(element: HTMLElement) => { selectReservation(element) }} />
                    </MoveableMap>
                </div>

                { /* prava strana map divu (staty, vsechny rezervace) */ }
                { enableReservationsStats && (
                    <div className={"stats" + (statsCollapsed ? " collapsed" : "")}>
                        <div className={"collapser" + (statsCollapsed ? " collapsed" : "" )} onClick={() => statsCollapsed ? setStatsCollapsed(false) : setStatsCollapsed(true) }></div>

                        { // pokud uzivatel nema povolene rezervace, zobraz info (jinak se nezobrazi nic)
                            loggedUser?.enableReservation === false && (
                                <div className="block reservationsaccountblocked">
                                    <h1>Tvůj účet nemá povolené rezervace!</h1>
                                    <p>Důvodem může být to, že nemáš zaplacený vstup. Pokud si myslíš, že se jedná o chybu, kontaktuj administrátora.</p>
                                </div>
                            )
                        }

                        <div className={"block mainstats"}>
                            <h1>Statistiky</h1>
                            <p>Počet rezervovaných PC: <span>{reservations?.filter(r => r.computer !== null).length}/{computers.length}</span></p>
                            <p>Počet rezervovaných míst: <span>{reservations?.filter(r => r.room !== null).length}/{roomsCapacity}</span></p>
                            <p>Celkem rezervací: <span>{reservations?.length}/{computers.length + roomsCapacity}</span></p>

                            <div className="chart">
                                <PieChart value={occupiedPercent} width={100} height={100} />
                                <div className="texts">
                                    <h1>{ occupiedPercent }%</h1>
                                    <p>Naplněné kapacity</p>
                                </div>
                            </div>
                        </div>

                        { loggedUser !== null ?
                            <div className={"block reservations"}>
                                <h1>Seznam rezervací</h1>

                                <div className={"reservations-parent"}>
                                    {
                                        reservations === null! ? (
                                            [0,1,2,3,4].map((index) => {
                                                return (
                                                    <div key={index} className={"reservation"}>
                                                        <Skeleton width={40} height={40} variant={"circular"} sx={{ bgcolor: 'var(--text-color-3)' }} animation={"wave"} />

                                                        <div className={"texts"}>
                                                            <Skeleton width={"30%"} sx={{ bgcolor: 'var(--text-color-3)' }} animation={"wave"} />
                                                            <Skeleton width={100} sx={{ bgcolor: 'var(--text-color-3)' }} animation={"wave"} />
                                                            <Skeleton width={"80%"} sx={{ bgcolor: 'var(--text-color-3)' }} animation={"wave"} />
                                                        </div>
                                                    </div>
                                                );
                                            })
                                        ) : reservations?.length === 0 ? (
                                            <>
                                                <p style={{ color: 'var(--text-color-3)' }}>Žádné rezervace ¯\_(ツ)_/¯</p>
                                            </>
                                        ) :
                                            reservations?.sort((a,b) => {
                                                return new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime();
                                            }).map((reservation, index) => {
                                                reservation = reservation as any;
                                                if(reservation.user === "unknown") return null;

                                                return (
                                                    <div key={index} className={"reservation"}>
                                                        <Avatar size={"40px"} src={reservation.user?.avatar} name={reservation.user?.displayName} />

                                                        <div className={"banner profilebannercustomization"} style={{ '--banner': `url(${reservation.user?.banner})`  } as CSSProperties}></div>

                                                        <div className="texts">
                                                            <p className={"name"}>{reservation.user?.displayName} <span>{reservation.user?.class}</span></p>
                                                            <p className={"id"}>{reservation.computer?.id ?? reservation.room?.label}</p>
                                                            <p className={"date"}>{new Date(reservation.createdAt).toLocaleString("cs-CZ" )}</p>
                                                        </div>
                                                    </div>
                                                );
                                            })
                                    }
                                </div>
                            </div>
                            : <div className={"block"}>
                                <p>Pro zobrazení více informací <Link to="/login">se přihlaš</Link>.</p>
                            </div>
                        }
                    </div>
                )}
            </div>
        </AppLayout>
    );
};

export default Reservations;