import "./Avatar.scss";
import React from "react";

type AvatarProps = {
    size?: string,
    src?: string,
    letter?: string,
    className?: string,
    backgroundColor?: string
}

export const Avatar = ({ size = "16px", src, letter, className, backgroundColor = "white" }: AvatarProps) => {
    return (
        <div className={"avatar" + (className ? " " + className : "")} style={{ backgroundColor: backgroundColor,  "--size": size } as React.CSSProperties}>
            <p className={"letter"}>{letter}</p>
            {
                src ?
                <img className={"image"} src={src} alt="avatar" />
                : null
            }
        </div>
    )
}