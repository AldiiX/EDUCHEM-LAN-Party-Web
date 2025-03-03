import { StrictMode, useEffect } from 'react';
import { createRoot } from 'react-dom/client';
import { BrowserRouter as Router, Navigate, Route, Routes, useLocation } from 'react-router-dom';
import Home from './pages/Home.tsx';
import {Reservations} from "./pages/app/Reservations.tsx";
import {Attendance} from "./pages/app/Attendance.tsx";
import "./assets/pure.css";
import './Main.scss';
import {getCookie} from "./utils.ts";
import {Administration} from "./pages/app/Administration.tsx";
import {Announcements} from "./pages/app/Announcements.tsx";
import {useStore} from "./store.tsx";
import {Tournaments} from "./pages/app/Tournaments.tsx";
import {Login} from "./pages/Login.tsx";

const RouteTitle = () => {
    const location = useLocation();

    useEffect(() => {
        const routeTitles: { [key: string]: string } = {
            '/': 'Home • EduchemLP',
            '/app': 'App • EduchemLP',
            '/app/reservations': 'Rezervace • EduchemLP',
            '/app/attendance': 'Příchody / Odchody • EduchemLP',
            '/app/administration': 'Administrace • EduchemLP',
        };

        document.title = routeTitles[location.pathname] || 'EduchemLP';
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

const LoggedUser = () => {
    const { loggedUser, setLoggedUser } = useStore();

    useEffect(() => {
        fetch("/api/v1/loggeduser").then(async res => {
            if(!res.ok) return;

            const data = await res.json();
            setLoggedUser(data);
        });
    }, []);

    return null;
}

createRoot(document.getElementById('root')!).render(
    <StrictMode>
        <Theme />
        <LoggedUser />

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
            </Routes>
        </Router>
    </StrictMode>
);