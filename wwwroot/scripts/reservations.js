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
    },
    methods: {
        main: function () {
            const _this = this;
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
    },
    computed: {},
});
