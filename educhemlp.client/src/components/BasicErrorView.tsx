import {Link, useLocation} from "react-router-dom";
import {ButtonPrimary} from "./buttons/ButtonPrimary.tsx";
import "./BasicErrorView.scss";

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
                <ButtonPrimary text="Zpět" />
            </Link>
        </div>
    )
}