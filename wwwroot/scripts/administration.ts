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
        },

        announcements: [],
        users: [],
    },





    methods: {
        main: function(): void {
            const _this = this as any;

            _this.fetchData();
        },

        fetchData: function(): void {
            const _this = this as any;

            fetch("/api/users/").then(response => response.json()).then(data => {
                _this.users = data;
            });
        },

        scrollToElement(elementId: string): void {
            scrollToElement(elementId);
        },

        addAnnouncement: function(text: any, type = "info", timeout = 5000) {
            addAnnouncement(this, text, type, timeout);
        },

        addPrefetchLink: function(url: string): void {
            addPrefetchLink(url);
        },

        copyToClipboard: function(text: string): void {
            copyToClipboard(text);
        },

        generateRandomKey: function(length = 48): string {
            const chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789';
            let key = '';
            for (let i = 0; i < length; i++) {
                const randomIndex = Math.floor(Math.random() * chars.length);
                key += chars[randomIndex];
            }

            return key;
        },

        sendKeyToEmail: function(userid: number): void {
            const _this = this as any;

            fetch(`/api/users/resetkey`, { method: "POST", body: `{ "id": "${userid}" }`,  headers: { "Content-Type": "application/json"}}).then(response => {
                if (response.ok) {
                    console.log("Key sent to email");
                    this.addAnnouncement("Key sent to email", "success");
                    _this.fetchData();
                } else {
                    console.error("Failed to send key to email");
                    this.addAnnouncement("Failed to send key to email", "error");
                }
            });
        },

        resetKey: function(userid: number): void {
            const _this = this as any;

            fetch(`/api/users/resetkey`, { method: "POST", body: `{ "id": "${userid}", "sendToEmail": false }`,  headers: { "Content-Type": "application/json"}}).then(async response => {
                if (response.ok) {
                    let data = await response.json();
                    console.log(data);
                    console.log(data.emailMessage);
                    //copyToClipboard(data.emailMessage);
                    this.addAnnouncement("Key reset", "success");
                    _this.fetchData();
                } else {
                    console.error("Failed to reset key");
                    this.addAnnouncement("Failed to reset key", "error");
                }
            });
        },
    },

    computed: {
    },
})