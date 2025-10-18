import {AppLayout, AppLayoutLoggedUserSection, AppLayoutTitleBarType} from "./AppLayout.tsx";
import style from "./Forum.module.scss";
import {Avatar} from "../../components/Avatar.tsx";
import {useEffect, useState} from "react";
import {ForumThread} from "../../interfaces.ts";

const ChatTitleBar = () => {

    return (
        <div className={style.titleBar + " " + "titlebar"}>
            <div className={style.wrapper}>
                <h1>Forum</h1>
                
                {/*<div className={style.right}>*/}
                {/*    <div className={style.searchBar}>*/}
                {/*        <div className={style.icon}></div>*/}
                {/*        <input className={style.input + " " + "input"} type="text" placeholder="Hledat..." />*/}
                {/*    </div>*/}
                {/*</div>*/}
                <AppLayoutLoggedUserSection />
            </div>
        </div>
    );
}

const beforeDate = (dateString: string) => {
    const date = new Date(dateString);
    const now = new Date();
    const diff = now.getTime() - date.getTime();
    
    const seconds = Math.floor(diff / 1000);
    const minutes = Math.floor(seconds / 60);
    const hours = Math.floor(minutes / 60);
    const days = Math.floor(hours / 24);
    const weeks = Math.floor(days / 7);
    const months = Math.floor(days / 30);
    const years = Math.floor(days / 365);
    
    if (years > 0) {
        return `před ${years} ${years === 1 ? "rokem" : "lety"}`;
    }
    if (months > 0) {
        return `před ${months} ${months === 1 ? "měsícem" : "měsíci"}`;
    }
    if (weeks > 0) {
        return `před ${weeks} ${weeks === 1 ? "týdnem" : "týdny"}`;
    }
    if (days > 0) {
        return `před ${days} ${days === 1 ? "dnem" : "dny"}`;
    }
    if (hours > 0) {
        return `před ${hours} ${hours === 1 ? "hodinou" : "hodinami"}`;
    }
    if (minutes > 0) {
        return `před ${minutes} ${minutes === 1 ? "minutou" : "minutami"}`;
    }
    return `před ${seconds} ${seconds === 1 ? "sekundou" : "sekundami"}`;
}

export const Forum = () => {
    
    const [threads, setThreads] = useState<ForumThread[] | null >(null);

    useEffect(() => {
        fetch("/api/v1/forum/threads").then(async (response: Response) => {
            if (!response.ok) {
                return;
            }
            const data = await response.json();
            setThreads(data);
            
        })
    }, []);
    
    return (
        <AppLayout customTitleBar={<ChatTitleBar/>} titleBarType={AppLayoutTitleBarType.CUSTOM}>
                <>
                    <div className={style.mainContainer}>
                        <div className={style.searchContainer}> 
                            <div className={style.searchBar}>
                                <div className={style.left}>
                                    <div className={style.icon}></div>
                                    <input className={style.input + " " + "input"} type="text" placeholder="Hledat..." />
                                </div>
                                <div className={style.right}>
                                    <div className={style.newPostButton}>
                                        <div className={style.postIcon}></div>
                                        <div className={style.postText}>Nový příspěvek</div>
                                    </div>
                                </div>
                            </div>
                        </div>
                        
                        <div className={style.filterContainer}>
                            <div className={style.filterIcon}></div>
                            <h4 className={style.filterText}>Poslední příspěvky</h4>
                        </div>

                        <div className={style.forumContainer} >
                            {
                                threads ? threads.map((thread: ForumThread) => (
                                    <div key={thread.uuid} className={style.forumThread}>
                                        <div className={style.tagsContainer}>
                                            <div className={style.pin}>
                                                {
                                                    thread.isPinned ? (
                                                        <div className={style.isPinned}>
                                                            <div className={style.pinIcon}></div>
                                                        </div>
                                                    ) : null
                                                }
                                            </div>
                                        </div>
                                        <div className={style.top}>
                                            <div className={style.title}>
                                                <h1> {thread.title} </h1>
                                            </div>
                                            <div className={style.createDate}>
                                                {beforeDate(thread.createdAt)}
                                            </div>
                                        </div>
                                        <div className={style.text}>
                                            {thread.text}
                                        </div>
                                        <div className={style.bottom}>
                                            {/*<div className={style.reaction}> </div> MB v budoucnu TODO: dodelat emoji */}
                                            
                                        </div>
                                    </div>
                                )) : <div>Načítání...</div>
                            }
                        </div>
                    </div>
                    
                </>
        </AppLayout>
    )
}

export default  Forum;