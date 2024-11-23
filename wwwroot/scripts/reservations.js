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
        },
        pcs: [],
        announcements: [],
    },
    methods: {
        main: function () {
            const _this = this;
            fetch("/api/computers/").then(response => response.json()).then(data => {
                _this.pcs = data;
            });
            _this.temp.room = localStorage.getItem("room");
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
            if (selectedPC == null) {
                obj.backgroundColor = "#dcdcdc";
            }
            else if (selectedPC?.reservedBy == null) {
                obj.backgroundColor = "#a5d6a7";
            }
            else if (selectedPC?.reservedByMe === true) {
                obj.backgroundColor = "#80e1ff";
            }
            else {
                obj.backgroundColor = "#ff8a80";
            }
            return obj;
        },
        getComputer: function (pcID) {
            const _this = this;
            return _this.pcs.find((x) => x.id === pcID);
        },
        saveRoomToLocalStorage: function () {
            const _this = this;
            localStorage.setItem('room', _this.temp.room);
        },
    },
    computed: {},
});
