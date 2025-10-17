import {AppLayout, AppLayoutLoggedUserSection, AppLayoutTitleBarType} from "./AppLayout.tsx";
import style from "./Forum.module.scss";
import {Avatar} from "../../components/Avatar.tsx";

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

export const Forum = () => {
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
                    </div>
                    <div className={style.forumContainer}>
                        <div className={style.tagsContainer}>
                            
                        </div>
                        <div className={style.title}>
                            
                        </div>
                        <div className={style.text}>
                             
                        </div>
                        <div className={style.bottom}>
                            {/*<div className={style.reaction}> </div> MB v budoucnu TODO: dodelat emoji */}
                            <div className={style.createDate}>
                                
                            </div>
                        </div>
                    </div>
                </>
        </AppLayout>
    )
}

export default  Forum;