import styles from './TextWithIcon.module.scss';

interface TextWithIconProps {
    text: string;
    iconSrc: string;
    iconSize?: number;
    textSize?: number;
    className?: string;
    color?: string;
    onClick?: () => void;
}

export const TextWithIcon = ({ text, iconSrc, iconSize = 14, textSize = 16, className, color = "var(--text-color)", onClick }: TextWithIconProps) => {
    const classes = [styles.main];
    if (className) classes.push(className);
    if(onClick) classes.push(styles.clickable);


    return (
        <div className={classes.join(" ")} onClick={onClick}>
            <div style={{ maskImage: `url(${iconSrc})`, width: iconSize, height: iconSize, maskPosition: "center", maskSize: "contain", backgroundColor: color, maskRepeat: "no-repeat" }}></div>
            <span style={{ fontSize: textSize, color: color }}>{text}</span>
        </div>
    );
}