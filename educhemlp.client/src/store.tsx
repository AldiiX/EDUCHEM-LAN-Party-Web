import { create } from "zustand";
import {AppSettings, CurrentPage, LoggedUser} from "./interfaces.ts";



type Store = {
    loggedUser: LoggedUser | null,
    setLoggedUser: (user: LoggedUser) => void,

    userAuthed: boolean,
    setUserAuthed: (authed: boolean) => void,

    selectedReservation: any,
    setSelectedReservation: (reservation: any) => void,

    menuOpened: boolean,
    setMenuOpened: (opened: boolean) => void,

    appSettings: AppSettings | null,
    setAppSettings: (settings: AppSettings) => void,

    isElectronApp: boolean,
    setIsElectronApp: (isElectronApp: boolean) => void,

    isElectronAppFullscreen: boolean,
    setIsElectronAppFullscreen: (isElectronAppFullscreen: boolean) => void,

    currentPage: CurrentPage | null,
    setCurrentPage: (page: CurrentPage) => void,

    syncSocket: WebSocket | null,
    setSyncSocket: (socket: WebSocket | null) => void,
};

export const useStore = create<Store | any>((set: any) => ({
    loggedUser: null,
    setLoggedUser: (user: LoggedUser) => set((state: Store) => {
        if (JSON.stringify(state.loggedUser) === JSON.stringify(user)) return {};
        return { loggedUser: user };
    }),

    userAuthed: false,
    setUserAuthed: (authed: boolean) => set({userAuthed: authed}),

    selectedReservation: null,
    setSelectedReservation: (reservation: any) => set({selectedReservation: reservation}),

    menuOpened: false,
    setMenuOpened: (opened: boolean) => set({menuOpened: opened}),

    appSettings: {
        reservationsStatus: "CLOSED",
        reservationsEnabledFrom: "9999-12-31T23:59:59.9999999",
        reservationsEnabledTo: "9999-12-31T23:59:59.9999999",
        reservationsEnabledRightNow: false,
    },
    setAppSettings: (settings: AppSettings) => set((state: Store) => {
        if (JSON.stringify(state.appSettings) === JSON.stringify(settings)) return {};
        return {appSettings: settings};
    }),

    isElectronApp: false,
    setIsElectronApp: (isElectronApp: boolean) => set({isElectronApp: isElectronApp}),

    isElectronAppFullscreen: false,
    setIsElectronAppFullscreen: (isElectronAppFullscreen: boolean) => set({isElectronAppFullscreen: isElectronAppFullscreen}),

    currentPage: null,
    setCurrentPage: (page: CurrentPage) => set((state: Store) => {
        if (JSON.stringify(state.currentPage) === JSON.stringify(page)) return {};
        return {currentPage: page};
    }),

    syncSocket: null,
    setSyncSocket: (socket: WebSocket | null) => set({syncSocket: socket }),
}));
