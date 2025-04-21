import {AppLayout} from "./AppLayout.tsx";
import "./Account.scss";
import {useEffect} from "react";
import {useNavigate} from "react-router-dom";
import {useStore} from "../../store.tsx";
import {Avatar} from "../../components/Avatar.tsx";
import {logout, toggleWebTheme} from "../../utils.ts";
import {Button} from "../../components/buttons/Button.tsx";
import {ButtonType} from "../../components/buttons/ButtonProps.ts";

export const Account = () => {
    const navigate = useNavigate();
    const { loggedUser, setLoggedUser } = useStore();
    const { userAuthed, setUserAuthed } = useStore();


    // kontrola přihlášení
    useEffect(() => {
        // Ověření oprávnění
        if (userAuthed && !loggedUser) {
            navigate("/app");
        }
    }, [userAuthed, navigate]);


    if (!userAuthed || (userAuthed && !loggedUser)) {
        navigate("/app");
        return null;
    }

    return (
        <AppLayout className="page-account">
            <h1>Můj účet</h1>

            <div className="info">
                <Avatar size={"200px"} src={loggedUser.avatar} name={loggedUser.displayName} />
                <h1>{loggedUser.displayName}</h1>
                <p className="email">{loggedUser.email}</p>
                {
                    loggedUser.accountType !== "STUDENT" ? (
                        <p className="type">{loggedUser.accountType}</p>
                    ) : null
                }
                <div className="buttons">
                    <Button type={ButtonType.SECONDARY} text="Změnit theme" icon="/images/icons/brush.svg" onClick={ () => toggleWebTheme() } />
                    <Button type={ButtonType.PRIMARY} text="Odhlásit" icon="/images/icons/door.svg" onClick={() => logout(setLoggedUser) } />
                </div>
            </div>
        </AppLayout>
    )
}

export default Account;