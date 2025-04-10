import { AppLayout } from "./AppLayout.tsx";
import {CSSProperties, useEffect, useState} from "react";
import { useStore } from "../../store.tsx";
import { useNavigate } from "react-router-dom";
import "./Administration.scss";
import {Avatar} from "../../components/Avatar.tsx";
import {Modal} from "../../components/modals/Modal.tsx";
import {ButtonSecondary} from "../../components/buttons/ButtonSecondary.tsx";
import {TextWithIcon} from "../../components/TextWithIcon.tsx";
import { ButtonPrimary } from "../../components/buttons/ButtonPrimary.tsx";
import { toast } from "react-toastify";
import Switch, { switchClasses } from '@mui/joy/Switch';
import {AccountType, Log} from "../../interfaces.ts";
import {enumIsGreaterOrEquals, enumIsSmaller} from "../../utils.ts";
import { create } from "zustand";




// region shared veci
enum Modals { USER, DELETE_CONFIRMATION, RESETPASSWORD_CONFIRMATION }

enum Tab { USERS, RESERVATIONS, LOGS }

interface User {
    id: string,
    name: string,
    email: string,
    avatar: string,
    class: string,
    accountType: AccountType,
    gender: string,
    lastUpdated: string,
    lastLoggedIn: string,
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
    fetchUsersFromApi: () => Promise<void>;
}

const useAdminStore = create<AdminStore>((set) => ({
    selectedUser: null,
    setSelectedUser: (user) => set({ selectedUser: user }),

    userModalOpen: false,
    setUserModalOpen: (open) => set({ userModalOpen: open }),

    userEditMode: false,
    setUserEditMode: (edit) => set({ userEditMode: edit }),

    userCreationMode: false,
    setUserCreationMode: (create) => set({ userCreationMode: create }),

    openedModal: null,
    setOpenedModal: (modal) => set({ openedModal: modal }),

    userModalEditMode: false,
    setUserModalEditMode: (edit) => set({ userModalEditMode: edit }),

    userModalCreationMode: false,
    setUserModalCreationMode: (create) => set({ userModalCreationMode: create }),

    closeModal: () => set({
        openedModal: null,
        userModalEditMode: false,
        userModalCreationMode: false,
        selectedUser: null
    }),

    logs: null,
    setLogs: (logs) => set({ logs: logs }),

    users: null,
    setUsers: (users) => set({ users: users }),

    fetchUsersFromApi: async () => {
        const res = await fetch("/api/v1/adm/users");
        if (!res.ok) {
            console.error("Chyba při načítání uživatelů");
            return;
        }

        const data = await res.json();
        set({ users: data });
    }
}));

