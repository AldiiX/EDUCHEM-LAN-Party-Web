import * as React from 'react';
import IconButton from '@mui/joy/IconButton';
import Menu from '@mui/joy/Menu';
import MoreVert from '@mui/icons-material/MoreVert';
import MenuButton from '@mui/joy/MenuButton';
import Dropdown from '@mui/joy/Dropdown';

export default function MenuPopover({children, className=""}: {children: React.ReactNode, className?: string}) {
    
    return (
        <Dropdown>
            <MenuButton
                slots={{ root: IconButton }}
                slotProps={{ root: { variant: 'outlined', color: 'neutral' } }}
            >
                <MoreVert/>    
            </MenuButton>
            <Menu placement={ className.split(" ").includes("own-message") ? "left-start" : "right-start" }
                  sx={{
                      backgroundColor: 'var(--element-bg)', // příklad pro dark mode
                      boxShadow: '0 4px 16px rgba(0,0,0,0.2)',
                      padding: '12px',
                      border: '1px solid var(--text-color-3)',
                  }}>
                
                {children}
            </Menu>
        </Dropdown>
    );
}