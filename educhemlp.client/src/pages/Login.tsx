import { useStore } from "../store.tsx";
import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";

export const Login = () => {
    const { loggedUser, setLoggedUser } = useStore();
    const [key, setKey] = useState("");
    const [error, setError] = useState("");
    const navigate = useNavigate();

    const login = async (event: React.FormEvent) => {
        event.preventDefault();

        const res = await fetch("/api/v1/loggeduser", {
            method: "POST",
            body: JSON.stringify({ key }),
            headers: {
                "Content-Type": "application/json",
            },
        });

        if (!res.ok) {
            setKey("");
            setError("Účet s tímto klíčem neexistuje.");
            return;
        }

        const user = await res.json();
        setLoggedUser(user);
        navigate("/app");
    };

    useEffect(() => {
        if (loggedUser) navigate("/app");
    }, [loggedUser, navigate]);

    return (
        <form onSubmit={login}>
            <input
                type="text"
                value={key}
                onChange={(e) => setKey(e.target.value)}
                placeholder="Enter key"
                required
            />
            <button type="submit">Login</button>
            <p>{ error }</p>
        </form>
    );
};