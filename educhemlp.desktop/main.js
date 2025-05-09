const { app, BrowserWindow, ipcMain } = require('electron');
const path = require('path');

let win;

function createWindow() {
    win = new BrowserWindow({
        width: 1200,
        height: 800,
        frame: false,
        webPreferences: {
            preload: path.join(__dirname, 'preload.js'),
        },
        minWidth: 1000,
        minHeight: 600,
        icon: path.join(__dirname, 'assets', 'icon.png'),
    });

    if(process.env.NODE_ENV === "development") {
        win.loadURL('http://localhost:3154/app').then(r => {});
        win.webContents.openDevTools();
    }

    else win.loadURL('https://educhemlan.emsio.cz/app').then(r => {});




    win.on('enter-full-screen', () => {
        win.webContents.send('fullscreen-changed', true);
    });

    win.on('leave-full-screen', () => {
        win.webContents.send('fullscreen-changed', false);
    });
}

app.whenReady().then(() => {
    createWindow();

    app.on('activate', () => {
        if (BrowserWindow.getAllWindows().length === 0) createWindow();
    });
});

app.on('window-all-closed', () => {
    if (process.platform !== 'darwin') app.quit();
});

ipcMain.on('window-control', (event, action) => {
    if (!win) return;

    switch (action) {
        case 'minimize':
            win.minimize();
            break;
        case 'maximize':
            win.isMaximized() ? win.unmaximize() : win.maximize();
            break;
        case 'close':
            win.close();
            break;
    }
});