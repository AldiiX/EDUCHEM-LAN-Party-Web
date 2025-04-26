import React, { useState } from "react";
import style from "./TabSelects.module.scss";

type TabSelectsProps = {
    defaultValue?: string | null;
    values: string[];
    onChange?: (value: string) => void;
    value: string
};

export const TabSelects: React.FC<TabSelectsProps> = ({ value, defaultValue, values, onChange }) => {

    const handleClick = (item: string) => {
        if (onChange) {
            onChange(item);
        }
    };

    return (
        <div className={style.parent}>
            {values.map((item, index) => (
                <p
                    key={index}
                    onClick={() => handleClick(item)}
                    className={value === item ? style.active : ""}
                >
                    {item}
                </p>
            ))}
        </div>
    );
};