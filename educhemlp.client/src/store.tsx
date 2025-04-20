import { create } from "zustand";
import {LoggedUser} from "./interfaces.ts";



type Store = {
    loggedUser: LoggedUser | null,
    setLoggedUser: (user: LoggedUser) => void,

    userAuthed: boolean,
    setUserAuthed: (authed: boolean) => void,

    selectedReservation: any,
    setSelectedReservation: (reservation: any) => void,

    menuOpened: boolean,
    setMenuOpened: (opened: boolean) => void,
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

}));
