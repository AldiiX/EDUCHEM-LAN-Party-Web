import {AppLayout, AppLayoutTitleBarType} from "./AppLayout.tsx";
import React, {useEffect, useRef, useState} from "react";
import "./Reservations.scss";
import {SpiralUpper} from "../../components/reservation_areas/SpiralUpper.tsx";
import {SpiralLower} from "../../components/reservation_areas/SpiralLower.tsx";
import {useStore} from "../../store.tsx";
import {PieChart} from "../../components/PieChart.tsx";
import {Avatar} from "../../components/Avatar.tsx";
import {Skeleton} from "@mui/material";
import {ButtonPrimary} from "../../components/buttons/ButtonPrimary.tsx";
import {ButtonSecondary} from "../../components/buttons/ButtonSecondary.tsx";
import {Link} from "react-router-dom";
import MoveableMap from "../../components/MoveableMap.tsx";
import {toast} from "react-toastify";
import { create } from "zustand";




// global store
const useReservationsStore = create((set: any) => ({
    selectedReservation: null as SelectedReservation | null,
    setSelectedReservation: (reservation: SelectedReservation | null) => set({ selectedReservation: reservation }),

    socket: null as WebSocket | null,
    setSocket: (socket: WebSocket | null) => set({ socket: socket }),
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
        avatar: string,
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
    const loggedUser = useStore((state) => state.loggedUser);
    const selectedReservation = useReservationsStore((state) => state.selectedReservation);
    const setSelectedReservation = useReservationsStore((state) => state.setSelectedReservation);
    const socket: WebSocket | null = useReservationsStore((state) => state.socket);

    const reserve = async (room: string | null, computer: string | null) => {
        if(!socket) {
            toast.error("Rezervace není dostupná, zkuste to prosím později.");
            return;
        }

        socket.send(JSON.stringify({
            action: "reserve",
            room: room,
            computer: computer,
        }));
    }

    const deleteReservation = async () => {
        if(!socket) return;

        socket.send(JSON.stringify({ action: "deleteReservation" }));
    }

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

                {/* prostřední info */}
                {selectedReservation?.type === "computer" ? (
                    selectedReservation?.reservations.length !== 0 ? (
                        loggedUser !== null && selectedReservation?.reservations?.[0]?.user !== "unknown" ? (
                            <div className="reservedby">
                                <div className="nameandavatar">
                                    <Avatar size="24px" src={selectedReservation?.reservations?.[0]?.user?.avatar} name={selectedReservation?.reservations?.[0]?.user?.displayName}/>
                                    <p>{selectedReservation.reservations[0]?.user?.displayName}</p>
                                </div>
                                <p className="class">{selectedReservation.reservations[0]?.user?.class}</p>
                            </div>
                        ) : (
                            <div className="reservedby">
                                <p>Obsazeno</p>
                            </div>
                        )
                    ) : null
                ) : (
                    <>
                        <div className="status">
                            <h2>Rezervace</h2>
                            <p>
                                {selectedReservation.reservations.length} /{" "}
                                {selectedReservation.limitOfSeats}
                            </p>
                        </div>

                        {loggedUser !== null ? (
                            <div className="reservations-parent">
                                {selectedReservation.reservations.map((reservation: any, index: number) => {
                                    return (
                                        <div
                                            key={index}
                                            className={
                                                "reservation" +
                                                (reservation.user.id === loggedUser?.id ? " you" : "")
                                            }
                                        >
                                            <Avatar
                                                size="24px"
                                                src={reservation.user?.avatar}
                                                name={reservation?.user?.displayName}
                                            />
                                            <div className="texts">
                                                <p className="name">
                                                    {reservation.user.displayName}{" "}
                                                    <span>{reservation.user.class}</span>
                                                </p>
                                                {/* <p className="date">
                                                                      {new Date(reservation.createdAt).toLocaleString("cs-CZ")}
                                                                    </p> */}
                                            </div>
                                        </div>
                                    );
                                })}
                            </div>
                        ) : (
                            selectedReservation.reservations.length < selectedReservation.limitOfSeats ? (
                                <>
                                    <div className="divider"></div>
                                    <p>Pro rezervování <Link to="/login" style={{ color: "var(--accent-color)"}}>se přihlaš</Link>.</p>
                                </>
                            ) : null
                        )}
                    </>
                )}

                {/* Tlačítka */}
                {loggedUser !== null ? (
                    selectedReservation?.type === "computer" ? (
                        selectedReservation?.reservations?.[0]?.user !== "unknown" && selectedReservation?.reservations?.[0]?.user?.id === loggedUser?.id ? (
                            <>
                                <div className="divider"></div>
                                <div className="buttons">
                                    <ButtonSecondary text="Zrušit rezervaci" icon="/images/icons/cancel.svg" onClick={() => deleteReservation()} />
                                </div>
                            </>
                        ) : selectedReservation?.reservations.length === 0 ? (
                            <>
                                <div className="divider"></div>
                                <div className="buttons">
                                    <ButtonPrimary text="Rezervovat" icon="/images/icons/computer.svg" onClick={() => reserve(null, selectedReservation?.id) } />
                                </div>
                            </>
                        ) : null
                    ) : selectedReservation?.reservations.find((r: any) => r.user?.id === loggedUser?.id) ? (
                        <>
                            <div className="divider"></div>
                            <div className="buttons">
                                <ButtonSecondary text="Zrušit rezervaci" icon="/images/icons/cancel.svg" onClick={() => deleteReservation() } />
                            </div>
                        </>
                    ) : selectedReservation?.reservations.length < selectedReservation?.limitOfSeats ? (
                        <>
                            <div className="divider"></div>
                            <div className="buttons">
                                <ButtonPrimary text="Rezervovat" icon="/images/icons/door.svg" onClick={() => reserve(selectedReservation?.id, null) } />
                            </div>
                        </>
                    ) : null
                ) : selectedReservation?.reservations.length === 0 ? (
                    <>
                        <div className="divider"></div>
                        <p>Pro rezervování <Link to="/login" style={{ color: "var(--accent-color)"}}>se přihlaš</Link>.</p>
                    </>
                ) : null}
            </div>
        </div>
    )
}














// main
export const Reservations = () => {
    const areas = [
        // { id: "havran-kulturni-dum", name: "Kulturní dům Havraň" },
        { id: "spiral-upper", name: "Spirála - Horní patro" },
        { id: "spiral-lower", name: "Spirála - Dolní patro" },
    ];

    const [selectedArea, setSelectedArea] = useState<string>(areas[0].id);
    const [computers, setComputers] = useState<Computer[]>([]);
    const [reservations, setReservations] = useState<Reservation[]>([]);
    const [rooms, setRooms] = useState<Room[]>([]);
    const [roomsCapacity, setRoomsCapacity] = useState<number>(0);
    const [occupiedPercent, setOccupiedPercent] = useState(0);
    const [clientsCount, setClientsCount] = useState<string>("?");
    const setSocket = useReservationsStore((state) => state.setSocket);
    const [socketStatus, setSocketStatus] = useState<ReservationSocketStatus>(ReservationSocketStatus.DEFAULT);
    const [statsCollapsed, setStatsCollapsed] = useState<boolean>(false);
    const [enableReservationsStats, setEnableReservationsStats] = useState<boolean>(true);
    const loggedUser = useStore((state) => state.loggedUser);
    const selectedReservation = useReservationsStore((state) => state.selectedReservation);
    const setSelectedReservation = useReservationsStore((state) => state.setSelectedReservation);
    const mapRef = useRef<HTMLDivElement>(null);


    // region ostatní funkce
    const receiveSocketMessage = (message: string) => {
        const object = JSON.parse(message);

        switch (object.action) {
            case "fetchAll": {
                setComputers(object.computers as any[])
                setReservations(object.reservations as any[]);
                setRooms(object.rooms as any[]);

                let roomsCapac = 0;
                for(let room of object.rooms) {
                    room = room as any;
                    roomsCapac += room.limitOfSeats;
                }

                setRoomsCapacity(roomsCapac);
                setSocketStatus(ReservationSocketStatus.CONNECTED);
            } break;

            case "status": {
                setSocketStatus(ReservationSocketStatus.CONNECTED);
                setClientsCount(object.connectedUsers.length.toString());
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
        for(let reservation of reservations) {
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
                if (loggedUser?.id && reservations.filter(r => r.room?.id === room.id && r.user !== "unknown" && r.user.id === loggedUser?.id).length > 0) {
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
    // endregion



    // připojení k websocketu
    useEffect(() => {
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

        setSocket(ws);

        return () => {
            ws.close();
            setSocketStatus(ReservationSocketStatus.DEFAULT);
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

            setOccupiedPercent(Math.round(reservations.length / (computers.length + roomsAllSeats) * 100));

            if(selectedReservation !== null) {
                selectReservation(selectedReservation.element);
            }
        }
    }, [computers, rooms, reservations, selectedArea]);

    // efekt co se zmeni kdyz se odloaduji rezervace
    useEffect(() => {
        if(reservations.length === 0 && socketStatus === ReservationSocketStatus.DISCONNECTED) {
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



    return (
        <AppLayout className="reservations" titleBarText="Rezervace" titleBarType={AppLayoutTitleBarType.STATIC}>
            <h1>Rezervace</h1>

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

            <div className={"map-wrapper"}>
                <div className="map" ref={mapRef}>
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
                            <p className="text">
                                { socketStatus === null
                                    ? "Připojování k serveru..."
                                    : socketStatus === ReservationSocketStatus.CONNECTED
                                        ? "Připojeno k serveru"
                                        : socketStatus === ReservationSocketStatus.DISCONNECTED
                                            ? "Chyba připojení k serveru, restartuj stránku"
                                            : null
                                }
                            </p>
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
                        {selectedArea === "spiral-upper" ? (
                            <SpiralUpper onHoverReservation={(element: HTMLElement) => { selectReservation(element) }} />
                        ) : selectedArea === "spiral-lower" ? (
                            <SpiralLower />
                        ) : null}
                    </MoveableMap>
                </div>

                { /* prava strana map divu (staty, vsechny rezervace) */ }
                { enableReservationsStats && (
                    <div className={"stats" + (statsCollapsed ? " collapsed" : "")}>
                        <div className={"collapser" + (statsCollapsed ? " collapsed" : "" )} onClick={() => statsCollapsed ? setStatsCollapsed(false) : setStatsCollapsed(true) }></div>

                        <div className={"block mainstats"}>
                            <h1>Statistiky</h1>
                            <p>Počet rezervovaných PC: <span>{reservations.filter(r => r.computer !== null).length}/{computers.length}</span></p>
                            <p>Počet rezervovaných míst: <span>{reservations.filter(r => r.room !== null).length}/{roomsCapacity}</span></p>
                            <p>Celkem rezervací: <span>{reservations.length}/{computers.length + roomsCapacity}</span></p>

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
                                        reservations.length === 0 ?
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
                                            :
                                            reservations.sort((a,b) => {
                                                return new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime();
                                            }).map((reservation, index) => {
                                                reservation = reservation as any;
                                                if(reservation.user === "unknown") return null;

                                                return (
                                                    <div key={index} className={"reservation"}>
                                                        <Avatar size={"40px"} src={reservation.user?.avatar} name={reservation.user?.displayName} />

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