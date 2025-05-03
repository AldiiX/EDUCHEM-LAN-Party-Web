import scss from './Banner.module.scss';
import {CSSProperties} from "react";


export const Banner = ({
    src = null as string | null,
    opacity = 0.25 as number,
    className = "" as string,
    sx = {} as CSSProperties
}) => {
    if(!src) return null;

    return (
        <div className={scss.banner + " " + className} style={{ '--banner': `url(${src})`, opacity, ...sx } as CSSProperties }></div>
    )
}