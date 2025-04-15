import React, { useRef, useState, useEffect } from 'react';
import styles from './MoveableMap.module.scss';

interface Translate {
    x: number;
    y: number;
}

interface MapProps {
    children?: React.ReactNode;
    displayControls?: boolean;
}

export const MoveableMap: React.FC<MapProps> = ({ children = null, displayControls = true }) => {
    const containerRef = useRef<HTMLDivElement>(null);
    const svgRef = useRef<SVGSVGElement>(null);

    const SVG_WIDTH = 1850;
    const SVG_HEIGHT = 1216;

    // scale – aktuální měřítko (zoom)
    const [scale, setScale] = useState<number>(1);
    // containerCenter – střed rodičovského divu
    const [containerCenter, setContainerCenter] = useState<Translate>({ x: 0, y: 0 });
    // dragOffset – posun udaný uživatelem
    const [dragOffset, setDragOffset] = useState<Translate>({ x: 0, y: 0 });

    const [isDragging, setIsDragging] = useState<boolean>(false);
    // startPos – počáteční offset (v souřadnicích, kde je posun relativní ke středu kontejneru)
    const [startPos, setStartPos] = useState<Translate>({ x: 0, y: 0 });

    const [isTouching, setIsTouching] = useState<boolean>(false);
    const [touchStartPos, setTouchStartPos] = useState<Translate>({ x: 0, y: 0 });
    const [initialDistance, setInitialDistance] = useState<number | null>(null);
    const [initialScale, setInitialScale] = useState<number>(1);

    // Přepočítá střed kontejneru při načtení a změně rozměrů
    useEffect(() => {
        const updateDimensions = () => {
            if (containerRef.current) {
                const { clientWidth, clientHeight } = containerRef.current;
                setContainerCenter({ x: clientWidth / 2, y: clientHeight / 2 });
            }
        };

        updateDimensions();
        window.addEventListener('resize', updateDimensions);
        return () => window.removeEventListener('resize', updateDimensions);
    }, []);

    // Při zoomu chceme zajistit, aby bod pod kurzorem zůstal fixní. Vzorec vychází z rovnice:
    // nová_dragOffset = dragOffset + (1 - newScale/scale) * (mousePosition - containerCenter)

    // registrace eventu na kolečko myši
    useEffect(() => {
        const svgEl = svgRef.current;
        if (!svgEl) return;

        const wheelHandler = (e: WheelEvent) => {
            e.preventDefault();
            const scaleFactor = 0.07;
            const newScale = e.deltaY < 0 ? Math.min(1.2, scale + scaleFactor) : Math.max(0.4, scale - scaleFactor);

            const rect = svgEl.getBoundingClientRect();
            const mouseX = e.clientX - rect.left;
            const mouseY = e.clientY - rect.top;

            setDragOffset(prev => ({
                x: prev.x + (1 - newScale / scale) * (mouseX - containerCenter.x),
                y: prev.y + (1 - newScale / scale) * (mouseY - containerCenter.y),
            }));

            setScale(newScale);
        };

        svgEl.addEventListener('wheel', wheelHandler, { passive: false });

        return () => {
            svgEl.removeEventListener('wheel', wheelHandler);
        };
    }, [scale, containerCenter]);

    const handleMouseDown = (e: React.MouseEvent<SVGSVGElement>) => {
        setIsDragging(true);
        if (containerRef.current) {
            const rect = containerRef.current.getBoundingClientRect();
            const mouseX = e.clientX - rect.left;
            const mouseY = e.clientY - rect.top;
            // Uložíme rozdíl mezi pozicí myši a aktuálním dragOffset
            setStartPos({
                x: mouseX - containerCenter.x - dragOffset.x,
                y: mouseY - containerCenter.y - dragOffset.y,
            });
            document.documentElement.classList.add('noselect');
        }
    };

    const handleMouseMove = (e: React.MouseEvent<SVGSVGElement>) => {
        if (!isDragging || !containerRef.current) return;
        const rect = containerRef.current.getBoundingClientRect();
        const mouseX = e.clientX - rect.left;
        const mouseY = e.clientY - rect.top;

        setDragOffset({
            x: mouseX - containerCenter.x - startPos.x,
            y: mouseY - containerCenter.y - startPos.y,
        });
    };

    const handleMouseUp = () => {
        setIsDragging(false);
        document.documentElement.classList.remove('noselect');
    };

    const getDistance = (touch1: Touch, touch2: Touch): number => {
        const dx = touch1.clientX - touch2.clientX;
        const dy = touch1.clientY - touch2.clientY;
        return Math.sqrt(dx * dx + dy * dy);
    };

    const handleTouchStart = (e: React.TouchEvent<SVGSVGElement>) => {
        if (e.touches.length === 1) {
            setIsTouching(true);
            if (containerRef.current) {
                const rect = containerRef.current.getBoundingClientRect();
                const touch = e.touches[0];
                const touchX = touch.clientX - rect.left;
                const touchY = touch.clientY - rect.top;
                setTouchStartPos({
                    x: touchX - containerCenter.x - dragOffset.x,
                    y: touchY - containerCenter.y - dragOffset.y,
                });
                document.documentElement.classList.add('noselect');
            }
        } else if (e.touches.length === 2) {
            const distance = getDistance(e.touches[0] as Touch, e.touches[1] as Touch);
            setInitialDistance(distance);
            setInitialScale(scale);
        }
    };

    const handleTouchMove = (e: React.TouchEvent<SVGSVGElement>) => {
        if (e.touches.length === 1 && isTouching && containerRef.current) {
            const rect = containerRef.current.getBoundingClientRect();
            const touch = e.touches[0];
            const touchX = touch.clientX - rect.left;
            const touchY = touch.clientY - rect.top;
            setDragOffset({
                x: touchX - containerCenter.x - touchStartPos.x,
                y: touchY - containerCenter.y - touchStartPos.y,
            });
        } else if (e.touches.length === 2 && initialDistance !== null) {
            const newDistance = getDistance(e.touches[0] as Touch, e.touches[1] as Touch);
            const scaleFactor = newDistance / initialDistance;
            const newScale = Math.min(1.5, Math.max(0.7, initialScale * scaleFactor));
            setScale(newScale);
        }
    };

    const handleTouchEnd = (e: React.TouchEvent<SVGSVGElement>) => {
        setIsTouching(false);
        setInitialDistance(null);
        document.documentElement.classList.remove('noselect');
    };

    return (
        <div
            ref={containerRef}
            style={{
                width: '100%',
                height: '100%',
                overflow: 'hidden',
                position: 'relative',
            }}
            className={styles.map}
        >
            {displayControls && (
                <div className={styles.zoomsettings}>
                    <div
                        className={styles.zoomIn}
                        onClick={() => {
                            const newScale = Math.min(1.2, scale + 0.1);
                            setScale(newScale);
                        }}
                    ></div>
                    <div
                        className={styles.zoomOut}
                        onClick={() => {
                            const newScale = Math.max(0.4, scale - 0.1);
                            setScale(newScale);
                        }}
                    ></div>
                </div>
            )}

            <svg
                ref={svgRef}
                width={SVG_WIDTH}
                height={SVG_HEIGHT}
                viewBox={`0 0 ${SVG_WIDTH} ${SVG_HEIGHT}`}
                onMouseDown={handleMouseDown}
                onMouseMove={handleMouseMove}
                onMouseUp={handleMouseUp}
                onMouseLeave={handleMouseUp}
                onTouchStart={handleTouchStart}
                onTouchMove={handleTouchMove}
                onTouchEnd={handleTouchEnd}
                style={{
                    cursor: isDragging ? 'grabbing' : 'grab',
                    touchAction: 'none',
                }}
            >
                <g
                    style={{
                        transform: `translate(${containerCenter.x + dragOffset.x}px, ${
                            containerCenter.y + dragOffset.y
                        }px) scale(${scale}) translate(-${SVG_WIDTH / 2}px, -${SVG_HEIGHT / 2}px)`,
                    }}
                >
                    {children ? (
                        children
                    ) : (
                        <>
                            <rect
                                x="0"
                                y="0"
                                width={SVG_WIDTH}
                                height={SVG_HEIGHT}
                                fill="#ddd"
                            />
                            <text
                                x="50%"
                                y="50%"
                                dominantBaseline="middle"
                                textAnchor="middle"
                                fontSize="48"
                                fill="#333"
                            >
                                Map Content
                            </text>
                        </>
                    )}
                </g>
            </svg>
        </div>
    );
};

export default MoveableMap;