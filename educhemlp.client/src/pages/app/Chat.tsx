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
    
    
    // Effect for permission check
    useEffect(() => {
        if (userAuthed && loggedUser === null) {
            navigate("/app");
        }
    }, [userAuthed, loggedUser, navigate]); // Add `loggedUser` to the dependencies array

    // Effect for WebSocket connection (always runs)
    useEffect(() => {
        const ws = new WebSocket(
            `${location.protocol === 'https:' ? 'wss' : 'ws'}://${window.location.host}/ws/chat`
        );

        ws.onopen = () => {
            console.log("connected");
        };

        ws.onmessage = (e) => {
            const data = JSON.parse(e.data);
            console.log(data);

            setMessages((prevMessages) => [...prevMessages, ...data.messages]);
            /*for (let message of data.messages ) {
                console.log(message);
                messages.push(message);
                /!*setMessages((prevMessages) => [...prevMessages, message]);*!/
            }*/
        };

        ws.onclose = () => {
            console.log("disconnected");
        };
        
        return () => {
            ws.close();
        };
    }, []); // This hook only runs once on mount and unmount
    
    // Ensure that the component only renders if the user is authenticated
    if (!userAuthed) return <></>;

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
                                                <Avatar size={"32px"} backgroundColor={"var(--accent-color)"} src={message.author.avatar} letter={message.author.name.split(" ")[0][0] + "" + message.author.name.split(" ")[1]?.[0]} />
                                                <div className="texts">
                                                    <h1>{message.author.name}</h1>
                                                    <article>{ message.message }</article>
                                                </div>
                                            </>
                                        ) :
                                        <p>
                                            {message.message}
                                        </p>
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
