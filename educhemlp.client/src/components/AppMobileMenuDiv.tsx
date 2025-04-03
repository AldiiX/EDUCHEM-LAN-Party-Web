import { Link } from "react-router-dom";
import { useStore } from "../store.tsx";
import "./AppMobileMenuDiv.scss";
import {AppMenu} from "./AppMenu.tsx";

export const AppMobileMenuDiv = () => {
    const { menuOpened, setMenuOpened } = useStore();

    if (!menuOpened) return null;

    // animované zavření menu
    const closeMenu = () => {
        const menu = document.querySelector(".mobile-menu");
        if (!menu) return;

        setTimeout(() => {
            menu.classList.add("closing");

            setTimeout(() => {
                setMenuOpened(false);
                menu.classList.remove("closing");
            }, 500);
        }, 0);
    }

    return (
        <div className="mobile-menu">
            <div className="title">
                <div className="logo"></div>
                <h1>Educhem<br/>LAN Party</h1>
            </div>

            <AppMenu onClick={() => closeMenu() } />

            <div className="bottom">
                <div className="close" onClick={() => closeMenu() }></div>
            </div>
        </div>
    );
};
