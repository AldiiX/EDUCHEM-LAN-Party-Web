import {AppLayout, AppLayoutTitleBarType} from "./AppLayout.tsx";
import "./Account.scss";
import React, {CSSProperties, useEffect, useState} from "react";
import {useNavigate} from "react-router-dom";
import {useStore} from "../../store.tsx";
import {Avatar} from "../../components/Avatar.tsx";
import {logout, toggleWebTheme} from "../../utils.ts";
import {Button} from "../../components/buttons/Button.tsx";
import {ButtonType} from "../../components/buttons/ButtonProps.ts";
import {create, StoreApi, UseBoundStore} from "zustand";
import {TabSelects} from "../../components/TabSelects.tsx";
import {BasicAPIResponse, LoggedUser} from "../../interfaces.ts";
import {ModalDestructive} from "../../components/modals/ModalDestructive.tsx";
import {toast} from "react-toastify";


// store
const useAccountStore: UseBoundStore<StoreApi<any>> = create((set) => ({
    selectedTab: Tab.OVERVIEW,
    setSelectedTab: (tab: Tab) => set({ selectedTab: tab }),
}));











enum Tab {
    OVERVIEW = "Přehled",
    SETTINGS = "Nastavení"
}



const SettingsTab = () => {
    const loggedUser: LoggedUser = useStore((state) => state.loggedUser);
    const setLoggedUser = useStore((state) => state.setLoggedUser);

    const [modalEnabled, setModalEnabled] = useState(false);
    const [modalSelectedPlatform, setModalSelectedPlatform] = useState("");


    const items = [
        {
            id: "ig",
            name: "Instagram",
            icon: "/images/icons/instagram.svg",
        },

        {
            id: "discord",
            name: "Discord",
            icon: "/images/icons/discord.svg",
            authLink: "https://discord.com/oauth2/authorize?client_id=1365461378432893008&response_type=code&redirect_uri=http%3A%2F%2Flocalhost%3A3154%2F_be%2Fdiscord%2Foauth&scope=identify"
        },

        {
            id: "google",
            name: "Google",
            icon: "/images/icons/google.svg",
        }
    ]

    // nastaveni linku itemu
    const discord = items.find(l => l.id === "discord");
    if (discord) {
        if (window.location.hostname === "localhost") {
            discord.authLink = "https://discord.com/oauth2/authorize?client_id=1365461378432893008&response_type=code&redirect_uri=http%3A%2F%2Flocalhost%3A3154%2F_be%2Fdiscord%2Foauth&scope=identify";
        } else {
            discord.authLink = "https://discord.com/oauth2/authorize?client_id=1365461378432893008&response_type=code&redirect_uri=https%3A%2F%2Feduchemlan.emsio.cz%2F_be%2Fdiscord%2Foauth&scope=identify";
        }
    }

    function removePlatform(platform: string) {
        fetch(`/api/v1/loggeduser/connections/`, {
            method: "DELETE",
            headers: {
                "Content-Type": "application/json",
            },
            body: JSON.stringify({
                p: platform,
            }),
        }).then(async (res) => {
            if(!res.ok) {
                const data = await res.json();
                console.error("Chyba při odpojování platformy: ", data.message);
                toast.error(`Chyba při odpojování platformy ${platform}: ${data.message}`);
                return;
            }

            toast.success(`Úspěšně odpojena platforma ${platform}`);
            setModalEnabled(false);
            setModalSelectedPlatform("");

            // aktualizace uživatelského účtu
            const updatedUser: LoggedUser = await fetch("/api/v1/loggeduser").then(res => res.json());
            setLoggedUser(updatedUser);
        })
    }



    return (
        <>
            <ModalDestructive
                title={"Potvrzení odpojení"}
                description={`Opravdu chceš odpojit platformu ${modalSelectedPlatform}? Tvůj ${modalSelectedPlatform} účet se přestane synchronizovat s tímto účtem.`}
                onClose={() => setModalEnabled(false)}
                enabled={modalEnabled}
                yesAction={() => removePlatform(modalSelectedPlatform) }
            />

            <div className="settingstab-flex">
                <div className="left">
                    <p className="nadpis">Propojení</p>

                    <div className="items">
                        {
                            items.sort((a,b) => {
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
                                        <div className="button" onClick={() => {
                                            if(loggedUser.connections?.includes(item.id.toUpperCase())) {
                                                setModalSelectedPlatform(item.name);
                                                setModalEnabled(true);
                                                return;
                                            }

                                            if(item.authLink) location.href = item.authLink
                                        }}></div>
                                    </div>
                                </div>
                            ))
                        }
                    </div>
                </div>

                <div className="right">
                    <p>Editace profilu</p>
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
    const { loggedUser, setLoggedUser } = useStore();
    const { userAuthed, setUserAuthed } = useStore();
    const selectedTab: Tab = useAccountStore((state) => state.selectedTab);
    const setSelectedTab = useAccountStore((state) => state.setSelectedTab);


    // kontrola přihlášení
    useEffect(() => {
        // Ověření oprávnění
        if (userAuthed && !loggedUser) {
            navigate("/app");
        }
    }, [userAuthed, navigate]);


    if (!userAuthed || (userAuthed && !loggedUser)) {
        navigate("/app");
        return null;
    }

    return (
        <AppLayout className="page-account" titleBarText="Můj účet">
            <TabSelects values={[Tab.OVERVIEW.valueOf(), Tab.SETTINGS.valueOf()]} defaultValue={selectedTab.valueOf()} onChange={(newVal: string) => setSelectedTab(newVal) } />

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