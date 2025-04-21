import { useStore } from "../store.tsx";
import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import "./Login.scss"; 
import TextField from "@mui/material/TextField";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";

export const Login = () => {
    const { loggedUser, setLoggedUser } = useStore();
    const [password, setPassword] = useState("");
    const [email, setEmail] = useState("");
    const [error, setError] = useState("");
    const navigate = useNavigate();

    const login = async (event: React.FormEvent) => {
        event.preventDefault();

        if(email === "" || password === "") {
            setError("Vyplňte všechna pole.");
            return;
        }

        const res = await fetch("/api/v1/loggeduser", {
            method: "POST",
            body: JSON.stringify({ email, password }),
            headers: {
                "Content-Type": "application/json",
            },
        });

        if (!res.ok) {
            setPassword("");
            setError("Účet s tímto uživatelským jménem a heslem neexistuje.");
            return;
        }

        const data = await res.json();
        setLoggedUser(data.account);
        navigate("/app");
    };

    useEffect(() => {
        if (loggedUser) navigate("/app");
    }, [loggedUser, navigate]);

    return (
        <>
        <div className={"parent"}>
            <div className={"left-side"}>
                <div className="title">
                    <div className="logo"></div>
                    <h1>Educhem<br/>LAN Party</h1></div>
                <div className={"login-container"}>
                    <Box
                        className={"login-form"}
                        component="form"
                        sx={{'& > :not(style)': {m: 1, width: '25ch'}}}
                        autoComplete="off"
                        onSubmit={login}
                    >
                        <TextField autoComplete={"educhemlp_email"}    className={"email"} id="email" label="E-mail" variant="outlined" type="text" name={"email"} onChange={(event) => { setEmail((event.target as HTMLInputElement).value) }} />
                        <TextField autoComplete={"educhemlp_password"} className={"password"} id="password" label="Heslo" variant="outlined" type="password" name={"password"} onChange={(event) => { setPassword((event.target as HTMLInputElement).value) }} />
                        <button className={"submit-button"} type="submit">Login</button>
                        <p>{error}</p>
                    </Box>
                </div>
            </div>
            <div className={"right-side"}>
                <div className={"image"}>

                </div>
                <div className={"PC-count"}>
                    
                </div>
            </div>
        </div>   
        </>
    );
};

export default Login;