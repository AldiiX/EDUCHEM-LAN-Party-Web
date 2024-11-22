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
    },





    methods: {
        main: function(): void {
            const _this = this as any;

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
    },

    computed: {
    },
})