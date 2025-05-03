import React, { useState, useRef, useEffect } from 'react';
import s from './MenuPopover.module.scss';

interface MenuPopoverProps {
    children: React.ReactNode;
    className?: string;
    mainComponent: React.ReactNode;
    placement?: 'left' | 'right';
    innerStyle?: React.CSSProperties;
    elementStyle?: React.CSSProperties;
    forceClose?: boolean;
}

const MenuPopover: React.FC<MenuPopoverProps> = ({
    children,
    className = '',
    mainComponent,
    placement = 'right',
    innerStyle = null,
    elementStyle = null,
    forceClose = false,
}) => {
    const [isOpen, setIsOpen] = useState(false);
    const [verticalPlacement, setVerticalPlacement] = useState<'top' | 'bottom'>('bottom');
    const buttonRef = useRef<HTMLButtonElement>(null);
    const menuRef = useRef<HTMLDivElement>(null);

    // zavření menu při kliknutí mimo komponentu
    const handleDocumentClick = (event: MouseEvent) => {
        if (
            menuRef.current &&
            !menuRef.current.contains(event.target as Node) &&
            buttonRef.current &&
            !buttonRef.current.contains(event.target as Node)
        ) {
            setIsOpen(false);
        }
    };

    useEffect(() => {
        document.addEventListener('click', handleDocumentClick);
        return () => {
            document.removeEventListener('click', handleDocumentClick);
        };
    }, []);

    const toggleMenu = () => {
        if (!isOpen && buttonRef.current) {
            const rect = buttonRef.current.getBoundingClientRect();
            const windowHeight = window.innerHeight;
            const isBottomHalf = rect.top > windowHeight / 2;
            setVerticalPlacement(isBottomHalf ? 'bottom' : 'top');
        }

        setIsOpen(prev => !prev);
    };

    // forceclose
    useEffect(() => {
        if (forceClose) {
            setIsOpen(false);
        }
    }, [forceClose]);




    // kontrola, jestli je menu ve viewportu (kdyz ne tak se zavre menu)
    useEffect(() => {
        const scrollContainer = document.querySelector('#app .right');

        const checkVisibility = () => {
            if (isOpen && menuRef.current) {
                const rect = menuRef.current.getBoundingClientRect();
                const outOfBounds =
                    rect.top < 0 ||
                    rect.left < 0 ||
                    rect.bottom > window.innerHeight ||
                    rect.right > window.innerWidth;

                if (outOfBounds) {
                    setIsOpen(false);
                }
            }
        };

        if (isOpen) {
            scrollContainer?.addEventListener('scroll', checkVisibility);
            window.addEventListener('scroll', checkVisibility);
            window.addEventListener('resize', checkVisibility);
            checkVisibility();
        }

        return () => {
            scrollContainer?.removeEventListener('scroll', checkVisibility);
            window.removeEventListener('scroll', checkVisibility);
            window.removeEventListener('resize', checkVisibility);
        };
    }, [isOpen]);



    return (
        <div className={`${s.menuPopover} ${className}`} style={{ ...elementStyle }}>
            <button
                ref={buttonRef}
                className="menu-button"
                style={{
                    background: 'none',
                    borderRadius: '0',
                    padding: '0',
                    border: 'none',
                    cursor: 'pointer',
                }}
                onClick={toggleMenu}
            >
                {mainComponent}
            </button>
            {isOpen && (
                <div
                    ref={menuRef}
                    className={s.menu}
                    style={{
                        [placement]: '0',
                        [verticalPlacement]: '100%',
                        ...(innerStyle ?? {}),
                    }}
                >
                    {children}
                </div>
            )}
        </div>
    );
};

export default MenuPopover;