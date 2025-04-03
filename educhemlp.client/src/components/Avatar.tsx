import "./Avatar.scss";
import React from "react";

const AVATAR_ENABLED: boolean = true;

type AvatarProps = {
    size?: string,
    name: string,
    src?: string,
    className?: string,
    backgroundColor?: string,
    onClick?: () => void,
}

export const Avatar = ({ size = "16px", src, className, backgroundColor, name, onClick }: AvatarProps) => {
    if(!backgroundColor) {
        // Výpočet hashe
        let hash = 0;
        for (let i = 0; i < name?.length; i++) {
            hash = name.charCodeAt(i) + ((hash << 5) - hash);
        }
        hash = Math.abs(hash);

        // Výpočet barevných složek
        let hue = hash % 360;
        let saturation = Math.floor((hash ?? 0) % 20 + 45);
        let lightness = Math.floor((hash ?? 0) % 20 + 45);



        // hardcoded colors
        if(name === "Stanislav Škudrna" || name === "Serhii Yavorskyi") {
            // fialova barva
            hue = 270;
            saturation = 50;
            lightness = 60;
        }



        // Vytvoření HSL barvy
        backgroundColor = `hsl(${hue}, ${saturation}%, ${lightness}%)`;
    }

    const letter = name?.split(" ").map((word) => word[0]).join("").slice(0, 2);

    return (
        <div onClick={onClick} className={"avatar" + (className ? " " + className : "")} style={{ backgroundColor: !src || !AVATAR_ENABLED ? backgroundColor : "transparent",  "--size": size } as React.CSSProperties}>
            {
                src && AVATAR_ENABLED ? (
                    <img className={"image"} src={src} alt="avatar" />
                ) : (
                    <p className={"letter"}>{letter?.toUpperCase()}</p>
                )
            }
        </div>
    )
}