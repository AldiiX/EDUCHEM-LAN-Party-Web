export function authUser(setLoggedUser: Function, setUserAuthed: Function) {
    fetch("/api/v1/loggeduser", {
        method: "GET",
        headers: {
            "Content-Type": "application/json",
        },
    }).then(async (response) => {
        const data = await response.json();

        if(!response.ok){
            setLoggedUser(null);
        } else setLoggedUser(data);

        setUserAuthed(true);
    });
}

export function getAppSettings(setAppSettings: Function) {
    fetch("/api/v1/appsettings", {
        method: "GET",
        headers: {
            "Content-Type": "application/json",
        },
    }).then(async (response) => {
        const data = await response.json();

        if(!response.ok){
            //setAppSettings(null);
        } else setAppSettings(data);
    });
}

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

export function parseEnumValue<T extends Record<string, string | number>>(
    enumType: T,
    value: string | null | undefined
): T[keyof T] | null {
    if (!value || !(value in enumType)) return null;
    return enumType[value as keyof T];
}


/*
*
* @returns -1 pokud a < b, 1 pokud a > b, 0 pokud a === b, null pokud a nebo b není v enumu
* */
export function compareEnumValues<T extends Record<string, string | number>>(
    enumType: T,
    a: string | null | undefined,
    b: string | null | undefined
): number | null {
    const aVal = parseEnumValue(enumType, a);
    const bVal = parseEnumValue(enumType, b);
    if (aVal === null || bVal === null) return null;
    return aVal < bVal ? -1 : aVal > bVal ? 1 : 0;
}


export function stringToEnum<T extends Record<string, string>>(
    enumObject: T,
    value: string | null
): T[keyof T] | undefined {
    if (!value) return undefined;

    const upperValue = value.toUpperCase();

    const matchingKey = Object.keys(enumObject).find(
        (key) => key.toUpperCase() === upperValue
    );

    if (matchingKey) {
        return enumObject[matchingKey as keyof T];
    }

    return undefined;
}

export function enumToString<T extends Record<string, string>>(
    enumObject: T,
    value: T[keyof T] | null
): string | undefined {
    if (value === null) return undefined;

    const matchingKey = Object.keys(enumObject).find(
        (key) => enumObject[key as keyof T] === value
    );

    if (matchingKey) {
        return matchingKey;
    }

    return undefined;
}

export function formatTime(ms: number): string {
    const totalSeconds = Math.floor(ms / 1000);
    const totalMinutes = Math.floor(totalSeconds / 60);
    const totalHours = Math.floor(totalMinutes / 60);
    const totalDays = Math.floor(totalHours / 24);
    const months = Math.floor(totalDays / 30);
    const days = totalDays % 30;
    const hours = totalHours % 24;
    const minutes = totalMinutes % 60;
    const seconds = totalSeconds % 60;

    const parts: string[] = [];

    if (months > 0) {
        parts.push(`${months}m`);
        parts.push(`${days}d`);
        parts.push(`${hours}h`);
        parts.push(`${minutes}min`);
        parts.push(`${seconds}s`);
    } else if (days > 0) {
        parts.push(`${days}d`);
        parts.push(`${hours}h`);
        parts.push(`${minutes}min`);
        parts.push(`${seconds}s`);
    } else if (hours > 0) {
        parts.push(`${hours}h`);
        parts.push(`${minutes}min`);
        parts.push(`${seconds}s`);
    } else if (minutes > 0) {
        parts.push(`${minutes}min`);
        parts.push(`${seconds}s`);
    } else {
        parts.push(`${seconds}s`);
    }

    return parts.join(' ');
}