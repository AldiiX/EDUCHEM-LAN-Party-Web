export type LoggedUser = {
    id: number,
    displayName: string,
    class: string | null,
    email: string,
    accountType: AccountType,
}

export enum AccountType {
    STUDENT,
    TEACHER,
    ADMIN,
    SUPERADMIN,
}

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