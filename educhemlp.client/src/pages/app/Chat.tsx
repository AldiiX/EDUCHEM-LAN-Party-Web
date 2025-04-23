import {AppLayout, AppLayoutLoggedUserSection, AppLayoutTitleBarType} from "./AppLayout.tsx";
import {useNavigate} from "react-router-dom";
import {useStore} from "../../store.tsx";
import React, {useEffect, useRef, useState} from "react";
import "./Chat.scss";
import {Avatar} from "../../components/Avatar.tsx";
import {enumIsGreater, enumIsGreaterOrEquals} from "../../utils.ts";
import {AccountType} from "../../interfaces.ts";
import {toast} from "react-toastify";
import {create} from "zustand/index";
import {Button} from "../../components/buttons/Button.tsx";
import {ButtonType} from "../../components/buttons/ButtonProps.ts";
import MenuPopover from "../../components/MenuPopover.tsx";
import {TextWithIcon} from "../../components/TextWithIcon.tsx";


enum ChatSocketState {
    LOADING, CONNECTED, DISCONNECTED
}


// component store
interface ConnectedUser {
    id: number,
    name: string,
    avatar?: string | null,
}

interface ChatStore {
    connectedUsers: ConnectedUser[];
    setConnectedUsers: (users: ConnectedUser[]) => void;
}

const useChatStore = create<ChatStore>((set) => ({
    connectedUsers: [],
    setConnectedUsers: (users) => set(() => ({ connectedUsers: users })),
}));











const MESSAGE_COOLDOWN_IN_SECONDS = 1;

const ChatTitleBar = () => {
    const connectedUsers = useChatStore((state) => state.connectedUsers);

    return (
        <div className="titlebar">
            <div className="wrapper">
                <h1>Chat</h1>

                {
                    connectedUsers.length > 0 ? (
                        <div className="online-users">
                            <p>Online uživatelé</p>
                            <div className="users">
                                {
                                    connectedUsers.map((user, index) => {
                                        return (
                                            <div className="user" key={index} title={user.name}>
                                                <Avatar size={"24px"} src={user.avatar} name={user.name} />
                                                {/*<span>{user.name}</span>*/}
                                            </div>
                                        )
                                    })
                                }
                            </div>
                        </div>
                    ) : null
                }

                <AppLayoutLoggedUserSection />
            </div>
        </div>
    );
}

