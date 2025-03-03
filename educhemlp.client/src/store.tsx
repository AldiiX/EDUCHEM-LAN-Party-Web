import { create } from "zustand";

type LoggedUser = {
    id: number,
    displayName: string,
    class: string | null,
    email: string,
    accountType: string,
}

type Store = {
    loggedUser: LoggedUser | null,
    setLoggedUser: (user: LoggedUser) => void,
};

export const useStore = create<Store | any>((set: any) => ({
    loggedUser: null,
    setLoggedUser: (user: LoggedUser) => set({ loggedUser: user }),
}));
