import { AppLayout } from "./AppLayout.tsx";
import {CSSProperties, useEffect, useState} from "react";
import { useStore } from "../../store.tsx";
import { useNavigate } from "react-router-dom";
import "./Administration.scss";
import {Avatar} from "../../components/Avatar.tsx";
import {Modal} from "../../components/modals/Modal.tsx";

export const Administration = () => {
    const navigate = useNavigate();
    const { loggedUser } = useStore();
    const { userAuthed, setUserAuthed } = useStore();
    const [selectedTab, setSelectedTab] = useState<string>("users");
    const [users, setUsers] = useState<any[] | null>(null);
    const [userModalShown, setUserModalShown] = useState(false);
    const [userModalEditMode, setUserModalEditMode] = useState(false);
    const [userModalCreationMode, setUserModalCreationMode] = useState(false);
    const [selectedUser, setSelectedUser] = useState<any | null>(null);
    const [searchTerm, setSearchTerm] = useState("");
    const [sortColumn, setSortColumn] = useState(null);
    const [sortDirection, setSortDirection] = useState("asc");

    useEffect(() => {
        // Ověření oprávnění
        if (userAuthed && loggedUser?.accountType !== "ADMIN" && loggedUser?.accountType !== "TEACHER") {
            navigate("/app");
        }

    }, [userAuthed, navigate]);

    useEffect(() => {
        switch (selectedTab) {
            case "users": {
                fetch("/api/v1/adm/users").then(async res => {
                    if (!res.ok) {
                        console.error("Chyba při načítání uživatelů");
                        return;
                    }

                    const data = await res.json();
                    setUsers(data);
                });
            } break;
        }
    }, [selectedTab]);

    if (!userAuthed) return <></>;


    function closeModal() {
        setUserModalShown(false);
        setUserModalEditMode(false);
        setUserModalCreationMode(false);
    }

    function translateGender(gender: string) {
        switch (gender) {
            case "MALE": return "Muž";
            case "FEMALE" : return "Žena";
            case "OTHER" : return "Ostatní";
            default: return "Neznámý";
        }
    }

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
            const aValue = a[sortColumn] || "";
            const bValue = b[sortColumn] || "";
            return (
                aValue.toString().localeCompare(bValue.toString(), "cs", { numeric: true }) *
                (sortDirection === "asc" ? 1 : -1)
            );
    });

    const addUser = () => {
        setUserModalShown(true);
        setUserModalEditMode(true);
        setUserModalCreationMode(true);

        setSelectedUser({});
    }




    return (
        <AppLayout>



            <Modal title={"test"} onClose={closeModal} enabled={userModalShown} className="user-modal">
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
                                    <input name="name"  defaultValue={selectedUser?.name} type={"text"}  required placeholder="Jméno" maxLength={30} />
                                )
                            }

                            {
                                !userModalEditMode ? (
                                    <div className="edit-delete-buttons-div">
                                        <button className="button-tertiary" style={{ flexGrow: 1 }} type="button" onClick={() => setUserModalEditMode(true)}>Upravit</button>
                                        <button className="button-tertiary" type="button">Smazat</button>
                                    </div>
                                ) : userModalCreationMode ? (
                                    <div className="edit-delete-buttons-div">
                                        <button className="button-tertiary" style={{ flexGrow: 1 }} type="button">Vytvořit uživatele</button>
                                        <button className="button-tertiary" type="button" onClick={() => {setUserModalEditMode(false); setUserModalShown(false) }}>Zrušit</button>
                                    </div>
                                ) : (
                                    <div className="edit-delete-buttons-div">
                                        <button className="button-tertiary" style={{ flexGrow: 1 }} type="button">Uložit změny</button>
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
                                                <option value="TEACHER">Učitel</option>
                                                <option value="ADMIN">Admin</option>
                                            </select>
                                        )
                                    }
                                </div>
                            </div>
                        </div>
                    </>
                ) : null}
            </Modal>



            <h1>Administrace</h1>

            <div className="area-selector">
                <p onClick={() => setSelectedTab("users")} className={selectedTab === "users" ? "active" : ""}>Uživatelé</p>
                <p onClick={() => setSelectedTab("reservations")} className={selectedTab === "reservations" ? "active" : ""}>Rezervace</p>
            </div>

            {
                selectedTab === "users" ? (
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
                                        className={loggedUser.id === user.id ? "loggeduser" : ""}
                                        onClick={() => {
                                            setSelectedUser(user);
                                            setUserModalShown(true);
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
                ) : null
            }
        </AppLayout>
    );
};

export default Administration;