import {Link, useLocation} from "react-router-dom";
import "./Layout.scss";
import {useEffect, useState} from "react";

export const AppLayout = ({ children }: { children: React.ReactNode}) => {
    const location = useLocation();
    let [currentPage, setCurrentPage] = useState<string>("");

    useEffect(() => {
        setCurrentPage(location.pathname);
    }, [location]);

    return (
        <div id="app">
            <div className={"left"}>
                <h1>EduchemLP</h1>
                <div className={"menu"}>
                    <Link to={"/app/reservations"} className={currentPage === "/app/reservations" ? "active" : ""}>Rezervace</Link>
                    <Link to={"/app/attendance"} className={currentPage === "/app/attendance" ? "active" : ""}>Příchody/Odchody</Link>
                </div>
            </div>

            <div className={"right"}>
                {children}
            </div>
        </div>
    )
}