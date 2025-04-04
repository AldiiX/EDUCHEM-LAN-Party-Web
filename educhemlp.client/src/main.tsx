import { StrictMode, useEffect, lazy } from 'react';
import { createRoot } from 'react-dom/client';
import { BrowserRouter as Router, Navigate, Route, Routes, useLocation } from 'react-router-dom';
import "./assets/pure.css";
import './Main.scss';
import {getCookie} from "./utils.ts";
import {useStore} from "./store.tsx";
import {Map} from "./pages/app/Map.tsx";
import {AppMobileMenuDiv} from "./components/AppMobileMenuDiv.tsx";

const Home = lazy(() => import('./pages/Home.tsx'));
const Login = lazy(() => import('./pages/Login.tsx'));
const Reservations = lazy(() => import('./pages/app/Reservations.tsx'));
const Attendance = lazy(() => import('./pages/app/Attendance.tsx'));
const Administration = lazy(() => import('./pages/app/Administration.tsx'));
const Announcements = lazy(() => import('./pages/app/Announcements.tsx'));
const Tournaments = lazy(() => import('./pages/app/Tournaments.tsx'));
const Chat = lazy(() => import('./pages/app/Chat.tsx'));
const Forum = lazy(() => import('./pages/app/Forum.tsx'));
const Account = lazy(() => import('./pages/app/Account.tsx'));


const RouteTitle = () => {
    const location = useLocation();

    useEffect(() => {
        const routeTitles: { [key: string]: string } = {
            '/': 'Domů • EDUCHEM LAN Party',
            '/app': 'Aplikace • EDUCHEM LAN Party',
            '/app/reservations': 'Rezervace • EDUCHEM LAN Party',
            '/app/map': 'Mapa • EDUCHEM LAN Party',
            '/app/attendance': 'Příchody / Odchody • EDUCHEM LAN Party',
            '/app/administration': 'Administrace • EDUCHEM LAN Party',
            '/app/tournaments': 'Turnaje • EDUCHEM LAN Party',
            '/app/announcements': 'Oznámení • EDUCHEM LAN Party',
            '/app/chat': 'Chat • EDUCHEM LAN Party',
            '/app/forum': 'Forum • EDUCHEM LAN Party',
            '/app/account': 'Můj účet • EDUCHEM LAN Party',
        };

        document.title = routeTitles[location.pathname] || 'EDUCHEM LAN Party';
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
                <AppMobileMenuDiv />
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
                    <Route path="/app/account" element={<Account />} />
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