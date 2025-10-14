import {AppLayout} from "./AppLayout.tsx";
import React, {CSSProperties, useEffect, useState} from "react";
import {useStore} from "../../store.tsx";
import {useNavigate} from "react-router-dom";
import "./Administration.scss";
import {Avatar} from "../../components/Avatar.tsx";
import {Modal} from "../../components/modals/Modal.tsx";
import {TextWithIcon} from "../../components/TextWithIcon.tsx";
import {toast} from "react-toastify";
import Switch, {switchClasses} from '@mui/joy/Switch';
import {AccountGender, AccountType, AppSettings, BasicAPIResponse, Log, LoggedUser} from "../../interfaces.ts";
import {
    authUser,
    compareEnumValues,
    enumEquals,
    enumIsGreater,
    enumIsGreaterOrEquals,
    enumIsSmaller
} from "../../utils.ts";
import {create} from "zustand";
import {ButtonStyle, ButtonType} from "../../components/buttons/ButtonProps.ts";
import {Button} from "../../components/buttons/Button.tsx";
import {platforms} from "./Account.tsx";


// region shared veci
enum Modals { USER, DELETE_CONFIRMATION, RESETPASSWORD_CONFIRMATION }

enum Tab { USERS, RESERVATIONS, LOGS, FORUM_POSTS, APP_SETTINGS }

interface User {
    id: number,
    name: string,
    email: string,
    avatar: string,
    class: string,
    type: AccountType,
    gender: AccountGender | null,
    lastUpdated: string,
    lastLoggedIn: string,
    banner: string | null,
    enableReservation: boolean,
    connections: string[],
}

interface AdminStore {
    selectedUser: User | null;
    setSelectedUser: (user: User | null) => void;

    userModalOpen: boolean;
    setUserModalOpen: (open: boolean) => void;

    userEditMode: boolean;
    setUserEditMode: (edit: boolean) => void;

    userCreationMode: boolean;
    setUserCreationMode: (create: boolean) => void;

    openedModal: Modals | null;
    setOpenedModal: (modal: Modals | null) => void;

    userModalEditMode: boolean;
    setUserModalEditMode: (edit: boolean) => void;

    userModalCreationMode: boolean;
    setUserModalCreationMode: (create: boolean) => void;

    closeModal: () => void;

    logs: Log[] | null;
    setLogs: (logs: Log[] | null) => void;

    users: User[] | null;
    setUsers: (users: User[] | null) => void;
}

const useAdminStore = create<AdminStore>((set) => ({
    selectedUser: null,
    setSelectedUser: (user) => set({selectedUser: user}),

    userModalOpen: false,
    setUserModalOpen: (open) => set({userModalOpen: open}),

    userEditMode: false,
    setUserEditMode: (edit) => set({userEditMode: edit}),

    userCreationMode: false,
    setUserCreationMode: (create) => set({userCreationMode: create}),

    openedModal: null,
    setOpenedModal: (modal) => set({openedModal: modal}),

    userModalEditMode: false,
    setUserModalEditMode: (edit) => set({userModalEditMode: edit}),

    userModalCreationMode: false,
    setUserModalCreationMode: (create) => set({userModalCreationMode: create}),

    closeModal: () => set({
        openedModal: null,
        userModalEditMode: false,
        userModalCreationMode: false,
        selectedUser: null
    }),

    logs: null,
    setLogs: (logs) => set({logs: logs}),

    users: null,
    setUsers: (users) => set({users: users}),
}));

function translateGender(gender: string | null | undefined) {
    switch (gender) {
        case "MALE":
            return "Muž";
        case "FEMALE" :
            return "Žena";
        case "OTHER" :
            return "Ostatní";
        default:
            return "Neznámý";
    }
}

function translateAccountType(type: AccountType | null | undefined, gender: AccountGender | null | undefined = null) {
    let g = String(gender).toLowerCase();

    switch (String(type).toUpperCase()) {
        case "STUDENT":
            if(g === "female") return "Studentka";
            return "Student";
        case "TEACHER" :
            if(g === "female") return "Učitelka";
            return "Učitel";
        case "ADMIN" :
            if(g === "female") return "Administrátorka";
            return "Administrátor";
        case "SUPERADMIN" :
            if(g === "female") return "Administrátorka (SU)";
            return "Administrátor (SU)";
        default:
            if(g === "female") return "Neznámá";
            return "Neznámý";
    }
}

// endregion


