const { contextBridge, ipcRenderer } = require('electron');

contextBridge.exposeInMainWorld('electronAPI', {
    onFullscreenChanged: (callback) => ipcRenderer.on('fullscreen-changed', (_, isFullscreen) => callback(isFullscreen)),
    controlWindow: (action) => ipcRenderer.send('window-control', action)
});