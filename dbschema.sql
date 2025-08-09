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
                        `deleted` tinyint(1) NOT NULL DEFAULT '0',
                        `replying_to_uuid` varchar(256) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL
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

--
-- Vypisuji data pro tabulku `computers`
--

INSERT INTO `computers` (`id`, `room_id`, `is_teachers_pc`, `available`) VALUES
                                                                             ('VRR_PC01', 'VRR', 0, 1),
                                                                             ('VRR_PC02', 'VRR', 0, 1),
                                                                             ('VRR_PC03', 'VRR', 0, 1),
                                                                             ('VRR_PC04', 'VRR', 0, 1),
                                                                             ('VRR_PC05', 'VRR', 0, 1),
                                                                             ('VRR_PC06', 'VRR', 0, 1),
                                                                             ('VRR_PC07', 'VRR', 0, 1),
                                                                             ('VRR_PC08', 'VRR', 0, 1),
                                                                             ('VRR_PC09', 'VRR', 1, 1),
                                                                             ('VT3_PC01', 'VT3', 0, 1),
                                                                             ('VT3_PC02', 'VT3', 0, 1),
                                                                             ('VT3_PC03', 'VT3', 0, 1),
                                                                             ('VT3_PC04', 'VT3', 0, 1),
                                                                             ('VT3_PC05', 'VT3', 0, 1),
                                                                             ('VT3_PC06', 'VT3', 0, 1),
                                                                             ('VT3_PC07', 'VT3', 0, 1),
                                                                             ('VT3_PC08', 'VT3', 0, 1),
                                                                             ('VT3_PC09', 'VT3', 0, 1),
                                                                             ('VT3_PC10', 'VT3', 0, 1),
                                                                             ('VT3_PC11', 'VT3', 0, 1),
                                                                             ('VT3_PC12', 'VT3', 0, 1),
                                                                             ('VT3_PC13', 'VT3', 0, 1),
                                                                             ('VT3_PC14', 'VT3', 0, 1),
                                                                             ('VT3_PC15', 'VT3', 0, 1),
                                                                             ('VT3_PC16', 'VT3', 0, 1),
                                                                             ('VT3_PC17', 'VT3', 0, 1),
                                                                             ('VT3_PC18', 'VT3', 0, 1),
                                                                             ('VT3_PC19', 'VT3', 0, 1),
                                                                             ('VT3_PC20', 'VT3', 0, 1),
                                                                             ('VT3_PC21', 'VT3', 0, 1),
                                                                             ('VT3_PC22', 'VT3', 0, 1),
                                                                             ('VT3_PC23', 'VT3', 0, 1),
                                                                             ('VT3_PC24', 'VT3', 0, 1),
                                                                             ('VT3_PC25', 'VT3', 1, 1),
                                                                             ('VT3_PC26', 'VT3', 0, 1),
                                                                             ('VT3_PC27', 'VT3', 0, 1),
                                                                             ('VT3_PC28', 'VT3', 0, 1),
                                                                             ('VT3_PC29', 'VT3', 0, 1);

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

--
-- Vypisuji data pro tabulku `rooms`
--

INSERT INTO `rooms` (`id`, `label`, `image`, `limit_of_seats`, `available`) VALUES
                                                                                ('07', 'Učebna 07', '07.jpg', 10, 1),
                                                                                ('08', 'Učebna 08', '08.jpg', 16, 1),
                                                                                ('DELICKA', 'Dělička', 'delicka.jpg', 8, 1),
                                                                                ('VRR', 'Učebna VRR+R', 'vrr.jpg', 5, 1),
                                                                                ('VT3', 'VT3', 'vt3_2.jpg', 0, 1);

-- --------------------------------------------------------

--
-- Struktura tabulky `settings`
--