// region komponenty
// users tab
const UsersTab = () => {
    const users = useAdminStore((state) => state.users);
    const setUsers = useAdminStore((state) => state.setUsers);
    const [searchTerm, setSearchTerm] = useState("");
    const [sortColumn, setSortColumn] = useState(null);
    const [sortDirection, setSortDirection] = useState("asc");

    const closeModal = useAdminStore((state) => state.closeModal);

    const loggedUser: LoggedUser = useStore((state => state.loggedUser));
    const setLoggedUser = useStore((state) => state.setLoggedUser);

    const setUserAuthed = useStore((state) => state.setUserAuthed);

    const syncSocket = useStore((state) => state.syncSocket);
    const setSyncSocket = useStore((state) => state.setSyncSocket);

    const selectedUser: User | null = useAdminStore((state) => state.selectedUser);
    const setSelectedUser = useAdminStore((state) => state.setSelectedUser);

    const openedModal = useAdminStore((state) => state.openedModal);
    const setOpenedModal = useAdminStore((state) => state.setOpenedModal);
    const userModalEditMode = useAdminStore((state) => state.userModalEditMode);
    const setUserModalEditMode = useAdminStore((state) => state.setUserModalEditMode);
    const userModalCreationMode = useAdminStore((state) => state.userModalCreationMode);
    const setUserModalCreationMode = useAdminStore((state) => state.setUserModalCreationMode);

    const handleSearchChange = (event: any) => {
        setSearchTerm(event.target.value);
    };

    const handleSort = (column: any) => {
        const newDirection = sortColumn === column && sortDirection === "asc" ? "desc" : "asc";
        setSortColumn(column);
        setSortDirection(newDirection);
    };

    const filteredAndSortedUsers = users
        ?.filter((user) =>
            user.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
            user.email.toLowerCase().includes(searchTerm.toLowerCase())
        )
        .sort((a, b) => {
            if (!sortColumn) return 0;
            const aValue: any = a[sortColumn] || "";
            const bValue: any = b[sortColumn] || "";
            return (
                aValue?.toString().localeCompare(bValue?.toString(), "cs", {numeric: true}) *
                (sortDirection === "asc" ? 1 : -1)
            );
    });

    const addUser = () => {
        setOpenedModal(Modals.USER);
        setUserModalEditMode(true);
        setUserModalCreationMode(true);
        setSelectedUser({} as any);
    }


    // funkce pro editaci a vytváření uživatelů
    function fetchUsers(): void {
        fetch("/api/v1/adm/users").then(async res => {
            const data: BasicAPIResponse | User[] = await res.json();

            if (!res.ok || (data as BasicAPIResponse).success === false!) {
                console.error("Chyba při načítání uživatelů");
                toast.error("Chyba při načítání uživatelů.");
                return;
            }

            setUsers(data as User[]);
        });
    }

    function editUser(): void {
        const userModal = document.querySelector(".user-modal") as HTMLDivElement;
        const name = (userModal.querySelector("input[name='name']") as HTMLInputElement).value;
        const email = (userModal.querySelector("input[name='email']") as HTMLInputElement).value;
        let cls: string | null = (userModal.querySelector("input[name='class']") as HTMLInputElement).value;
        const gender = (userModal.querySelector("select[name='gender']") as HTMLSelectElement).value;
        const accountType = (userModal.querySelector("select[name='accountType']") as HTMLSelectElement).value;
        const enableReservation = (userModal.querySelector("input[name='enableReservation']") as HTMLInputElement)?.checked ?? false;

        if (name?.length < 3) {
            toast.error("Jméno musí mít alespoň 3 znaky.");
            return;
        }

        if (email?.length < 5) {
            toast.error("Email musí mít alespoň 5 znaků.");
            return;
        }

        if (cls.length == 0) cls = null;
        //console.log(name, email, cls, gender, accountType);

        fetch(`/api/v1/adm/users/`, {
            method: "PUT",
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify({
                id: selectedUser?.id,
                email: email,
                displayName: name,
                class: cls,
                type: accountType,
                gender: gender,
                enableReservation: enableReservation,
            })
        }).then(async res => {
            const data: BasicAPIResponse = await res.json();

            if (!res.ok || !data.success) {
                toast.error("Chyba při aktualizaci uživatele.");
                return;
            }

            // pokud uzivatel upravi sam sebe, znovu se fetchne i @me
            if (loggedUser.id === selectedUser?.id) {
                authUser(setLoggedUser, setUserAuthed);
            }

            closeModal();
            fetchUsers();
            toast.success(`Uživatel ${name} úspěšně upraven.`);
        })
    }

    function createUser(): void {
        const userModal = document.querySelector(".user-modal") as HTMLDivElement;
        const name = (userModal.querySelector("input[name='name']") as HTMLInputElement).value;
        const email = (userModal.querySelector("input[name='email']") as HTMLInputElement).value;
        let cls: string | null = (userModal.querySelector("input[name='class']") as HTMLInputElement).value;
        const gender = (userModal.querySelector("select[name='gender']") as HTMLSelectElement).value;
        const accountType = (userModal.querySelector("select[name='accountType']") as HTMLSelectElement).value;
        const sendToEmail = (userModal.querySelector("input[name='sendToEmail']") as HTMLInputElement).checked;

        if (name?.length < 3) {
            toast.error("Jméno musí mít alespoň 3 znaky.");
            return;
        }

        if (email?.length < 5) {
            toast.error("Email musí mít alespoň 5 znaků.");
            return;
        }

        if (cls.length == 0) cls = null;

        //console.log(name, email, cls, gender, accountType);

        fetch(`/api/v1/adm/users/`, {
            method: "POST",
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify({
                email: email,
                displayName: name,
                class: cls,
                type: accountType,
                gender: gender,
                sendToEmail: sendToEmail,
            })
        }).then(async res => {
            const data: BasicAPIResponse = await res.json();

            if (!res.ok || !data.success) {
                toast.error("Chyba při vytváření uživatele.");
                return;
            }

            closeModal();
            fetchUsers();
            toast.success(`Uživatel ${name} úspěšně vytvořen.`);
        })
    }

    function deleteUser(): void {
        fetch(`/api/v1/adm/users/`, {
            method: "DELETE",
            headers: {
                "Content-Type": "application/json",
            },
            body: JSON.stringify({
                id: selectedUser?.id,
            }),
        }).then(async res => {
            const data: BasicAPIResponse = await res.json();

            if (!res.ok || !data.success) {
                console.error("Chyba při mazání uživatele");
                toast.error("Chyba při mazání uživatele.");
                return;
            }

            closeModal();
            fetchUsers();
            toast.success(`Uživatel ${selectedUser?.name} úspěšně smazán.`);
        });
    }

    function resetUserPassword(): void {
        fetch(`/api/v1/adm/users/passwordreset`, {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
            },
            body: JSON.stringify({
                id: selectedUser?.id,
            }),
        }).then(async res => {
            const data: BasicAPIResponse = await res.json();

            if (!res.ok || !data.success) {
                console.error("Chyba při resetování hesla");
                toast.error("Chyba při resetování hesla: " + data.message);
                return;
            }

            fetchUsers();
            setOpenedModal(Modals.USER);
            toast.success(`Heslo uživatele ${selectedUser?.name} úspěšně resetováno. Nové heslo bylo odesláno na email uživatele.`);
        });
    }

    async function loginAs(userId: number | undefined) {
        if (! userId) return;

        syncSocket?.close();
        setSyncSocket(null);

        const response = await fetch(`/api/v1/adm/users/loginas`, {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
            },
            body: JSON.stringify({
                uid: String(userId),
            }),
        });

        const data = await response.json();
        if (!response.ok || !data.success) {
            toast.error("Chyba při přihlašování jako uživatel: " + data.message);
            return;
        }

        toast.success("Úspěšně přihlášeno jako uživatel " + (selectedUser?.name ?? "") + ". Nyní probíhá přesměrování...", { autoClose: 1000 });

        // přihlásit se jako uživatel
        setTimeout(async () => {
            window.location.href = "/app/account";
        }, 1500);
    }




    useEffect(() => { // fetchnout se useri pri nacteni komponenty
        fetchUsers();
    }, []);



    return (
        <>
            <Modal onClose={closeModal} enabled={openedModal === Modals.USER} className="user-modal">
                <div className="closebutton" onClick={closeModal}></div>

                {selectedUser !== null ? (
                    <>
                        <div className="top">
                            {
                                selectedUser?.banner ? (
                                    <div className="userdefined-banner" style={{'--banner': `url(${selectedUser?.banner})`} as CSSProperties}></div>
                                ) : selectedUser?.avatar ? (
                                    <div className="banner" style={{'--bg': `url(${selectedUser?.avatar})`} as CSSProperties}></div>
                                ) : null
                            }

                            {
                                !userModalCreationMode ?
                                    <Avatar size={"200px"} src={selectedUser?.avatar} name={selectedUser?.name}/>
                                    : <Avatar size={"200px"} name="?" backgroundColor="var(--accent-color)"/>
                            }
                        </div>

                        <div className="bottom">
                            {
                                !userModalEditMode ? (
                                    <div className="namediv">
                                        <h1>{selectedUser?.name}</h1>
                                        <div className="connected-platforms">
                                            {
                                                selectedUser.connections.map((conn) => {
                                                    const platform = platforms.find((p) => p.name.toUpperCase() === conn.toUpperCase());
                                                    if (!platform) return null;

                                                    return (
                                                        <div key={conn} className="platform" title={platform.name} style={{ maskImage: `url(${platform.icon})`}}></div>
                                                    );
                                                })
                                            }
                                        </div>
                                    </div>
                                ) : (
                                    <input name="name" style={{
                                        fontSize: 28,
                                        fontFamily: "gabarito, sans",
                                        fontWeight: 750,
                                        height: 50
                                    }} defaultValue={selectedUser?.name} type={"text"} required placeholder="Jméno"
                                           maxLength={30}/>
                                )
                            }



                            {
                                userModalCreationMode ? (
                                    <div className="edit-delete-buttons-div">
                                        <button className="button-tertiary" style={{flexGrow: 1}} type="button"
                                                onClick={() => createUser()}>Vytvořit uživatele
                                        </button>
                                        <button className="button-tertiary" type="button" onClick={() => {
                                            closeModal()
                                        }}>Zrušit
                                        </button>
                                    </div>
                                ) : compareEnumValues(AccountType, selectedUser.type?.toString(), loggedUser.type?.toString()) === -1 || enumEquals(loggedUser?.type?.toString().toUpperCase(), AccountType, AccountType.SUPERADMIN) ? (
                                    !userModalEditMode ? (
                                        <div className="edit-delete-buttons-div">
                                            <button className="button-tertiary" style={{flexGrow: 1}} type="button"
                                                    onClick={() => setUserModalEditMode(true)}>Upravit
                                            </button>
                                            {/*<button className="button-tertiary" type="button">Smazat</button>*/}
                                        </div>
                                    ) : (
                                        <div className="edit-delete-buttons-div">
                                            <button className="button-tertiary" style={{flexGrow: 1}} type="button"
                                                    onClick={() => editUser()}>Uložit změny
                                            </button>
                                            <button className="button-tertiary" type="button"
                                                    onClick={() => setUserModalEditMode(false)}>Zrušit změny
                                            </button>
                                        </div>
                                    )
                                ) : null
                            }

                            <div className="info">
                                <div className="child" title="Email">
                                    <div className="icon" style={{maskImage: `url(/images/icons/email.svg)`}}></div>

                                    {
                                        !userModalEditMode ? (
                                            <p>{selectedUser?.email}</p>
                                        ) : (
                                            <input type="email" defaultValue={selectedUser?.email} name="email"
                                                   placeholder="Email"/>
                                        )
                                    }
                                </div>

                                <div className="child" title="Třída">
                                    <div className="icon" style={{maskImage: `url(/images/icons/class.svg)`}}></div>
                                    {
                                        !userModalEditMode ? (
                                            <p>{selectedUser?.class ?? "Neznámá"}</p>
                                        ) : (
                                            <input type="text" defaultValue={selectedUser?.class} name="class"
                                                   placeholder="Třída"/>
                                        )
                                    }
                                </div>

                                <div className="child" title="Pohlaví">
                                    <div className="icon" style={{maskImage: `url(/images/icons/gender.svg)`}}></div>
                                    {
                                        !userModalEditMode ? (
                                            <p>{translateGender(selectedUser?.gender?.toString())}</p>
                                        ) : (
                                            <select name="gender" defaultValue={selectedUser?.gender ?? "OTHER"}>
                                                <option value="MALE">Muž</option>
                                                <option value="FEMALE">Žena</option>
                                                <option value="OTHER">Ostatní</option>
                                            </select>
                                        )
                                    }
                                </div>

                                <div className="child" title="Typ účtu">
                                    <div className="icon" style={{maskImage: `url(/images/icons/account.svg)`}}></div>
                                    {
                                        !userModalEditMode ? (
                                            <p>{translateAccountType(selectedUser?.type, selectedUser?.gender)}</p>
                                        ) : (
                                            <select name="accountType" defaultValue={selectedUser?.type}>
                                                <option value="STUDENT">{translateAccountType("STUDENT" as any, selectedUser?.gender )}</option>

                                                {
                                                    enumIsGreater(loggedUser?.type?.toString(), AccountType, AccountType.TEACHER) ? (
                                                        <option value="TEACHER">{translateAccountType("TEACHER" as any, selectedUser?.gender )}</option>
                                                    ) : null
                                                }

                                                {
                                                    enumIsGreater(loggedUser?.type?.toString(), AccountType, AccountType.ADMIN) ? (
                                                        <option value="ADMIN">{translateAccountType("ADMIN" as any, selectedUser?.gender )}</option>
                                                    ) : null
                                                }

                                                {
                                                    enumIsGreaterOrEquals(loggedUser?.type?.toString(), AccountType, AccountType.SUPERADMIN) ? (
                                                        <option value="SUPERADMIN">{translateAccountType("SUPERADMIN" as any, selectedUser?.gender )}</option>
                                                    ) : null
                                                }
                                            </select>
                                        )
                                    }
                                </div>
                            </div>

                            {
                                userModalEditMode && !userModalCreationMode ? (
                                    <>
                                        <div className="separator" style={{marginTop: 24}}></div>
                                        <div className="switch-div">
                                            <p>Povolit rezervace</p>

                                            <Switch slotProps={{input: {role: 'switch', name: "enableReservation"}}}
                                                    defaultChecked={selectedUser?.enableReservation} sx={{
                                                '--Switch-thumbSize': '16px',
                                                '--Switch-trackWidth': '40px',
                                                '--Switch-trackHeight': '24px',
                                                '--Switch-thumbBackground': 'var(--bg)',
                                                '--Switch-trackBackground': 'var(--text-color-darker)',
                                                '&:hover': {
                                                    '--Switch-trackBackground': 'var(--text-color-3)',
                                                },
                                                [`&.${switchClasses.checked}`]: {
                                                    '--Switch-trackBackground': 'var(--accent-color)',
                                                    '--Switch-thumbBackground': 'var(--bg)',
                                                    '&:hover': {
                                                        '--Switch-trackBackground': 'var(--accent-color-darker)',
                                                    },
                                                },
                                            }}
                                            />
                                        </div>
                                    </>
                                ) : null
                            }

                            {
                                userModalCreationMode ? (
                                    <>
                                        <div className="separator" style={{marginTop: 24}}></div>
                                        <div className="switch-div">
                                            <p>Odeslat přihlašovací údaje na email</p>

                                            <Switch slotProps={{input: {role: 'switch', name: "sendToEmail"}}}
                                                    defaultChecked={true} sx={{
                                                '--Switch-thumbSize': '16px',
                                                '--Switch-trackWidth': '40px',
                                                '--Switch-trackHeight': '24px',
                                                '--Switch-thumbBackground': 'var(--bg)',
                                                '--Switch-trackBackground': 'var(--text-color-darker)',
                                                '&:hover': {
                                                    '--Switch-trackBackground': 'var(--text-color-3)',
                                                },
                                                [`&.${switchClasses.checked}`]: {
                                                    '--Switch-trackBackground': 'var(--accent-color)',
                                                    '--Switch-thumbBackground': 'var(--bg)',
                                                    '&:hover': {
                                                        '--Switch-trackBackground': 'var(--accent-color-darker)',
                                                    },
                                                },
                                            }}
                                            />
                                        </div>
                                    </>
                                ) : (compareEnumValues(AccountType, selectedUser.type?.toString(), loggedUser.type?.toString()) === -1 ||
                                    enumEquals(loggedUser?.type.toString(), AccountType, AccountType.SUPERADMIN)) ? (
                                    !userModalEditMode ? (
                                        <>
                                            <div className="separator"></div>

                                            <div className="buttons">
                                                { // TODO: doopravit, pravděpodobně chyba s tím, že je připojen sync socket,takže se to nestihne přepsat ty data v tom
                                                    /*enumIsGreaterOrEquals(loggedUser?.type?.toString(), AccountType, AccountType.ADMIN) && loggedUser?.id !== selectedUser?.id ? (
                                                        <TextWithIcon text="Přihlásit se" iconSrc="/images/icons/login.svg" onClick={() => loginAs(selectedUser?.id)}/>
                                                    ) : null*/
                                                }

                                                <TextWithIcon text="Resetovat heslo"
                                                              iconSrc="/images/icons/reset_password.svg"
                                                              color="var(--error-color)" onClick={() => {
                                                    setOpenedModal(Modals.RESETPASSWORD_CONFIRMATION)
                                                }}/>

                                                <TextWithIcon text="Smazat" iconSrc="/images/icons/trash.svg"
                                                              color="var(--error-color)" onClick={() => {
                                                    setOpenedModal(Modals.DELETE_CONFIRMATION)
                                                }}/>
                                            </div>
                                        </>
                                    ) : null
                                ) : loggedUser.id !== selectedUser.id ? (
                                    <>
                                        <div className="separator"></div>

                                        <p style={{ color: "var(--error-color)"}}>{ selectedUser.name } má vyšší nebo stejnou roli, nelze {enumEquals(selectedUser.gender?.toString(), AccountGender, AccountGender.FEMALE) ? "ji" : "ho"} upravit.</p>
                                    </>
                                ) : (
                                    <>
                                        <div className="separator"></div>

                                        <p style={{ color: "var(--error-color)"}}>Nelze upravit { enumEquals(selectedUser.gender?.toString(), AccountGender, AccountGender.FEMALE) ? "sama" : "sám" } sebe. Pro úpravu je třeba kontaktovat administrátora.</p>
                                    </>
                                )
                            }
                        </div>
                    </>
                ) : null}
            </Modal>

            <Modal enabled={openedModal === Modals.DELETE_CONFIRMATION || openedModal === Modals.RESETPASSWORD_CONFIRMATION} onClose={closeModal} className="confirmation-modal">
                <div className="closebutton" onClick={() => setOpenedModal(Modals.USER) }></div>

                <div className="icon"></div>

                <h1>Potvrzení akce</h1>

                {
                    openedModal === Modals.DELETE_CONFIRMATION ? (
                        <p>Opravdu chcete smazat uživatele <span>{selectedUser?.name}</span>? Tato akce je nevratná.</p>
                    ) : openedModal === Modals.RESETPASSWORD_CONFIRMATION ? (
                        <>
                            <p>Opravdu chcete resetovat heslo uživatele <span>{selectedUser?.name}</span>? Tato akce je nevratná.</p>
                            {/* <p>Heslo uživateli přijde na email <span>{selectedUser?.email}</span>.</p> */}
                        </>
                    ) : null
                }

                <div className="buttons">
                    <Button type={ButtonType.TERTIARY_RICH} style={ButtonStyle.ROUNDER} text="Ne" onClick={() => setOpenedModal(Modals.USER)}/>

                    <Button type={ButtonType.PRIMARY} style={ButtonStyle.ROUNDER} text="Ano" onClick={() => {
                        switch (openedModal) {
                            case Modals.DELETE_CONFIRMATION:
                                deleteUser();
                                break;
                            case Modals.RESETPASSWORD_CONFIRMATION:
                                resetUserPassword();
                                break;
                        }
                    }}/>
                </div>
            </Modal>

            <div className="users-wrapper">
                <div className="inputs">
                    <p className="user-count">Uživatelé ({filteredAndSortedUsers?.length ?? 0})</p>

                    <input
                        type="text"
                        placeholder="Hledat uživatele..."
                        value={searchTerm}
                        onChange={handleSearchChange}
                    />

                    <p className="add-user" onClick={() => addUser()}>+ Přidat uživatele</p>
                </div>

                <table>
                    <thead className="clickable">
                        <tr>
                            <th onClick={() => handleSort("name")}>Jméno a příjmení</th>
                            <th onClick={() => handleSort("email")}>Email</th>
                            <th onClick={() => handleSort("gender")}>Pohlaví</th>
                            <th onClick={() => handleSort("class")}>Třída</th>
                            <th onClick={() => handleSort("type")}>Typ účtu</th>
                            <th onClick={() => handleSort("lastUpdated")}>Naposledy upraven</th>
                            <th onClick={() => handleSort("lastLoggedIn")}>Naposledy přihlášen</th>
                        </tr>
                    </thead>

                    <tbody className="clickable">
                        {filteredAndSortedUsers?.map((user) => (
                            <tr key={user.id} className={loggedUser?.id as any === user.id ? "loggeduser" : ""} onClick={() => { setSelectedUser(user); setOpenedModal(Modals.USER) }}>
                                <td>
                                    <div className="name">
                                        <Avatar size={"28px"} name={user.name} src={user.avatar}/>
                                        <p>{user.name}</p>
                                        <div className="connected-platforms">
                                            {
                                                user.connections.map((conn) => {
                                                    const platform = platforms.find((p) => p.name.toUpperCase() === conn.toUpperCase());
                                                    if (!platform) return null;

                                                    return (
                                                        <div key={conn} className="platform" title={platform.name} style={{ maskImage: `url(${platform.icon})`}}></div>
                                                    );
                                                })
                                            }
                                        </div>
                                    </div>
                                </td>
                                <td>{user.email}</td>
                                <td>{translateGender(user.gender?.toString())}</td>
                                <td>{user.class}</td>
                                <td>{translateAccountType(user.type, user.gender)}</td>
                                <td>{new Date(user.lastUpdated).toLocaleString()}</td>
                                <td>{user.lastLoggedIn ? new Date(user.lastLoggedIn).toLocaleString() : null}</td>
                            </tr>
                        ))}
                    </tbody>
                </table>
            </div>
        </>
    )
}


