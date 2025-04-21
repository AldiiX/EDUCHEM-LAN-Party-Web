import {AppLayout} from "./AppLayout.tsx";
import {useEffect, useRef, useState} from "react";
import "./Map.scss";
import {PieChart} from "../../components/PieChart.tsx";
import {Avatar} from "../../components/Avatar.tsx";
import {Link} from "react-router-dom";
import {SpiralUpper} from "../../components/reservation_areas/SpiralUpper.tsx";
import {SpiralLower} from "../../components/reservation_areas/SpiralLower.tsx";
import MoveableMap from "../../components/MoveableMap.tsx";

export const Map = () => {
    const areas = [
        //{ id: "havran-kulturni-dum", name: "Kulturní dům Havraň" },
        { id: "spiral-upper", name: "Spirála - Horní patro" },
        { id: "spiral-lower", name: "Spirála - Dolní patro" },
    ];

    const [selectedArea, setSelectedArea] = useState<string>(areas[0].id);
    const [scale, setScale] = useState<number>(1);
    const [translate, setTranslate] = useState({ x: 0, y: 0 });
    const [isDragging, setIsDragging] = useState(false);
    const [startPos, setStartPos] = useState({ x: 0, y: 0 });
    const mapRef = useRef<HTMLDivElement>(null);

    // region funkce
    const handleZoom = (e: React.WheelEvent<SVGSVGElement>) => {
        //e.preventDefault();
        const scaleFactor = 0.07;
        const newScale =
            e.deltaY < 0 ? Math.min(1.2, scale + scaleFactor) : Math.max(0.4, scale - scaleFactor);

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

        // locknuti selectu vsech elementu
        document.documentElement.classList.add("noselect");
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

        // locknuti selectu vsech elementu
        document.documentElement.classList.remove("noselect");
    };

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


    return (
        <AppLayout className={"map-page"}>
            <h1>Mapa</h1>

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

            <div className="map">
                <MoveableMap displayControls={true}>
                    { 
                        selectedArea === "spiral-upper" ? (
                            <SpiralUpper />
                        ) : selectedArea === "spiral-lower" ? (
                            // <SpiralLower />
                            null
                        ) : null
                    }
                </MoveableMap>
            </div>

        </AppLayout>
    )
}