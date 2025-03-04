import * as React from 'react';
import { useState, useEffect, useRef } from 'react';
import { Gauge, gaugeClasses } from '@mui/x-charts/Gauge';

export const PieChart = ({
                             value = 60,
                             width = 200,
                             height = 200,
                             fontSize = 24,
                         }: {
    value?: number;
    width?: number;
    height?: number;
    fontSize?: number;
}) => {
    const [displayValue, setDisplayValue] = useState(value);
    const previousValueRef = useRef(value);

    // Funkce pro ease in-out animaci
    const easeInOutQuad = (t: number): number => {
        return t < 0.5 ? 2 * t * t : -1 + (4 - 2 * t) * t;
    };

    useEffect(() => {
        const duration = 1000; // animace trvá 1 sekundu
        const startTime = performance.now();
        const startValue = previousValueRef.current;
        const change = value - startValue;

        const animate = (currentTime: number) => {
            const elapsed = currentTime - startTime;
            const progress = Math.min(elapsed / duration, 1);
            const easedProgress = easeInOutQuad(progress);
            setDisplayValue(startValue + change * easedProgress);
            if (progress < 1) {
                requestAnimationFrame(animate);
            } else {
                previousValueRef.current = value;
            }
        };

        requestAnimationFrame(animate);
    }, [value]);

    return (
        <Gauge
            width={width}
            height={height}
            value={displayValue}
            cornerRadius="50%"
            sx={() => ({
                [`& .${gaugeClasses.valueText}`]: {
                    display: 'none',
                    fontSize: fontSize,
                },
                [`& .${gaugeClasses.valueArc}`]: {
                    fill: 'var(--accent-color)',
                },
                [`& .${gaugeClasses.referenceArc}`]: {
                    fill: 'var(--text-color-3)',
                },
            })}
        />
    );
};