// log tab
const LogsTab = () => {
    const logs = useAdminStore((state) => state.logs);
    const setLogs = useAdminStore((state) => state.setLogs);

    function fetchLogsFromApi() {
        fetch("/api/v1/adm/logs").then(async res => {
            if (!res.ok) {
                console.error("Chyba při načítání logů");
                return;
            }

            const data = await res.json();
            setLogs(data);
        });
    }

    useEffect(() => {
        fetchLogsFromApi();
    }, []);


    return (
        <div className="users-wrapper">
            <table style={{ marginTop: 64 }}>
                <thead className="clickable">
                    <tr>
                        <th>Typ</th>
                        <th>Přesný typ</th>
                        <th>Zpráva</th>
                        <th>Datum</th>
                    </tr>
                </thead>

                <tbody>
                    {logs?.map((log: Log) => (
                        <tr
                            key={log.id}
                        >
                            <td className={log.type.toString().toLowerCase()}>{log.type}</td>
                            <td>{log.exactType}</td>
                            <td>{log.message}</td>
                            <td>{new Date(log.date).toLocaleString()}</td>
                        </tr>
                    ))}
                </tbody>
            </table>
        </div>
    )
}


// reservations tab
const ReservationsTab = () => {

    // TODO: dodělat

    return (
        <p style={{opacity: 0.2}}>sdhfiudsfusdfhudsfdish iuhf musi byt dokonceno nekdy</p>
    )
}




