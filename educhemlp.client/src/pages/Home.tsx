import {Header} from "../components/Header.tsx";
import {Link} from "react-router-dom";
import "./Home.scss";

const Home = () => {
    return (
        <>
            <div className="centerobj">
                <h1>EDUCHEM LAN Party</h1>

                <div className="buttons">
                    <a className="button-secondary" href="/info.pdf" target="_blank">Informace o akci</a>
                    <Link to="/app/reservations" className="button-primary">Rezervace</Link>
                </div>
            </div>
        </>

    )
}

export default Home;