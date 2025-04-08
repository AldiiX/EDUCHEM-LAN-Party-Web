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