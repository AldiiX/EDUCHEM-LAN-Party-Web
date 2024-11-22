"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.copyToClipboard = exports.addPrefetchLink = exports.addAnnouncement = exports.scrollToElement = exports.generateUUID = exports.openModal = exports.setWebTheme = exports.removeCookie = exports.getCookie = exports.addCookie = exports.getRootDomain = exports.getCurrentSubdomain = exports.getCurrentFileName = exports.redirect = void 0;
function redirect(href, event) {
    var element = event.currentTarget;
    if (element)
        element.style.pointerEvents = 'none';
    window.location.href = href;
}
exports.redirect = redirect;
function getCurrentFileName() {
    var fullPath = window.location.pathname;
    return fullPath.split("/").pop();
}
exports.getCurrentFileName = getCurrentFileName;
function getCurrentSubdomain() {
    var currentHost = window.location.host;
    var parts = currentHost.split('.');
    if (parts.length <= 2) {
        return null;
    }
    if (parts[0] === 'localhost' || /^\d+$/.test(parts[0].replace(/\./g, ''))) {
        return null;
    }
    return parts[0];
}
exports.getCurrentSubdomain = getCurrentSubdomain;
function getRootDomain() {
    var hostname = window.location.hostname;
    var parts = hostname.split(".");
    // Pokud adresa obsahuje méně než dvě tečky, nebo je to IP adresa, není to pod doménou
    if (parts.length < 3 || parts.every(function (part) { return !isNaN(parseInt(part)); })) {
        return null;
    }
    return parts.slice(-2).join(".");
}
exports.getRootDomain = getRootDomain;
function addCookie(prop, value, expires) {
    if (expires === void 0) { expires = null; }
    var today = new Date();
    var expirationDate = new Date();
    if (expires !== null)
        expirationDate = expires;
    else
        expirationDate.setFullYear(today.getFullYear() + 1);
    document.cookie = "".concat(prop, "=").concat(value, "; path=/;").concat(getRootDomain() ? "domain=.".concat(getRootDomain(), ";") : '', "; expires=").concat(expirationDate.toString());
}
exports.addCookie = addCookie;
function getCookie(prop) {
    var cookies = document.cookie.split("; ");
    for (var i = 0; i < cookies.length; i++) {
        var cookie = cookies[i].split("=");
        if (cookie[0] === prop) {
            return cookie[1];
        }
    }
    return null;
}
exports.getCookie = getCookie;
function removeCookie(prop) {
    var cookies = document.cookie.split(";");
    cookies.forEach(function (cookie) {
        var cookieParts = cookie.split("=");
        if (cookieParts[0].trim() === prop) {
            var cookieName = cookieParts[0].trim();
            var cookieDomain = ".".concat(getRootDomain());
            document.cookie = "".concat(cookieName, "=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/;").concat(getRootDomain() ? 'domain=' + getRootDomain() + ';' : '');
        }
    });
}
exports.removeCookie = removeCookie;
function setWebTheme(theme) {
    addCookie('theme', theme);
}
exports.setWebTheme = setWebTheme;
function openModal(vue, modalId) {
    if (modalId === null) {
        vue.modalOpened = null;
        return;
    }
    vue.modalOpened = modalId;
}
exports.openModal = openModal;
function generateUUID() {
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'
        .replace(/[xy]/g, function (c) {
        var r = Math.random() * 16 | 0, v = c === 'x' ? r : (r & 0x3 | 0x8);
        return v.toString(16);
    });
}
exports.generateUUID = generateUUID;
function scrollToElement(elementId) {
    var element = document.getElementById(elementId);
    element.scrollIntoView({ behavior: "smooth" });
}
exports.scrollToElement = scrollToElement;
function addAnnouncement(vue, text, type, timeout) {
    /*
    *
    * Vyžaduje frontend ve vue, kde je nutné, aby vue.annoucements bylo []
    *
    * */
    if (type === void 0) { type = 'info'; }
    if (timeout === void 0) { timeout = 5000; }
    var announcement = { text: text, id: ("a" + generateUUID()) };
    var annParent = document.getElementById('announcements');
    if (annParent == null)
        return;
    vue.announcements.push(announcement);
    // vytvoření divu s anouncmentem
    var announcementDiv = document.createElement('div');
    announcementDiv.className = type;
    announcementDiv.id = announcement.id;
    announcementDiv.innerText = announcement.text;
    annParent.appendChild(announcementDiv);
    // odebrání anouncementu po 5s
    setTimeout(function () {
        document.querySelector("#announcements #".concat(announcement.id)).classList.add('fade-out');
        setTimeout(function () {
            annParent.removeChild(document.getElementById(announcement.id));
            vue.announcements.filter(function (ann) { return ann.id !== announcement.id; });
        }, 500);
    }, timeout);
}
exports.addAnnouncement = addAnnouncement;
function addPrefetchLink(url) {
    var link = document.createElement("link");
    link.rel = "prefetch";
    link.href = url;
    link.as = "script";
    link.classList.add("prefetch-link");
    document.head.appendChild(link);
}
exports.addPrefetchLink = addPrefetchLink;
function copyToClipboard(text) {
    // Create a temporary input element to copy the text
    var tempInput = document.createElement('input');
    tempInput.value = text;
    document.body.appendChild(tempInput);
    tempInput.select();
    tempInput.setSelectionRange(0, 99999); // For mobile devices
    // Copy the text inside the input field
    document.execCommand('copy');
    document.body.removeChild(tempInput);
}
exports.copyToClipboard = copyToClipboard;
window.addCookie = addCookie;
