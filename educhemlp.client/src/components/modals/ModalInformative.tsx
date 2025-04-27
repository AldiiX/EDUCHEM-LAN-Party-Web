import {Modal} from "./Modal.tsx";
import {Button} from "../buttons/Button.tsx";
import {ButtonStyle, ButtonType} from "../buttons/ButtonProps.ts";
import React from "react";
import "./ModalInformative.scss";

interface ModalInformativeActionProps {
    description: string;
    onClose: () => void;
    enabled: boolean;
    className?: string;
    okAction: () => void;
    okText?: string;
    title?: string | null;
    canBeClosedByClickingOutside?: boolean;
}

export const ModalInformative = ({ title = null, description, enabled, onClose, className = "", okAction, okText = "Rozumím", canBeClosedByClickingOutside = true }: ModalInformativeActionProps) => {
    return (
        <Modal onClose={onClose} enabled={enabled} className={"modalinformative-fe1613b6-531d-40c1-a2ba-52d0ed659dfd " + className} canBeClosedByClickingOutside={canBeClosedByClickingOutside}>
            <div className="closebutton" onClick={onClose}></div>

            <div className="icon"></div>

            {
                title && <h1>{ title }</h1>
            }

            <p>{ description }</p>

            <div className="buttons">
                <Button type={ButtonType.PRIMARY} style={ButtonStyle.ROUNDER} text={okText} onClick={okAction}/>
            </div>
        </Modal>
    )
}