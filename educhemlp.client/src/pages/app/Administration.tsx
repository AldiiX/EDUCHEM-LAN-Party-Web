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
    const [selectedUser, setSelectedUser] = useState<any | null>(null);

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
    }

    function translateGender(gender: string) {
        switch (gender) {
            case "MALE": return "Muž";
            case "FEMALE" : return "Žena";
            case "OTHER" : return "Ostatní";
            default: return "Neznámý";
        }
    }



    return (
        <AppLayout>



            <Modal title={"test"} onClose={closeModal} enabled={userModalShown} className="user-modal">
                { selectedUser !== null ? (
                    <>
                        <div className="top">
                            {
                                selectedUser.avatar ? (
                                    <div className="banner" style={{ '--bg': `url(${selectedUser.avatar})`} as CSSProperties}></div>
                                ) : null
                            }
                            <Avatar size={"200px"} src={selectedUser.avatar} letter={ selectedUser.name?.split(" ")[0][0] + "" + selectedUser.name?.split(" ")[1]?.[0] } backgroundColor={"var(--accent-color)"} />
                        </div>

                        <div className="bottom">
                            {
                                !userModalEditMode ? (
                                    <h1>{ selectedUser.name }</h1>
                                ) : (
                                    <input name="name"  defaultValue={selectedUser.name} type={"text"}  required placeholder="Jméno" maxLength={30} />
                                )
                            }

                            {
                                !userModalEditMode ? (
                                    <div className="edit-delete-buttons-div">
                                        <button className="button-tertiary" style={{ flexGrow: 1 }} type="button" onClick={() => setUserModalEditMode(true)}>Upravit</button>
                                        <button className="button-tertiary" type="button">Smazat</button>
                                    </div>
                                ) :(
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
                                            <p>{ selectedUser.email }</p>
                                        ) : (
                                            <input type="email" defaultValue={ selectedUser.email } name="email" placeholder="Email" />
                                        )
                                    }
                                </div>

                                <div className="child">
                                    <div className="icon" style={{ maskImage: `url(/images/icons/class.svg)` }}></div>
                                    {
                                        !userModalEditMode ? (
                                            <p>{ selectedUser.class ?? "Neznámá" }</p>
                                        ) : (
                                            <input type="text" defaultValue={ selectedUser.class } name="class" placeholder="Třída" />
                                        )
                                    }
                                </div>

                                <div className="child">
                                    <div className="icon" style={{ maskImage: `url(/images/icons/gender.svg)` }}></div>
                                    {
                                        !userModalEditMode ? (
                                            <p>{ translateGender(selectedUser.gender) }</p>
                                        ) : (
                                            <select name="gender" defaultValue={selectedUser.gender}>
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
                                            <p>{ selectedUser.accountType }</p>
                                        ) : (
                                            <select name="accountType"  defaultValue={selectedUser.accountType}>
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

                        </div>

                        <table>
                            <thead>
                            <tr>
                                <th>Jméno a příjmení</th>
                                <th>Email</th>
                                <th>Pohlaví</th>
                                <th>Třída</th>
                                <th>Typ účtu</th>
                                <th>Naposledy upraven</th>
                                <th>Naposledy přihlášen</th>
                            </tr>
                            </thead>

                            <tbody>
                                {users?.map(user => (
                                    <tr key={user.id} className={loggedUser.id === user.id ? "loggeduser" : ""} onClick={()=>{setSelectedUser(user); setUserModalShown(true)}}>
                                        <td>
                                            <div className="name">
                                                <Avatar size={"28px"} letter={user.name?.split(" ")[0][0] + "" + user.name?.split(" ")[1]?.[0]} backgroundColor={"var(--accent-color)"} src={user.avatar} />
                                                <p>{user.name}</p>
                                            </div>
                                        </td>
                                        <td>{user.email}</td>
                                        <td>{user.gender}</td>
                                        <td>{user.class}</td>
                                        <td>{user.accountType}</td>
                                        <td>{new Date(user.lastUpdated).toLocaleString() }</td>
                                        <td>{user.lastLoggedIn ? new Date(user.lastLoggedIn).toLocaleString() : null }</td>
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