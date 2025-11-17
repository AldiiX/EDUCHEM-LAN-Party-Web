import style from "./Switch.module.scss";
import MuiSwitch, { switchClasses } from "@mui/joy/Switch";
import React from "react";

interface AppSwitchProps {
    defaultChecked?: boolean;
    name?: string;
    slotProps?: any;
    sx?: any;
    [key: string]: any;
}

export default function Switch({
                                   defaultChecked = false,
                                   name,
                                   slotProps,
                                   sx,
                                   ...rest
                               }: AppSwitchProps) {
    const sp: any = slotProps ?? {};

    return (
        <MuiSwitch
            {...rest}
            slotProps={{
                ...sp,
                track: {
                    ...(sp.track ?? {}),
                    sx: {
                        transitionDuration: "0.3s",
                        transitionProperty: "background-color",
                        ...(sp.track?.sx ?? {}),
                    },
                },
                input: {
                    ...(sp.input ?? {}),
                    role: "switch",
                    name: name,
                },
                thumb: {
                    ...(sp.thumb ?? {}),
                    sx: {
                        transitionDuration: "0.3s",
                        transition:
                            "left 300ms cubic-bezier(.2,.8,.2,1), transform 180ms cubic-bezier(.2,.8,.2,1)",
                        transitionProperty: "left, transform, background-color",
                        ...(sp.thumb?.sx ?? {}),
                    },
                },
            }}
            defaultChecked={defaultChecked}
            sx={{
                "--Switch-thumbSize": "16px",
                "--Switch-trackWidth": "40px",
                "--Switch-trackHeight": "24px",
                "--Switch-thumbBackground": "var(--bg)",
                "--Switch-trackBackground": "var(--text-color-darker)",
                "transition-duration": "0.3s",

                "&:hover": {
                    "--Switch-trackBackground": "var(--text-color-3)",
                },

                [`&.${switchClasses.checked}`]: {
                    "--Switch-trackBackground": "var(--accent-color)",
                    "--Switch-thumbBackground": "var(--bg)",
                    "&:hover": {
                        "--Switch-trackBackground": "var(--accent-color-darker)",
                    },
                },

                ...(sx ?? {}),
            }}
        />
    );
}