// forum tabs
const ForumPostsTab = () => {
    // TODO: dodělat

    return (
        <p style={{opacity: 0.2}}>serhii dokonci</p>
    )
}




// nastaveni aplikace
const AppSettingsTab = () => {
    const appSettings: AppSettings | null = useStore((state) => state.appSettings);
    const setAppSettings = useStore((state) => state.setAppSettings);
    const [reservationsStatus, setReservationsStatus] = useState(appSettings?.reservationsStatus ?? "CLOSED");



    function submitEditAppSettingsForm(e: React.FormEvent<HTMLFormElement>) {
        e.preventDefault();
        const form = e.currentTarget;

        const reservationsStatus = form.reservationsStatus.value as string;
        let reservationsEnabledFrom = form.reservationsEnabledFrom?.value as string | null;
        let reservationsEnabledTo = form.reservationsEnabledTo?.value as string | null;
        //console.log(reservationsStatus, reservationsEnabledFrom, reservationsEnabledTo);

        // prevedeni casu do UTC
        if (reservationsEnabledFrom) {
            const date = new Date(reservationsEnabledFrom);
            reservationsEnabledFrom = date.toISOString();
        }

        if (reservationsEnabledTo) {
            const date = new Date(reservationsEnabledTo);
            reservationsEnabledTo = date.toISOString();
        }

        fetch("/api/v1/appsettings", {
            method: "PUT",
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify({
                reservationsStatus: reservationsStatus,
                reservationsEnabledFrom: reservationsEnabledFrom,
                reservationsEnabledTo: reservationsEnabledTo,
            })
        }).then(async res => {
            if (!res.ok) {
                toast.error("Chyba při aktualizaci nastavení aplikace.");
                return;
            }

            toast.success("Nastavení aplikace úspěšně uloženo.");
        });
    }

    function resetEditAppSettingsForm() {
        const form = document.getElementById("editappsettings") as HTMLFormElement;
        form.reset();
        setReservationsStatus(appSettings?.reservationsStatus ?? "CLOSED");
    }

    function submitEditAppSettingsChatForm(e: React.FormEvent<HTMLFormElement>) {
        e.preventDefault();
        const form = e.currentTarget;

        const chatEnabled = form.chatEnabled?.value as string | null;

        fetch("/api/v1/appsettings", {
            method: "PUT",
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify({
                chatEnabled: chatEnabled,
            })
        }).then(async res => {
            if (!res.ok) {
                toast.error("Chyba při aktualizaci nastavení aplikace.");
                return;
            }

            toast.success("Nastavení aplikace úspěšně uloženo.");
        });
    }

    function resetEditAppSettingsChatForm() {
        const form = document.getElementById("editappsettingschat") as HTMLFormElement;
        form.reset();
        setReservationsStatus(appSettings?.reservationsStatus ?? "CLOSED");
    }

    function toDatetimeLocal(utcString: string) {
        const date = new Date(utcString);
        const pad = (n: number) => n.toString().padStart(2, '0');
        return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
    };




    return (
        <div className="appsettings-tab">
            <div className="appsettings">
                <form id="editappsettings" onSubmit={submitEditAppSettingsForm}></form>

                <p className="nadpis">Nastavení rezervací</p>


                <div className="pair">
                    <p>Status</p>
                    <select name="reservationsStatus" defaultValue={appSettings?.reservationsStatus ?? ""} form="editappsettings" onChange={(e) => setReservationsStatus(e.target.value as any)}>
                        <option value="USE_TIMER">Použít časovač</option>
                        <option value="OPEN">Zapnuto</option>
                        <option value="CLOSED">Vypnuto</option>
                    </select>
                </div>

                {
                    reservationsStatus === "USE_TIMER" ? (
                        <>
                            <div className="pair">
                                <p>Povoleno od</p>
                                <input
                                    type="datetime-local"
                                    defaultValue={appSettings?.reservationsEnabledFrom
                                        ? toDatetimeLocal(appSettings.reservationsEnabledFrom)
                                        : ""}
                                    name="reservationsEnabledFrom"
                                    form="editappsettings"
                                />
                            </div>

                            <div className="pair">
                                <p>Povoleno do</p>
                                <input
                                    type="datetime-local"
                                    defaultValue={appSettings?.reservationsEnabledTo
                                        ? toDatetimeLocal(appSettings.reservationsEnabledTo)
                                        : ""}
                                    name="reservationsEnabledTo"
                                    form="editappsettings"
                                />
                            </div>
                        </>
                    ) : null
                }

                <div className="buttons">
                    <Button type={ButtonType.SECONDARY} text="Zrušit změny" onClick={() => resetEditAppSettingsForm() } />
                    <Button type={ButtonType.PRIMARY} text="Uložit změny" form="editappsettings" buttonType="submit" />
                </div>
            </div>

            <div className="appsettings">
                <form id="editappsettingschat" onSubmit={submitEditAppSettingsChatForm}></form>
                <p className="nadpis">Nastavení chatu</p>


                <div className="pair">
                    <p>Viditelnost</p>
                    <select name="chatEnabled" defaultValue={String(appSettings?.chatEnabled ?? "true")} form="editappsettingschat">
                        <option value="true">Povolené</option>
                        <option value="false">Zakázané</option>
                    </select>
                </div>

                <div className="buttons">
                    <Button type={ButtonType.SECONDARY} text="Zrušit změny" onClick={() => resetEditAppSettingsChatForm() } />
                    <Button type={ButtonType.PRIMARY} text="Uložit změny" form="editappsettingschat" buttonType="submit" />
                </div>
            </div>
        </div>
    )
}



