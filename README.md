# EDUCHEM LAN Party App

Tento projekt poskytuje webovou platformu pro správu a organizaci LAN party událostí na EDUCHEM. Umožňuje registraci týmů, plánování turnajů a poskytuje informace o událostech.

### Funkcionality

- **Informace o LAN Party**: Umožňuje uživatelům prohlížet informace o nadcházejících LAN party událostech.
- **Rezervace počítače nebo stolu**: Uživatelé mohou na interaktivní mapě rezervovat počítače nebo stoly (místa) pro účast na LAN party.
- **Chat**: Uživatelé mohou komunikovat prostřednictvím chatu během události.
- **Správa turnajů**: Organizátoři mohou spravovat turnaje, včetně registrace týmů a sledování výsledků.
- **Správa uživatelů**: Umožňuje administrátorům spravovat uživatelské účty a jejich oprávnění.
- a spoustu dalších funkcí, které usnadňují organizaci a účast na LAN party!!

### Technologie

- **Backend**: ![C#](https://img.shields.io/badge/c%23-%23239120.svg?style=for-the-badge&logo=csharp&logoColor=white) ![.Net](https://img.shields.io/badge/.NET-5C2D91?style=for-the-badge&logo=.net&logoColor=white)
- **Frontend**: ![Vite](https://img.shields.io/badge/vite-%23646CFF.svg?style=for-the-badge&logo=vite&logoColor=white) ![React](https://img.shields.io/badge/react-%2320232a.svg?style=for-the-badge&logo=react&logoColor=%2361DAFB) ![React Router](https://img.shields.io/badge/React_Router-CA4245?style=for-the-badge&logo=react-router&logoColor=white) ![SASS](https://img.shields.io/badge/SASS-hotpink.svg?style=for-the-badge&logo=SASS&logoColor=white) ![TypeScript](https://img.shields.io/badge/typescript-%23007ACC.svg?style=for-the-badge&logo=typescript&logoColor=white)
- **Databáze**: ![MySQL](https://img.shields.io/badge/mysql-4479A1.svg?style=for-the-badge&logo=mysql&logoColor=white)


# Instalace a spuštění v DEV prostředí

1. **Klonování repozitáře**:

   ```bash
   git clone https://github.com/AldiiX/EDUCHEM-LAN-Party-Web.git

2. **Instalace .NET SDK a Node.js**:

   - Ujistěte se, že máte nainstalované .NET SDK 9.0 a Node.js. Můžete je stáhnout z následujících odkazů:
     - [.NET SDK](https://dotnet.microsoft.com/download)
     - [Node.js](https://nodejs.org/)

3. **Nastavení databáze**:
   1. Aplikace používá MySQL databázi. Stáhněte si jakkoliv MySQL databázi na svůj počítač. Například [zde](https://dev.mysql.com/downloads/installer/).
      - Doporučujeme k tomu stáhnout i PhpMyAdmin, abyste mohli snadno spravovat databázi. [Zde](https://www.phpmyadmin.net/downloads/) je odkaz na stažení.
      - Můžete nainstalovat program XAMPP, kde PhpMyAdmin a MySQL jsou již součástí balíčku.
   2. Přihlašte se do PhpMyAdmin/MySQL cli pomocí těchto příkazů:
   ```
   username: admin
   password: password
    ```
   3. Otevřete soubor `dbschema.sql`, zkopírujte obsah a toto provedte v MySQL jako dotaz, tím se vám vytvoří databáze.

4. **Vytvoření .env**
    - Vytvořte soubor `.env` v `/EduchemLP.Server/` a vložte následující text:

    ```dotenv
    DATABASE_IP=localhost   # nebo nějaká vzdálená IP adresa
    DATABASE_DBNAME=educhem_lanparty
    DATABASE_USERNAME=admin
    DATABASE_PASSWORD=password
    
    # pokud chcete posílat emaily, tak nastavte i váš vlastni SMTP
    SMTP_HOST=???
    SMTP_PORT=???
    SMTP_EMAIL_USERNAME=???
    SMTP_EMAIL_PASSWORD=???
   ```

5. **Spuštění aplikace**
    - Otevřete terminál a přejděte do adresáře `/EduchemLP.Server/` a spusťte následující příkazy:

    ```bash
    dotnet restore
    dotnet build
    dotnet run
    ```
   
    - tím se spustí backend a otevře se vám okno, kde běží frontend (Vite)
    - aplikace běží na http://localhost:3154
6. **Přihlášení do admin panelu aplikace**
    - na adrese http://localhost:3154/login se můžete přihlásit na admin účet pomocí 
   ```
   email: admin@admin.admin
   password: password
   ```
   - potom na adrese http://localhost:3154/app/administration můžete vytvářet nové uživatele a spravovat je