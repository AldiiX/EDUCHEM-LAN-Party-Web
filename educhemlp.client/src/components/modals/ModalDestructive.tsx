import {Modal} from "./Modal.tsx";
import {Button} from "../buttons/Button.tsx";
import {ButtonStyle, ButtonType} from "../buttons/ButtonProps.ts";
import React from "react";
import "./ModalDestructive.scss";

interface ModalDestructiveActionProps {
    description: string;
    onClose: () => void;
    enabled: boolean;
    className?: string;
    yesAction: () => void;
    yesText?: string;
    noAction?: () => void;
    noText?: string;
    title?: string;
}

export const ModalDestructive = ({ title = "Potvrzení akce", description, enabled, onClose, className = "", yesAction, noAction = onClose, yesText = "Ano", noText = "Ne" }: ModalDestructiveActionProps) => {
    return (
        <Modal onClose={onClose} enabled={enabled} className={"modaldestructive-51bd1a61-b3c0-4e83-9c9d-4897316db398 " + className}>
            <div className="closebutton" onClick={onClose}></div>

            <div className="icon"></div>

            <h1>{ title }</h1>

            <p>{ description }</p>

            <div className="buttons">
                <Button type={ButtonType.TERTIARY_RICH} style={ButtonStyle.ROUNDER} text={noText} onClick={noAction}/>

                <Button type={ButtonType.PRIMARY} style={ButtonStyle.ROUNDER} text={yesText} onClick={yesAction}/>
            </div>
        </Modal>
    )
}