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
        <div className={"avatar" + (className ? " " + className : "")} style={{ backgroundColor: !src ? backgroundColor : "transparent",  "--size": size } as React.CSSProperties}>
            {
                src ? (
                    <img className={"image"} src={src} alt="avatar" />
                ) : (
                    <p className={"letter"}>{letter?.toUpperCase()}</p>
                )
            }
        </div>
    )
}