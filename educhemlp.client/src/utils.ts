import {useStore} from "./store.tsx";
import {AccountType} from "./interfaces.ts";

export const getCookie = (name: string) => {
    const value = `; ${document.cookie}`;
    const parts = value.split(`; ${name}=`);
    if (parts.length === 2) return parts.pop()!.split(';').shift();
}

export const setCookie = (name: string, value: string, days: number) => {
    const date = new Date();
    date.setTime(date.getTime() + (days * 24 * 60 * 60 * 1000));
    const expires = `expires=${date.toUTCString()}`;
    document.cookie = `${name}=${value};${expires};path=/`;
}

export const generateRandomNumberFromTo = (from: number, to: number) => {
    return Math.floor(Math.random() * (to - from + 1) + from);
}

export const setWebTheme = (theme: "light" | "dark") => {
    document.documentElement.classList.remove('theme-dark', 'theme-light');
    document.documentElement.classList.add(`theme-${theme}`);
    setCookie("theme", theme, 365);
}

export const toggleWebTheme = () => {
    const theme = document.documentElement.classList.contains('theme-dark') ? "light" : "dark";
    setWebTheme(theme);
}

export const logout = (setLoggedUser: Function) => {
    fetch("/api/v1/loggeduser", {
        method: "DELETE",
        headers: {
            "Content-Type": "application/json",
        },
    }).then(() => {
        setLoggedUser(null);
    });
}

export function enumEquals(value: string | null | undefined, enumClass: any, targetEnum: any): boolean {
    if(!value) return false;

    const enumValue = enumClass[value as keyof typeof enumClass];
    return enumValue === targetEnum;
}

export function enumIsGreater(value: string | null | undefined, enumClass: any, targetEnum: any): boolean {
    if(!value) return false;

    const enumValue = enumClass[value as keyof typeof enumClass];
    return enumValue > targetEnum;
}

export function enumIsGreaterOrEquals(value: string | null | undefined, enumClass: any, targetEnum: any): boolean {
    if(!value) return false;

    const enumValue = enumClass[value as keyof typeof enumClass];
    return enumValue >= targetEnum;
}

export function enumIsSmaller(value: string | null | undefined, enumClass: any, targetEnum: any): boolean {
    if(!value) return false;

    const enumValue = enumClass[value as keyof typeof enumClass];
    return enumValue < targetEnum;
}

export function enumIsSmallerOrEquals(value: string | null | undefined, enumClass: any, targetEnum: any): boolean {
    if(!value) return false;

    const enumValue = enumClass[value as keyof typeof enumClass];
    return enumValue <= targetEnum;
}