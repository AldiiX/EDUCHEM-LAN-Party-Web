﻿// @ts-ignore
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
        },

        pcs: [],
        announcements: [],
    },





    methods: {
        main: function(): void {
            const _this = this as any;

            fetch("/api/computers/").then(response => response.json()).then(data => {
                _this.pcs = data;
            });

            _this.temp.room = localStorage.getItem("room");
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

            if(selectedPC == null) {
                obj.backgroundColor = "#dcdcdc";
            } else if(selectedPC?.reservedBy == null) {
                obj.backgroundColor = "#a5d6a7";
            } else if(selectedPC?.reservedByMe === true) {
                obj.backgroundColor = "#80e1ff";
            } else {
                obj.backgroundColor = "#ff8a80";
            }

            return obj;
        },

        getComputer: function(pcID: string): {} {
            const _this = this as any;
            return _this.pcs.find((x: any) => x.id === pcID);
        },

        saveRoomToLocalStorage: function(): void {
            const _this = this as any;
            localStorage.setItem('room', _this.temp.room)
        },
    },

    computed: {
    },
})