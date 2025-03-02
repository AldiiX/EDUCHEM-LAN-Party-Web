import { StrictMode, useEffect } from 'react';
import { createRoot } from 'react-dom/client';
import { BrowserRouter as Router, Navigate, Route, Routes, useLocation } from 'react-router-dom';
import Home from './pages/Home.tsx';
import {Reservations} from "./pages/app/Reservations.tsx";
import {Attendance} from "./pages/app/Attendance.tsx";
import './Main.scss';
import {getCookie} from "./utils.ts";

const RouteTitle = () => {
    const location = useLocation();

    useEffect(() => {
        const routeTitles: { [key: string]: string } = {
            '/': 'Home • EduchemLP',
            '/app': 'App • EduchemLP',
            '/app/reservations': 'Rezervace • EduchemLP',
            '/app/attendance': 'Příchody / Odchody • EduchemLP',
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

createRoot(document.getElementById('root')!).render(
    <StrictMode>
        <Theme />
        <Router>
            <RouteTitle />
            <Routes>
                <Route path="/" element={<Home />} />
                <Route path="/app" element={<Navigate to="/app/reservations" />} />
                <Route path="/app/reservations" element={<Reservations />} />
                <Route path="/app/attendance" element={<Attendance />} />
            </Routes>
        </Router>
    </StrictMode>
);