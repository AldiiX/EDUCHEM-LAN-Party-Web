import {ButtonStyle, ButtonType} from "./ButtonProps.ts";
import { type ButtonProps } from "./ButtonProps.ts";

export const Button = ({ text = "Odeslat", icon = null, onClick = () => {}, style = ButtonStyle.NORMAL, type, className = ""}: ButtonProps) => {
    return (
        <button className={`button-${type} style-${style} ${className}`} onClick={onClick}>
            {
                icon ?
                    <div className={"icon"} style={{ maskImage: `url(${icon})`}}></div>
                : null
            }

            { text }
        </button>
    );
}