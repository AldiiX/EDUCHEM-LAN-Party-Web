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
        temp: {},
        announcements: [],
        users: [],
    },
    methods: {
        main: function () {
            const _this = this;
            _this.fetchData();
        },
        fetchData: function () {
            const _this = this;
            fetch("/api/users/").then(response => response.json()).then(data => {
                _this.users = data;
            });
        },
        scrollToElement(elementId) {
            scrollToElement(elementId);
        },
        addAnnouncement: function (text, type = "info", timeout = 5000) {
            addAnnouncement(this, text, type, timeout);
        },
        addPrefetchLink: function (url) {
            addPrefetchLink(url);
        },
        copyToClipboard: function (text) {
            copyToClipboard(text);
        },
        generateRandomKey: function (length = 48) {
            const chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789';
            let key = '';
            for (let i = 0; i < length; i++) {
                const randomIndex = Math.floor(Math.random() * chars.length);
                key += chars[randomIndex];
            }
            return key;
        },
        sendKeyToEmail: function (userid) {
            const _this = this;
            fetch(`/api/users/resetkey`, { method: "POST", body: `{ "id": "${userid}" }`, headers: { "Content-Type": "application/json" } }).then(response => {
                if (response.ok) {
                    console.log("Key sent to email");
                    this.addAnnouncement("Key sent to email", "success");
                    _this.fetchData();
                }
                else {
                    console.error("Failed to send key to email");
                    this.addAnnouncement("Failed to send key to email", "error");
                }
            });
        },
        resetKey: function (userid) {
            const _this = this;
            fetch(`/api/users/resetkey`, { method: "POST", body: `{ "id": "${userid}", "sendToEmail": false }`, headers: { "Content-Type": "application/json" } }).then(async (response) => {
                if (response.ok) {
                    let data = await response.json();
                    console.log(data);
                    console.log(data.emailMessage);
                    this.addAnnouncement("Key reset", "success");
                    _this.fetchData();
                }
                else {
                    console.error("Failed to reset key");
                    this.addAnnouncement("Failed to reset key", "error");
                }
            });
        },
    },
    computed: {},
});
