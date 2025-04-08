SET SQL_MODE = "NO_AUTO_VALUE_ON_ZERO";
START TRANSACTION;
SET time_zone = "+00:00";


/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8mb4 */;

--
-- db
--
CREATE DATABASE IF NOT EXISTS `educhem_lan_party_dev` DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci;
USE `educhem_lan_party_dev`;

-- --------------------------------------------------------

--
-- Struktura tabulky `chat`
--

DROP TABLE IF EXISTS `chat`;
CREATE TABLE IF NOT EXISTS `chat` (
                                      `user_id` int NOT NULL,
                                      `message` text NOT NULL,
                                      `uuid` varchar(256) NOT NULL,
                                      `date` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
                                      PRIMARY KEY (`uuid`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- --------------------------------------------------------

--
-- Struktura tabulky `computers`
--

DROP TABLE IF EXISTS `computers`;
CREATE TABLE IF NOT EXISTS `computers` (
                                           `id` varchar(12) NOT NULL,
                                           `room_id` varchar(12) DEFAULT NULL,
                                           `is_teachers_pc` tinyint(1) NOT NULL DEFAULT '0',
                                           `available` tinyint(1) NOT NULL DEFAULT '1',
                                           PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- --------------------------------------------------------

--
-- Struktura tabulky `reservations`
--

DROP TABLE IF EXISTS `reservations`;
CREATE TABLE IF NOT EXISTS `reservations` (
                                              `user_id` int NOT NULL,
                                              `room_id` varchar(12) DEFAULT NULL,
                                              `computer_id` varchar(12) DEFAULT NULL,
                                              `note` text,
                                              `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
                                              PRIMARY KEY (`user_id`),
                                              UNIQUE KEY `computer_id` (`computer_id`),
                                              KEY `rooms_fk` (`room_id`)
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
CREATE TABLE IF NOT EXISTS `rooms` (
                                       `id` varchar(12) NOT NULL,
                                       `label` varchar(32) DEFAULT NULL,
                                       `image` varchar(512) DEFAULT NULL,
                                       `limit_of_seats` tinyint UNSIGNED DEFAULT NULL,
                                       `available` tinyint(1) NOT NULL DEFAULT '1',
                                       PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- --------------------------------------------------------

--
-- Struktura tabulky `settings`
--

DROP TABLE IF EXISTS `settings`;
CREATE TABLE IF NOT EXISTS `settings` (
                                          `property` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
                                          `value` json NOT NULL,
                                          PRIMARY KEY (`property`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- --------------------------------------------------------

--
-- Struktura tabulky `users`
--

DROP TABLE IF EXISTS `users`;
CREATE TABLE IF NOT EXISTS `users` (
                                       `id` int NOT NULL AUTO_INCREMENT,
                                       `display_name` varchar(64) NOT NULL DEFAULT 'Neznámé jméno',
                                       `email` varchar(64) DEFAULT NULL,
                                       `password` varchar(1024) NOT NULL DEFAULT '_',
                                       `class` varchar(12) DEFAULT NULL,
                                       `gender` set('MALE','FEMALE','OTHER') DEFAULT NULL,
                                       `last_updated` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
                                       `last_logged_in` datetime DEFAULT NULL,
                                       `account_type` enum('STUDENT','TEACHER','ADMIN') NOT NULL DEFAULT 'STUDENT',
                                       `avatar` varchar(1024) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
                                       PRIMARY KEY (`id`),
                                       UNIQUE KEY `email` (`email`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

--
-- Triggery `users`
--
DROP TRIGGER IF EXISTS `trg_before_update_users`;
DELIMITER $$
CREATE TRIGGER `trg_before_update_users` BEFORE UPDATE ON `users` FOR EACH ROW BEGIN
    IF (NEW.last_logged_in <=> OLD.last_logged_in) THEN
        SET NEW.last_updated = NOW();
    END IF;
END
$$
DELIMITER ;

--
-- Omezení pro exportované tabulky
--

--
-- Omezení pro tabulku `reservations`
--
ALTER TABLE `reservations`
    ADD CONSTRAINT `computers_fk` FOREIGN KEY (`computer_id`) REFERENCES `computers` (`id`) ON DELETE CASCADE ON UPDATE CASCADE,
    ADD CONSTRAINT `rooms_fk` FOREIGN KEY (`room_id`) REFERENCES `rooms` (`id`) ON DELETE CASCADE ON UPDATE CASCADE,
    ADD CONSTRAINT `users_fk` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE CASCADE ON UPDATE CASCADE;
COMMIT;

/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;





# vytvoreni uctu
CREATE USER 'educhem_lan_party'@'%' IDENTIFIED BY 'educhem_lan_party';

GRANT ALL PRIVILEGES ON educhem_lan_party_dev.* TO 'educhem_lan_party'@'%';

FLUSH PRIVILEGES;