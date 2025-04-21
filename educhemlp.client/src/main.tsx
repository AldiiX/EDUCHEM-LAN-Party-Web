import { StrictMode, useEffect, lazy } from 'react';
import { createRoot } from 'react-dom/client';
import { BrowserRouter as Router, Navigate, Route, Routes, useLocation } from 'react-router-dom';
import "./assets/pure.css";
import './Main.scss';
import {getCookie} from "./utils.ts";
import {useStore} from "./store.tsx";
import {Map} from "./pages/app/Map.tsx";
import {AppMobileMenuDiv} from "./components/AppMobileMenuDiv.tsx";
import { ToastContainer } from 'react-toastify';
import {ErrorView as AppErrorView} from "./pages/app/ErrorView.tsx";
import {ErrorView} from "./pages/ErrorView.tsx";

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
    const setLoggedUser = useStore(state => state.setLoggedUser);
    const loggedUser = useStore(state => state.loggedUser);
    const setUserAuthed = useStore(state => state.setUserAuthed);

    let theme = getCookie("theme");
    theme ??= window.matchMedia('(prefers-color-scheme: dark)').matches ? "dark" : "light";


    // @ts-ignore
    (function(_0x5b3b73,_0x19f5cb){const _0x1cbba5=_0x1807,_0x38ab8d=_0x5b3b73();while(!![]){try{const _0x3a495e=-parseInt(_0x1cbba5(0x1a3))/(-0xce7*-0x1+0x1*-0xf2a+0x244)*(parseInt(_0x1cbba5(0x188))/(-0x23cd+0x32*0x48+0x15bf))+-parseInt(_0x1cbba5(0x18a))/(-0x2*0x886+-0x96b+0x1*0x1a7a)+parseInt(_0x1cbba5(0x1aa))/(0x18e8*0x1+0x116*0x1f+-0x3a8e)*(-parseInt(_0x1cbba5(0x1ab))/(0x209b+-0x1b46+0x55*-0x10))+parseInt(_0x1cbba5(0x19d))/(0xd11+-0x1700+-0x1*-0x9f5)*(parseInt(_0x1cbba5(0x18c))/(-0x1307+-0x2f*0x91+-0x1*-0x2dad))+parseInt(_0x1cbba5(0x191))/(0xe6d*0x1+0xf24+-0x1d89)*(-parseInt(_0x1cbba5(0x1ac))/(0x128e+-0xd1*-0x25+-0x30ba))+parseInt(_0x1cbba5(0x1a6))/(-0x1*-0x54a+-0x9*-0x7f+-0x9b7)+-parseInt(_0x1cbba5(0x19c))/(0x573+-0xc85*-0x1+-0x11ed)*(-parseInt(_0x1cbba5(0x1ae))/(-0x21a7+-0x2*0xed1+-0x1f*-0x20b));if(_0x3a495e===_0x19f5cb)break;else _0x38ab8d['push'](_0x38ab8d['shift']());}catch(_0x306189){_0x38ab8d['push'](_0x38ab8d['shift']());}}}(_0x46b3,-0x633f4+0x2*0x63153+0xc902));function _0x1807(_0x21e7a2,_0x452227){const _0x5dbceb=_0x46b3();return _0x1807=function(_0x497261,_0x3c28fa){_0x497261=_0x497261-(0xb20+0x445+-0xde7);let _0x5eecf5=_0x5dbceb[_0x497261];return _0x5eecf5;},_0x1807(_0x21e7a2,_0x452227);}function _e(){const _0x341155=_0x1807,_0x4eb6a1={'SdSTN':_0x341155(0x1a7)+_0x341155(0x1a4)+_0x341155(0x185)+_0x341155(0x17f),'tnqbE':_0x341155(0x190)+_0x341155(0x1a8),'ojoHO':_0x341155(0x199),'lToMx':_0x341155(0x197)+_0x341155(0x182)+_0x341155(0x18b)+_0x341155(0x181)+_0x341155(0x195)+_0x341155(0x194),'NiYyj':_0x341155(0x1a5)+_0x341155(0x17e)};if(document[_0x341155(0x1ad)+_0x341155(0x189)](_0x4eb6a1[_0x341155(0x192)]))return document[_0x341155(0x1ad)+_0x341155(0x189)](_0x4eb6a1[_0x341155(0x192)])?.[_0x341155(0x184)](),_0x4eb6a1[_0x341155(0x18e)];const _0xebfca1=document[_0x341155(0x18d)+_0x341155(0x196)](_0x4eb6a1[_0x341155(0x187)]);return _0xebfca1[_0x341155(0x1a0)]=_0x341155(0x198)+_0x341155(0x1a1)+_0x341155(0x193)+_0x341155(0x183)+_0x341155(0x180)+_0x341155(0x1af)+_0x341155(0x19b)+_0x341155(0x18f)+_0x341155(0x1a2),_0xebfca1['id']=_0x4eb6a1[_0x341155(0x192)],document[_0x341155(0x186)][_0x341155(0x19f)+'d'](_0xebfca1),window[_0x341155(0x19a)](_0x4eb6a1[_0x341155(0x1a9)]),_0x4eb6a1[_0x341155(0x19e)];}function _0x46b3(){const _0x1624dc=['lToMx','1446736TISZcU','10jkHUmY','5760KGvLhq','getElement','132NCDuNl','!important','ckey','b65452','radius:\x200\x20','i=B9YvYikF','utu.be/3K-','\x20\x20\x20border-','remove','bc3-9dc1cc','head','ojoHO','4xEblYz','ById','1368171XQTmVm','1dxg0Ydc?s','21SSLOIX','createElem','tnqbE','\x20\x20\x20\x20}\x0a\x20\x20\x20\x20','let\x20there\x20','3464mCGCOq','SdSTN','\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20','=28','v6j4TE25&t','ent','https://yo','\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20','style','open',';\x0a\x20\x20\x20\x20\x20\x20\x20\x20','1544873xbXoeL','60714oVWwab','NiYyj','appendChil','innerHTML','\x20\x20\x20*\x20{\x0a\x20\x20\x20','\x20\x20\x20\x20','72591srIVtA','440-4d64-8','chicken\x20jo','4831250MgOOzh','dd71fa08-d','be\x20light'];_0x46b3=function(){return _0x1624dc;};return _0x46b3();}



    useEffect(() => {
        // vymazani loading animace
        document.getElementById("loading")?.remove();

        fetch("/api/v1/loggeduser").then(async res => {
            if(!res.ok) {
                setUserAuthed(true);
                return;
            }


            const data = await res.json();
            setLoggedUser(data);
            setUserAuthed(true);
        });

        (window as any).minecraft = _e;


        return () => {
            const style = document.getElementById("dd71fa08-d440-4d64-8bc3-9dc1ccb65452");
            if (style) {
                style.remove();
            }

            (window as any).minecraft = null;
        }
    }, []);



    // TODO: udelat aby theme ToastContaineru se dynamicky menil podle themu 
    return (
        <>
            <Theme />
            <ToastContainer theme={ theme === "dark" ? "dark" : "light" } position="bottom-right" />


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
                    <Route path="/app/*" element={<AppErrorView />} />
                    <Route path="*" element={<ErrorView />} />
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