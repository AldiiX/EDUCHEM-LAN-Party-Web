import { StrictMode, useEffect, lazy } from 'react';
import { createRoot } from 'react-dom/client';
import { BrowserRouter as Router, Navigate, Route, Routes, useLocation } from 'react-router-dom';
import Home from './pages/Home.tsx';
import {Reservations} from "./pages/app/Reservations.tsx";
import {Attendance} from "./pages/app/Attendance.tsx";
import "./assets/pure.css";
import './Main.scss';
import {getCookie} from "./utils.ts";
import {Announcements} from "./pages/app/Announcements.tsx";
import {useStore} from "./store.tsx";
import {Tournaments} from "./pages/app/Tournaments.tsx";
import {Login} from "./pages/Login.tsx";
import Administration from "./pages/app/Administration.tsx";
import {Chat} from "./pages/app/Chat.tsx";
import {Forum} from "./pages/app/Forum.tsx";


const RouteTitle = () => {
    const location = useLocation();

    useEffect(() => {
        const routeTitles: { [key: string]: string } = {
            '/': 'Home • Educhem LAN Party',
            '/app': 'App • Educhem LAN Party',
            '/app/reservations': 'Rezervace • Educhem LAN Party',
            '/app/attendance': 'Příchody / Odchody • Educhem LAN Party',
            '/app/administration': 'Administrace • Educhem LAN Party',
            '/app/tournaments': 'Turnaje • Educhem LAN Party',
            '/app/announcements': 'Oznámení • Educhem LAN Party',
            '/app/chat': 'Chat • Educhem LAN Party',
            '/app/forum': 'Forum • Educhem LAN Party',
        };

        document.title = routeTitles[location.pathname] || 'Educhem LAN Party';
    }, [location]);

    return null;
};

const Theme = () => {
    useEffect(() => {
        // zjištění z cookies
        let theme = "light";//getCookie("theme");
        theme ??= window.matchMedia('(prefers-color-scheme: dark)').matches ? "dark" : "light";


        document.documentElement.classList.remove('darkmode', 'lightmode');

        switch (theme) {
            case "dark":
                document.documentElement.classList.add('darkmode');
                break;
            default:
                document.documentElement.classList.add('lightmode');
                break;
        }
    }, []);

    return null;
}

const App = () => {
    const { loggedUser, setLoggedUser } = useStore();
    const { userAuthed, setUserAuthed } = useStore();

    useEffect(() => {
        fetch("/api/v1/loggeduser").then(async res => {
            if(!res.ok) {
                setUserAuthed(true);
                return;
            }

            const data = await res.json();
            setLoggedUser(data);
            setUserAuthed(true);
        });
    }, []);

    return (
        <>
            <Theme />

            <Router>
                <RouteTitle />
                <Routes>
                    <Route path="/" element={<Home />} />
                    <Route path="/login" element={<Login />} />
                    <Route path="/app" element={<Navigate to="/app/reservations" />} />
                    <Route path="/app/reservations" element={<Reservations />} />
                    <Route path="/app/attendance" element={<Attendance />} />
                    <Route path="/app/administration" element={<Administration />} />
                    <Route path="/app/announcements" element={<Announcements />} />
                    <Route path="/app/tournaments" element={<Tournaments />} />
                    <Route path="/app/chat" element={<Chat />} />
                    <Route path="/app/forum" element={<Forum />} />
                </Routes>
            </Router>
        </>
    );
}

createRoot(document.getElementById('root')!).render(
    <StrictMode>
        <App />
    </StrictMode>
);