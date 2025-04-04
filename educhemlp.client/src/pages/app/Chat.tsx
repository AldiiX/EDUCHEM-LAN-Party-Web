import { AppLayout } from "./AppLayout.tsx";
import { useNavigate } from "react-router-dom";
import { useStore } from "../../store.tsx";
import {useEffect, useState} from "react";
import "./Chat.scss";
import {Avatar} from "../../components/Avatar.tsx";

export const Chat = () => {
    const navigate = useNavigate();
    const { loggedUser } = useStore();
    const { userAuthed, setUserAuthed } = useStore();
    const [messages, setMessages] = useState<any[]>([]);
    

    useEffect(() => {
        if (userAuthed && loggedUser === null) {
            navigate("/app");
        }
    }, [userAuthed, loggedUser, navigate]);


    useEffect(() => {
        const ws = new WebSocket(
            `${location.protocol === 'https:' ? 'wss' : 'ws'}://${window.location.host}/ws/chat`
        );

        ws.onopen = () => {
            //console.log("connected");
        };

        ws.onmessage = (e) => {
            const data = JSON.parse(e.data);
            //console.log(data);

            const messages = data.messages?.reverse();
            console.log(messages);

            setMessages((prevMessages) => [...prevMessages, ...messages ]);
            /*for (let message of data.messages ) {
                console.log(message);
                messages.push(message);
                /!*setMessages((prevMessages) => [...prevMessages, message]);*!/
            }*/
        };

        ws.onclose = () => {
            //console.log("disconnected");
        };
        
        return () => {
            ws.close();
        };
    }, []);




    if (!userAuthed || (userAuthed && !loggedUser)) {
        navigate("/app");
        return null;
    }

    return (
        <AppLayout>
            <h1>Chat</h1>
            <div className={"chat-parent"}>
                <div className={"messages"}>
                    {
                        messages.map((message, index) => (
                            <div key={index} className={`chat-message ${message.author.id === loggedUser.id ? "own-message" : "other-message"}`}>
                                {
                                    message.author.id !== loggedUser.id ? (
                                        <>
                                            <Avatar size={"32px"} src={message.author.avatar} name={message.author.name} />
                                            <div className="texts">
                                                <h1>{message.author.name}</h1>
                                                <article>{ message.message }</article>
                                            </div>
                                        </>
                                    ) : (
                                        <p>
                                            {message.message}
                                        </p>
                                    )
                                }
                            </div>
                        ))
                    }
                </div>
                <div className={"inputdiv"}>
                    <input type="text" placeholder="Napiš zprávu..."/>
                    <button className={"sent-message-button"} /*onClick={sendMessage}*/></button>
                </div>
            </div>
        </AppLayout>
    );
};

export default Chat;
