import { AppLayout } from "./AppLayout.tsx";
import { useNavigate } from "react-router-dom";
import { useStore } from "../../store.tsx";
import {useEffect, useState} from "react";
import "./Chat.scss";
import {Avatar} from "../../components/Avatar.tsx";
import {enumIsGreater} from "../../utils.ts";
import {AccountType} from "../../interfaces.ts";
import {toast} from "react-toastify";
import {create} from "zustand/index";
import {ButtonPrimary} from "../../components/buttons/ButtonPrimary.tsx";
import MenuPopover from "../../components/MenuPopover.tsx";

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
    const wsRef = useRef<WebSocket | null>(null);
    const [inputText, setInputText] = useState("");
    let lastRenderedDate = "";
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
    // websocket
    useEffect(() => {
        const ws = new WebSocket(
            `${location.protocol === 'https:' ? 'wss' : 'ws'}://${window.location.host}/ws/chat`
        );
        wsRef.current = ws;

        ws.onopen = () => {
            //console.log("connected");
        };
        
        ws.onmessage = (e) => {
            const data = JSON.parse(e.data);
            //console.log(data);

            const messages = data.messages?.reverse();
            //console.log(messages);

            setMessages((prevMessages) => [...prevMessages, ...messages ]);
            
            // scrollnuti na konec
            const messagesDiv = document.querySelector(".messages");
            const rightSection = document.querySelector(".right");

            setTimeout(() => {
                messagesDiv?.scrollTo({
                    top: messagesDiv.scrollHeight,
                    behavior: "smooth"
                });

                rightSection?.scrollTo({
                    top: rightSection.scrollHeight,
                    behavior: "smooth"
                });
            }, 1)
        };

        ws.onclose = () => {
            //console.log("disconnected");
        };

        return () => {
            ws.close();
        };
    }, []);
    
    const sendMessage = () => {
        if (!inputText.trim() || !wsRef.current || wsRef.current.readyState !== WebSocket.OPEN) return;

        wsRef.current.send(JSON.stringify({
            action: "sendMessage",
            message: inputText
        }));

        setInputText(""); // Vyčistit input po odeslání
    };
    


    // zamezení přístupu k administraci spatnym uzivatelum
    useEffect(() => {
        if (userAuthed && !loggedUser) {
            navigate("/app");
            return;
        }
    }, [userAuthed, loggedUser, navigate]);

    if (!userAuthed || !loggedUser) {
        return null;
    }



    return (
        <AppLayout>
            <h1>Chat</h1>
            <div className="chat-parent">
                <div className="messages">
                    
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

                                    <div className={`chat-message ${isOwn ? "own-message" : "other-message"}`}>
                                        {!isOwn ? (
                                            <>
                                                <Avatar size={"32px"} src={message.author.avatar} name={message.author.name} />
                                                <div className="texts">
                                                    <h1>{message.author.name}</h1>
                                                    <article>{ message.message }</article>
                                                    <span className="message-meta">{time}</span>
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
                                                    <MenuPopover>
                                                        <div className={"reactions"}>
                                                            
                                                        </div>
                                                        <div className={"reply"}>
                                                            
                                                        </div>
                                                        <div className={"more"}>
                                                            <div className={"copy"}>
                                                                
                                                            </div>
                                                            <div className={"delete"}>
                                                                
                                                            </div>
                                                        </div>
                                                    </MenuPopover>
                                                </div>
                                            </div>
                                        )}
                                    </div>
                                </React.Fragment>
                            );
                        })
                    }
                </div>
                <div className={"inputdiv"}>
                    <input
                        type="text"
                        value={inputText}
                        onChange={(e) => setInputText(e.target.value)}
                        placeholder="Napiš zprávu..."
                        onKeyDown={(e) => e.key === "Enter" && sendMessage()}
                    />
                    <button className={"sent-message-button"} onClick={sendMessage}></button>
                </div>
            </div>
        </AppLayout>
    );
};

export default Chat;
