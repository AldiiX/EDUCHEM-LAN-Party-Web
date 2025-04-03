import {AppLayout} from "./AppLayout.tsx";
import "./Account.scss";
import {useEffect} from "react";
import {useNavigate} from "react-router-dom";
import {useStore} from "../../store.tsx";
import {Avatar} from "../../components/Avatar.tsx";

export const Account = () => {
    const navigate = useNavigate();
    const { loggedUser } = useStore();
    const { userAuthed, setUserAuthed } = useStore();


    // kontrola přihlášení
    useEffect(() => {
        // Ověření oprávnění
        if (userAuthed && !loggedUser) {
            navigate("/app");
        }
    }, [userAuthed, navigate]);


    if (!userAuthed || (userAuthed && !loggedUser)) return null;

    return (
        <AppLayout className="page-account">
            <h1>Můj účet</h1>

            <div className="info">
                <Avatar size={"200px"} src={loggedUser.avatar} name={loggedUser.displayName} />
                <h1>{loggedUser.displayName}</h1>
                <p className="email">{loggedUser.email}</p>
            </div>
        </AppLayout>
    )
}

export default Account;