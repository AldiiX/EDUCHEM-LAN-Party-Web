/*
* datový typ určující strukturu uživatelského účtu získaného z /api/v1/loggeduser
* */
export type LoggedUser = {
    id: number,
    displayName: string,
    class: string | null,
    email: string,
    accountType: AccountType,
    gender: AccountGender | null,
    avatar: string | null,
    connections: string[] | null,
    banner: string | null,
    enableReservation: boolean,
}

export interface CurrentPage {
    name: string,
    id: string,
    icon: string,
    url: string,
    title: string,
}


export type AppSettings = {
    reservationsStatus: "USE_TIMER" | "OPEN" | "CLOSED",
    reservationsEnabledFrom: string,
    reservationsEnabledTo: string,
    reservationsEnabledRightNow: boolean,
    chatEnabled: boolean,
}

/*
* typ účtu LoggedUser classy
* */
export enum AccountType {
    STUDENT,
    TEACHER,
    ADMIN,
    SUPERADMIN,
}

export enum AccountGender {
    MALE, FEMALE, OTHER
}

/*
* datový typ určující strukturu uživatelského účtu získaného z /api/v1/adm/logs
* */
export interface Log {
    id: number,
    type: LogType,
    exactType: string,
    message: string,
    date: string,
}

export enum LogType {
    INFO, WARN, ERROR
}

/*
* typ pro basic /api/v1/ odpověď
* */
export type BasicAPIResponse = {
    success: boolean,
    message: string,
}