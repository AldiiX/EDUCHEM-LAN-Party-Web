import {Link, useLocation} from "react-router-dom";
import "./BasicErrorView.scss";
import {Button} from "./buttons/Button.tsx";
import {ButtonType} from "./buttons/ButtonProps.ts";

export const BasicErrorView = () => {
    const location = useLocation();
    const backButtonLink = location.state?.from ?? location.pathname.startsWith("/app/") ? "/app/account" : "/";

    return (
        <div className="errordiv">
            <div className="error-nadpis">
                <h1>4</h1>
                <div className="logo"></div>
                <h1>4</h1>
            </div>

            <h2>Stránka nenalezena</h2>
            <p>Omlouváme se, ale stránka, kterou hledáte, nebyla nalezena.</p>
            <Link to={backButtonLink}>
                <Button type={ButtonType.PRIMARY} text="Zpět" />
            </Link>
        </div>
    )
}