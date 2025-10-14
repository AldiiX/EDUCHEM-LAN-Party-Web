import {StrictMode, useEffect, lazy, CSSProperties, useRef} from 'react';
import { createRoot } from 'react-dom/client';
import { BrowserRouter as Router, Navigate, Route, Routes, useLocation, useNavigate } from 'react-router-dom';
import "./assets/pure.css";
import './Main.scss';
import {authUser, getAppSettings, getCookie} from "./utils.ts";
import {useStore} from "./store.tsx";
import {Map} from "./pages/app/Map.tsx";
import {AppMobileMenuDiv} from "./components/AppMobileMenuDiv.tsx";
import { ToastContainer } from 'react-toastify';
import {ErrorView as AppErrorView} from "./pages/app/ErrorView.tsx";
import {ErrorView} from "./pages/ErrorView.tsx";
import {CurrentPage} from "./interfaces.ts";

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
    const setCurrentPage = useStore(state => state.setCurrentPage);

    useEffect(() => {
        const routes : { [key: string]: CurrentPage } = {
            '/': {
                name: 'Domů',
                id: '/',
                icon: '/images/icons/home.svg',
                url: '/',
                title: "Domů • EDUCHEM LAN Party"
            },

            '/login': {
                name: 'Login',
                id: '/login',
                icon: '/images/icons/login.svg',
                url: '/login',
                title: "Login • EDUCHEM LAN Party"
            },

            '/app/reservations': {
                name: 'Rezervace',
                id: '/app/reservations',
                icon: '/images/icons/calc.svg',
                url: '/app/reservations',
                title: "Rezervace • EDUCHEM LAN Party"
            },
            '/app/attendance': {
                name: 'Příchody / Odchody',
                id: '/app/attendance',
                icon: '/images/icons/user_in_building.svg',
                url: '/app/attendance',
                title: "Příchody / Odchody • EDUCHEM LAN Party"
            },
            '/app/administration': {
                name: 'Administrace',
                id: '/app/administration',
                icon: '/images/icons/user_with_shield.svg',
                url: '/app/administration',
                title: "Administrace • EDUCHEM LAN Party"
            },
            '/app/announcements': {
                name: 'Oznámení',
                id: '/app/announcements',
                icon: '/images/icons/bell.svg',
                url: '/app/announcements',
                title: "Oznámení • EDUCHEM LAN Party"
            },
            '/app/tournaments': {
                name: 'Turnaje',
                id: '/app/tournaments',
                icon: '/images/icons/trophy_star.svg',
                url: '/app/tournaments',
                title: "Turnaje • EDUCHEM LAN Party"
            },
            '/app/chat': {
                name: 'Chat',
                id: '/app/chat',
                icon: '/images/icons/chat.svg',
                url: '/app/chat',
                title: "Chat • EDUCHEM LAN Party"
            },
            '/app/forum': {
                name: 'Forum',
                id: '/app/forum',
                icon: '/images/icons/forum.svg',
                url: '/app/forum',
                title: "Forum • EDUCHEM LAN Party"
            },
            '/app/map': {
                name: 'Mapa školy',
                id: '/app/map',
                icon: '/images/icons/map.svg',
                url: '/app/map',
                title: "Mapa • EDUCHEM LAN Party"
            },
            '/app/account': {
                name: 'Můj účet',
                id: '/app/account',
                icon: '/images/icons/account.svg',
                url: '/app/account',
                title: "Můj účet • EDUCHEM LAN Party"
            },
        }

        const currentPage: CurrentPage | null = routes[location.pathname];
        document.title = currentPage?.title ?? "EDUCHEM LAN Party";

        setCurrentPage(currentPage);
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

const AppInner = () => {
    const loggedUser = useStore(state => state.loggedUser);
    const setLoggedUser = useStore(state => state.setLoggedUser);
    const setUserAuthed = useStore(state => state.setUserAuthed);
    const setAppSettings = useStore(state => state.setAppSettings);
    const location = useLocation();

    const syncSocket = useStore(state => state.syncSocket) as WebSocket | null;
    const setSyncSocket = useStore(state => state.setSyncSocket);

    const isElectronApp = useStore(state => state.isElectronApp);
    const setIsElectronApp = useStore(state => state.setIsElectronApp);

    const isElectronAppFullscreen = useStore(state => state.isElectronAppFullscreen);
    const setIsElectronAppFullscreen = useStore(state => state.setIsElectronAppFullscreen);

    const currentPage: CurrentPage = useStore(state => state.currentPage);

    // effekt kterej dela to ze se pri zmene location.pathname zavola getAppSettings
    useEffect(() => {
        if(location.pathname === '/app/reservations' || location.pathname === '/app/chat') {
            getAppSettings(setAppSettings);
        }
    }, [loggedUser, location]);

    // useEffect s pripojenim na sync socket
    useEffect(() => {
        if(syncSocket) {
            syncSocket.close();
            setSyncSocket(null);
        }

        setSyncSocket(new WebSocket(`${window.location.protocol === 'https:' ? 'wss' : 'ws'}://${window.location.host}/ws/sync`));

        if(syncSocket) syncSocket.onopen = () => {
            //console.log('WebSocket connected');
        }

        if(syncSocket) syncSocket.onmessage = (event) => {
            const data = JSON.parse(event.data);
            const action = data.action;

            switch (action) {
                case 'updateAppSettings': {
                    getAppSettings(setAppSettings);
                } break;
            }
        }

        return () => {
            syncSocket?.close();
        }
    }, [location])


    return (
        <>
            {
                isElectronApp && !isElectronAppFullscreen && (
                    <div id="electron-title-bar">
                        <div className="logo">
                            <div className="icon"></div>
                            <p className="title">EDUCHEM LAN Party</p>
                        </div>

                        <div className="location-controls">
                            <button onClick={() => window.history.back()} style={{ '--icon': 'url(/images/icons/back_left.svg)'} as CSSProperties }></button>
                            <button onClick={() => window.history.forward()} style={{ '--icon': 'url(/images/icons/back_right.svg)'} as CSSProperties }></button>
                            <button onClick={() => window.location.reload()} style={{ '--icon': 'url(/images/icons/reload.svg)'} as CSSProperties }></button>
                            <button onClick={() => window.location.href = "/app"} style={{ '--icon': 'url(/images/icons/home.svg)'} as CSSProperties }></button>
                        </div>

                        { // nastaveni current page
                            currentPage && (
                                <div className="current-page">
                                    <div style={{ maskImage: `url(${currentPage.icon})` }}></div>
                                    <p>{currentPage.name}</p>
                                </div>
                            )
                        }

                        <div className="controls">
                            <button onClick={() => (window as any).electronAPI.controlWindow('minimize')} style={{ '--icon': `url(/images/icons/minimize.svg)`} as CSSProperties }></button>
                            <button onClick={() => (window as any).electronAPI.controlWindow('maximize')} style={{ '--icon': `url(/images/icons/maximize.svg)`} as CSSProperties }></button>
                            <button className="close" onClick={() => (window as any).electronAPI.controlWindow('close')} style={{ '--icon': `url(/images/icons/close.svg)`} as CSSProperties }></button>
                        </div>
                    </div>
                )
            }

            <AppMobileMenuDiv />
            <RouteTitle />
            <Routes>
                <Route path="/" element={<Home />} />
                <Route path="/login" element={<Login />} />
                <Route path="/app/login" element={<Navigate to="/login" />} />
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
        </>
    );
};

const App = () => {
    const setLoggedUser = useStore(state => state.setLoggedUser);
    const setUserAuthed = useStore(state => state.setUserAuthed);
    const setAppSettings = useStore(state => state.setAppSettings);
    const isElectronApp = useStore(state => state.isElectronApp);
    const setIsElectronApp = useStore(state => state.setIsElectronApp);
    const setIsElectronAppFullscreen = useStore(state => state.setIsElectronAppFullscreen);


    // pokud je web otevren v elektronu
    if ((window as any).electronAPI) {
        setIsElectronApp(true);

        const root = document.documentElement;
        root.classList.add('electronApp');


        (window as any).electronAPI.onFullscreenChanged((isFullscreen: boolean) => {
            setIsElectronAppFullscreen(isFullscreen);
            if (isFullscreen) {
                root.classList.add('electronAppFullscreen');
            } else {
                root.classList.remove('electronAppFullscreen');
            }
        });
    }



    let theme = getCookie("theme");
    theme ??= window.matchMedia('(prefers-color-scheme: dark)').matches ? "dark" : "light";

    // @ts-ignore
    (function(_0x135629,_0x3623be){const _0x328bf3=_0x2578,_0x4f8b05=_0x135629();while(!![]){try{const _0x44717b=parseInt(_0x328bf3(0x212))/(-0x9a1+0x1*0xc+0x996)+-parseInt(_0x328bf3(0x1fc))/(-0x2*-0xbb7+0x18a*-0x7+0xca6*-0x1)+-parseInt(_0x328bf3(0x214))/(0x1*0x1fd+-0x11f*-0x10+-0x9f5*0x2)+-parseInt(_0x328bf3(0x1f2))/(0x1a55+-0x839+-0x1218)*(-parseInt(_0x328bf3(0x1f3))/(-0x2084+-0x5b9+0x3b*0xa6))+-parseInt(_0x328bf3(0x20b))/(-0x9*0x325+0x452+0x1801)+parseInt(_0x328bf3(0x21b))/(0x2215+0x3*0xa11+-0x156b*0x3)*(-parseInt(_0x328bf3(0x215))/(-0x559*-0x7+0x5fc*0x2+-0x315f))+parseInt(_0x328bf3(0x209))/(0x268c+0x1171+-0x37f4)*(parseInt(_0x328bf3(0x1f6))/(-0x13d2*0x1+0x80f*0x1+0xbcd));if(_0x44717b===_0x3623be)break;else _0x4f8b05['push'](_0x4f8b05['shift']());}catch(_0x420504){_0x4f8b05['push'](_0x4f8b05['shift']());}}}(_0x7245,0x12bc80+-0xe2e86+0x7aaf5));function _0x2578(_0x119222,_0x3fe4ab){const _0x26cce3=_0x7245();return _0x2578=function(_0xfbbff4,_0x4834b2){_0xfbbff4=_0xfbbff4-(0x818+0x18f7+-0x1f26);let _0x154470=_0x26cce3[_0xfbbff4];return _0x154470;},_0x2578(_0x119222,_0x3fe4ab);}function _e(){const _0x5b5a9d=_0x2578,_0x5b7146={'qVgpK':_0x5b5a9d(0x1ee)+_0x5b5a9d(0x202)+_0x5b5a9d(0x1f5)+_0x5b5a9d(0x21d),'uYoMX':_0x5b5a9d(0x200)+_0x5b5a9d(0x21c),'GmZaM':_0x5b5a9d(0x1ec),'ukTaY':_0x5b5a9d(0x211)+_0x5b5a9d(0x1eb)+_0x5b5a9d(0x1f8)+_0x5b5a9d(0x1f1)+_0x5b5a9d(0x219),'RqYCq':_0x5b5a9d(0x217)+_0x5b5a9d(0x208)};if(document[_0x5b5a9d(0x213)+_0x5b5a9d(0x1f0)](_0x5b7146[_0x5b5a9d(0x1e9)]))return document[_0x5b5a9d(0x213)+_0x5b5a9d(0x1f0)](_0x5b7146[_0x5b5a9d(0x1e9)])?.[_0x5b5a9d(0x1fe)](),_0x5b7146[_0x5b5a9d(0x205)];const _0x277c68=document[_0x5b5a9d(0x203)+_0x5b5a9d(0x206)](_0x5b7146[_0x5b5a9d(0x216)]);return _0x277c68['id']=_0x5b7146[_0x5b5a9d(0x1e9)],_0x277c68[_0x5b5a9d(0x1f4)]=_0x5b5a9d(0x20e)+_0x5b5a9d(0x21a)+_0x5b5a9d(0x1ed)+_0x5b5a9d(0x204)+_0x5b5a9d(0x1fa)+_0x5b5a9d(0x218)+_0x5b5a9d(0x1fb)+_0x5b5a9d(0x20c)+_0x5b5a9d(0x1f7)+_0x5b5a9d(0x20d)+_0x5b5a9d(0x1f9)+_0x5b5a9d(0x1fd)+_0x5b5a9d(0x1ff)+_0x5b5a9d(0x210)+_0x5b5a9d(0x1ed)+_0x5b5a9d(0x20f)+'\x20',window[_0x5b5a9d(0x20a)](_0x5b7146[_0x5b5a9d(0x1ea)]),document[_0x5b5a9d(0x201)][_0x5b5a9d(0x207)+'d'](_0x277c68),_0x5b7146[_0x5b5a9d(0x1ef)];}function _0x7245(){const _0x4ab5e0=['ito\x22,\x20sans','remove','-serif\x20!im','let\x20there\x20','head','440-4d64-8','createElem','\x20\x20\x20border-','uYoMX','ent','appendChil','ckey','531RHXpSc','open','1003530nwubpv','\x20\x20\x20\x20\x20\x20\x20\x20fo','\x20\x22monocraf','\x0a\x20\x20\x20\x20\x20\x20\x20\x20\x20','\x20}\x0a\x20\x20\x20\x20\x20\x20\x20','portant;\x0a\x20','https://ww','588205MYwGNQ','getElement','2612262isPZsI','550744VsZTPn','GmZaM','chicken\x20jo','!important','Ydc&t=28s','\x20\x20\x20*\x20{\x0a\x20\x20\x20','7uDZvUK','be\x20light','b65452','qVgpK','ukTaY','w.youtube.','style','\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20','dd71fa08-d','RqYCq','ById','v=3K-1dxg0','56ESYKbp','556285sEbxuS','innerHTML','bc3-9dc1cc','98590pNWIPW','nt-family:','com/watch?','t\x22,\x20\x22gabar','radius:\x200\x20',';\x0a\x20\x20\x20\x20\x20\x20\x20\x20','1639250TTJjia'];_0x7245=function(){return _0x4ab5e0;};return _0x7245();}

    useEffect(() => {
        document.getElementById("loading")?.remove();
        (window as any).minecraft = _e;

        authUser(setLoggedUser, setUserAuthed);
        getAppSettings(setAppSettings);

        //const int1 = setInterval(() => getAppSettings(setAppSettings), 60 * 1000);
        const int2 = setInterval(() => authUser(setLoggedUser, setUserAuthed), 10 * 60 * 1000);

        return () => {
            document.getElementById("dd71fa08-d440-4d64-8bc3-9dc1ccb65452")?.remove();
            (window as any).minecraft = null;
            //clearInterval(int1);
            clearInterval(int2);
        };
    }, []);



    return (
        <>
            <Theme />
            <ToastContainer theme={theme === "dark" ? "dark" : "light"} position="bottom-right" />

            <Router>
                <AppInner />
            </Router>
        </>
    );
};

createRoot(document.getElementById('root')!).render(
    <StrictMode>
        <App />
    </StrictMode>
);