function translateGender(gender: string) {
    switch (gender) {
        case "MALE": return "Muž";
        case "FEMALE" : return "Žena";
        case "OTHER" : return "Ostatní";
        default: return "Neznámý";
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
    const loggedUser = useStore((state => state.loggedUser));

    const selectedUser = useAdminStore((state) => state.selectedUser);
    const setSelectedUser = useAdminStore((state) => state.setSelectedUser);
    const fetchUsersFromApi = useAdminStore((state) => state.fetchUsersFromApi);

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
                aValue?.toString().localeCompare(bValue?.toString(), "cs", { numeric: true }) *
                (sortDirection === "asc" ? 1 : -1)
            );
    });

    const addUser = () => {
        setOpenedModal(Modals.USER);
        setUserModalEditMode(true);
        setUserModalCreationMode(true);
        setSelectedUser({} as any);
    }




    useEffect(() => {
        fetchUsersFromApi().then();
    }, []);

    return (
        <>
            <Modal onClose={closeModal} enabled={openedModal === Modals.USER } className="user-modal">
                { selectedUser !== null ? (
                    <>
                        <div className="top">
                            {
                                selectedUser?.avatar ? (
                                    <div className="banner" style={{ '--bg': `url(${selectedUser?.avatar})`} as CSSProperties}></div>
                                ) : null
                            }

                            {
                                !userModalCreationMode ?
                                    <Avatar size={"200px"} src={selectedUser?.avatar} name={selectedUser?.name} />
                                    : <Avatar size={"200px"} name="?" backgroundColor="var(--accent-color)" />
                            }
                        </div>

                        <div className="bottom">
                            {
                                !userModalEditMode ? (
                                    <h1>{ selectedUser?.name }</h1>
                                ) : (
                                    <input name="name" style={{fontSize: 28, fontFamily: "gabarito, sans", fontWeight: 750, height: 50 }} defaultValue={selectedUser?.name} type={"text"}  required placeholder="Jméno" maxLength={30} />
                                )
                            }

                            {
                                !userModalEditMode ? (
                                    <div className="edit-delete-buttons-div">
                                        <button className="button-tertiary" style={{ flexGrow: 1 }} type="button" onClick={() => setUserModalEditMode(true)}>Upravit</button>
                                        {/*<button className="button-tertiary" type="button">Smazat</button>*/}
                                    </div>
                                ) : userModalCreationMode ? (
                                    <div className="edit-delete-buttons-div">
                                        <button className="button-tertiary" style={{ flexGrow: 1 }} type="button" onClick={() => {
                                            const userModal = document.querySelector(".user-modal") as HTMLDivElement;
                                            const name = (userModal.querySelector("input[name='name']") as HTMLInputElement).value;
                                            const email = (userModal.querySelector("input[name='email']") as HTMLInputElement).value;
                                            let cls: string | null = (userModal.querySelector("input[name='class']") as HTMLInputElement).value;
                                            const gender = (userModal.querySelector("select[name='gender']") as HTMLSelectElement).value;
                                            const accountType = (userModal.querySelector("select[name='accountType']") as HTMLSelectElement).value;
                                            const sendToEmail = (userModal.querySelector("input[name='sendToEmail']") as HTMLInputElement).checked;

                                            if(name?.length < 3) {
                                                toast.error("Jméno musí mít alespoň 3 znaky.");
                                                return;
                                            }

                                            if(email?.length < 5) {
                                                toast.error("Email musí mít alespoň 5 znaků.");
                                                return;
                                            }

                                            if(cls.length == 0) cls = null;

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
                                                    accountType: accountType,
                                                    gender: gender,
                                                    sendToEmail: sendToEmail,
                                                })
                                            }).then(async res => {
                                                if (!res.ok) {
                                                    toast.error("Chyba při vytváření uživatele.");
                                                    return;
                                                }

                                                //const data = await res.json();
                                                closeModal();
                                                fetchUsersFromApi().then();
                                                toast.success(`Uživatel ${name} úspěšně vytvořen.`);
                                            })
                                        }}>Vytvořit uživatele</button>
                                        <button className="button-tertiary" type="button" onClick={() => {closeModal()}}>Zrušit</button>
                                    </div>
                                ) : (
                                    <div className="edit-delete-buttons-div">
                                        <button className="button-tertiary" style={{ flexGrow: 1 }} type="button" onClick={() => {
                                            const userModal = document.querySelector(".user-modal") as HTMLDivElement;
                                            const name = (userModal.querySelector("input[name='name']") as HTMLInputElement).value;
                                            const email = (userModal.querySelector("input[name='email']") as HTMLInputElement).value;
                                            let cls: string | null = (userModal.querySelector("input[name='class']") as HTMLInputElement).value;
                                            const gender = (userModal.querySelector("select[name='gender']") as HTMLSelectElement).value;
                                            const accountType = (userModal.querySelector("select[name='accountType']") as HTMLSelectElement).value;

                                            if(name?.length < 3) {
                                                toast.error("Jméno musí mít alespoň 3 znaky.");
                                                return;
                                            }

                                            if(email?.length < 5) {
                                                toast.error("Email musí mít alespoň 5 znaků.");
                                                return;
                                            }

                                            if(cls.length == 0) cls = null;
                                            
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
                                                    accountType: accountType,
                                                    gender: gender,
                                                })
                                            }).then(async res => {
                                                if (!res.ok) {
                                                    toast.error("Chyba při aktualizaci uživatele.");
                                                    return;
                                                }

                                                closeModal();
                                                fetchUsersFromApi().then();
                                                toast.success(`Uživatel ${name} úspěšně upraven.`);
                                            })
                                        }}>Uložit změny</button>
                                        <button className="button-tertiary" type="button" onClick={() => setUserModalEditMode(false)}>Zrušit změny</button>
                                    </div>
                                )
                            }

                            <div className="info">
                                <div className="child">
                                    <div className="icon" style={{ maskImage: `url(/images/icons/email.svg)` }}></div>

                                    {
                                        !userModalEditMode ? (
                                            <p>{ selectedUser?.email }</p>
                                        ) : (
                                            <input type="email" defaultValue={ selectedUser?.email } name="email" placeholder="Email" />
                                        )
                                    }
                                </div>

                                <div className="child">
                                    <div className="icon" style={{ maskImage: `url(/images/icons/class.svg)` }}></div>
                                    {
                                        !userModalEditMode ? (
                                            <p>{ selectedUser?.class ?? "Neznámá" }</p>
                                        ) : (
                                            <input type="text" defaultValue={ selectedUser?.class } name="class" placeholder="Třída" />
                                        )
                                    }
                                </div>

                                <div className="child">
                                    <div className="icon" style={{ maskImage: `url(/images/icons/gender.svg)` }}></div>
                                    {
                                        !userModalEditMode ? (
                                            <p>{ translateGender(selectedUser?.gender) }</p>
                                        ) : (
                                            <select name="gender" defaultValue={selectedUser?.gender}>
                                                <option value="MALE">Muž</option>
                                                <option value="FEMALE">Žena</option>
                                                <option value="OTHER">Ostatní</option>
                                            </select>
                                        )
                                    }
                                </div>

                                <div className="child">
                                    <div className="icon" style={{ maskImage: `url(/images/icons/account.svg)` }}></div>
                                    {
                                        !userModalEditMode ? (
                                            <p>{ selectedUser?.accountType }</p>
                                        ) : (
                                            <select name="accountType"  defaultValue={selectedUser?.accountType}>
                                                <option value="STUDENT">Student</option>

                                                {
                                                    enumIsGreaterOrEquals(loggedUser?.accountType?.toString(), AccountType, AccountType.TEACHER) ? (
                                                        <option value="TEACHER">Učitel</option>
                                                    ) : null
                                                }

                                                {
                                                    enumIsGreaterOrEquals(loggedUser?.accountType?.toString(), AccountType, AccountType.ADMIN) ? (
                                                        <option value="ADMIN">Admin</option>
                                                    ) : null
                                                }

                                                {
                                                    enumIsGreaterOrEquals(loggedUser?.accountType?.toString(), AccountType, AccountType.ADMIN) ? (
                                                        <option value="SUPERADMIN">Superadmin</option>
                                                    ) : null
                                                }
                                            </select>
                                        )
                                    }
                                </div>
                            </div>

                            {
                                userModalCreationMode ? (
                                    <>
                                        <div className="separator" style={{ marginTop: 24}}></div>
                                        <div className="switch-div">
                                            <p>Odeslat přihlašovací údaje na email</p>

                                            <Switch slotProps={{ input: { role: 'switch', name: "sendToEmail" } }} defaultChecked={true} sx={{
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
                                userModalEditMode
                                    ? null
                                    : (
                                        <>
                                            <div className="separator"></div>

                                            <div className="buttons">
                                                <TextWithIcon text="Resetovat heslo" iconSrc="/images/icons/reset_password.svg" color="var(--error-color)" onClick={() => { setOpenedModal(Modals.RESETPASSWORD_CONFIRMATION) }} />
                                                <TextWithIcon text="Smazat uživatele" iconSrc="/images/icons/trash.svg" color="var(--error-color)" onClick={() => { setOpenedModal(Modals.DELETE_CONFIRMATION) }} />
                                            </div>
                                        </>
                                    )
                            }

                        </div>
                    </>
                ) : null}
            </Modal>

            <Modal enabled={openedModal === Modals.DELETE_CONFIRMATION || openedModal === Modals.RESETPASSWORD_CONFIRMATION} onClose={closeModal} className="confirmation-modal">
                {
                    openedModal === Modals.DELETE_CONFIRMATION ? (
                        <p>Opravdu chcete smazat uživatele <span>{selectedUser?.name}</span>?</p>
                    ): openedModal === Modals.RESETPASSWORD_CONFIRMATION ? (
                        <>
                            <p>Opravdu chcete resetovat heslo uživatele <span>{selectedUser?.name}</span>?</p>
                            {/* <p>Heslo uživateli přijde na email <span>{selectedUser?.email}</span>.</p> */}
                        </>
                    ) : null
                }

                <div className="buttons">
                    <ButtonPrimary text="Ano" onClick={() => {
                        switch (openedModal) {
                            case Modals.DELETE_CONFIRMATION: {
                                fetch(`/api/v1/adm/users/`, {
                                    method: "DELETE",
                                    headers: {
                                        "Content-Type": "application/json",
                                    },
                                    body: JSON.stringify({
                                        id: selectedUser?.id,
                                    }),
                                }).then(async res => {
                                    if (!res.ok) {
                                        console.error("Chyba při mazání uživatele");
                                        toast.error("Chyba při mazání uživatele.");
                                        return;
                                    }

                                    closeModal();
                                    fetchUsersFromApi().then();
                                    toast.success(`Uživatel ${selectedUser?.name} úspěšně smazán.`);
                                });
                            } break;

                            case Modals.RESETPASSWORD_CONFIRMATION: {
                                fetch(`/api/v1/adm/users/passwordreset`, {
                                    method: "POST",
                                    headers: {
                                        "Content-Type": "application/json",
                                    },
                                    body: JSON.stringify({
                                        id: selectedUser?.id,
                                    }),
                                }).then(async res => {
                                    const data = await res.json();

                                    if (!res.ok) {
                                        console.error("Chyba při resetování hesla");
                                        toast.error("Chyba při resetování hesla: " + data.message);
                                        return;
                                    }

                                    fetchUsersFromApi().then();
                                    setOpenedModal(Modals.USER);
                                    toast.success(`Heslo uživatele ${selectedUser?.name} úspěšně resetováno. Nové heslo bylo odesláno na email uživatele.`);
                                });
                            }
                        }
                    }} />
                    <ButtonSecondary text="Ne" onClick={() => setOpenedModal(Modals.USER) } />
                </div>
            </Modal>

            <div className="users-wrapper">
                <div className="inputs">
                    <p className="user-count">Uživatelé ({ filteredAndSortedUsers?.length ?? 0 })</p>

                    <input
                        type="text"
                        placeholder="Hledat uživatele..."
                        value={searchTerm}
                        onChange={handleSearchChange}
                    />

                    <p className="add-user" onClick={() => addUser()}>+ Přidat uživatele</p>
                </div>

                <table>
                    <thead>
                    <tr>
                        <th onClick={() => handleSort("name")}>Jméno a příjmení</th>
                        <th onClick={() => handleSort("email")}>Email</th>
                        <th onClick={() => handleSort("gender")}>Pohlaví</th>
                        <th onClick={() => handleSort("class")}>Třída</th>
                        <th onClick={() => handleSort("accountType")}>Typ účtu</th>
                        <th onClick={() => handleSort("lastUpdated")}>Naposledy upraven</th>
                        <th onClick={() => handleSort("lastLoggedIn")}>Naposledy přihlášen</th>
                    </tr>
                    </thead>

                    <tbody>
                    {filteredAndSortedUsers?.map((user) => (
                        <tr
                            key={user.id}
                            className={loggedUser?.id === user.id ? "loggeduser" : ""}
                            onClick={() => {
                                setSelectedUser(user);
                                setOpenedModal(Modals.USER);
                            }}
                        >
                            <td>
                                <div className="name">
                                    <Avatar size={"28px"} name={user.name} src={user.avatar} />
                                    <p>{user.name}</p>
                                </div>
                            </td>
                            <td>{user.email}</td>
                            <td >{translateGender(user.gender)}</td>
                            <td>{user.class}</td>
                            <td>{user.accountType}</td>
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
            <table>
                <thead>
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
                        <td className={ log.type.toString().toLowerCase() }>{ log.type }</td>
                        <td>{ log.exactType }</td>
                        <td>{ log.message }</td>
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
        <p style={{ opacity: 0.2 }}>sdhfiudsfusdfhudsfdish iuhf musi byt dokonceno nekdy</p>
    )
}





// main komponent
export const Administration = () => {
    const navigate = useNavigate();
    const { loggedUser } = useStore();
    const { userAuthed, setUserAuthed } = useStore();
    const [selectedTab, setSelectedTab] = useState<Tab>(Tab.USERS);


    // zamezení přístupu k administraci spatnym uzivatelum
    useEffect(() => {
        if (userAuthed && !loggedUser) {
            navigate("/app");
            return;
        }

        if (loggedUser && enumIsSmaller(loggedUser?.accountType, AccountType, AccountType.TEACHER)) {
            navigate("/app");
        }
    }, [userAuthed, loggedUser, navigate]);

    if (!userAuthed || !loggedUser) {
        return null;
    }



    return (
        <AppLayout>
            <h1>Administrace</h1>

            <div className="area-selector">
                <p onClick={() => setSelectedTab(Tab.USERS)} className={selectedTab === Tab.USERS ? "active" : ""}>Uživatelé</p>
                <p onClick={() => setSelectedTab(Tab.RESERVATIONS)} className={selectedTab === Tab.RESERVATIONS ? "active" : ""}>Rezervace</p>
                <p onClick={() => setSelectedTab(Tab.LOGS)} className={selectedTab === Tab.LOGS ? "active" : ""}>Bezpečnostní logy</p>
            </div>

            {
                selectedTab === Tab.USERS ? (
                    <UsersTab />
                ) : selectedTab === Tab.LOGS ? (
                    <LogsTab />
                ) : selectedTab === Tab.RESERVATIONS ? (
                    <ReservationsTab />
                ) : null
            }
        </AppLayout>
    );
};

export default Administration;
// endregion