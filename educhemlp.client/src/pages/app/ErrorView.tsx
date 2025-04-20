import {Link} from "react-router-dom";
import "./ErrorView.scss";
import {AppLayout} from "./AppLayout.tsx";
import {ButtonPrimary} from "../../components/buttons/ButtonPrimary.tsx";
import {BasicErrorView} from "../../components/BasicErrorView.tsx";

export const ErrorView = () => {
    return (
        <AppLayout className="page-error">
            <BasicErrorView />
        </AppLayout>
    );
}