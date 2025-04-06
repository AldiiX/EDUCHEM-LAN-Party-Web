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

    const [scale, setScale] = useState<number>(1);
    const [translate, setTranslate] = useState<Translate>({ x: 0, y: 0 });
    const [isDragging, setIsDragging] = useState<boolean>(false);
    const [startPos, setStartPos] = useState<Translate>({ x: 0, y: 0 });
    const [isTouching, setIsTouching] = useState<boolean>(false);
    const [touchStartPos, setTouchStartPos] = useState<Translate>({ x: 0, y: 0 });
    const [initialDistance, setInitialDistance] = useState<number | null>(null);
    const [initialScale, setInitialScale] = useState<number>(1);

    const getDistance = (touch1: Touch | any, touch2: Touch | any): number => {
        const dx = touch1.clientX - touch2.clientX;
        const dy = touch1.clientY - touch2.clientY;
        return Math.sqrt(dx * dx + dy * dy);
    };

    useEffect(() => {
        const updateDimensions = () => {
            if (containerRef.current) {
                const { clientWidth, clientHeight } = containerRef.current;
                const widthScale = clientWidth / SVG_WIDTH;
                const heightScale = clientHeight / SVG_HEIGHT;
                const initialScale = Math.min(widthScale, heightScale);
                setScale(initialScale);

                const contentHeight = SVG_HEIGHT * initialScale;
                const initialTranslateX = (clientWidth - SVG_WIDTH * initialScale) / 2;
                const initialTranslateY = (clientHeight - contentHeight) / 2;
                setTranslate({ x: initialTranslateX, y: initialTranslateY });
            }
        };

        updateDimensions();
        window.addEventListener('resize', updateDimensions);
        return () => window.removeEventListener('resize', updateDimensions);
    }, []);

    const handleWheel = (e: React.WheelEvent<SVGSVGElement>) => {
        //e.preventDefault();
        const scaleFactor = 0.07;
        const newScale =
            e.deltaY < 0 ? Math.min(1.2, scale + scaleFactor) : Math.max(0.4, scale - scaleFactor);

        if (svgRef.current) {
            const rect = svgRef.current.getBoundingClientRect();
            const mouseX = e.clientX - rect.left;
            const mouseY = e.clientY - rect.top;
            const deltaX = (mouseX - translate.x) * (newScale / scale - 1);
            const deltaY = (mouseY - translate.y) * (newScale / scale - 1);

            setTranslate({
                x: translate.x - deltaX,
                y: translate.y - deltaY,
            });
        }
        setScale(newScale);
    };

    const handleMouseDown = (e: React.MouseEvent<SVGSVGElement>) => {
        setIsDragging(true);
        if (svgRef.current) {
            const rect = svgRef.current.getBoundingClientRect();
            const mouseX = (e.clientX - rect.left) / scale;
            const mouseY = (e.clientY - rect.top) / scale;
            setStartPos({
                x: mouseX - translate.x,
                y: mouseY - translate.y,
            });
            document.documentElement.classList.add('noselect');
        }
    };

    const handleMouseMove = (e: React.MouseEvent<SVGSVGElement>) => {
        if (!isDragging || !svgRef.current) return;
        const rect = svgRef.current.getBoundingClientRect();
        const mouseX = (e.clientX - rect.left) / scale;
        const mouseY = (e.clientY - rect.top) / scale;
        setTranslate({
            x: mouseX - startPos.x,
            y: mouseY - startPos.y,
        });
    };

    const handleMouseUp = () => {
        setIsDragging(false);
        document.documentElement.classList.remove('noselect');
    };

    const handleTouchStart = (e: React.TouchEvent<SVGSVGElement>) => {
        if (e.touches.length === 1) {
            setIsTouching(true);
            if (svgRef.current) {
                const rect = svgRef.current.getBoundingClientRect();
                const touch = e.touches[0];
                const touchX = (touch.clientX - rect.left) / scale;
                const touchY = (touch.clientY - rect.top) / scale;
                setTouchStartPos({
                    x: touchX - translate.x,
                    y: touchY - translate.y,
                });
                document.documentElement.classList.add('noselect');
            }
        } else if (e.touches.length === 2) {
            const distance = getDistance(e.touches[0], e.touches[1]);
            setInitialDistance(distance);
            setInitialScale(scale);
        }
    };

    const handleTouchMove = (e: React.TouchEvent<SVGSVGElement>) => {
        // Pokud použijete styl touchAction: 'none', není třeba volat e.preventDefault()
        if (e.touches.length === 1 && isTouching && svgRef.current) {
            const rect = svgRef.current.getBoundingClientRect();
            const touch = e.touches[0];
            const touchX = (touch.clientX - rect.left) / scale;
            const touchY = (touch.clientY - rect.top) / scale;
            setTranslate({
                x: touchX - touchStartPos.x,
                y: touchY - touchStartPos.y,
            });
        } else if (e.touches.length === 2 && initialDistance !== null) {
            const newDistance = getDistance(e.touches[0], e.touches[1]);
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
                position: 'relative'
            }}
            className={styles.map}
        >
            {
                displayControls && (
                    <div className={styles.zoomsettings}>
                        <div className={styles.zoomIn} onClick={() => setScale(Math.min(1.2, scale + 0.1))}></div>
                        <div className={styles.zoomOut} onClick={() => setScale(Math.max(0.4, scale - 0.1))}></div>
                    </div>
                )
            }

            <svg
                ref={svgRef}
                width={SVG_WIDTH}
                height={SVG_HEIGHT}
                viewBox={`0 0 ${SVG_WIDTH} ${SVG_HEIGHT}`}
                onWheel={handleWheel}
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
                    transformOrigin: '0 0',
                }}>


                <g style={{ transform: `translate(${translate.x}px, ${translate.y}px) scale(${scale})`,transformOrigin: '0 0',  }}>
                    {
                        children ? (
                            children
                        ) : (
                            <>
                                <rect x="0" y="0" width={SVG_WIDTH} height={SVG_HEIGHT} fill="#ddd" />
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
                        )
                    }
                </g>
            </svg>
        </div>
    );
};

export default MoveableMap;