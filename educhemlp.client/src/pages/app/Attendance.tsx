import {AppLayout} from "./AppLayout.tsx";
import "./Attendance.scss";

export const Attendance = () => {
    return (
        <AppLayout>
            <h1>Příchody / Odchody</h1>
            <p style={{ marginTop: 16, opacity: 0.25 }}>Bude implementováno ve verzi <a href={'https://github.com/AldiiX/EDUCHEM-LAN-Party-Web/milestone/2'} target='_blank'>3.5.0</a> (na další LAN Party)</p>
        </AppLayout>
    )
}

export default Attendance;