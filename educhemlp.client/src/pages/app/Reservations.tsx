import { AppLayout } from "./Layout";
import { useEffect, useRef, useState } from "react";
import "./Reservations.scss";
import { SpiralUpper } from "../../components/reservation_areas/SpiralUpper.tsx";
import { SpiralLower } from "../../components/reservation_areas/SpiralLower.tsx";
import { useStore } from "../../store.tsx";
import {PieChart} from "../../components/PieChart.tsx";

export const Reservations = () => {
    const areas = [
        { id: "spiral-upper", name: "Spirála - Horní patro" },
        { id: "spiral-lower", name: "Spirála - Dolní patro" },
    ];

    // počáteční scale a translation se budou počítat dle rozměrů .map kontejneru
    const [scale, setScale] = useState<number>(1);
    const [translate, setTranslate] = useState({ x: 0, y: 0 });
    const [isDragging, setIsDragging] = useState(false);
    const [startPos, setStartPos] = useState({ x: 0, y: 0 });
    const [selectedArea, setSelectedArea] = useState<string>(areas[0].id);
    const [computers, setComputers] = useState<any[]>([]);
    const [reservations, setReservations] = useState<any[]>([]);
    const [rooms, setRooms] = useState<any[]>([]);
    const [occupiedPercent, setOccupiedPercent] = useState(0);
    const { loggedUser } = useStore();
    const mapRef = useRef<HTMLDivElement>(null);

    useEffect(() => {
        if (mapRef.current) {
            const { clientWidth, clientHeight } = mapRef.current;
            // Spočítáme scale tak, aby se celý SVG vešel do kontejneru
            const initialScale = Math.min(clientWidth / 1960, clientHeight / 1216);
            setScale(initialScale);
            // Vypočítáme vykreslované rozměry SVG při daném scale
            const contentWidth = 1960 * initialScale;
            const contentHeight = 1216 * initialScale;
            // Vycentrujeme obsah – rozdíl dělený dvěma
            const initialTranslateX = (clientWidth - contentWidth) / 2;
            const initialTranslateY = (clientHeight - contentHeight) / 2;
            setTranslate({ x: initialTranslateX, y: initialTranslateY });
        }
    }, []);

    // region funkce
    const handleZoom = (e: React.WheelEvent<SVGSVGElement>) => {
        //e.preventDefault();
        const scaleFactor = 0.07;
        const newScale =
            e.deltaY < 0 ? Math.min(1.2, scale + scaleFactor) : Math.max(0.6, scale - scaleFactor);

        const rect = e.currentTarget.getBoundingClientRect();
        const mouseX = e.clientX - rect.left;
        const mouseY = e.clientY - rect.top;

        const deltaX = (mouseX - translate.x) * (newScale / scale - 1);
        const deltaY = (mouseY - translate.y) * (newScale / scale - 1);

        setTranslate({
            x: translate.x - deltaX,
            y: translate.y - deltaY,
        });
        setScale(newScale);
    };

    const forceZoom = (delta: number) => {
        setScale(prevScale => Math.min(1.5, Math.max(0.7, prevScale + delta)));
    };

    const handleMouseDown = (e: React.MouseEvent<SVGSVGElement>) => {
        setIsDragging(true);
        const rect = e.currentTarget.getBoundingClientRect();
        // Převod pozice myši do SVG souřadnic
        const mouseX = (e.clientX - rect.left) / scale;
        const mouseY = (e.clientY - rect.top) / scale;
        setStartPos({
            x: mouseX - translate.x,
            y: mouseY - translate.y,
        });
    };

    const handleMouseMove = (e: React.MouseEvent<SVGSVGElement>) => {
        if (!isDragging) return;
        const rect = e.currentTarget.getBoundingClientRect();
        const mouseX = (e.clientX - rect.left) / scale;
        const mouseY = (e.clientY - rect.top) / scale;
        setTranslate({
            x: mouseX - startPos.x,
            y: mouseY - startPos.y,
        });
    };

    const handleMouseUp = () => {
        setIsDragging(false);
    };

    const receiveSocketMessage = (message: string) => {
        const object = JSON.parse(message);

        if(object.action === "fetchAll") {
            setComputers(object.computers as any[])
            setReservations(object.reservations as any[]);
            setRooms(object.rooms as any[]);
        }
    }

    const setCirclesStyle = () => {
        // compy
        for(let computer of computers) {
            computer = computer as any;
            const id = String(computer.id).toUpperCase();
            const element = document.getElementById(id);
            if (!element) continue;

            element.classList.add("available");
        }

        // rezervace
        for(let reservation of reservations) {
            reservation = reservation as any[];

            // v pripade ze si uzivatel rezerovoval pc
            if (reservation.computer !== null) {
                const id = String(reservation.computer?.id).toUpperCase();
                const element = document.getElementById(id);
                if (!element) continue;

                element.classList.remove("available");
                if (loggedUser?.id && reservation.user?.id === loggedUser?.id) {
                    element.classList.add("taken-by-you");
                } else {
                    element.classList.add("unavailable");
                }
            }

            // v pripade ze si uzivatel rezervoval mistnost
        }
    }
    // endregion



    // připojení k websocketu
    useEffect(() => {
        const isLocalhost = false;
        const ws = new WebSocket(
            `${isLocalhost ? "ws" : "wss"}://${window.location.host}/ws/reservations`
        );
        ws.onopen = () => {
            console.log("connected");
        };

        ws.onmessage = (e) => {
            receiveSocketMessage(e.data);
        };

        ws.onclose = () => {
            console.log("disconnected");
        };

        return () => {
            ws.close();
        };
    }, []);

    useEffect(() => {
        if (computers.length > 0 && reservations.length > 0) {
            setCirclesStyle();

            let roomsAllSeats = 0;
            for(let room of rooms) {
                room = room as any;
                roomsAllSeats += room.limitOfSeats;
            }

            setOccupiedPercent(Math.round(reservations.length / (computers.length + roomsAllSeats) * 100));
        }
    }, [computers, reservations, selectedArea]);



    return (
        <AppLayout className="reservations">
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

            <div className="map" ref={mapRef}>
                <div className="zoomsettings" style={{ marginBottom: "1rem" }}>
                    <div className="zoom-in" onClick={() => forceZoom(0.1)}></div>
                    <div className="zoom-out" onClick={() => forceZoom(-0.1)}></div>
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
                        <p>Tvoje místo</p>
                    </div>
                </div>

                <div className="chart">
                    <PieChart value={occupiedPercent} width={100} height={100} />
                    <div className="texts">
                        <h1>{ occupiedPercent }%</h1>
                        <p>Naplněné kapacity</p>
                    </div>
                </div>

                <svg
                    id="spiral-upper"
                    width="1960"
                    height="1216"
                    viewBox="0 0 1960 1216"
                    fill="none"
                    xmlns="http://www.w3.org/2000/svg"
                    onWheel={handleZoom}
                    onMouseDown={handleMouseDown}
                    onMouseMove={handleMouseMove}
                    onMouseUp={handleMouseUp}
                    onMouseLeave={handleMouseUp}
                    style={{ cursor: isDragging ? "grabbing" : "grab" }}
                >
                    <g transform={`translate(${translate.x}, ${translate.y}) scale(${scale})`}>
                        <g className="scaleTransition">
                            {selectedArea === "spiral-upper" ? (
                                <SpiralUpper />
                            ) : selectedArea === "spiral-lower" ? (
                                <SpiralLower />
                            ) : null}
                        </g>
                    </g>
                </svg>
            </div>
        </AppLayout>
    );
};

export default Reservations;
