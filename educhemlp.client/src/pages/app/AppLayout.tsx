import {Link, useLocation} from "react-router-dom";
import "./AppLayout.scss";
import {CSSProperties, useEffect, useState} from "react";
import {useStore} from "../../store.tsx";
import {Avatar} from "../../components/Avatar.tsx";
import {toggleWebTheme} from "../../utils.ts";
import {ButtonPrimary} from "../../components/buttons/ButtonPrimary.tsx";

export const AppLayout = ({ children, className }: { children: React.ReactNode, className?: string }) => {
    const location = useLocation();
    const [currentPage, setCurrentPage] = useState<string>("");
    const { loggedUser, setLoggedUser } = useStore();
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

                <div className="footer">
                    <p>© { new Date().getFullYear() } Educhem LAN Party</p>
                    <p>Vytvořili: <a href="https://stanislavskudrna.cz" target="_blank">Stanislav Škudrna</a>, <a href="https://github.com/WezeAnonymm" target="_blank">Serhii Yavorskyi</a></p>
                </div>
            </div>

            <div className={"right"}>
                {
                    loggedUser !== null ? (
                        <div className="loggeduser">
                            <div className="texts">
                                <p>{ loggedUser?.accountType === "STUDENT" ? "Přihlášen jako" : normalizeText(loggedUser?.accountType) }</p>
                                <h2>{ loggedUser?.displayName }</h2>
                            </div>

                            <Avatar size={"40px"} src={loggedUser?.avatar} backgroundColor={"var(--accent-color)"}  letter={loggedUser?.displayName?.split(" ")[0][0] + "" + loggedUser?.displayName?.split(" ")[1]?.[0]} className={"avatar"} />

                            <div className={"popover"}>
                                <p onClick={ () => toggleWebTheme() }>Změnit theme</p>
                                <p onClick={() => {
                                    fetch("/api/v1/loggeduser", {
                                        method: "DELETE",
                                        headers: {
                                            "Content-Type": "application/json",
                                        },
                                    }).then(() => {
                                        setLoggedUser(null);
                                    });
                                }}>Odhlásit se</p>
                            </div>
                        </div>
                    ) : (
                        <div className="loggeduser">
                            <ButtonPrimary text={"Přihlásit se"} onClick={() => {window.location.href = '/login'}} />
                        </div>
                    )
                }

                {children}
            </div>
        </div>
    )
}