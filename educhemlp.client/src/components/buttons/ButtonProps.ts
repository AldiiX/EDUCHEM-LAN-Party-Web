export interface ButtonProps {
    text?: string;
    icon?: string | null;
    onClick?: () => void;
    className?: string;
    type: ButtonType;
    style?: ButtonStyle;
    form?: string | null;
    buttonType?: "button" | "submit" | "reset";
    name?: string | null;
    disabled?: boolean;
}
export enum ButtonType {
    PRIMARY = "primary",
    SECONDARY = "secondary",
    TERTIARY = "tertiary",
    TERTIARY_RICH = "tertiary-rich",
}

export enum ButtonStyle {
    NORMAL = "normal",
    ROUNDER = "rounder",
}