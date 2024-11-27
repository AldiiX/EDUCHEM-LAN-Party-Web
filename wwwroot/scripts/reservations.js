const module = await import(`/scripts/functions.js?v=${new Date().getTime()}`);
const { addAnnouncement, scrollToElement, addPrefetchLink, copyToClipboard } = module;
export const vue = new Vue({
    el: "#app",
    mounted: function () {
        this.main();
    },
    data: {
        currentPage: null,
        vueLoaded: true,
        temp: {
            ownSetup: false,
            room: null,
            actionLoading: false,
            webSocketError: false,
            webSocketErrorAttempts: 0,
        },
        static: {
            pcrooms: {
                vt3: {
                    label: "(tabule)",
                    pcs: [
                        [
                            ['25', ''], ['', '']
                        ],
                        [
                            ['04', '03'], ['02', '01']
                        ],
                        [
                            ['08', '07'], ['06', '05']
                        ],
                        [
                            ['12', '11'], ['10', '09']
                        ],
                        [
                            ['16', '15'], ['14', '13']
                        ],
                        [
                            ['20', '19'], ['18', '17']
                        ],
                        [
                            ['24', '23'], ['22', '21']
                        ],
                        [
                            ['29', '28'], ['27', '26']
                        ]
                    ],
                },
                vrr: {
                    label: "(okna)",
                    pcs: [
                        [
                            ['', '09']
                        ],
                        [
                            ['08', '01']
                        ],
                        [
                            ['07', '02']
                        ],
                        [
                            ['06', '03']
                        ],
                        [
                            ['05', '04']
                        ],
                    ],
                },
            },
        },
        pcs: [],
        rooms: [],
        announcements: [],
    },
    methods: {
        main: function () {
            const _this = this;
            _this.temp.actionLoading = true;
            _this.reloadDb();
            _this.connectToSSE();
            _this.temp.actionLoading = false;
            _this.temp.room = localStorage.getItem("room");
        },
        connectToSSE: function () {
            const _this = this;
            if (_this.temp.webSocketErrorAttempts >= 5)
                return;
            const eventSource = new EventSource("/api/sse/main");
            _this.temp.webSocketError = false;
            eventSource.onmessage = function (event) {
                const data = JSON.parse(event.data);
                if (data.clientAction === "refresh") {
                    _this.reloadDb();
                }
            };
            eventSource.onerror = function (event) {
                console.error("Nepodařilo se připojit k serverovým událostem!");
                _this.temp.webSocketError = true;
                _this.temp.webSocketErrorAttempts++;
                eventSource.close();
                setTimeout(() => {
                    _this.connectToSSE();
                    _this.reloadDb();
                    console.warn("Obnovuje se připojení k serverovým událostem...");
                }, 5000);
            };
        },
        reloadDb: function () {
            const _this = this;
            fetch("/api/computers/").then(response => response.json()).then(data => {
                _this.pcs = data;
            });
            fetch("/api/rooms/").then(response => response.json()).then(data => {
                _this.rooms = data;
            });
        },
        scrollToElement(elementId) {
            scrollToElement(elementId);
        },
        addAnnouncement: function (text, type = "info", timeout = 5000) {
            addAnnouncement(this, text, type, timeout);
        },
        copyToClipboard: function (text) {
            copyToClipboard(text);
        },
        setPCStyle: function (pcID) {
            const _this = this;
            const obj = {};
            const selectedPC = _this.getComputer(pcID);
            function checkLastTwoChars(str) {
                const lastTwoChars = str.slice(-2);
                const isNumber = !isNaN(lastTwoChars) && lastTwoChars.length === 2;
                return isNumber;
            }
            if (selectedPC == null && !checkLastTwoChars(pcID)) {
                obj.opacity = "0";
                obj.pointerEvents = "none";
            }
            else if (selectedPC == null) {
                obj['--bg'] = "#c2c2c2";
                obj['--modal-pointer-events'] = "all";
            }
            else if (selectedPC?.reservedBy == null) {
                obj['--bg'] = "#a5d6a7";
                obj['--modal-pointer-events'] = "all";
            }
            else if (selectedPC?.reservedByMe === true) {
                obj['--bg'] = "#80e1ff";
                obj['--modal-pointer-events'] = "all";
            }
            else {
                obj['--bg'] = "#ff8a80";
                obj['--modal-pointer-events'] = "none";
            }
            return obj;
        },
        setRoomStyle: function (room) {
            const _this = this;
            const obj = {};
            if (room == null) {
                obj['--bg'] = "#c2c2c2";
                obj['--modal-pointer-events'] = "all";
            }
            else if (room?.reservedByMe === true) {
                obj['--bg'] = "#80e1ff";
                obj['--modal-pointer-events'] = "all";
            }
            else if (room?.reservedBy?.length >= room?.limitOfSeats) {
                obj['--bg'] = "#ff8a80";
                obj['--modal-pointer-events'] = "all";
            }
            else {
                obj['--bg'] = "#a5d6a7";
                obj['--modal-pointer-events'] = "all";
            }
            return obj;
        },
        getComputer: function (pcID) {
            const _this = this;
            return _this.pcs.find((x) => x.id === pcID);
        },
        reserveComputer: function (pcID) {
            const _this = this;
            if (_this.temp.actionLoading)
                return;
            const pc = _this.getComputer(pcID);
            if (pc == null) {
                console.error("Počítač nebyl nalezen!");
                return;
            }
            if (pc.reservedBy != null) {
                console.error("Počítač je již rezervován!");
                return;
            }
            _this.temp.actionLoading = true;
            fetch(`/api/computers/reserve`, { method: "POST", body: `{ "id": "${pcID}" }`, headers: { "Content-Type": "application/json" } }).then(response => {
                if (response.ok) {
                    _this.addAnnouncement("Počítač byl úspěšně zarezervován!", "success");
                    console.log("Počítač byl úspěšně zarezervován!");
                    _this.reloadDb();
                }
                else {
                    console.error("Něco se nepovedlo!");
                }
                _this.temp.actionLoading = false;
            });
        },
        unreserveComputer: function (pcID) {
            const _this = this;
            if (_this.temp.actionLoading)
                return;
            const pc = _this.getComputer(pcID);
            if (pc == null) {
                console.error("Počítač nebyl nalezen!");
                return;
            }
            if (pc.reservedBy == null) {
                console.error("Počítač není rezervován!");
                return;
            }
            _this.temp.actionLoading = true;
            fetch(`/api/computers/reserve`, { method: "DELETE", body: `{ "id": "${pcID}" }`, headers: { "Content-Type": "application/json" } }).then(response => {
                if (response.ok) {
                    _this.addAnnouncement("Počítač byl úspěšně odrezervován!", "success");
                    console.log("Počítač byl úspěšně odrezervován!");
                    _this.reloadDb();
                }
                else {
                    console.error("Něco se nepovedlo!");
                }
                _this.temp.actionLoading = false;
            });
        },
        reserveRoom: function (roomID) {
            const _this = this;
            if (_this.temp.actionLoading)
                return;
            _this.temp.actionLoading = true;
            fetch(`/api/rooms/reserve`, { method: "POST", body: `{ "id": "${roomID}" }`, headers: { "Content-Type": "application/json" } }).then(response => {
                if (response.ok) {
                    _this.addAnnouncement("Počítač byl úspěšně zarezervován!", "success");
                    console.log("Počítač byl úspěšně zarezervován!");
                    _this.reloadDb();
                }
                else {
                    console.error("Něco se nepovedlo!");
                }
                _this.temp.actionLoading = false;
            });
        },
        unreserveRoom: function (roomID) {
            const _this = this;
            if (_this.temp.actionLoading)
                return;
            _this.temp.actionLoading = true;
            fetch(`/api/rooms/reserve`, { method: "DELETE", body: `{ "id": "${roomID}" }`, headers: { "Content-Type": "application/json" } }).then(response => {
                if (response.ok) {
                    _this.addAnnouncement("Počítač byl úspěšně odrezervován!", "success");
                    console.log("Počítač byl úspěšně odrezervován!");
                    _this.reloadDb();
                }
                else {
                    console.error("Něco se nepovedlo!");
                }
                _this.temp.actionLoading = false;
            });
        },
        saveRoomToLocalStorage: function () {
            const _this = this;
            localStorage.setItem('room', _this.temp.room);
        },
        saveToLocalStorage: function (prop, value) {
            const _this = this;
            localStorage.setItem(prop, value);
        },
        getRoomsMax: function () {
            let max = 0;
            const _this = this;
            for (const room of _this.rooms)
                max += room.limitOfSeats;
            return max;
        },
        getRoomsReserved: function () {
            let reserved = 0;
            const _this = this;
            for (const room of _this.rooms)
                reserved += room.reservedBy.length;
            return reserved;
        },
    },
    computed: {},
});
