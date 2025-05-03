import {ButtonStyle, ButtonType} from "./ButtonProps.ts";
import { type ButtonProps } from "./ButtonProps.ts";

export const Button = ({ text = "Odeslat", icon = null, onClick = () => {}, style = ButtonStyle.NORMAL, type, className = "", form = null, buttonType = "button", name = null, disabled = false}: ButtonProps) => {
    return (
        <button className={`button-${type} style-${style} ${className}`} onClick={onClick} {...form ? { form: form } : {}} type={buttonType} { ...name ? { name: name } : {} } { ...disabled ? { disabled: disabled } : {} }>
            {
                icon ?
                    <div className={"icon"} style={{ maskImage: `url(${icon})`}}></div>
                : null
            }

            { text }
        </button>
    );
}