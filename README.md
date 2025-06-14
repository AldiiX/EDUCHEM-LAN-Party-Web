# EDUCHEM LAN Party App

Tento projekt poskytuje webovou platformu pro správu a organizaci LAN party událostí na EDUCHEM. Umožňuje registraci týmů, plánování turnajů a poskytuje aktuální informace o událostech.


## Obsah

- [Popis projektu](#popis-projektu)
- [Funkcionality](#funkcionality)
- [Technologie](#technologie)
- [Instalace a spuštění dev verze](#instalace-a-spuštění-dev-verze)
    - [1. Klonování repozitáře](#1-klonování-repozitáře)
    - [2. Instalace požadovaného softwaru](#2-instalace-požadovaného-softwaru)
    - [3. Spuštění containerizované (Docker) databáze](#3-spuštění-containerizované-docker-databáze)
    - [4. Vytvoření souboru `.env`](#4-vytvoření-souboru-env)
    - [5. Spuštění aplikace](#5-spuštění-aplikace)
    - [6. Přihlášení do admin panelu](#6-přihlášení-do-admin-panelu)
    - [7. Seznam všech adres](#7-seznam-všech-adres)
- [Screenshoty](#screenshoty)
- [Pravidla commitování](#pravidla-commitování-předpony)

## Popis projektu

Tento projekt představuje plnohodnotnou webovou aplikaci určenou pro správu a organizaci LAN party událostí. Uživatelé se mohou registrovat, rezervovat místa, komunikovat v chatu a sledovat průběh turnajů. Administrace je pak zjednodušena díky bohatým funkcím a intuitivnímu rozhraní.


## Funkcionality

- **Informace o LAN Party**: Prohlížení detailů a rozpisu nadcházejících událostí.
- **Rezervace míst**: Rezervace počítačů a stolů prostřednictvím interaktivní mapy.
- **Chat**: Reálný časový chat pro komunikaci účastníků.
- **Správa turnajů**: Organizace turnajů, registrace týmů a sledování výsledků.
- **Správa uživatelů**: Administrace uživatelských účtů a jejich oprávnění.
- **Další funkce**: Vylepšená podpora pro celkovou organizaci a účast na LAN party.


## Technologie

### Backend
![C#](https://img.shields.io/badge/c%23-%23239120.svg?style=for-the-badge&logo=csharp&logoColor=white) ![.NET](https://img.shields.io/badge/.NET-5C2D91?style=for-the-badge&logo=.net&logoColor=white)

### Frontend
![Vite](https://img.shields.io/badge/vite-%23646CFF.svg?style=for-the-badge&logo=vite&logoColor=white) ![React](https://img.shields.io/badge/react-%2320232a.svg?style=for-the-badge&logo=react&logoColor=%2361DAFB) ![React Router](https://img.shields.io/badge/React_Router-CA4245?style=for-the-badge&logo=react-router&logoColor=white) ![SASS](https://img.shields.io/badge/SASS-hotpink.svg?style=for-the-badge&logo=SASS&logoColor=white) ![TypeScript](https://img.shields.io/badge/typescript-%23007ACC.svg?style=for-the-badge&logo=typescript&logoColor=white)

### Databáze
![MySQL](https://img.shields.io/badge/mysql-4479A1.svg?style=for-the-badge&logo=mysql&logoColor=white) ![Redis](https://img.shields.io/badge/redis-%23DD0031.svg?style=for-the-badge&logo=redis&logoColor=white)


## Instalace a spuštění dev verze

### 1. Klonování repozitáře

```bash
git clone https://github.com/AldiiX/EDUCHEM-LAN-Party-Web.git
```

### 2. Instalace požadovaného softwaru
   - .NET SDK: Ujisti se, že máš nainstalované .NET SDK 9.0. [Stáhni .NET SDK](https://dotnet.microsoft.com/download) 
   - Node.js: [Stáhni Node.js](https://nodejs.org/)

### 3. Spuštění containerizované (Docker) databáze:
1. Nainstaluj [Docker](https://www.docker.com/products/docker-desktop) a spusť ho.
2. Otevři terminál a přejdi do adresáře, kde jsi klonoval repozitář.
3. Spusť následující příkaz pro stažení a spuštění MySQL a Redis kontejnerů:
    ```bash
    docker-compose up
    ```
4. Počkej, až se kontejnery plně spustí. Při prvním stažení to trvá tak 1-2 minuty.
5. Jakmile je vše připraveno, MySQL běží na `localhost:3306` a Redis na `localhost:6379`. Můžeš to ověřit pomocí příkazů:
    ```bash
    docker ps
    ```
   - Měl bys vidět běžící kontejnery pro MySQL a Redis.
6. Pro správu databáze můžeš použít PhpMyAdmin, který je dostupný na `http://localhost:8080`.
7. Pokud chceš vypnout kontejnery, použij:
    ```bash
    docker-compose down
    ```

### 4. Vytvoření souboru .env
- V adresáři /EduchemLP.Server/ vytvoř soubor .env a vlož do něj následující obsah:
    ```dotenv
    # lokální containerizovaná databáze; pokud máš vlastní MySQL/redis server, uprav tyto hodnoty na své
    DATABASE_IP=localhost
    DATABASE_DBNAME=educhem_lan_party_dev
    DATABASE_USERNAME=root
    DATABASE_PASSWORD=root
    REDIS_PORT=6379
    REDIS_PASSWORD=
    
    # pokud chceš posílat emaily, nakonfiguruj svůj SMTP server
    SMTP_HOST=???
    SMTP_PORT=???
    SMTP_EMAIL_USERNAME=???
    SMTP_EMAIL_PASSWORD=???
    ```
### 5. Spuštění aplikace
- Otevři terminál a přejdi do adresáře /EduchemLP.Server/. Poté spusť následující příkazy:
    ```bash
    dotnet restore
    dotnet build
    dotnet run
    ```
- Backend se spustí a zároveň se otevře frontend prostřednictvím Vite. Aplikace poběží na http://localhost:3154.
### 6. Přihlášení do admin panelu
- Navštiv http://localhost:3154/login a přihlas se pomocí těchto údajů:
    ```
    Email: admin@admin.admin
    Heslo: admin
    ```
- Admin panel najdeš zde: http://localhost:3154/app/administration, kde můžeš spravovat uživatele a další administrativní nastavení.
### 7. Seznam všech adres
- Web: http://localhost:3154
- Admin panel: http://localhost:3154/app/administration
- PhpMyAdmin: http://localhost:8080
- Redis: http://localhost:6379
- MySQL: http://localhost:3306

## Screenshoty
![img1](https://stanislavskudrna.cz/images/websites/educhemlp/1.png)

## Pravidla commitování (předpony)
- `FEAT` – přidána nová funkce
- `FIX` – oprava chyby
- `CHORE` – změny nesouvisející s opravou nebo funkcí, které nemodifikují src nebo test soubory (např. aktualizace závislostí)
- `REFACTOR` – refaktorizace kódu, která neopravuje chybu ani nepřidává funkci
- `DOCS` – aktualizace dokumentace, jako je README nebo jiné markdown soubory
- `STYLE` – změny, které neovlivňují význam kódu, obvykle souvisejí s formátováním kódu (např. mezery, chybějící středníky atd.)
- `TEST` – přidání nových nebo oprava stávajících testů
- `PERF` – vylepšení výkonu
- `CI` - změny týkající se kontinuální integrace
- `BUILD` – změny, které ovlivňují systém sestavení nebo externí závislosti
- `REVERT` – návrat k předchozímu commitu