import { AppLayout } from "./AppLayout.tsx";
import { useNavigate } from "react-router-dom";
import { useStore } from "../../store.tsx";
import {useEffect, useState} from "react";
import "./Chat.scss";
import {Avatar} from "../../components/Avatar.tsx";
import {useRef} from "react";
import React from "react";
import {enumIsGreater} from "../../utils.ts";
import {AccountType} from "../../interfaces.ts";
import {toast} from "react-toastify";



export const Chat = () => {
    const navigate = useNavigate();
    const { loggedUser } = useStore();
    const { userAuthed, setUserAuthed } = useStore();
    const [messages, setMessages] = useState<any[]>([]);
    const wsRef = useRef<WebSocket | null>(null);
    const [inputText, setInputText] = useState("");
    let lastRenderedDate = "";
    const messagesRef = useRef<HTMLDivElement | null>(null);
    const firstMessageRender = useRef<boolean>(true);



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
        const messagesDiv = messagesRef.current;
        if (!messagesDiv) return;

        // loadovani starsich zprav pri scrollu
        if (!firstMessageRender.current && messagesDiv.scrollTop === 0 && wsRef.current?.readyState === WebSocket.OPEN && messages.length > 0) {
            const oldestMessage = messages[0];
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
            const action = data.action;

            // kdyz prijdou novy zpravy ze socketu
            if(action === "sendMessages" || action === "loadOlderMessages") {
                const newMessages = data.messages?.reverse();
                if (!newMessages || newMessages.length === 0) return;

                const messagesDiv = messagesRef.current;

                // zjisteni, jestli je uzivatel dole (aby se mu to např. nescrolovalo dolu kdyz si cte starsi zpravy)
                const wasNearBottom = messagesDiv
                    ? messagesDiv.scrollHeight - messagesDiv.scrollTop - messagesDiv.clientHeight < 50
                    : false;

                // ulozeni scroll vzdalenosti od spodu (bude vyuzito pro scrollovani na stejnou pozici)
                const prevScrollBottomOffset = messagesDiv
                    ? messagesDiv.scrollHeight - messagesDiv.scrollTop
                    : 0;

                setMessages((prevMessages) => {
                    // nejdriv se novy zpravy mergnou s tema staryma
                    const mergedMessages = [...prevMessages, ...newMessages];

                    // pak se sortnou podle datumu (aby to nebylo random rozhazeny)
                    const sortedMessages = mergedMessages.sort((a, b) => {
                        const dateA = new Date(a.date).getTime();
                        const dateB = new Date(b.date).getTime();
                        return dateA - dateB;
                    });

                    // pak se z toho odstrani duplicitni zpravy podle uuid (je mozne ze se to muze stat)
                    const uniqueMessages = sortedMessages.filter((message, index, self) =>
                        index === self.findIndex((m) => m.uuid === message.uuid)
                    );

                    setTimeout(() => {
                        if (!messagesDiv) return;

                        // pokud uzivatel byl dole, scrollne se mu to dolu
                        if (wasNearBottom) {
                            messagesDiv.scrollTo({
                                top: messagesDiv.scrollHeight,
                                behavior: firstMessageRender.current ? "instant" : "smooth"
                            });
                        }

                        // pokud uzivatel neni dole tak to scrollne na stejnou pozici (aby to pri nacteni novych zprav nesjelo nahoru)
                        else {
                            // zachováme scroll pozici (uživatel není dole)
                            const newScrollTop = messagesDiv.scrollHeight - prevScrollBottomOffset;
                            messagesDiv.scrollTo({
                                top: newScrollTop,
                                behavior: "instant"
                            });
                        }
                    }, 1);

                    return uniqueMessages;
                });

                // pokud se jedna o prvni render, tak se firstMessageRender nastavi na false
                if (firstMessageRender.current) {
                    setTimeout(() => {
                        firstMessageRender.current = false;
                    }, 1000);
                }
            }

        };



        ws.onerror = (e) => {
            toast.error("Chyba při připojení k chatu. Refreshněte stránku.");
        }

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

        setInputText("");
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
                <div className="messages" ref={messagesRef} onScroll={handleScroll}>
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
