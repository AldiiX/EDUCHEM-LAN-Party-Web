interface ButtonProps {
    text?: string;
    icon?: string | null;
    onClick?: () => void;
}

export const ButtonPrimary = ({ text = "Odeslat", icon = null, onClick = () => {}}: ButtonProps) => {
    return (
        <button className={"button-primary"} onClick={onClick}>
            {   icon ?
                    <div className={"icon"} style={{ maskImage: `url(${icon})`}}></div>
                : null
            }
            {text}
        </button>
    );
}