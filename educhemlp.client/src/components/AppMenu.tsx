import {Link, useLocation} from "react-router-dom";
import {useStore} from "../store.tsx";
import {useEffect, useState} from "react";
import {AccountType, AppSettings, LoggedUser} from "../interfaces.ts";

interface AppMenuProps {
    onClick?: () => void;
}

export const AppMenu = ({ onClick }: AppMenuProps) => {
    const { loggedUser: lg } = useStore();
    const [currentPage, setCurrentPage] = useState<string>("");
    const { setMenuOpened } = useStore();
    const location = useLocation();
    const loggedUser = useStore().loggedUser as LoggedUser;
    const appSettings: AppSettings = useStore((state) => state.appSettings);

    useEffect(() => {
        setCurrentPage(location.pathname);
    }, [location]);

    return (
        <div className={"menu"}>
            <Link to={"/app/announcements"} onClick={onClick} className={currentPage === "/app/announcements" ? "active" : ""}>
                <div style={{ maskImage: 'url(/images/icons/bell.svg)' }}></div>
                <p>Oznámení</p>
            </Link>

            <Link to={"/app/map"} onClick={onClick} className={currentPage === "/app/map" ? "active" : ""}>
                <div style={{ maskImage: 'url(/images/icons/map.svg)' }}></div>
                <p>Mapa</p>
            </Link>

            <Link to={"/app/reservations"} onClick={onClick} className={currentPage === "/app/reservations" ? "active" : ""}>
                <div style={{ maskImage: 'url(/images/icons/calc.svg)' }}></div>
                <p>Rezervace</p>
            </Link>

            <Link to={"/app/forum"} onClick={onClick}  className={currentPage === "/app/forum" ? "active" : ""}>
                <div style={{ maskImage: 'url(/images/icons/forum.svg)' }}></div>
                <p>Forum</p>
            </Link>

            {
                loggedUser !== null && appSettings.chatEnabled
                    ? <Link to={"/app/chat"} onClick={onClick} className={currentPage === "/app/chat" ? "active" : ""}>
                        <div style={{ maskImage: 'url(/images/icons/chat.svg)' }}></div>
                        <p>Chat</p>
                    </Link>
                    : null
            }

            <Link to={"/app/attendance"} onClick={onClick} className={currentPage === "/app/attendance" ? "active" : ""}>
                <div style={{ maskImage: 'url(/images/icons/user_in_building.svg)' }}></div>
                <p>Příchody / Odchody</p>
            </Link>

            <Link to={"/app/tournaments"} onClick={onClick} className={currentPage === "/app/tournaments" ? "active" : ""}>
                <div style={{ maskImage: 'url(/images/icons/trophy_star.svg)' }}></div>
                <p>Turnaje</p>
            </Link>

            { AccountType[loggedUser?.accountType as unknown as keyof typeof AccountType] >= AccountType.TEACHER ?
                <Link to={"/app/administration"} onClick={onClick}  className={currentPage === "/app/administration" ? "active" : ""}>
                    <div style={{ maskImage: 'url(/images/icons/user_with_shield.svg)' }}></div>
                    <p>Administrace</p>
                </Link> : null
            }
        </div>
    );
}