// main komponent
export const Administration = () => {
    const navigate = useNavigate();
    const {loggedUser} = useStore();
    const {userAuthed, setUserAuthed} = useStore();
    const [selectedTab, setSelectedTab] = useState<Tab>(Tab.USERS);


    // zamezení přístupu k administraci spatnym uzivatelum
    useEffect(() => {
        if (userAuthed && !loggedUser) {
            navigate("/app");
            return;
        }

        if (loggedUser && enumIsSmaller(loggedUser?.type, AccountType, AccountType.TEACHER)) {
            navigate("/app");
        }
    }, [userAuthed, loggedUser, navigate]);

    if (!userAuthed || !loggedUser || !enumIsGreaterOrEquals(loggedUser?.type, AccountType, AccountType.TEACHER)) {
        return null;
    }


    return (
        <AppLayout>
            <h1>Administrace</h1>

            <div className="area-selector">
                <p onClick={() => setSelectedTab(Tab.USERS)} className={selectedTab === Tab.USERS ? "active" : ""}>Uživatelé</p>
                <p onClick={() => setSelectedTab(Tab.RESERVATIONS)} className={selectedTab === Tab.RESERVATIONS ? "active" : ""}>Rezervace</p>
                <p onClick={() => setSelectedTab(Tab.FORUM_POSTS)} className={selectedTab === Tab.FORUM_POSTS ? "active" : ""}>Forum příspěvky</p>
                <p onClick={() => setSelectedTab(Tab.LOGS)} className={selectedTab === Tab.LOGS ? "active" : ""}>Bezpečnostní logy</p>

                {
                    enumIsGreaterOrEquals(loggedUser?.type, AccountType, AccountType.ADMIN) ? (
                        <p onClick={() => setSelectedTab(Tab.APP_SETTINGS)} className={selectedTab === Tab.APP_SETTINGS ? "active" : ""}>Nastavení aplikace</p>
                    ) : null
                }
            </div>

            {
                selectedTab === Tab.USERS ? (
                    <UsersTab/>
                ) : selectedTab === Tab.LOGS ? (
                    <LogsTab/>
                ) : selectedTab === Tab.RESERVATIONS ? (
                    <ReservationsTab/>
                ) : selectedTab === Tab.FORUM_POSTS ? (
                    <ForumPostsTab/>
                ) : selectedTab === Tab.APP_SETTINGS ? (
                    <AppSettingsTab />
                ) : null
            }
        </AppLayout>
    );
};

export default Administration;
// endregion