DROP TABLE IF EXISTS `settings`;
CREATE TABLE `settings` (
                            `property` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
                            `value` varchar(2048) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='data lze menit pouze v pripade, ze je aplikace vypnuta';

--
-- Vypisuji data pro tabulku `settings`
--

INSERT INTO `settings` (`property`, `value`) VALUES
                                                 ('chat_enabled', 'True'),
                                                 ('reservations_enabled_from', '2025-08-08 06:56:00'),
                                                 ('reservations_enabled_to', '2025-08-15 09:05:00'),
                                                 ('reservations_status', 'OPEN');

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
                         `avatar` varchar(1024) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
                         `banner` varchar(1024) DEFAULT NULL,
                         `enable_reservation` tinyint(1) NOT NULL DEFAULT '0'
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
                                       `token_type` set('BEARER') CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL DEFAULT 'BEARER',
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
    ADD PRIMARY KEY (`uuid`),
  ADD KEY `chat_user_id` (`user_id`);

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
-- Omezení pro tabulku `chat`
--
ALTER TABLE `chat`
    ADD CONSTRAINT `chat_fk1` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE CASCADE ON UPDATE CASCADE;

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
SET NAMES 'utf8mb4';
SET CHARACTER SET utf8mb4;
ALTER TABLE users CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci;

INSERT INTO `users` (`id`, `display_name`, `email`, `password`, `class`, `gender`, `last_updated`, `last_logged_in`, `account_type`, `avatar`, `banner`) VALUES
(1, 'Administrator', 'admin@admin.admin', '$2a$11$7XnPVemd77pb8WWQItw5d.azyOvrGgKN941CxPTVYy3515cCOhZ0K', 'SPECIAL', 'OTHER', '2025-04-09 00:14:10', '2025-04-08 22:12:45', 'SUPERADMIN', 'https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcSzrViu6UGEqo8eEnSGQkf8497JBfHSLViSoQ&s', 'https://drscdn.500px.org/photo/1113771318/q%3D80_m%3D600/v2?sig=c18424903b06b1b1595af3bd506268530d582216cc8e319b110d3dfe7ec2d242'),
(2, 'Miloš Navrátil', 'milos.navratil@example.com', '_', '1.A', 'MALE', '2025-06-14 14:56:11', NULL, 'STUDENT', NULL, NULL),
(3, 'Marek Svoboda', 'marek.svoboda@example.com', '_', NULL, 'MALE', '2025-06-14 14:56:11', NULL, 'TEACHER', 'https://imgv3.fotor.com/images/blog-cover-image/a-shadow-of-a-boy-carrying-the-camera-with-red-sky-behind.jpg', NULL),
(4, 'Jana Pokorná', 'jana.pokorna@example.com', '_', NULL, 'FEMALE', '2025-06-14 14:56:11', NULL, 'ADMIN', 'https://iso.500px.com/wp-content/uploads/2016/02/stock-photo-114337435.jpg', NULL),
(5, 'Natálie Novotná', 'natalie.novotna@example.com', '_', '2.B', 'FEMALE', '2025-06-14 14:56:11', NULL, 'STUDENT', 'https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcThisxh_l6BA8BgSe7z2MUiaRj553YtM11PNA&s', NULL),
(6, 'Tomáš Dvořák', 'tomas.dvorak@example.com', '_', '3.A', 'MALE', '2025-06-14 14:59:34', NULL, 'STUDENT', 'https://img.freepik.com/free-photo/couple-making-heart-from-hands-sea-shore_23-2148019887.jpg?semt=ais_hybrid&w=740', NULL),
(7, 'Lucie Králová', 'lucie.kralova@example.com', '_', '1.B', 'FEMALE', '2025-06-14 14:59:34', NULL, 'STUDENT', NULL, NULL),
(8, 'Petr Černý', 'petr.cerny@example.com', '_', '2.A', 'MALE', '2025-06-14 14:59:34', NULL, 'STUDENT', 'https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcRvFBa3G11OUBYADP7ouSBgwiiRzSYorF4dfg&s', NULL),
(9, 'Eliška Horáková', 'eliska.horakova@example.com', '_', '3.B', 'FEMALE', '2025-06-14 14:59:34', NULL, 'STUDENT', NULL, NULL),
(10, 'Adam Nový', 'adam.novy@example.com', '_', NULL, 'MALE', '2025-06-14 14:59:34', NULL, 'TEACHER', 'https://www.shutterstock.com/image-photo/passport-photo-portrait-young-man-260nw-2437772333.jpg', 'https://static.vecteezy.com/system/resources/thumbnails/022/902/924/small/ai-generative-modern-luxury-real-estate-house-for-sale-and-rent-luxury-property-residence-concept-photo.jpg'),
(11, 'Simona Benešová', 'simona.benesova@example.com', '_', NULL, 'FEMALE', '2025-06-14 14:59:34', NULL, 'TEACHER', 'https://img.freepik.com/premium-photo/giving-rose-png-badge-sticker-valentines-day-photo-heart-shape-transparent-background_53876-950839.jpg?semt=ais_hybrid&w=740', 'https://static.vecteezy.com/system/resources/thumbnails/054/587/101/small_2x/romantic-rose-backdrop-with-intertwined-blooms-and-hearts-for-valentines-day-highlighting-love-and-passion-with-a-warm-color-palette-perfect-for-romantic-settings-and-event-design-concepts-photo.jpeg'),
(12, 'Ondřej Kolář', 'ondrej.kolar@example.com', '_', '1.C', 'MALE', '2025-06-14 14:59:34', NULL, 'STUDENT', 'https://5.imimg.com/data5/SELLER/Default/2023/4/300935887/RK/PR/IE/4354612/img-20220703-wa0001-500x500.jpg', NULL),
(13, 'Tereza Malá', 'tereza.mala@example.com', '_', '2.C', 'FEMALE', '2025-06-14 14:59:34', NULL, 'STUDENT', NULL, NULL),
(14, 'Roman Havel', 'roman.havel@example.com', '_', NULL, 'MALE', '2025-06-14 14:59:34', NULL, 'ADMIN', NULL, NULL),
(15, 'Barbora Němcová', 'barbora.nemcova@example.com', '_', '3.C', 'FEMALE', '2025-06-14 14:59:34', NULL, 'STUDENT', 'https://st2.depositphotos.com/2001755/5408/i/450/depositphotos_54081723-stock-photo-beautiful-nature-landscape.jpg', NULL),
(16, 'Filip Růžička', 'filip.ruzicka@example.com', '_', '1.A', 'MALE', '2025-06-14 14:59:34', NULL, 'STUDENT', NULL, NULL),
(17, 'Nikola Procházková', 'nikola.prochazkova@example.com', '_', '2.B', 'FEMALE', '2025-06-14 14:59:34', NULL, 'STUDENT', NULL, NULL),
(18, 'Daniel Konečný', 'daniel.konecny@example.com', '_', '4.A', 'MALE', '2025-06-14 14:59:34', NULL, 'STUDENT', 'https://i.pinimg.com/236x/d3/4b/ec/d34becc9ecf6f8b4b5c861bf61109176.jpg', NULL),
(19, 'Veronika Blažková', 'veronika.blazkova@example.com', '_', '1.B', 'FEMALE', '2025-06-14 14:59:34', NULL, 'STUDENT', NULL, NULL),
(20, 'Jakub Fiala', 'jakub.fiala@example.com', '_', '3.A', 'MALE', '2025-06-14 14:59:34', NULL, 'STUDENT', NULL, NULL),
(21, 'Kristýna Doležalová', 'kristyna.dolezalova@example.com', '_', NULL, 'FEMALE', '2025-06-14 14:59:34', NULL, 'ADMIN', NULL, NULL),
(22, 'David Marek', 'david.marek@example.com', '_', '2.C', 'MALE', '2025-06-14 14:59:34', NULL, 'STUDENT', NULL, NULL),
(23, 'Kateřina Holubová', 'katerina.holubova@example.com', '_', '3.B', 'FEMALE', '2025-06-14 14:59:34', NULL, 'STUDENT', NULL, NULL),
(24, 'Matěj Šimek', 'matej.simek@example.com', '_', NULL, 'MALE', '2025-06-14 14:59:34', NULL, 'TEACHER', 'https://i.pinimg.com/236x/39/8f/da/398fdab4318b3baa65d36baf5ab3fab4.jpg', 'https://static8.depositphotos.com/1491329/1068/i/450/depositphotos_10687188-stock-photo-foggy-landscape-early-morning-mist.jpg'),
(25, 'Alena Sedláčková', 'alena.sedlackova@example.com', '_', '1.C', 'FEMALE', '2025-06-14 14:59:34', NULL, 'STUDENT', NULL, NULL);

INSERT INTO `reservations` (`user_id`, `room_id`, `computer_id`, `note`, `created_at`) VALUES
(3, NULL, 'VT3_PC15', NULL, '2025-06-14 14:57:51'),
(5, 'DELICKA', NULL, NULL, '2025-06-14 14:57:51'),
(7, NULL, 'VT3_PC10', NULL, '2025-06-14 15:07:53'),
(8, 'DELICKA', NULL, NULL, '2025-06-14 15:12:52'),
(9, NULL, 'VRR_PC06', NULL, '2025-06-14 15:12:52'),
(13, NULL, 'VRR_PC07', NULL, '2025-06-14 15:12:52'),
(23, NULL, 'VT3_PC06', NULL, '2025-06-14 15:07:53'),
(24, 'VRR', NULL, NULL, '2025-06-14 15:13:33');