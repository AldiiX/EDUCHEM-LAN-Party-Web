import {Link} from "react-router-dom";
import "./Layout.scss";

export const AppLayout = ({ children }: { children: React.ReactNode}) => {
    return (
        <div id="app">
            <div className={"left"}>
                <h1>EduchemLP</h1>
                <div className={"menu"}>
                    <Link to={"/app/reservations"}>Rezervace</Link>
                    <Link to={"/app/attendance"}>Příchody/Odchody</Link>
                </div>
            </div>

            <div className={"right"}>
                {children}
            </div>
        </div>
    )
}