export interface ButtonProps {
    text?: string;
    icon?: string | null;
    onClick?: () => void;
    className?: string;
    type: ButtonType;
    style?: ButtonStyle;
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