export const Chat = () => {
    const navigate = useNavigate();
    const { loggedUser } = useStore();
    const { userAuthed, setUserAuthed } = useStore();
    const [messages, setMessages] = useState<any[]>([]);
    const [socketState, setSocketState] = useState<ChatSocketState>(ChatSocketState.LOADING);
    const [moreMessagesLoading, setMoreMessagesLoading] = useState<boolean>(false);
    const [noMoreMessagesToFetch, setNoMoreMessagesToFetch] = useState<boolean>(false);
    const messagesRef = useRef<any[]>([]);
    const wsRef = useRef<WebSocket | null>(null);
    const [inputText, setInputText] = useState("");
    let lastRenderedDate = "";
    const firstMessageRender = useRef<boolean>(true);
    const lastSendTimeRef = useRef<number>(0);
    const setConnectedUsers = useChatStore((state) => state.setConnectedUsers);
    const [forceCloseMenuPopover, setForceCloseMenuPopover] = useState<boolean>(false);
    


    // datumy v cestine textem
    const formatDateToCzech = (dateString: string) => {
        const date = new Date(dateString);
        const months = ["led", "úno", "bře", "dub", "kvě", "čvn", "čvc", "srp", "zář", "říj", "lis", "pro"];

        const day = date.getDate();
        const month = months[date.getMonth()];
        const hours = date.getHours().toString().padStart(2, "0");
        const minutes = date.getMinutes().toString().padStart(2, "0");

        return `${day}. ${month} ${hours}:${minutes}`;
    };

    const handleScroll = () => {
        const scrollContainer = document.querySelector("body #app .right") as HTMLElement | null;
        if (!scrollContainer) return;

        //console.log(!firstMessageRender.current && scrollContainer.scrollTop === 0 && wsRef.current?.readyState === WebSocket.OPEN && messagesRef.current.length > 0)
        //console.log(!firstMessageRender.current , scrollContainer.scrollTop === 0 , wsRef.current?.readyState === WebSocket.OPEN , messagesRef.current.length > 0)
        //console.log(messagesRef.current);

        // loadovani starsich zprav pri scrollu
        if (!firstMessageRender.current && scrollContainer.scrollTop === 0 && wsRef.current?.readyState === WebSocket.OPEN && messagesRef.current.length > 0) {
            setMoreMessagesLoading(true);
            const oldestMessage = messagesRef.current[0];

            wsRef.current.send(JSON.stringify({
                action: "loadOlderMessages",
                beforeUuid: oldestMessage.uuid
            }));
        }
    };

    function accountTypeTranslate(accountType: string): string {
        switch (accountType) {
            case "STUDENT":
                return "Žák";
            case "TEACHER":
                return "Učitel";
            case "ADMIN":
                return "Admin";
            case "SUPERADMIN":
                return "Admin";
            default:
                return "";
        }
    }

    function connectToWebSocket() {
        const ws = new WebSocket(
            `${location.protocol === 'https:' ? 'wss' : 'ws'}://${window.location.host}/ws/chat`
        );

        wsRef.current = ws;

        ws.onopen = () => {
            setSocketState(ChatSocketState.CONNECTED);
        }

        ws.onmessage = (e) => { // kdyz prijdou novy zpravy ze socketu
            const data = JSON.parse(e.data);
            const action = data.action;
            
            if (action === "sendMessages") {
                const newMessages = data.messages?.reverse();
                if (!newMessages || newMessages.length === 0) return;

                const scrollContainer = document.querySelector("body #app .right") as HTMLElement | null;
                if (!scrollContainer) return;

                // zjisteni, jestli je uzivatel dole (aby se mu to např. nescrolovalo dolu kdyz si cte starsi zpravy)
                const wasNearBottom = scrollContainer.scrollHeight - scrollContainer.scrollTop - scrollContainer.clientHeight < 50;

                // ulozeni scroll vzdalenosti od spodu (bude vyuzito pro scrollovani na stejnou pozici)
                const prevScrollBottomOffset = scrollContainer.scrollHeight - scrollContainer.scrollTop;

                setMessages((prevMessages) => {
                    const merged = [...prevMessages, ...newMessages]; // nejdriv se novy zpravy mergnou s tema staryma
                    const sorted = merged.sort((a, b) => new Date(a.date).getTime() - new Date(b.date).getTime()); // pak se sortnou podle datumu (aby to nebylo random rozhazeny)
                    const unique = sorted.filter((msg, i, self) => i === self.findIndex(m => m.uuid === msg.uuid)); // pak se z toho odstrani duplicitni zpravy podle uuid (je mozne ze se to muze stat)

                    messagesRef.current = unique;

                    requestAnimationFrame(() => {
                        if (!scrollContainer) return;

                        // pokud je to prvni render, tak se scrollne na spodek
                        if(firstMessageRender.current) {
                            scrollContainer.scrollTo({
                                top: scrollContainer.scrollHeight,
                                behavior: "instant"
                            });
                        }

                        // pokud byl uzivatel blizko spodu, tak se scrollne dolů
                        else if (wasNearBottom) {
                            scrollContainer.scrollTo({
                                top: scrollContainer.scrollHeight,
                                behavior: "smooth"
                            });
                        }

                        // jinak se scrollne na stejnou pozici jako predtim
                        else {
                            if(data.isLoadMoreAction) scrollContainer.scrollTo({
                                top: scrollContainer.scrollHeight - prevScrollBottomOffset,
                                behavior: "instant"
                            });
                        }
                    });


                    return unique;
                });

                // pokud se jedna o prvni render, tak se firstMessageRender nastavi na false
                if (firstMessageRender.current) {
                    setSocketState(ChatSocketState.CONNECTED);

                    setTimeout(() => {
                        firstMessageRender.current = false;
                    }, 100);
                }

                // ostatni veci
                setMoreMessagesLoading(false);
                setNoMoreMessagesToFetch(false);
            }

            else if (action === "deleteMessage") {
                const uuid = data.uuid;
                if (!uuid) return;

                setMessages((prevMessages) => {
                    const updated = prevMessages.filter(msg => msg.uuid !== uuid);
                    messagesRef.current = updated;
                    return updated;
                });
            }

            else if (action === "noMoreMessagesToFetch") {
                setNoMoreMessagesToFetch(true);
                setMoreMessagesLoading(false);
            }

            else if (action === "updateConnectedUsers") {
                setConnectedUsers(data.users as ConnectedUser[]);
            }

            else if (action === "error") {
                const errorMessage = data.message;
                if (errorMessage) {
                    toast.error(errorMessage);
                }
            }
        };
        
        ws.onerror = () => toast.error("Chyba při připojení k chatu. Refreshněte stránku.");

        ws.onclose = () => {
            setSocketState(ChatSocketState.DISCONNECTED);
            setConnectedUsers([]);
        };
    }



    // pri nacteni komponenty
    useEffect(() => {
        const body = document.querySelector("body");
        if(body) body.style.overscrollBehavior = "none";


        const checkAndAddScrollListener = () => {
            const scrollContainer = document.querySelector("body #app .right");
            if (!scrollContainer) {
                setTimeout(checkAndAddScrollListener, 100); // retry za 100ms
            } else {
                scrollContainer.addEventListener("scroll", handleScroll);
            }
        };

        checkAndAddScrollListener();

        return () => {
            wsRef.current?.close();

            const scrollContainer = document.querySelector("body #app .right");
            if (scrollContainer) scrollContainer.removeEventListener("scroll", handleScroll);

            const body = document.querySelector("body");
            if(body) body.style.overscrollBehavior = "auto";
        };
    }, []);
    const handleDeleteMessage = (messageUuid: string) => {
        if (!wsRef.current || wsRef.current.readyState !== WebSocket.OPEN) return;

        wsRef.current.send(
            JSON.stringify({
                action: "deleteMessage",
                uuid: messageUuid,
            })
        );

        // zavreni vsech popoveru
        setForceCloseMenuPopover((prev) => !prev);
        requestAnimationFrame(() => {
            setForceCloseMenuPopover((prev) => !prev);
        });

        setMessages((prevMessages) =>
            prevMessages.map((message) =>
                message.uuid === messageUuid ? { ...message, deleted: 1 } : message
            )
        );
    };

    const sendMessage = () => {
        const now = Date.now();
        if (now - lastSendTimeRef.current < MESSAGE_COOLDOWN_IN_SECONDS * 1000) return;

        if (!inputText.trim() || !wsRef.current || wsRef.current.readyState !== WebSocket.OPEN) return;

        wsRef.current.send(JSON.stringify({
            action: "sendMessage",
            message: inputText
        }));

        lastSendTimeRef.current = now; // uložení času odeslání
        setInputText("");
    };




    // zamezení přístupu k administraci spatnym uzivatelum
    useEffect(() => {
        if (userAuthed && !loggedUser) {
            navigate("/app");
            return;
        }

        if(userAuthed && loggedUser) connectToWebSocket(); // pripojeni na socket az kdyz je uzivatel prihlasen
    }, [userAuthed, loggedUser, navigate]);

    if (!userAuthed || !loggedUser) {
        return null;
    }



    return (
        <AppLayout className="page-chat" customTitleBar={<ChatTitleBar />} titleBarType={AppLayoutTitleBarType.CUSTOM}>
            <div className="chat-parent">
                {
                    socketState === ChatSocketState.DISCONNECTED ? (
                        <div className="loading">
                            <span style={{ textAlign: "center" }}>Chat odpojen, obnov stránku (F5).</span>
                            <Button type={ButtonType.PRIMARY} onClick={() => {
                                // znovupripojeni na socket
                                setSocketState(ChatSocketState.LOADING);
                                connectToWebSocket();
                            }} text="Obnovit" />
                        </div>
                    ) : socketState === ChatSocketState.LOADING ? (
                        <div className="loading">
                            <div className="loader"></div>
                            <span>Načítání zpráv...</span>
                        </div>
                    ) : socketState === ChatSocketState.CONNECTED ? (
                        <div className="messages">
                            {
                                moreMessagesLoading ? (
                                    <div className="moremessages-loading"></div>
                                ) : null
                            }

                            {
                                noMoreMessagesToFetch ? (
                                    <p style={{ textAlign: "center", marginTop: 20, fontSize: 14 }}>Žádné další zprávy k zobrazení, tohle je začátek chatu.</p>
                                ) : null
                            }

                            {
                                messages.map((message, index) => {
                                    const isOwn = message.author.id === loggedUser.id;

                                    const dateObj = new Date(message.date);
                                    const dateOnly = dateObj.toLocaleDateString("cs-CZ");
                                    const time = dateObj.toLocaleTimeString("cs-CZ", {
                                        hour: '2-digit',
                                        minute: '2-digit',
                                        hour12: false
                                    });

                                    const showDateSeparator = dateOnly !== lastRenderedDate;
                                    if (showDateSeparator) lastRenderedDate = dateOnly;

                                    return (
                                        <React.Fragment key={index}>
                                            {showDateSeparator && (
                                                <div className="date-divider">
                                                    <span>{formatDateToCzech(message.date)}</span>
                                                </div>
                                            )}
                                            <div className={`wrapped-message ${isOwn ? "own-message" : "other-message"}`}>
                                                <div className={`chat-message ${isOwn ? "own-message" : "other-message"}`}>
                                                    {!isOwn ? (
                                                        <>
                                                            <Avatar size={"32px"} src={message.author.avatar} name={message.author.name} />
                                                            <div className="texts">
                                                                <div className="name-and-date">
                                                                    <h1>
                                                                        {message.author.name}
    
                                                                        { message.author.class && (
                                                                            <span className="class">
                                                                        &nbsp;• {message.author.class}
                                                                    </span>
                                                                        )}
    
                                                                        {
                                                                            enumIsGreater(message.author.accountType, AccountType, AccountType.STUDENT) && (
                                                                                <span className="role">&lt;{ accountTypeTranslate(message.author.accountType) }&gt;</span>
                                                                            )
                                                                        }
                                                                    </h1>
                                                                    <span className="msg-time">{time}</span>
                                                                </div>
                                                                <article>{ message.message }</article>
                                                            </div>
                                                        </>
                                                    ) : (
                                                        <div className="texts">
                                                            <p>{message.message}</p>
                                                            <span className="msg-time">{time}</span>
                                                        </div>
                                                    )}
                                                </div>
                                                <div className={"buttons"}>
                                                    <MenuPopover forceClose={forceCloseMenuPopover} className={isOwn ? "own-message" : ""} mainComponent={
                                                        <div style={{
                                                            width: 12,
                                                            height: 12,
                                                            mask: "url(/images/icons/more_vert.svg) no-repeat center",
                                                            maskSize: "contain",
                                                            backgroundColor: "var(--text-color-3)",
                                                            margin: 12,
                                                        }}></div>
                                                    } placement={isOwn ? "right" : "left"} innerStyle={{ display: "flex", flexDirection: "column" }}>
                                                            <TextWithIcon
                                                                text={"Kopírovat"}
                                                                onClick={() => {
                                                                    navigator.clipboard.writeText(message.message).then();
                                                                    toast.success("Zpráva zkopírována do schránky.", { autoClose: 1500 });
                                                                }}
                                                                iconSrc={"/images/icons/copy.svg"}
                                                                style={{ padding: "4px 0" }}
                                                            />

                                                            <TextWithIcon
                                                                text={"Odpovědět"}
                                                                iconSrc={"/images/icons/reply.svg"}
                                                                onClick={() => {}}
                                                                style={{ padding: "4px 0" }}
                                                            />

                                                            {
                                                               isOwn || enumIsGreaterOrEquals(loggedUser.accountType, AccountType, AccountType.TEACHER) ? (
                                                                    <TextWithIcon
                                                                        onClick={() => handleDeleteMessage(message.uuid)}
                                                                        color={"var(--error-color)"}
                                                                        text={"Smazat"}
                                                                        iconSrc={"/images/icons/trash.svg"}
                                                                        style={{ padding: "4px 0" }}
                                                                    />
                                                               ) : null
                                                            }
                                                    </MenuPopover>
                                                </div>
                                            </div>
                                        </React.Fragment>
                                    );
                                })
                            }
                        </div>
                    ) : null
                }
            </div>

            <div className="chat-input">
                <div className="inputdiv">
                    <input
                        type="text"
                        value={inputText}
                        onChange={(e) => setInputText(e.target.value)}
                        placeholder="Napiš zprávu..."
                        onKeyDown={(e) => e.key === "Enter" && sendMessage()}
                        maxLength={1024}
                    />
                    <button className={"sent-message-button"} onClick={sendMessage}></button>
                </div>
            </div>
        </AppLayout>
    );
};

export default Chat;
