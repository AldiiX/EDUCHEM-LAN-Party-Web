import React from "react";
import "./Modal.scss";

interface ModalProps {
    title: string;
    onClose: () => void;
    children: React.ReactNode;
    enabled: boolean;
    className?: string;
}

export const Modal = ({ children, title, onClose, enabled, className = "" }: ModalProps) => {
    if (!enabled) return null; // Pokud není modal povolený, nevykresluj ho

    return (
        <div className={"modal" + (className ? " " + className : "")}>
            <div className="close-div" onClick={onClose}></div>

            <div className="modal-content">
                {children}
            </div>
        </div>
    );
};
