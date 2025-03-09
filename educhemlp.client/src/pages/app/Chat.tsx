import { AppLayout } from "./AppLayout.tsx";
import { useNavigate } from "react-router-dom";
import { useStore } from "../../store.tsx";
import {useEffect, useState} from "react";
import "./Chat.scss";

export const Chat = () => {
    const navigate = useNavigate();
    const { loggedUser } = useStore();
    const { userAuthed, setUserAuthed } = useStore();
    const  [messages, setMessages] = useState<any[]>([]);
    

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
                {
                    messages.map((message, index) => (
                        <div key={index} className={`chat-message ${message.author.id === loggedUser.id ? "own-message" : "other-message"}`}>
                            {message.message} 
                        </div>
                    ))
                }
            </div>
        </AppLayout>
    );
};

export default Chat;
