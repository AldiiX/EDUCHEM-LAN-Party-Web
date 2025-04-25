import React, { useState } from "react";
import style from "./TabSelects.module.scss";

type TabSelectsProps = {
    defaultValue?: string | null;
    values: string[];
    onChange?: (value: string) => void;
};

export const TabSelects: React.FC<TabSelectsProps> = ({ defaultValue, values, onChange }) => {
    const [selectedItem, setSelectedItem] = useState(defaultValue);

    const handleClick = (item: string) => {
        setSelectedItem(item);
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
                    className={selectedItem === item ? style.active : ""}
                >
                    {item}
                </p>
            ))}
        </div>
    );
};