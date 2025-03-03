import {AppLayout} from "./Layout";
import {useNavigate} from "react-router-dom";
import {useStore} from "../../store.tsx";
import {useEffect} from "react";

export const Chat = () => {
    const navigate = useNavigate();
    const { loggedUser } = useStore();
    const { userAuthed, setUserAuthed } = useStore();

    useEffect(() => {
        // Ověření oprávnění
        if (userAuthed && loggedUser === null) {
            navigate("/app");
        }

    }, [userAuthed, navigate]);

    if (!userAuthed) return <></>;


    return (
        <AppLayout>
            <h1>Chat</h1>
        </AppLayout>
    )
}