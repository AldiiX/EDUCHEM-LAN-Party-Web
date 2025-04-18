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


const MESSAGE_COOLDOWN_IN_SECONDS = 1;

export const Chat = () => {
    const navigate = useNavigate();
    const { loggedUser } = useStore();
    const { userAuthed, setUserAuthed } = useStore();
    const [messages, setMessages] = useState<any[]>([]);
    const [socketLoading, setSocketLoading] = useState(true);
    const [moreMessagesLoading, setMoreMessagesLoading] = useState<boolean>(false);
    const [noMoreMessagesToFetch, setNoMoreMessagesToFetch] = useState<boolean>(false);
    const messagesRef = useRef<any[]>([]);
    const wsRef = useRef<WebSocket | null>(null);
    const [inputText, setInputText] = useState("");
    let lastRenderedDate = "";
    const firstMessageRender = useRef<boolean>(true);
    const lastSendTimeRef = useRef<number>(0);



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



    // pri nacteni komponenty
    useEffect(() => {
        const ws = new WebSocket(
            `${location.protocol === 'https:' ? 'wss' : 'ws'}://${window.location.host}/ws/chat`
        );

        wsRef.current = ws;

        // kdyz prijdou novy zpravy ze socketu
        ws.onmessage = (e) => {
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

                        if (wasNearBottom) {
                            scrollContainer.scrollTo({
                                top: scrollContainer.scrollHeight,
                                behavior: firstMessageRender.current ? "instant" : "smooth"
                            });
                        } else {
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
                    setSocketLoading(false);

                    setTimeout(() => {
                        firstMessageRender.current = false;
                    }, 100);
                }

                // ostatni veci
                setMoreMessagesLoading(false);
                setNoMoreMessagesToFetch(false);
            }

            else if (action === "noMoreMessagesToFetch") {
                setNoMoreMessagesToFetch(true);
                setMoreMessagesLoading(false);
            }
        };

        ws.onerror = () => toast.error("Chyba při připojení k chatu. Refreshněte stránku.");
        ws.onclose = () => {};

        const checkAndAddScrollListener = () => {
            const scrollContainer = document.querySelector("body #app .right");
            if (!scrollContainer) {
                setTimeout(checkAndAddScrollListener, 100); // retry za 100ms
            } else {
                scrollContainer.addEventListener("scroll", handleScroll);
                console.log(scrollContainer as HTMLElement);
            }
        };

        checkAndAddScrollListener();

        return () => {
            ws.close();

            const scrollContainer = document.querySelector("body #app .right");
            if (scrollContainer) {
                scrollContainer.removeEventListener("scroll", handleScroll);
            }
        };
    }, []);

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
    }, [userAuthed, loggedUser, navigate]);

    if (!userAuthed || !loggedUser) {
        return null;
    }



    return (
        <AppLayout>
            <h1>Chat</h1>
            <div className="chat-parent">
                {
                    !socketLoading ? (
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
                    ) : (
                        <div className="loading">
                            <div className="loader"></div>
                            <span>Načítání zpráv...</span>
                        </div>
                    )
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
                    />
                    <button className={"sent-message-button"} onClick={sendMessage}></button>
                </div>
            </div>
        </AppLayout>
    );
};

export default Chat;
