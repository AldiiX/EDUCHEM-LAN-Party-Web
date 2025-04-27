import React from "react";
import "./Modal.scss";

interface ModalProps {
    onClose: () => void;
    children: React.ReactNode;
    enabled: boolean;
    className?: string;
    canBeClosedByClickingOutside?: boolean;
}

export const Modal: React.FC<ModalProps> = ({ children, onClose, enabled, className = "", canBeClosedByClickingOutside = true }: ModalProps) => {
    if (!enabled) return null; // pokud není modal povolený nebude renderovanej

    return (
        <div className={"modal" + (className ? " " + className : "")}>
            <div className="close-div" onClick={canBeClosedByClickingOutside ? onClose : () => {}}></div>

            <div className="modal-content">
                {children}
            </div>
        </div>
    );
};