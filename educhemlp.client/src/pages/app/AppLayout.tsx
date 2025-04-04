import {Link, useLocation} from "react-router-dom";
import "./AppLayout.scss";
import {CSSProperties, useEffect, useState} from "react";
import {useStore} from "../../store.tsx";
import {Avatar} from "../../components/Avatar.tsx";
import {logout, toggleWebTheme} from "../../utils.ts";
import {ButtonPrimary} from "../../components/buttons/ButtonPrimary.tsx";
import {LoggedUser} from "../../interfaces.ts";
import {AppMobileMenuDiv} from "../../components/AppMobileMenuDiv.tsx";
import {AppMenu} from "../../components/AppMenu.tsx";

export const AppLayout = ({ children, className }: { children: React.ReactNode, className?: string }) => {
    const location = useLocation();
    const [currentPage, setCurrentPage] = useState<string>("");
    const { loggedUser, setLoggedUser } = useStore();
    const { userAuthed, setUserAuthed } = useStore();
    const { menuOpened, setMenuOpened } = useStore();

    useEffect(() => {
        setCurrentPage(location.pathname);
    }, [location]);

    
    const normalizeText = (text: string) => {
        return text[0].toUpperCase() + text.slice(1).toLowerCase();
    }


    if (!userAuthed) return <></>;




    return (
        <div id="app" className={className} onContextMenu={(e) => e.preventDefault()}>
            <div className="left">


                {/*<h1>Educhem<br/>LAN Party</h1>*/}
                <div className="title">
                    <div className="logo"></div>
                    <h1>EDUCHEM<br/>LAN Party</h1>
                </div>

                <AppMenu />

                <div className="footer">
                    <p>© { new Date().getFullYear() } EDUCHEM LAN Party</p>
                    <p>Vytvořili: <a href="https://stanislavskudrna.cz" target="_blank">Stanislav Škudrna</a>, <a href="https://github.com/WezeAnonymm" target="_blank">Serhii Yavorskyi</a></p>
                </div>
            </div>

            <div className="left-mobile">
                <div className="menu">
                    <Link to={"/app/announcements"} className={currentPage === "/app/announcements" ? "active" : ""}>
                        <div style={{ maskImage: 'url(/images/icons/bell.svg)' }}></div>
                    </Link>

                    <Link to={"/app/reservations"} className={currentPage === "/app/reservations" ? "active" : ""}>
                        <div style={{ maskImage: 'url(/images/icons/calc.svg)' }}></div>
                    </Link>

                    <a className="menu-icon" onClick={() => setMenuOpened(true) }>
                        <div style={{ maskImage: 'url(/images/icons/menu.svg)' }}></div>
                    </a>

                    <Link to={"/app/map"} className={currentPage === "/app/map" ? "active" : ""}>
                        <div style={{ maskImage: 'url(/images/icons/map.svg)' }}></div>
                    </Link>

                    {
                        loggedUser !== null ? (
                            <Link to="/app/account" className="ignorestyle">
                                <Avatar size="16px" name={loggedUser.displayName} src={loggedUser.avatar} className={currentPage === "/app/account" ? "active" : ""} />
                            </Link>
                        ) : (
                            <Link to={"/app/forum"} className={currentPage === "/app/forum" ? "active" : ""}>
                                <div style={{ maskImage: 'url(/images/icons/forum.svg)' }}></div>
                            </Link>
                        )
                    }
                </div>
            </div>

            <div className="right">
                {
                    loggedUser !== null ? (
                        <div className="loggeduser">
                            <Link to="/app/account">
                                <div className="texts">
                                    <p>{ loggedUser?.accountType === "STUDENT" ? "Přihlášen jako" : normalizeText(loggedUser?.accountType) }</p>
                                    <h2>{ loggedUser?.displayName }</h2>
                                </div>

                                <Avatar size="48px" src={loggedUser?.avatar} name={loggedUser?.displayName}  />
                            </Link>

                            <div className={"popover"}>
                                <p onClick={ () => toggleWebTheme() }>Změnit theme</p>
                                <p onClick={() => logout(setLoggedUser)}>Odhlásit se</p>
                            </div>
                        </div>
                    ) : (
                        <div className="loggeduser">
                            <div className="changetheme"></div>
                            <Link to="/login" className={"button-primary"}>Přihlásit se</Link>
                        </div>
                    )
                }

                {children}
            </div>
        </div>
    )
}