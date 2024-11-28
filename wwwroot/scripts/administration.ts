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
    },

    computed: {
    },
})