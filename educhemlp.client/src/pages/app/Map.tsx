import {AppLayout} from "./AppLayout.tsx";
import React, {useEffect, useRef, useState} from "react";
import "./Map.scss";
import {PieChart} from "../../components/PieChart.tsx";
import {Avatar} from "../../components/Avatar.tsx";
import {Link} from "react-router-dom";
import {SpiralUpper} from "../../components/reservation_areas/SpiralUpper.tsx";
import MoveableMap from "../../components/MoveableMap.tsx";
import {ITHub} from "../../components/reservation_areas/ITHub.tsx";


export const areas = [
    // { id: "havran-kulturni-dum", name: "Kulturní dům Havraň" },
    { id: "ithub", name: "IT Hub (Spodní patro)", component: ITHub },
    { id: "spiral-upper", name: "Spirála (Horní patro)", component: SpiralUpper },
];

export function RoomMap({ selectedArea, selectReservation }: {
    selectedArea: string;
    selectReservation: (element: HTMLElement) => void;
}) {
    // nalezeni komponenty dle vybrane oblasti
    const AreaComponent = areas.find(a => a.id === selectedArea)?.component;

    // vykresleni s predanim props
    return AreaComponent ? (
        <AreaComponent onHoverReservation={selectReservation} />
    ) : null;
}

export const Map = () => {
    const [selectedArea, setSelectedArea] = useState<string>(areas[0].id);



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
                    <RoomMap selectedArea={selectedArea} selectReservation={() => {}} />
                </MoveableMap>
            </div>

        </AppLayout>
    )
}