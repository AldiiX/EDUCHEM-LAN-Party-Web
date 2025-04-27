import {AppLayout} from "./AppLayout.tsx";
import "./Account.scss";
import React, {CSSProperties, useEffect, useRef, useState} from "react";
import {useNavigate} from "react-router-dom";
import {useStore} from "../../store.tsx";
import {Avatar} from "../../components/Avatar.tsx";
import {logout, toggleWebTheme} from "../../utils.ts";
import {Button} from "../../components/buttons/Button.tsx";
import {ButtonType} from "../../components/buttons/ButtonProps.ts";
import {create} from "zustand";
import {TabSelects} from "../../components/TabSelects.tsx";
import {LoggedUser} from "../../interfaces.ts";
import {ModalDestructive} from "../../components/modals/ModalDestructive.tsx";
import {toast} from "react-toastify";
import {ModalInformative} from "../../components/modals/ModalInformative.tsx";


// store
interface AccountStore {
}

const useAccountStore = create<AccountStore>((set) => ({
}));











enum Tab {
    OVERVIEW = "Přehled",
    SETTINGS = "Nastavení"
}



const SettingsTab = () => {
    const loggedUser: LoggedUser = useStore((state) => state.loggedUser);
    const setLoggedUser = useStore((state) => state.setLoggedUser);

    // podminky pro zmenu hesla
    const pwdconddiv = useRef<HTMLParagraphElement>(null);
    const pwdcond1 = useRef<HTMLParagraphElement>(null);
    const pwdcond2 = useRef<HTMLParagraphElement>(null);
    const pwdcond3 = useRef<HTMLParagraphElement>(null);
    const pwdcond4 = useRef<HTMLParagraphElement>(null);
    const pwdcond5 = useRef<HTMLParagraphElement>(null);

    enum Modal {
        REMOVE_PLATFORM, REMOVE_AVATAR, CHANGE_AVATAR
    }

    const [modalOpened, setModalOpened] = useState<Modal | null>(null);
    const [modalSelectedPlatform, setModalSelectedPlatform] = useState("");

    const [userAvatar, setUserAvatar] = useState<string | null>(null);


    useEffect(() => {
        if (loggedUser) {
            setUserAvatar(loggedUser.avatar);
        }
    }, [loggedUser]);


    interface Platform {
        id: string;
        name: string;
        icon: string;
        authLink?: string | null;
    }

    const platforms: Platform[] = [
        {
            id: "ig",
            name: "Instagram",
            icon: "/images/icons/instagram.svg",
        },

        {
            id: "discord",
            name: "Discord",
            icon: "/images/icons/discord.svg",
        },

        {
            id: "google",
            name: "Google",
            icon: "/images/icons/google.svg",
        },

        {
            id: "github",
            name: "GitHub",
            icon: "/images/icons/github.svg",
        }
    ]

    // nastaveni linku itemu
    const discord = platforms.find(l => l.id === "discord");
    const google = platforms.find(l => l.id === "google");
    const github = platforms.find(l => l.id === "github");

    if (discord) {
        if (window.location.hostname === "localhost") {
            discord.authLink = "https://discord.com/oauth2/authorize?client_id=1365461378432893008&response_type=code&redirect_uri=http%3A%2F%2Flocalhost%3A3154%2F_be%2Fdiscord%2Foauth&scope=identify";
        } else {
            discord.authLink = "https://discord.com/oauth2/authorize?client_id=1365461378432893008&response_type=code&redirect_uri=https%3A%2F%2Feduchemlan.emsio.cz%2F_be%2Fdiscord%2Foauth&scope=identify";
        }
    }

    if(google) {
        if (window.location.hostname === "localhost") {
            google.authLink = "https://accounts.google.com/o/oauth2/v2/auth?client_id=772644450521-bf77npvasajiq98f16kf5gjjehi829go.apps.googleusercontent.com&redirect_uri=http%3A%2F%2Flocalhost%3A3154%2F_be%2Fgoogle%2Foauth&response_type=code&scope=openid%20email%20profile&access_type=offline&prompt=consent";
        } else {
            google.authLink = "https://accounts.google.com/o/oauth2/v2/auth?client_id=772644450521-bf77npvasajiq98f16kf5gjjehi829go.apps.googleusercontent.com&redirect_uri=https%3A%2F%2Feduchemlan.emsio.cz%2F_be%2Fgoogle%2Foauth&response_type=code&scope=openid%20email%20profile&access_type=offline&prompt=consent";
        }
    }

    if(github) {
        if (window.location.hostname === "localhost") {
            github.authLink = "https://github.com/login/oauth/authorize?client_id=Ov23lizqvCi4jOHJ0gZN&redirect_uri=http://localhost:3154/_be/github/oauth&scope=read:user%20user:email";
        } else {
            github.authLink = "https://github.com/login/oauth/authorize?client_id=Ov23liJJaDd7e0gn3drQ&redirect_uri=https://educhemlan.emsio.cz/_be/github/oauth&scope=read:user%20user:email";
        }
    }




    function removePlatformFromAccount(platform: string) {
        fetch(`/api/v1/loggeduser/connections/`, {
            method: "DELETE",
            headers: {
                "Content-Type": "application/json",
            },
            body: JSON.stringify({
                platform: platform,
            }),
        }).then(async (res) => {
            if(!res.ok) {
                const data = await res.json();
                console.error("Chyba při odpojování platformy: ", data.message);
                toast.error(`Chyba při odpojování platformy ${platform}: ${data.message}`);
                return;
            }

            toast.success(`Úspěšně odpojena platforma ${platform}`);
            setModalOpened(null);
            setModalSelectedPlatform("");

            // aktualizace uživatelského účtu
            const updatedUser: LoggedUser = await fetch("/api/v1/loggeduser").then(res => res.json());
            setLoggedUser(updatedUser);
        })
    }

    function handleClickPlatform(platform: Platform): string | null {
        if(loggedUser.connections?.includes(platform.id.toUpperCase())) {
            setModalSelectedPlatform(platform.name);
            setModalOpened(Modal.REMOVE_PLATFORM);
            return null;
        }

        if(platform.authLink) return platform.authLink
        return null;
    }

    function submitChangePasswordForm(e: React.FormEvent<HTMLFormElement>) {
        console.log("Submitting change password form");

        e.preventDefault();
        const form = e.currentTarget;
        const oldPassword = form.oldPassword.value;
        const newPassword = form.newPassword.value;
        const newPasswordConfirmation = form.newPasswordConfirmation.value;

        if (newPassword !== newPasswordConfirmation) {
            toast.error("Nová hesla se neshodují");
            return;
        }

        fetch("/api/v1/loggeduser/password", {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
            },
            body: JSON.stringify({
                oldPassword,
                newPassword,
            }),
        }).then(async (res) => {
            if(!res.ok) {
                const data = await res.json();
                console.error("Chyba při změně hesla: ", data.message);
                toast.error(`Chyba při změně hesla: ${data.message}`);
                return;
            }

            toast.success("Úspěšně změněno heslo");
            setModalOpened(null);
            form.reset();
        })
    }

    function checkPasswordConditions() {
        const form = document.getElementById("changepasswordform") as HTMLFormElement;
        const newPassword = form.newPassword.value;
        if(!newPassword) pwdconddiv.current!.style.display = "none";
        else pwdconddiv.current!.style.display = "block";

        if (newPassword.length >= 8) {
            pwdcond1.current!.classList.add("valid");
        } else {
            pwdcond1.current!.classList.remove("valid");
        }

        if (/[A-Z]/.test(newPassword)) {
            pwdcond2.current!.classList.add("valid");
        } else {
            pwdcond2.current!.classList.remove("valid");
        }

        if (/[a-z]/.test(newPassword)) {
            pwdcond5.current!.classList.add("valid");
        } else {
            pwdcond5.current!.classList.remove("valid");
        }

        if (/\d/.test(newPassword)) {
            pwdcond3.current!.classList.add("valid");
        } else {
            pwdcond3.current!.classList.remove("valid");
        }

        if (/[^a-zA-Z0-9]/.test(newPassword)) {
            pwdcond4.current!.classList.add("valid");
        } else {
            pwdcond4.current!.classList.remove("valid");
        }

        if(pwdcond1.current!.classList.contains("valid") && pwdcond2.current!.classList.contains("valid") && pwdcond3.current!.classList.contains("valid") && pwdcond4.current!.classList.contains("valid") && pwdcond5.current!.classList.contains("valid")) {
            form.submitButton?.removeAttribute("disabled");
        } else {
            form.submitButton?.setAttribute("disabled", "disabled");
        }
    }



    return (
        <>
            <ModalDestructive
                title={"Potvrzení odpojení"}
                description={`Opravdu chceš odpojit ${modalSelectedPlatform}? Tvůj ${modalSelectedPlatform} účet se přestane synchronizovat s tímto účtem.`}
                onClose={() => setModalOpened(null)}
                enabled={modalOpened === Modal.REMOVE_PLATFORM}
                yesAction={() => removePlatformFromAccount(modalSelectedPlatform) }
            />

            <ModalInformative
                title="Změna avataru"
                description={"Avatar si můžeš změnit pouze tak, že propojíš svůj účet s nějakou platformou (např. Google, Discord, Instagram...). Po propojení se avatar automaticky změní na ten, který máš na této platformě."}
                onClose={() => setModalOpened(null)}
                enabled={modalOpened === Modal.CHANGE_AVATAR}
                okAction={() => setModalOpened(null) }
            />

            <ModalDestructive
                description="Opravdu chceš smazat tvůj avatar? Pokud máš propojený účet s nějakou platformou, budeš muset platformy odpojit, jinak se ti avatar znovu synchronizuje."
                onClose={() => setModalOpened(null)}
                enabled={modalOpened === Modal.REMOVE_AVATAR}
                yesAction={() => { setUserAvatar(null); setModalOpened(null) } }
            />

            <form id="changepasswordform" onSubmit={submitChangePasswordForm}></form>
            <form id="editprofileform"></form>

            <div className="settingstab-flex">
                <div className="left">
                    <p className="nadpis">Propojení</p>

                    <div className="items">
                        {
                            platforms.sort((a,b) => {
                                if (a.name < b.name) {
                                    return -1;
                                } else if (a.name > b.name) {
                                    return 1;
                                } else {
                                    return 0;
                                }
                            }).sort((a,b) => {
                                if (loggedUser.connections?.includes(a.id.toUpperCase()) && !loggedUser.connections?.includes(b.id.toUpperCase())) {
                                    return -1;
                                } else if (!loggedUser.connections?.includes(a.id.toUpperCase()) && loggedUser.connections?.includes(b.id.toUpperCase())) {
                                    return 1;
                                } else {
                                    return 0;
                                }
                            }).map((item, index) => (
                                <div className={"item" + " " + (loggedUser.connections?.includes(item.id.toUpperCase()) ? "active" : "")} key={index}>
                                    <div className="content">
                                        <div className="icon" style={{ "--icon": `url(${item.icon})` } as CSSProperties }></div>
                                        <p>{item.name}</p>
                                        <div
                                            className="button"
                                            onClick={() => {
                                                const link = handleClickPlatform(item);
                                                if (link) {
                                                    window.location.href = link;
                                                }
                                            }}
                                        ></div>

                                    </div>
                                </div>
                            ))
                        }
                    </div>
                </div>

                <div className="middle">
                    <p className="nadpis">Změna hesla</p>

                    <div className="pair">
                        <p>Staré heslo</p>
                        <input type="password" name="oldPassword" form="changepasswordform" autoComplete="new-password" />
                    </div>

                    <div className="pair">
                        <p>Nové heslo</p>
                        <input type="password" name="newPassword" form="changepasswordform" autoComplete="old-password" onInput={checkPasswordConditions} />
                    </div>



                    <div className="pair">
                        <p>Nové heslo potvrzení</p>
                        <input type="password" name="newPasswordConfirmation" form="changepasswordform" autoComplete="old-password" />
                    </div>

                    <div className="pair pwdconditions" ref={pwdconddiv} style={{ display: "none"}}>
                        <p ref={pwdcond1}>Alespoň 8 znaků</p>
                        <p ref={pwdcond5}>Alespoň 1 malé písmeno</p>
                        <p ref={pwdcond2}>Alespoň 1 velké písmeno</p>
                        <p ref={pwdcond3}>Alespoň 1 číslo</p>
                        <p ref={pwdcond4}>Alespoň 1 speciální znak</p>
                    </div>

                    <div className="buttons">
                        <Button type={ButtonType.PRIMARY} text="Uložit změny" form="changepasswordform" buttonType="submit" name="submitButton" disabled />
                    </div>
                </div>

                <div className="right">
                    <div className="texts">
                        <p className="nadpis">Editace profilu</p>

                        <div className="pair">
                            <p>Jméno</p>
                            <input type="text" disabled value={loggedUser.displayName} name="displayName" form="editprofileform" />
                        </div>

                        <div className="pair">
                            <p>Email</p>
                            <input type="text" disabled value={loggedUser.email} name="email" form="editprofileform" />
                        </div>

                        <div className="pair">
                            <p>Třída</p>
                            <input type="text" disabled value={loggedUser.class ?? "Žádná"} name="class" form="editprofileform" />
                        </div>

                        <div className="pair">
                            <p>Pohlaví</p>
                            <select name="gender" defaultValue={loggedUser?.gender ?? "NULL"} form="editprofileform">
                                <option value="MALE">Muž</option>
                                <option value="FEMALE">Žena</option>
                                <option value="OTHER">Ostatní</option>
                                <option value="OTHER">Tank</option>
                                <option value="OTHER">Helikoptéra</option>
                                <option value="OTHER">Tatarka</option>
                                <option value="OTHER">Vesmírná loď</option>
                                <option value="OTHER">Drak</option>
                                <option value="OTHER">Čajová konvice</option>
                                <option value="OTHER">Kobliha</option>
                                <option value="NULL">Neurčeno</option>
                            </select>
                        </div>

                        <div className="buttons">
                            <Button type={ButtonType.SECONDARY} text="Zrušit změny" />
                            <Button type={ButtonType.PRIMARY} text="Uložit změny" form="editprofileform" />
                        </div>
                    </div>

                    <div className="avatar-edit">
                        <Avatar size={"248px"} src={userAvatar} name={loggedUser.displayName} />
                        <div className="buttons">
                            <div className="edit" style={{ '--m': 'url(/images/icons/edit.svg)'} as CSSProperties} title="Upravit" onClick={() => setModalOpened(Modal.CHANGE_AVATAR) }></div>
                            <div className="delete" style={{ '--m': 'url(/images/icons/trash.svg)'} as CSSProperties} title="Smazat" onClick={() => setModalOpened(Modal.REMOVE_AVATAR) }></div>
                        </div>
                    </div>
                </div>
            </div>
        </>
    )
}


