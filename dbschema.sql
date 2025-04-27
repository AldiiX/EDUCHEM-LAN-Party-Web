SET SQL_MODE = "NO_AUTO_VALUE_ON_ZERO";
START TRANSACTION;
SET time_zone = "+00:00";


/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8mb4 */;

--
-- Databáze: `educhem_lan_party_dev`
--
CREATE DATABASE IF NOT EXISTS `educhem_lan_party_dev` DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci;
USE `educhem_lan_party_dev`;

-- --------------------------------------------------------

--
-- Struktura tabulky `announcements`
--

DROP TABLE IF EXISTS `announcements`;
CREATE TABLE `announcements` (
                                 `id` int UNSIGNED NOT NULL,
                                 `author_id` int NOT NULL,
                                 `message` text NOT NULL,
                                 `date` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- --------------------------------------------------------

--
-- Struktura tabulky `chat`
--

DROP TABLE IF EXISTS `chat`;
CREATE TABLE `chat` (
                        `user_id` int NOT NULL,
                        `message` varchar(1024) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
                        `uuid` varchar(256) NOT NULL,
                        `date` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
                        `deleted` tinyint(1) NOT NULL DEFAULT '0'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- --------------------------------------------------------

--
-- Struktura tabulky `computers`
--

DROP TABLE IF EXISTS `computers`;
CREATE TABLE `computers` (
                             `id` varchar(12) NOT NULL,
                             `room_id` varchar(12) DEFAULT NULL,
                             `is_teachers_pc` tinyint(1) NOT NULL DEFAULT '0',
                             `available` tinyint(1) NOT NULL DEFAULT '1'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- --------------------------------------------------------

--
-- Struktura tabulky `logs`
--

DROP TABLE IF EXISTS `logs`;
CREATE TABLE `logs` (
                        `id` int UNSIGNED NOT NULL,
                        `type` set('INFO','ERROR','WARN') NOT NULL DEFAULT 'INFO',
                        `exact_type` varchar(32) NOT NULL DEFAULT 'basic',
                        `message` text CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
                        `date` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- --------------------------------------------------------

--
-- Struktura tabulky `reservations`
--

DROP TABLE IF EXISTS `reservations`;
CREATE TABLE `reservations` (
                                `user_id` int NOT NULL,
                                `room_id` varchar(12) DEFAULT NULL,
                                `computer_id` varchar(12) DEFAULT NULL,
                                `note` text,
                                `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

--
-- Triggery `reservations`
--
DROP TRIGGER IF EXISTS `check_double_values_res`;
DELIMITER $$
CREATE TRIGGER `check_double_values_res` BEFORE UPDATE ON `reservations` FOR EACH ROW BEGIN
    IF (NEW.room_id IS NOT NULL AND NEW.computer_id IS NOT NULL) OR
       (NEW.room_id IS NULL AND NEW.computer_id IS NULL) THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'Musí být vyplněn buď "room_id", nebo "computer_id", ale ne oba.';
    END IF;
END
$$
DELIMITER ;
DROP TRIGGER IF EXISTS `check_double_values_res2`;
DELIMITER $$
CREATE TRIGGER `check_double_values_res2` BEFORE INSERT ON `reservations` FOR EACH ROW BEGIN
    IF (NEW.room_id IS NOT NULL AND NEW.computer_id IS NOT NULL) OR
       (NEW.room_id IS NULL AND NEW.computer_id IS NULL) THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'Musí být vyplněn buď "room_id", nebo "computer_id", ale ne oba.';
    END IF;
END
$$
DELIMITER ;
DROP TRIGGER IF EXISTS `check_room_capacity`;
DELIMITER $$
CREATE TRIGGER `check_room_capacity` BEFORE INSERT ON `reservations` FOR EACH ROW BEGIN
    -- Deklarace proměnných
    DECLARE current_occupancy INT DEFAULT 0;
    DECLARE room_capacity INT DEFAULT 0;

    -- Kontrola, zda je room_id vyplněno
    IF NEW.room_id IS NOT NULL THEN
        -- Získání počtu již rezervovaných míst pro danou místnost
        SELECT COUNT(*)
        INTO current_occupancy
        FROM reservations
        WHERE room_id = NEW.room_id;

        -- Získání kapacity místnosti
        SELECT limit_of_seats
        INTO room_capacity
        FROM rooms
        WHERE id = NEW.room_id;

        -- Porovnání kapacity a aktuálního počtu lidí
        IF (current_occupancy + 1) > room_capacity THEN
            SIGNAL SQLSTATE '45000'
                SET MESSAGE_TEXT = 'Kapacita místnosti je překročena. Rezervace není možná.';
        END IF;
    END IF;
END
$$
DELIMITER ;
DROP TRIGGER IF EXISTS `check_room_capacity2`;
DELIMITER $$
CREATE TRIGGER `check_room_capacity2` BEFORE UPDATE ON `reservations` FOR EACH ROW BEGIN
    -- Deklarace proměnných
    DECLARE current_occupancy INT DEFAULT 0;
    DECLARE room_capacity INT DEFAULT 0;

    -- Kontrola, zda je `room_id` změněno
    IF NEW.room_id IS NOT NULL AND NEW.room_id != OLD.room_id THEN
        -- Získání počtu již rezervovaných míst pro novou místnost
        SELECT COUNT(*)
        INTO current_occupancy
        FROM reservations
        WHERE room_id = NEW.room_id;

        -- Získání kapacity nové místnosti
        SELECT limit_of_seats
        INTO room_capacity
        FROM rooms
        WHERE id = NEW.room_id;

        -- Porovnání kapacity a aktuálního počtu lidí
        IF (current_occupancy + 1) > room_capacity THEN
            SIGNAL SQLSTATE '45000'
                SET MESSAGE_TEXT = 'Kapacita místnosti je překročena. Rezervace není možná.';
        END IF;
    END IF;
END
$$
DELIMITER ;

-- --------------------------------------------------------

--
-- Struktura tabulky `rooms`
--

DROP TABLE IF EXISTS `rooms`;
CREATE TABLE `rooms` (
                         `id` varchar(12) NOT NULL,
                         `label` varchar(32) DEFAULT NULL,
                         `image` varchar(512) DEFAULT NULL,
                         `limit_of_seats` tinyint UNSIGNED DEFAULT NULL,
                         `available` tinyint(1) NOT NULL DEFAULT '1'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- --------------------------------------------------------

--
-- Struktura tabulky `settings`
--

DROP TABLE IF EXISTS `settings`;
CREATE TABLE `settings` (
                            `property` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
                            `value` json NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- --------------------------------------------------------

--
-- Struktura tabulky `users`
--

DROP TABLE IF EXISTS `users`;
CREATE TABLE `users` (
                         `id` int NOT NULL,
                         `display_name` varchar(64) NOT NULL DEFAULT 'Neznámé jméno',
                         `email` varchar(64) DEFAULT NULL,
                         `password` varchar(1024) NOT NULL DEFAULT '_',
                         `class` varchar(12) DEFAULT NULL,
                         `gender` set('MALE','FEMALE','OTHER') DEFAULT NULL,
                         `last_updated` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
                         `last_logged_in` datetime DEFAULT NULL,
                         `account_type` enum('STUDENT','TEACHER','ADMIN','SUPERADMIN') CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL DEFAULT 'STUDENT',
                         `avatar` varchar(1024) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- --------------------------------------------------------

--
-- Struktura tabulky `users_access_tokens`
--

DROP TABLE IF EXISTS `users_access_tokens`;
CREATE TABLE `users_access_tokens` (
                                       `user_id` int NOT NULL,
                                       `platform` set('DISCORD','INSTAGRAM','GOOGLE','GITHUB') CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
                                       `access_token` varchar(1024) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
                                       `refresh_token` varchar(1024) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
                                       `token_type` set('BEARER') NOT NULL,
                                       `date` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

--
-- Indexy pro exportované tabulky
--

--
-- Indexy pro tabulku `announcements`
--
ALTER TABLE `announcements`
    ADD PRIMARY KEY (`id`),
    ADD KEY `announcements_fk1` (`author_id`);

--
-- Indexy pro tabulku `chat`
--
ALTER TABLE `chat`
    ADD PRIMARY KEY (`uuid`);

--
-- Indexy pro tabulku `computers`
--
ALTER TABLE `computers`
    ADD PRIMARY KEY (`id`);

--
-- Indexy pro tabulku `logs`
--
ALTER TABLE `logs`
    ADD PRIMARY KEY (`id`);

--
-- Indexy pro tabulku `reservations`
--
ALTER TABLE `reservations`
    ADD PRIMARY KEY (`user_id`),
    ADD UNIQUE KEY `computer_id` (`computer_id`),
    ADD KEY `rooms_fk` (`room_id`);

--
-- Indexy pro tabulku `rooms`
--
ALTER TABLE `rooms`
    ADD PRIMARY KEY (`id`);

--
-- Indexy pro tabulku `settings`
--
ALTER TABLE `settings`
    ADD PRIMARY KEY (`property`);

--
-- Indexy pro tabulku `users`
--
ALTER TABLE `users`
    ADD PRIMARY KEY (`id`),
    ADD UNIQUE KEY `email` (`email`);

--
-- Indexy pro tabulku `users_access_tokens`
--
ALTER TABLE `users_access_tokens`
    ADD PRIMARY KEY (`platform`,`user_id`),
    ADD KEY `users_access_tokens_fk1` (`user_id`);

--
-- AUTO_INCREMENT pro tabulky
--

--
-- AUTO_INCREMENT pro tabulku `announcements`
--
ALTER TABLE `announcements`
    MODIFY `id` int UNSIGNED NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT pro tabulku `logs`
--
ALTER TABLE `logs`
    MODIFY `id` int UNSIGNED NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT pro tabulku `users`
--
ALTER TABLE `users`
    MODIFY `id` int NOT NULL AUTO_INCREMENT;

--
-- Omezení pro exportované tabulky
--

--
-- Omezení pro tabulku `announcements`
--
ALTER TABLE `announcements`
    ADD CONSTRAINT `announcements_fk1` FOREIGN KEY (`author_id`) REFERENCES `users` (`id`) ON DELETE CASCADE ON UPDATE CASCADE;

--
-- Omezení pro tabulku `reservations`
--
ALTER TABLE `reservations`
    ADD CONSTRAINT `computers_fk` FOREIGN KEY (`computer_id`) REFERENCES `computers` (`id`) ON DELETE CASCADE ON UPDATE CASCADE,
    ADD CONSTRAINT `rooms_fk` FOREIGN KEY (`room_id`) REFERENCES `rooms` (`id`) ON DELETE CASCADE ON UPDATE CASCADE,
    ADD CONSTRAINT `users_fk` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE CASCADE ON UPDATE CASCADE;

--
-- Omezení pro tabulku `users_access_tokens`
--
ALTER TABLE `users_access_tokens`
    ADD CONSTRAINT `users_access_tokens_fk1` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE CASCADE ON UPDATE CASCADE;
COMMIT;

/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;





















# vytvoreni uctu
CREATE USER 'educhem_lan_party'@'%' IDENTIFIED BY 'educhem_lan_party';

GRANT ALL PRIVILEGES ON educhem_lan_party_dev.* TO 'educhem_lan_party'@'%';

FLUSH PRIVILEGES;


# postnuti veci do db
INSERT INTO `users`
(`display_name`, `email`, `password`, `class`, `gender`, `last_updated`, `last_logged_in`, `account_type`, `avatar`)
VALUES (
           'Admin',
           'admin@admin.admin',
           'b109f3bbbc244eb82441917ed06d618b9008dd09b3befd1b5e07394c706a8bb980b1d7785e5976ec049b46df5f1326af5a2ea6d103fd07c95385ffab0cacbc864dca00da67c692296690e90c50c96b79',
           NULL,
           'OTHER',
           NOW(),
           NOW(),
           'SUPERADMIN',
        NULL
       );