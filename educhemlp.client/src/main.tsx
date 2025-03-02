import { StrictMode, useEffect } from 'react';
import { createRoot } from 'react-dom/client';
import { BrowserRouter as Router, Navigate, Route, Routes, useLocation } from 'react-router-dom';
import Home from './pages/Home.tsx';
import {Reservations} from "./pages/app/Reservations.tsx";
import {Attendance} from "./pages/app/Attendance.tsx";
import './Main.scss';

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

createRoot(document.getElementById('root')!).render(
    <StrictMode>
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