const OverviewTab = () => {
    const loggedUser = useStore((state) => state.loggedUser);
    const setLoggedUser = useStore((state) => state.setLoggedUser);

    return (
        <div className="info">
            <Avatar size={"200px"} src={loggedUser.avatar} name={loggedUser.displayName} />
            <h1>{loggedUser.displayName}</h1>
            <p className="email">{loggedUser.email}</p>
            {
                loggedUser.accountType !== "STUDENT" ? (
                    <p className="type">{loggedUser.accountType}</p>
                ) : null
            }
            <div className="buttons">
                <Button type={ButtonType.SECONDARY} text="Změnit theme" icon="/images/icons/brush.svg" onClick={ () => toggleWebTheme() } />
                <Button type={ButtonType.PRIMARY} text="Odhlásit" icon="/images/icons/door.svg" onClick={() => logout(setLoggedUser) } />
            </div>
        </div>
    )
}












export const Account = () => {
    const navigate = useNavigate();
    const userAuthed = useStore((state) => state.userAuthed);
    const loggedUser = useStore((state) => state.loggedUser);
    const setLoggedUser = useStore((state) => state.setLoggedUser);
    const [selectedTab, setSelectedTab] = useState(Tab.OVERVIEW);


    // kontrola přihlášení
    useEffect(() => {
        // Ověření oprávnění
        if (userAuthed && !loggedUser) {
            navigate("/app");
        }
    }, [userAuthed, navigate, loggedUser]);

    /*useEffect(() => { // TODO: udělat
        // zjisteni query parametru, podle toho se nastavi tab
        const urlParams = new URLSearchParams(window.location.search);
        const tab = urlParams.get("tab")?.toUpperCase();
        if (tab) {
            setSelectedTab(tab as Tab);
        } else {
            setSelectedTab(Tab.OVERVIEW);
        }
    }, [])*/


    if (!userAuthed || (userAuthed && !loggedUser)) {
        return null;
    }

    return (
        <AppLayout className="page-account" titleBarText="Můj účet">
            <TabSelects values={[Tab.OVERVIEW.valueOf(), Tab.SETTINGS.valueOf()]} value={selectedTab} defaultValue={Tab.OVERVIEW} onChange={(newVal: string) => setSelectedTab(newVal as any) } />

            {
                selectedTab === Tab.OVERVIEW ? (
                    <OverviewTab />
                ) : selectedTab === Tab.SETTINGS ? (
                    <SettingsTab />
                ) : null
            }
        </AppLayout>
    )
}

export default Account;