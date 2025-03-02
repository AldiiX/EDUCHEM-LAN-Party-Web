import "./Header.scss";
import {useEffect} from "react";

export const Header = () => {

    useEffect(() => {
        function headerScroll() {
            const header = document.querySelector('header');
            if (header) {
                if (window.scrollY > 0) {
                    header.classList.add('scrolled');
                } else {
                    header.classList.remove('scrolled');
                }
            }
        }

        window.addEventListener('scroll', headerScroll);

        return () => {
            window.removeEventListener('scroll', headerScroll);
        }
    }, [])

    return (
        <header>
            <h1>Header</h1>
        </header>
    )
}