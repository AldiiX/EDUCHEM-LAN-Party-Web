// @ts-ignore
const module = await import(`/scripts/functions.js?v=${new Date().getTime()}`);
const { addAnnouncement, scrollToElement, addPrefetchLink, copyToClipboard } = module;

// @ts-ignore
export const vue = new Vue({
    el: "#app",
    mounted: function(){
        this.main();
    },






    data: {
        currentPage: null,
        vueLoaded: true,
        temp: {
            ownSetup: false,
            room: null,
            actionLoading: false,
        },

        static: {
            // roomky s počítačema
            pcrooms: {
                vt3: {
                    label: "(tabule)",
                    pcs: [
                        [
                            ['25', ''], ['','']
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
                            ['','09']
                        ],
                        [
                            ['08','01']
                        ],
                        [
                            ['07','02']
                        ],
                        [
                            ['06','03']
                        ],
                        [
                            ['05','04']
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
        main: function(): void {
            const _this = this as any;

            _this.temp.actionLoading = true;
            _this.reloadDb();

            // fetchnutí db v loopu
            setInterval(() => { _this.reloadDb() }, 5 * 1000); // TODO: Udělat to jako SSE

            _this.temp.actionLoading = false;
            _this.temp.room = localStorage.getItem("room");
            //_this.temp.ownSetup = localStorage.getItem("ownSetup") ?? false;
        },

        reloadDb: function(): void {
            const _this = this as any;

            fetch("/api/computers/").then(response => response.json()).then(data => {
                _this.pcs = data;
            });

            fetch("/api/rooms/").then(response => response.json()).then(data => {
                _this.rooms = data;
            });
        },

        scrollToElement(elementId: string): void {
            scrollToElement(elementId);
        },

        addAnnouncement: function(text: any, type = "info", timeout = 5000) {
            addAnnouncement(this, text, type, timeout);
        },

        /*addPrefetchLink: function(url: string): void {
             addPrefetchLink(url);
        },*/

        copyToClipboard: function(text: string): void {
            copyToClipboard(text);
        },

        setPCStyle: function(pcID: string): {} {
            const _this = this as any;
            const obj: any = {};
            const selectedPC = _this.getComputer(pcID);
            function checkLastTwoChars(str: string): boolean {
                const lastTwoChars: any = str.slice(-2);
                const isNumber = !isNaN(lastTwoChars) && lastTwoChars.length === 2;

                return isNumber;
            }

            if(selectedPC == null && !checkLastTwoChars(pcID)) {
                obj.opacity = "0";
                obj.pointerEvents = "none";
            } else if(selectedPC == null) {
                obj['--bg'] = "#c2c2c2";
                obj['--modal-pointer-events'] = "all";
            } else if(selectedPC?.reservedBy == null) {
                obj['--bg'] = "#a5d6a7";
                obj['--modal-pointer-events'] = "all";
            } else if(selectedPC?.reservedByMe === true) {
                obj['--bg'] = "#80e1ff";
                obj['--modal-pointer-events'] = "all";
            } else {
                obj['--bg'] = "#ff8a80";
                obj['--modal-pointer-events'] = "none";
            }

            return obj;
        },

        setRoomStyle: function(room: any): {} {
            const _this = this as any;
            const obj: any = {};

            if(room == null) {
                obj['--bg'] = "#c2c2c2";
                obj['--modal-pointer-events'] = "all";
            } else if(room?.reservedByMe === true) {
                obj['--bg'] = "#80e1ff";
                obj['--modal-pointer-events'] = "all";
            } else if(room?.reservedBy?.length >= room?.limitOfSeats) {
                obj['--bg'] = "#ff8a80";
                obj['--modal-pointer-events'] = "all";
            } else {
                obj['--bg'] = "#a5d6a7";
                obj['--modal-pointer-events'] = "all";
            }

            return obj;
        },

        getComputer: function(pcID: string): {} {
            const _this = this as any;
            return _this.pcs.find((x: any) => x.id === pcID);
        },



        // metody pro rezervaci (nenapadl me lepsi nazev nez „unreserve”)
        reserveComputer: function(pcID: string): void {
            const _this = this as any;
            if(_this.temp.actionLoading) return;

            const pc = _this.getComputer(pcID);

            if(pc == null) {
                console.error("Počítač nebyl nalezen!");
                return;
            }

            if(pc.reservedBy != null) {
                console.error("Počítač je již rezervován!");
                return;
            }

            _this.temp.actionLoading = true;
            fetch(`/api/computers/reserve`, { method: "POST", body: `{ "id": "${pcID}" }`, headers: { "Content-Type": "application/json"} }).then(response => {
                if(response.ok) {
                    _this.addAnnouncement("Počítač byl úspěšně zarezervován!", "success");
                    console.log("Počítač byl úspěšně zarezervován!");

                    _this.reloadDb();
                } else {
                    console.error("Něco se nepovedlo!");
                }

                _this.temp.actionLoading = false;
            });
        },

        unreserveComputer: function(pcID: string): void {
            const _this = this as any;
            if(_this.temp.actionLoading) return;

            const pc = _this.getComputer(pcID);

            if(pc == null) {
                console.error("Počítač nebyl nalezen!");
                return;
            }

            if(pc.reservedBy == null) {
                console.error("Počítač není rezervován!");
                return;
            }

            _this.temp.actionLoading = true;
            fetch(`/api/computers/reserve`, { method: "DELETE", body: `{ "id": "${pcID}" }`, headers: { "Content-Type": "application/json"} }).then(response => {
                if(response.ok) {
                    _this.addAnnouncement("Počítač byl úspěšně odrezervován!", "success");
                    console.log("Počítač byl úspěšně odrezervován!");

                    _this.reloadDb();
                } else {
                    console.error("Něco se nepovedlo!");
                }

                _this.temp.actionLoading = false;
            });
        },

        reserveRoom: function(roomID: string): void {
            const _this = this as any;
            if(_this.temp.actionLoading) return;

            _this.temp.actionLoading = true;
            fetch(`/api/rooms/reserve`, { method: "POST", body: `{ "id": "${roomID}" }`, headers: { "Content-Type": "application/json"} }).then(response => {
                if(response.ok) {
                    _this.addAnnouncement("Počítač byl úspěšně zarezervován!", "success");
                    console.log("Počítač byl úspěšně zarezervován!");

                    _this.reloadDb();
                } else {
                    console.error("Něco se nepovedlo!");
                }

                _this.temp.actionLoading = false;
            });
        },

        unreserveRoom: function(roomID: string): void {
            const _this = this as any;
            if(_this.temp.actionLoading) return;

            _this.temp.actionLoading = true;
            fetch(`/api/rooms/reserve`, { method: "DELETE", body: `{ "id": "${roomID}" }`, headers: { "Content-Type": "application/json"} }).then(response => {
                if(response.ok) {
                    _this.addAnnouncement("Počítač byl úspěšně odrezervován!", "success");
                    console.log("Počítač byl úspěšně odrezervován!");

                    _this.reloadDb();
                } else {
                    console.error("Něco se nepovedlo!");
                }

                _this.temp.actionLoading = false;
            });
        },



        saveRoomToLocalStorage: function(): void {
            const _this = this as any;
            localStorage.setItem('room', _this.temp.room)
        },

        saveToLocalStorage: function(prop: string, value: any | null): void {
            const _this = this as any;
            localStorage.setItem(prop, value);
        },

        getRoomsMax: function (): number {
            let max = 0;
            const _this = this as any;

            for(const room of _this.rooms) max += room.limitOfSeats;

            return max;
        },

        getRoomsReserved: function (): number {
            let reserved = 0;
            const _this = this as any;

            for(const room of _this.rooms) reserved += room.reservedBy.length;

            return reserved;
        },
    },

    computed: {
    },
})