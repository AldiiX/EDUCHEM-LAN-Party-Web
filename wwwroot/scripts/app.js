"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.vue = void 0;
// @ts-ignore
var module = await Promise.resolve("".concat("/scripts/functions.js?v=".concat(new Date().getTime()))).then(function (s) { return require(s); });
var addAnnouncement = module.addAnnouncement, scrollToElement = module.scrollToElement, addPrefetchLink = module.addPrefetchLink, copyToClipboard = module.copyToClipboard;
// @ts-ignore
exports.vue = new Vue({
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
            var _this = this;
        },
        scrollToElement: function (elementId) {
            scrollToElement(elementId);
        },
        addAnnouncement: function (text, type, timeout) {
            if (type === void 0) { type = "info"; }
            if (timeout === void 0) { timeout = 5000; }
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
