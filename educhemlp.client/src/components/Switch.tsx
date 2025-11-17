import style from "./Switch.module.scss";
import {switchClasses} from '@mui/joy/Switch';
import MuiSwitch, { type SwitchTypeMap } from '@mui/joy/Switch';
import React from "react";

export default function Switch({
    defaultChecked = false as boolean,
    name = undefined as string | undefined,
}) {
    return <MuiSwitch slotProps={{ track: { sx: {transitionDuration: '0.3s', transitionProperty: 'background-color' } }, input: { role: 'switch', name: name }, thumb: { sx: { transitionDuration: '0.3s', transition: "left 300ms cubic-bezier(.2,.8,.2,1), transform 180ms cubic-bezier(.2,.8,.2,1)", transitionProperty: 'left, transform, background-color' }}}}
            defaultChecked={defaultChecked} sx={{
        '--Switch-thumbSize': '16px',
        '--Switch-trackWidth': '40px',
        '--Switch-trackHeight': '24px',
        '--Switch-thumbBackground': 'var(--bg)',
        '--Switch-trackBackground': 'var(--text-color-darker)',
        'transition-duration': '0.3s',

        '&:hover': {
            '--Switch-trackBackground': 'var(--text-color-3)',
        },

        [`&.${switchClasses.checked}`]: {
            '--Switch-trackBackground': 'var(--accent-color)',
            '--Switch-thumbBackground': 'var(--bg)',
            '&:hover': {
                '--Switch-trackBackground': 'var(--accent-color-darker)',
            },
        },
    }}
    />
}