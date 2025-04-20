import * as React from 'react';
import IconButton from '@mui/joy/IconButton';
import Menu from '@mui/joy/Menu';
import MoreVert from '@mui/icons-material/MoreVert';
import MenuButton from '@mui/joy/MenuButton';
import Dropdown from '@mui/joy/Dropdown';

export default function MenuPopover({children}: {children: React.ReactNode}) {
    return (
        <Dropdown>
            <MenuButton
                slots={{ root: IconButton }}
                slotProps={{ root: { variant: 'outlined', color: 'neutral' } }}
            >
                <MoreVert />
            </MenuButton>
            <Menu placement="bottom-end">
                {children}
            </Menu>
        </Dropdown>
    );
}