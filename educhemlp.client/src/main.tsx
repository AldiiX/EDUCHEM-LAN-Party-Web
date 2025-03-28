import { StrictMode, useEffect, lazy } from 'react';
import { createRoot } from 'react-dom/client';
import { BrowserRouter as Router, Navigate, Route, Routes, useLocation } from 'react-router-dom';
import "./assets/pure.css";
import './Main.scss';
import {getCookie} from "./utils.ts";
import {useStore} from "./store.tsx";
import {Map} from "./pages/app/Map.tsx";

const Home = lazy(() => import('./pages/Home.tsx'));
const Login = lazy(() => import('./pages/Login.tsx'));
const Reservations = lazy(() => import('./pages/app/Reservations.tsx'));
const Attendance = lazy(() => import('./pages/app/Attendance.tsx'));
const Administration = lazy(() => import('./pages/app/Administration.tsx'));
const Announcements = lazy(() => import('./pages/app/Announcements.tsx'));
const Tournaments = lazy(() => import('./pages/app/Tournaments.tsx'));
const Chat = lazy(() => import('./pages/app/Chat.tsx'));
const Forum = lazy(() => import('./pages/app/Forum.tsx'));


const RouteTitle = () => {
    const location = useLocation();

    useEffect(() => {
        const routeTitles: { [key: string]: string } = {
            '/': 'Domů • Educhem LAN Party',
            '/app': 'Aplikace • Educhem LAN Party',
            '/app/reservations': 'Rezervace • Educhem LAN Party',
            '/app/map': 'Mapa • Educhem LAN Party',
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
        let cookieTheme = getCookie("theme");
        cookieTheme ??= window.matchMedia('(prefers-color-scheme: dark)').matches ? "dark" : "light";


        document.documentElement.classList.remove('theme-dark', 'theme-light');


        switch (cookieTheme) {
            case "dark":
                document.documentElement.classList.add('theme-dark');
                break;
            default:
                document.documentElement.classList.add('theme-light');
                break;
        }

        const body = document.querySelector('body');
        if (body) {
            body.style.backgroundColor = "var(--background-bg)";
            body.style.color = "var(--text-color)";
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
                    <Route path="/app/map" element={<Map />} />
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