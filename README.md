# EDUCHEM LAN Party App

Tento projekt poskytuje webovou platformu pro správu a organizaci LAN party událostí na EDUCHEM. Umožňuje registraci týmů, plánování turnajů a poskytuje aktuální informace o událostech.


## Obsah

- [Popis projektu](#popis-projektu)
- [Funkcionality](#funkcionality)
- [Technologie](#technologie)
- [Instalace a spuštění](#instalace-a-spuštění)
    - [1. Klonování repozitáře](#1-klonování-repozitáře)
    - [2. Instalace požadovaného softwaru](#2-instalace-požadovaného-softwaru)
    - [3. Nastavení databáze](#3-nastavení-databáze)
    - [4. Vytvoření souboru `.env`](#4-vytvoření-souboru-env)
    - [5. Spuštění aplikace](#5-spuštění-aplikace)
    - [6. Přihlášení do admin panelu](#6-přihlášení-do-admin-panelu)
- [Screenshoty](#screenshoty)

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

- ![C#](https://img.shields.io/badge/c%23-%23239120.svg?style=for-the-badge&logo=csharp&logoColor=white)
- ![.NET](https://img.shields.io/badge/.NET-5C2D91?style=for-the-badge&logo=.net&logoColor=white)

### Frontend

- ![Vite](https://img.shields.io/badge/vite-%23646CFF.svg?style=for-the-badge&logo=vite&logoColor=white)
- ![React](https://img.shields.io/badge/react-%2320232a.svg?style=for-the-badge&logo=react&logoColor=%2361DAFB)
- ![React Router](https://img.shields.io/badge/React_Router-CA4245?style=for-the-badge&logo=react-router&logoColor=white)
- ![SASS](https://img.shields.io/badge/SASS-hotpink.svg?style=for-the-badge&logo=SASS&logoColor=white)
- ![TypeScript](https://img.shields.io/badge/typescript-%23007ACC.svg?style=for-the-badge&logo=typescript&logoColor=white)

### Databáze

- ![MySQL](https://img.shields.io/badge/mysql-4479A1.svg?style=for-the-badge&logo=mysql&logoColor=white)


## Instalace a spuštění

### 1. Klonování repozitáře

```bash
git clone https://github.com/AldiiX/EDUCHEM-LAN-Party-Web.git
```

### 2. Instalace požadovaného softwaru
   - .NET SDK: Ujisti se, že máš nainstalované .NET SDK 9.0. [Stáhni .NET SDK](https://dotnet.microsoft.com/download) 
   - Node.js: [Stáhni Node.js](https://nodejs.org/)

### 3. Nastavení databáze:
  1. Aplikace používá MySQL databázi. Stáhněte si jakkoliv MySQL databázi na svůj počítač. Například [zde](https://dev.mysql.com/downloads/installer/).
        - Doporučujeme k tomu stáhnout i _PhpMyAdmin_, abyste mohli snadno spravovat databázi. [Zde je odkaz na stažení](https://www.phpmyadmin.net/downloads/).
        - Můžete nainstalovat program _XAMPP_, kde PhpMyAdmin a MySQL jsou již součástí balíčku. [Zde můžete stáhnout](https://www.apachefriends.org/)
  2. Přihlašte se do PhpMyAdmin/MySQL cli pomocí těchto příkazů:
   ```
   username: admin
   password: password
   ```
3. Otevřete soubor `dbschema.sql`, zkopírujte obsah a toto provedte v MySQL jako dotaz, tím se vám vytvoří databáze.
### 4. Vytvoření souboru .env
   - V adresáři /EduchemLP.Server/ vytvoř soubor .env a vlož do něj následující obsah:
   ```dotenv
    DATABASE_IP=localhost   # případně zadej vzdálenou IP adresu
    DATABASE_DBNAME=educhem_lanparty
    DATABASE_USERNAME=admin
    DATABASE_PASSWORD=password
    
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
    Heslo: password
```
   - Admin panel najdeš zde: http://localhost:3154/app/administration, kde můžeš spravovat uživatele a další administrativní nastavení.


## Screenshoty
![img1](https://stanislavskudrna.cz/images/websites/educhemlp/1.png)