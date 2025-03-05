import {Link, useLocation} from "react-router-dom";
import "./Layout.scss";
import {useEffect, useState} from "react";
import {useStore} from "../../store.tsx";

export const AppLayout = ({ children, className }: { children: React.ReactNode, className?: string }) => {
    const location = useLocation();
    const [currentPage, setCurrentPage] = useState<string>("");
    const { loggedUser } = useStore();
    const { userAuthed, setUserAuthed } = useStore();

    useEffect(() => {
        setCurrentPage(location.pathname);
    }, [location]);

    
    const normalizeText = (text: string) => {
        return text[0].toUpperCase() + text.slice(1).toLowerCase();
    }


    if (!userAuthed) return <></>;

    return (
        <div id="app" className={className}>
            <div className={"left"}>


                {/*<h1>Educhem<br/>LAN Party</h1>*/}
                <div className="title">
                    <div className="logo"></div>
                    <h1>Educhem<br/>LAN Party</h1>
                </div>


                <div className={"menu"}>
                    <Link to={"/app/announcements"} className={currentPage === "/app/announcements" ? "active" : ""}>
                        <div style={{ maskImage: 'url(../../../public/images/icons/bell.svg)' }}></div>
                        Oznámení
                    </Link>

                    <Link to={"/app/reservations"} className={currentPage === "/app/reservations" ? "active" : ""}>
                        <div style={{ maskImage: 'url(../../../public/images/icons/calc.svg)' }}></div>
                        Rezervace
                    </Link>

                    <Link to={"/app/forum"} className={currentPage === "/app/forum" ? "active" : ""}>
                        <div style={{ maskImage: 'url(../../../public/images/icons/forum.svg)' }}></div>
                        Forum
                    </Link>

                    {
                        loggedUser !== null
                            ? <Link to={"/app/chat"} className={currentPage === "/app/chat" ? "active" : ""}>
                                <div style={{ maskImage: 'url(../../../public/images/icons/chat.svg)' }}></div>
                                Chat
                            </Link>
                            : null
                    }

                    <Link to={"/app/attendance"} className={currentPage === "/app/attendance" ? "active" : ""}>
                        <div style={{ maskImage: 'url(../../../public/images/icons/user_in_building.svg)' }}></div>
                        Příchody / Odchody
                    </Link>

                    <Link to={"/app/tournaments"} className={currentPage === "/app/tournaments" ? "active" : ""}>
                        <div style={{ maskImage: 'url(../../../public/images/icons/trophy_star.svg)' }}></div>
                        Turnaje
                    </Link>

                    { loggedUser?.accountType === "ADMIN" ?
                        <Link to={"/app/administration"} className={currentPage === "/app/administration" ? "active" : ""}>
                            <div style={{ maskImage: 'url(../../../public/images/icons/user_with_shield.svg)' }}></div>
                            Administrace
                        </Link> : null
                    }

                </div>
            </div>

            <div className={"right"}>
                {
                    loggedUser !== null ?
                    <div className="loggeduser">
                        <div className="texts">
                            <p>{ loggedUser?.accountType === "STUDENT" ? "Přihlášen jako" : normalizeText(loggedUser?.accountType) }</p>
                            <h2>{ loggedUser?.displayName }</h2>
                        </div>
                        <div className="avatar" style={{ backgroundImage: `url(${loggedUser?.avatar})`, '--letter': `'${loggedUser?.displayName[0]}'`}}></div>
                    </div>
                    : null
                }

                {children}
            </div>
        </div>
    )
}