import {Link, useLocation} from "react-router-dom";
import "./AppLayout.scss";
import React, {CSSProperties, useEffect, useState} from "react";
import {useStore} from "../../store.tsx";
import {Avatar} from "../../components/Avatar.tsx";
import {AppMenu} from "../../components/AppMenu.tsx";
import {enumEquals, logout, toggleWebTheme} from "../../utils.ts";
import {AccountGender, AccountType, LoggedUser} from "../../interfaces.ts";
import {Button} from "../../components/buttons/Button.tsx";
import {ButtonType} from "../../components/buttons/ButtonProps.ts";
import {Banner} from "../../components/Banner.tsx";


export enum AppLayoutTitleBarType {
    STATIC = "applayout-titlebar-type-static",
    STICKY = "applayout-titlebar-type-sticky",
    CUSTOM = "applayout-titlebar-type-custom"
}


export const AppLayoutLoggedUserSection = ({ style }: { style?: CSSProperties}) => {
    const loggedUser: LoggedUser = useStore((state) => state.loggedUser);
    const userAuthed = useStore((state) => state.userAuthed);
    const setLoggedUser = useStore((state) => state.setLoggedUser);

    if (!userAuthed) return <></>;

    function setRoleText(account: LoggedUser) {
        if(enumEquals(loggedUser.type.toString(), AccountType, AccountType.STUDENT)) {
            if(enumEquals(account.gender?.toString(), AccountGender, AccountGender.FEMALE))
                return "Přihlášena jako"

            return "Přihlášen jako"
        }

        else if(enumEquals(loggedUser.type.toString(), AccountType, AccountType.TEACHER)) {
            if(enumEquals(account.gender?.toString(), AccountGender, AccountGender.FEMALE))
                return "Učitelka"

            return "Učitel"
        }

        else if(enumEquals(loggedUser.type.toString(), AccountType, AccountType.ADMIN)) {
            if(enumEquals(account.gender?.toString(), AccountGender, AccountGender.FEMALE))
                return "Administrátorka"

            return "Administrátor"
        }

        else if(enumEquals(loggedUser.type.toString(), AccountType, AccountType.SUPERADMIN)) {
            if(enumEquals(account.gender?.toString(), AccountGender, AccountGender.FEMALE))
                return "Administrátorka (SU)"

            return "Administrátor (SU)"
        }
    }

    if(loggedUser) return (
        <>
            <Banner src={loggedUser?.banner} sx={{ height: "100%" }} className="profilebannercustomization" />
            <div className="loggeduser" style={style}>
                <Link to="/app/account">
                    <div className="texts">
                        <p>{ setRoleText(loggedUser) }</p>
                        <h2>{ loggedUser?.displayName }</h2>
                    </div>

                    <Avatar size="48px" src={loggedUser?.avatar} name={loggedUser?.displayName}  />
                </Link>

                <div className={"popover"}>
                    <p onClick={ () => toggleWebTheme() }>Změnit theme</p>
                    <p onClick={ () => logout(setLoggedUser)}>Odhlásit se</p>
                </div>
            </div>
        </>
    )

    if(!loggedUser) return (
        <div className="loggeduser" style={style}>
            <Link to="/login">
                <Button type={ButtonType.PRIMARY} text="Přihlásit se" />
            </Link>
        </div>
    )
}













export const AppLayout = ({ children, className, customTitleBar, titleBarType = AppLayoutTitleBarType.STATIC, titleBarText, mainContentPadding }: { children: React.ReactNode, className?: string, titleBarText?: string, titleBarType?: AppLayoutTitleBarType, customTitleBar?: React.ReactNode | null, mainContentPadding?: string }) => {
    const location = useLocation();
    const [currentPage, setCurrentPage] = useState<string>("");
    const { loggedUser, setLoggedUser } = useStore();
    const { userAuthed, setUserAuthed } = useStore();
    const { menuOpened, setMenuOpened } = useStore();

    useEffect(() => {
        setCurrentPage(location.pathname);
    }, [location]);

    



    if (!userAuthed) return <></>;



    return (
        <main
            id="app"
            className={className + " " + titleBarType}
            onContextMenu={(e) => e.preventDefault()}
            >

            <section className="left">


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
            </section>

            <section className="left-mobile">
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
            </section>

            <section className="right">
                {
                    titleBarType === AppLayoutTitleBarType.STICKY ? (
                        <div className="titlebar">
                            {
                                titleBarText ? (
                                    <h1>{ titleBarText }</h1>
                                ) : null
                            }

                            <AppLayoutLoggedUserSection />
                        </div>
                    ) : titleBarType === AppLayoutTitleBarType.STATIC ? (
                            <AppLayoutLoggedUserSection style={{ position: "absolute", right: 48, top: 32 }} />
                    ) : titleBarType === AppLayoutTitleBarType.CUSTOM ? (
                            customTitleBar
                    ) : null
                }

                <div className="content-wrapper" style={{ padding: mainContentPadding }}>
                    {
                        titleBarText && titleBarType === AppLayoutTitleBarType.STATIC ? (
                            <h1>{ titleBarText }</h1>
                        ) : null
                    }

                    {children}
                </div>
            </section>
        </main>
    )
}