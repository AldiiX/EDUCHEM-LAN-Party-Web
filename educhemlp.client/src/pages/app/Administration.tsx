import { AppLayout } from "./AppLayout.tsx";
import { useEffect, useState } from "react";
import { useStore } from "../../store.tsx";
import { useNavigate } from "react-router-dom";

export const Administration = () => {
    const navigate = useNavigate();
    const { loggedUser } = useStore();
    const { userAuthed, setUserAuthed } = useStore();

    useEffect(() => {
        // Ověření oprávnění
        if (userAuthed && loggedUser?.accountType !== "ADMIN" && loggedUser?.accountType !== "TEACHER") {
            navigate("/app");
        }

    }, [userAuthed, navigate]);

    if (!userAuthed) return <></>;

    return (
        <AppLayout>
            <h1>Administrace</h1>
        </AppLayout>
    );
};

export default Administration;