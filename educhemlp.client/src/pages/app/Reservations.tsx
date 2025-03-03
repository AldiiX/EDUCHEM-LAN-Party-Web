import { AppLayout } from "./Layout";
import { useEffect, useState } from "react";
import "./Reservations.scss";
import { SpiralUpper } from "../../components/reservation_areas/SpiralUpper.tsx";
import { SpiralLower } from "../../components/reservation_areas/SpiralLower.tsx";

export const Reservations = () => {
    const areas = [
        { id: "spiral-upper", name: "Spirála - Horní patro" },
        { id: "spiral-lower", name: "Spirála - Dolní patro" },
    ];
    const [scale, setScale] = useState<number>(0.8);
    const [translate, setTranslate] = useState({ x: 384, y: 216 });
    const [isDragging, setIsDragging] = useState(false);
    const [startPos, setStartPos] = useState({ x: 0, y: 0 });
    const [fetchedComputers, setFetchComputers] = useState<any[] | null>(null);
    const [selectedArea, setSelectedArea] = useState<string>(areas[0].id);



    // region funkce
    const handleZoom = (e: React.WheelEvent<SVGSVGElement>) => {
        //e.preventDefault();

        const scaleFactor = 0.07;
        const newScale = e.deltaY < 0 ? Math.min(1.2, scale + scaleFactor) : Math.max(0.8, scale - scaleFactor);

        const rect = e.currentTarget.getBoundingClientRect();
        const mouseX = e.clientX - rect.left;
        const mouseY = e.clientY - rect.top;

        const deltaX = (mouseX - translate.x) * (newScale / scale - 1);
        const deltaY = (mouseY - translate.y) * (newScale / scale - 1);

        setTranslate({
            x: translate.x - deltaX,
            y: translate.y - deltaY
        });

        setScale(newScale);
    };

    const forceZoom = (newScale: number) => {
        setScale(scale + newScale);
    };

    const handleMouseDown = (e: React.MouseEvent<SVGSVGElement>) => {
        setIsDragging(true);

        // limit na pohyb mimo obrazovku
        if (e.clientX < 0 || e.clientY < 0) {
            setIsDragging(false);
            return;
        }

        setStartPos({ x: e.clientX - translate.x, y: e.clientY - translate.y });
    };

    const handleMouseMove = (e: React.MouseEvent<SVGSVGElement>) => {
        if (!isDragging) return;

        // limit na pohyb mimo obrazovku
        if (e.clientX < 0 || e.clientY < 0) {
            setIsDragging(false);
            return;
        }

        setTranslate({ x: e.clientX - startPos.x, y: e.clientY - startPos.y });
    };

    const handleMouseUp = () => {
        setIsDragging(false);
    };

    const setComputersStatus = () => {
        if(fetchedComputers === null || typeof(fetchedComputers) !== "object") return;

        fetchedComputers.forEach(computer => {
            const element = document.querySelector(`svg .pc:is(#${computer.id.toUpperCase()})`);
            if(!element) return;


        });
    }
    // endregion



    // pripojeni k websocketu
    useEffect(() => {
        const isLocalhost = false; //window.location.hostname === "localhost";
        const ws = new WebSocket(`${isLocalhost ? "ws" : "wss"}://${window.location.host}/ws/reservations`);
        ws.onopen = () => {
            console.log("connected");
        };

        ws.onmessage = (e) => {
            console.log(e.data);
        };

        ws.onclose = () => {
            console.log("disconnected");
        };

        return () => {
            ws.close();
        };
    }, []);




    // html
    return (
        <AppLayout className="reservations">
            <h1>Rezervace</h1>

            <div className="area-selector">
                { areas.map(area => (
                    <p
                        onClick={() => setSelectedArea(area.id)}
                        key={area.id}
                        className={ selectedArea === area.id ? "active" : "" }
                    >{ area.name }</p>
                )) }
            </div>

            <div className="map">
                <div className="zoomsettings" style={{ marginBottom: "1rem" }}>
                    <button onClick={() => forceZoom(0.1) }>Zoom In (+)</button>
                    <button onClick={() => forceZoom(-0.1) }>Zoom Out (−)</button>
                </div>
                
                <div className="legend">
                    <h3 >Legenda mapy:</h3>
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
                        <p>Obsazeno / Nedostupné </p>
                    </div>
                    <div className="legend-item">
                        <div style={{ backgroundColor: "var(--pc-taken-by-you)" }}></div>
                        <p>Tvoje místo</p>
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
                        <g className="scaleTransition" transform={`scale(${scale})`}>
                            {
                                selectedArea === "spiral-upper"
                                    ? <SpiralUpper /> :
                                selectedArea === "spiral-lower"
                                    ? <SpiralLower /> :
                                null
                            }
                        </g>
                    </g>
                </svg>
            </div>
        </AppLayout>
    );
};