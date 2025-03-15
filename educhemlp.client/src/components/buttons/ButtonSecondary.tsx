interface ButtonProps {
    text?: string;
    icon?: string | null;
}

export const ButtonSecondary = ({ text = "Odeslat", icon = null}: ButtonProps) => {
    return (
        <button className={"button-secondary"}>
            {   icon ?
                <div className={"icon"} style={{ maskImage: `url(${icon})`}}></div>
                : null
            }
            {text}
        </button>
    );
}