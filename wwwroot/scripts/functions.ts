interface Vue {}

export function redirect(href: string, event: Event): void {
    let element: HTMLElement = event.currentTarget as HTMLElement;
    if(element) element.style.pointerEvents = 'none';

    window.location.href = href;
}

export function getCurrentFileName(): string|undefined {
    let fullPath = window.location.pathname;
    return fullPath.split("/").pop();
}

export function getCurrentSubdomain(): string|null {
    const currentHost = window.location.host;

    const parts = currentHost.split('.');

    if (parts.length <= 2) {
        return null;
    }

    if (parts[0] === 'localhost' || /^\d+$/.test(parts[0].replace(/\./g, ''))) {
        return null;
    }

    return parts[0];
}

export function getRootDomain(): string|null {
    const hostname = window.location.hostname;
    const parts = hostname.split(".");

    // Pokud adresa obsahuje méně než dvě tečky, nebo je to IP adresa, není to pod doménou
    if (parts.length < 3 || parts.every(part => !isNaN(parseInt(part)))) {
        return null;
    }

    return parts.slice(-2).join(".");
}

export function addCookie(prop: string, value: string|number, expires: Date|null = null): void {
    let today = new Date();
    let expirationDate = new Date();

    if(expires !== null) expirationDate = expires;
    else expirationDate.setFullYear(today.getFullYear() + 1);


    document.cookie = `${prop}=${value}; path=/;${getRootDomain() ? `domain=.${getRootDomain()};` : ''}; expires=${expirationDate.toString()}`;
}

export function getCookie(prop: string): string|null {
    const cookies = document.cookie.split("; ");
    for (let i = 0; i < cookies.length; i++) {
        const cookie = cookies[i].split("=");
        if (cookie[0] === prop) {
            return cookie[1];
        }
    }

    return null;
}

export function removeCookie(prop: string): void {
    const cookies = document.cookie.split(";");

    cookies.forEach((cookie) => {
        const cookieParts = cookie.split("=");

        if (cookieParts[0].trim() === prop) {
            const cookieName = cookieParts[0].trim();
            const cookieDomain = `.${getRootDomain()}`;
            document.cookie = `${cookieName}=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/;${getRootDomain() ? 'domain=' + getRootDomain() + ';' : ''}`;
        }
    });
}

export function setWebTheme(theme: string): void {
    addCookie('theme', theme);
}

export function openModal(vue: Vue|any, modalId: string): void {
    if(modalId === null) {
        vue.modalOpened = null;
        return;
    }

    vue.modalOpened = modalId;
}



export function generateUUID(): string {
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'
        .replace(/[xy]/g, function (c) {
            const r = Math.random() * 16 | 0,
                v = c === 'x' ? r : (r & 0x3 | 0x8);
            return v.toString(16);
        });
}

export function scrollToElement(elementId: string): void {
    const element: HTMLElement = document.getElementById(elementId) as HTMLElement;
    element.scrollIntoView({behavior: "smooth"});
}

export function addAnnouncement(vue: any, text: string, type = 'info', timeout = 5000) {
    /*
    *
    * Vyžaduje frontend ve vue, kde je nutné, aby vue.annoucements bylo []
    *
    * */

    const announcement = { text: text, id: ("a" + generateUUID()) };
    const annParent = document.getElementById('announcements');
    if(annParent == null) return;

    vue.announcements.push(announcement);

    // vytvoření divu s anouncmentem
    const announcementDiv = document.createElement('div');
    announcementDiv.className = type;
    announcementDiv.id = announcement.id;
    announcementDiv.innerText = announcement.text;
    annParent.appendChild(announcementDiv);

    // odebrání anouncementu po 5s
    setTimeout(() => {
        (document.querySelector(`#announcements #${announcement.id}`) as HTMLElement).classList.add('fade-out');

        setTimeout(() => {
            annParent.removeChild(document.getElementById(announcement.id) as HTMLElement);
            vue.announcements.filter((ann: any) => ann.id !== announcement.id);
        }, 500);
    }, timeout);
}

export function addPrefetchLink(url: string): void {
    const link = document.createElement("link");
    link.rel = "prefetch";
    link.href = url;
    link.as = "script";
    link.classList.add("prefetch-link");
    document.head.appendChild(link);
}

export function copyToClipboard(text: string): void {
    // Create a temporary input element to copy the text
    let tempInput = document.createElement('input');
    tempInput.value = text;
    document.body.appendChild(tempInput);
    tempInput.select();
    tempInput.setSelectionRange(0, 99999); // For mobile devices

    // Copy the text inside the input field
    document.execCommand('copy');
    document.body.removeChild(tempInput);
}


(window as any).addCookie = addCookie;