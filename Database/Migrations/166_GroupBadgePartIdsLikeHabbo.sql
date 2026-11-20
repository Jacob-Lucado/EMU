-- Group badge symbols use Habbo's badge code ids, and the badge editor lists every part and colour in Habbo's order.
ALTER TABLE groups_items ADD COLUMN IF NOT EXISTS order_num INT NOT NULL DEFAULT 0;

DROP TEMPORARY TABLE IF EXISTS migration_166_symbols;
CREATE TEMPORARY TABLE migration_166_symbols (
    id INT NOT NULL,
    firstvalue VARCHAR(255) NOT NULL,
    secondvalue VARCHAR(2000) NOT NULL,
    order_num INT NOT NULL,
    PRIMARY KEY (firstvalue)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

INSERT INTO migration_166_symbols (id, firstvalue, secondvalue, order_num) VALUES
(1, 'symbol_background_1.gif', '', 1),
(2, 'symbol_background_2.gif', '', 2),
(3, 'symbol_background_3_part2.gif', 'symbol_background_3_part1.gif', 3),
(4, 'symbol_ball_1_part2.gif', 'symbol_ball_1_part1.gif', 4),
(5, 'symbol_ball_2_part2.gif', 'symbol_ball_2_part1.gif', 5),
(6, 'symbol_bobba.gif', '', 6),
(7, 'symbol_bomb_part2.gif', 'symbol_bomb_part1.gif', 7),
(8, 'symbol_bow.gif', '', 8),
(9, 'symbol_box_1.gif', '', 9),
(10, 'symbol_box_2.gif', '', 10),
(11, 'symbol_bunting_1.gif', '', 11),
(12, 'symbol_bunting_2.gif', '', 12),
(13, 'symbol_butterfly_part2.gif', 'symbol_butterfly_part1.gif', 13),
(14, 'symbol_cowskull_part2.gif', 'symbol_cowskull_part1.gif', 14),
(15, 'symbol_cross.gif', '', 15),
(16, 'symbol_diamond.gif', '', 16),
(17, 'symbol_diploma_part2.gif', 'symbol_diploma_part1.gif', 17),
(18, 'symbol_eyeball_part2.gif', 'symbol_eyeball_part1.gif', 18),
(19, 'symbol_fist.gif', '', 19),
(20, 'symbol_flame_1.gif', '', 20),
(21, 'symbol_flame_2.gif', '', 21),
(22, 'symbol_flash.gif', '', 22),
(23, 'symbol_flower_1_part2.gif', 'symbol_flower_1_part1.gif', 23),
(24, 'symbol_flower_2.gif', '', 24),
(25, 'symbol_flower_3.gif', '', 25),
(26, 'symbol_flower_4.gif', '', 26),
(27, 'symbol_football.gif', '', 27),
(28, 'symbol_heart_1_part2.gif', 'symbol_heart_1_part1.gif', 28),
(29, 'symbol_heart_2_part2.gif', 'symbol_heart_2_part1.gif', 29),
(30, 'symbol_jingjang_part2.gif', 'symbol_jingjang_part1.gif', 30),
(31, 'symbol_lips_part2.gif', 'symbol_lips_part1.gif', 31),
(32, 'symbol_note.gif', '', 32),
(33, 'symbol_peace.gif', '', 33),
(34, 'symbol_planet_part2.gif', 'symbol_planet_part1.gif', 34),
(35, 'symbol_rainbow_part2.gif', 'symbol_rainbow_part1.gif', 35),
(36, 'symbol_rosete.gif', '', 36),
(37, 'symbol_shape.gif', '', 37),
(38, 'symbol_star_1.gif', '', 38),
(39, 'symbol_star_2.gif', '', 39),
(40, 'symbol_sword_1_part2.gif', 'symbol_sword_1_part1.gif', 40),
(41, 'symbol_sword_2_part2.gif', 'symbol_sword_2_part1.gif', 41),
(42, 'symbol_sword_3_part2.gif', 'symbol_sword_3_part1.gif', 42),
(43, 'symbol_wings_1.gif', '', 43),
(44, 'symbol_wings_2.gif', '', 44),
(45, 'symbol_arrow_down.gif', '', 45),
(46, 'symbol_arrow_left.gif', '', 46),
(47, 'symbol_arrow_right.gif', '', 47),
(48, 'symbol_arrow_up.gif', '', 48),
(49, 'symbol_arrowbig_up.gif', '', 49),
(50, 'symbol_axe_part2.gif', 'symbol_axe_part1.gif', 50),
(51, 'symbol_bug_part2.gif', 'symbol_bug_part1.gif', 51),
(52, 'symbol_capsbig_part2.gif', 'symbol_capsbig_part1.gif', 52),
(53, 'symbol_capssmall_part2.gif', 'symbol_capssmall_part1.gif', 53),
(54, 'symbol_cloud.gif', '', 54),
(55, 'symbol_crown_part2.gif', 'symbol_crown_part1.gif', 55),
(56, 'symbol_diamsmall2.gif', '', 56),
(57, 'symbol_diamsmall.gif', '', 57),
(58, 'symbol_drop.gif', '', 58),
(59, 'symbol_fingersheavy.gif', '', 59),
(60, 'symbol_fingersv.gif', '', 60),
(61, 'symbol_gtr_part2.gif', 'symbol_gtr_part1.gif', 61),
(62, 'symbol_hat.gif', '', 62),
(63, 'symbol_oval_part2.gif', 'symbol_oval_part1.gif', 63),
(64, 'symbol_pawprint.gif', '', 64),
(65, 'symbol_screw.gif', '', 65),
(66, 'symbol_stickL_part2.gif', 'symbol_stickL_part1.gif', 66),
(67, 'symbol_stickR_part2.gif', 'symbol_stickR_part1.gif', 67),
(104, 'symbol_alligator.gif', '', 68),
(105, 'symbol_americanfootball_part2.gif', 'symbol_americanfootball_part1.gif', 69),
(106, 'symbol_award_part2.gif', 'symbol_award_part1.gif', 70),
(107, 'symbol_bananapeel.gif', '', 71),
(108, 'symbol_battleball.gif', '', 72),
(109, 'symbol_biohazard.gif', '', 73),
(110, 'symbol_bird.gif', '', 74),
(111, 'symbol_bishop.gif', '', 75),
(112, 'symbol_coalion.gif', '', 76),
(113, 'symbol_cocoamug.gif', '', 77),
(115, 'symbol_dashflag.gif', '', 78),
(116, 'symbol_diamondring_part2.gif', 'symbol_diamondring_part1.gif', 79),
(117, 'symbol_discoball_part2.gif', 'symbol_discoball_part1.gif', 80),
(118, 'symbol_dog.gif', '', 81),
(119, 'symbol_electricguitarh_part2.gif', 'symbol_electricguitarh_part1.gif', 82),
(120, 'symbol_electricguitarv_part2.gif', 'symbol_electricguitarv_part1.gif', 83),
(121, 'symbol_film.gif', '', 84),
(122, 'symbol_flame_part2.gif', 'symbol_flame_part1.gif', 85),
(123, 'symbol_gamepad.gif', '', 86),
(124, 'symbol_gem1_part2.gif', 'symbol_gem1_part1.gif', 87),
(125, 'symbol_gem2_part2.gif', 'symbol_gem2_part1.gif', 88),
(126, 'symbol_gem3_part2.gif', 'symbol_gem3_part1.gif', 89),
(127, 'symbol_hawk.gif', '', 90),
(128, 'symbol_hearts_down.gif', '', 91),
(129, 'symbol_hearts_up.gif', '', 92),
(130, 'symbol_horseshoe.gif', '', 93),
(131, 'symbol_inksplatter.gif', '', 94),
(132, 'symbol_leaf.gif', '', 95),
(133, 'symbol_micstand.gif', '', 96),
(134, 'symbol_mirror_part2.gif', 'symbol_mirror_part1.gif', 97),
(135, 'symbol_monkeywrench.gif', '', 98),
(136, 'symbol_note1.gif', '', 99),
(137, 'symbol_note2.gif', '', 100),
(138, 'symbol_note3.gif', '', 101),
(139, 'symbol_nursecross.gif', '', 102),
(140, 'symbol_pencil_part2.gif', 'symbol_pencil_part1.gif', 103),
(142, 'symbol_queen.gif', '', 104),
(143, 'symbol_rock.gif', '', 105),
(144, 'symbol_rook.gif', '', 106),
(145, 'symbol_skate.gif', '', 107),
(146, 'symbol_smallring_part2.gif', 'symbol_smallring_part1.gif', 108),
(147, 'symbol_snowstorm_part2.gif', 'symbol_snowstorm_part1.gif', 109),
(148, 'symbol_sphere.gif', '', 110),
(149, 'symbol_spraycan_part2.gif', 'symbol_spraycan_part1.gif', 111),
(150, 'symbol_stars1.gif', '', 112),
(151, 'symbol_stars2.gif', '', 113),
(152, 'symbol_stars3.gif', '', 114),
(153, 'symbol_stars4.gif', '', 115),
(154, 'symbol_stars5.gif', '', 116),
(156, 'symbol_waterdrop_part2.gif', 'symbol_waterdrop_part1.gif', 117),
(157, 'symbol_wolverine.gif', '', 118),
(68, 'symbol_0.gif', '', 119),
(69, 'symbol_1.gif', '', 120),
(70, 'symbol_2.gif', '', 121),
(71, 'symbol_3.gif', '', 122),
(72, 'symbol_4.gif', '', 123),
(73, 'symbol_5.gif', '', 124),
(74, 'symbol_6.gif', '', 125),
(75, 'symbol_7.gif', '', 126),
(76, 'symbol_8.gif', '', 127),
(77, 'symbol_9.gif', '', 128),
(78, 'symbol_a.gif', '', 129),
(79, 'symbol_b.gif', '', 130),
(80, 'symbol_c.gif', '', 131),
(81, 'symbol_d.gif', '', 132),
(82, 'symbol_e.gif', '', 133),
(83, 'symbol_f.gif', '', 134),
(84, 'symbol_g.gif', '', 135),
(85, 'symbol_h.gif', '', 136),
(86, 'symbol_i.gif', '', 137),
(87, 'symbol_j.gif', '', 138),
(88, 'symbol_k.gif', '', 139),
(89, 'symbol_l.gif', '', 140),
(90, 'symbol_m.gif', '', 141),
(91, 'symbol_n.gif', '', 142),
(92, 'symbol_o.gif', '', 143),
(93, 'symbol_p.gif', '', 144),
(94, 'symbol_q.gif', '', 145),
(95, 'symbol_r.gif', '', 146),
(96, 'symbol_s.gif', '', 147),
(97, 'symbol_t.gif', '', 148),
(98, 'symbol_u.gif', '', 149),
(99, 'symbol_v.gif', '', 150),
(100, 'symbol_w.gif', '', 151),
(101, 'symbol_x.gif', '', 152),
(102, 'symbol_y.gif', '', 153),
(103, 'symbol_z.gif', '', 154),
(141, 'symbol_pixel_part2.gif', 'symbol_pixel_part1.gif', 155),
(114, 'symbol_credit_part2.gif', 'symbol_credit_part1.gif', 156),
(158, 'symbol_hc_part2.gif', 'symbol_hc_part1.gif', 157),
(155, 'symbol_vip_part2.gif', 'symbol_vip_part1.gif', 158);

-- Old symbol id to new symbol id. A symbol Habbo does not have keeps its id unless a Habbo symbol needs it.
DROP TEMPORARY TABLE IF EXISTS migration_166_ids;
CREATE TEMPORARY TABLE migration_166_ids (
    old_id INT NOT NULL,
    new_id INT NOT NULL,
    PRIMARY KEY (old_id)
) ENGINE=InnoDB;

INSERT INTO migration_166_ids (old_id, new_id)
SELECT COALESCE(MIN(IF(i.id = s.id, i.id, NULL)), MIN(i.id)), s.id
FROM groups_items i
JOIN migration_166_symbols s ON s.firstvalue = i.firstvalue
WHERE i.type = 'symbol'
GROUP BY s.id;

INSERT INTO migration_166_ids (old_id, new_id)
WITH RECURSIVE candidates AS (
    SELECT 159 AS id
    UNION ALL
    SELECT id + 1 FROM candidates WHERE id < 999
),
free_ids AS (
    SELECT c.id, ROW_NUMBER() OVER (ORDER BY c.id) AS n
    FROM candidates c
    WHERE NOT EXISTS (SELECT 1 FROM groups_items i WHERE i.type = 'symbol' AND i.id = c.id)
),
displaced AS (
    SELECT i.id, ROW_NUMBER() OVER (ORDER BY i.id) AS n
    FROM groups_items i
    JOIN migration_166_symbols s ON s.id = i.id
    WHERE i.type = 'symbol' AND i.id NOT IN (SELECT old_id FROM migration_166_ids)
)
SELECT d.id, f.id FROM displaced d LEFT JOIN free_ids f ON f.n = d.n;

DELETE FROM migration_166_ids WHERE old_id = new_id;

-- Badge codes are letter + id + two colour digits + one position digit. Symbols 100 to 199 use "t" with id - 100.
DROP TEMPORARY TABLE IF EXISTS migration_166_badges;
CREATE TEMPORARY TABLE migration_166_badges (
    group_id INT UNSIGNED NOT NULL,
    badge VARCHAR(50) NOT NULL,
    PRIMARY KEY (group_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

INSERT INTO migration_166_badges (group_id, badge)
WITH RECURSIVE layers AS (
    SELECT id AS group_id, 1 AS n,
           CAST(REGEXP_SUBSTR(badge, '^[bst][0-9]+') AS CHAR(50)) AS part,
           CAST(SUBSTRING(badge, CHAR_LENGTH(REGEXP_SUBSTR(badge, '^[bst][0-9]+')) + 1) AS CHAR(50)) AS rest
    FROM `groups`
    WHERE badge REGEXP '^([bst][0-9]{4,6})+$'
    UNION ALL
    SELECT group_id, n + 1,
           CAST(REGEXP_SUBSTR(rest, '^[bst][0-9]+') AS CHAR(50)),
           CAST(SUBSTRING(rest, CHAR_LENGTH(REGEXP_SUBSTR(rest, '^[bst][0-9]+')) + 1) AS CHAR(50))
    FROM layers
    WHERE rest <> ''
),
symbols AS (
    SELECT group_id, n, part,
           CAST(SUBSTRING(part, 2, IF(CHAR_LENGTH(part) = 7, 3, 2)) AS UNSIGNED)
               + IF(LEFT(part, 1) = 't', 100, 0) AS old_id,
           IF(CHAR_LENGTH(part) = 5, CONCAT(RIGHT(part, 2), '0'), RIGHT(part, 3)) AS colour_position
    FROM layers
),
parts AS (
    SELECT s.group_id, s.n,
           IF(LEFT(s.part, 1) = 'b', IF(CHAR_LENGTH(s.part) = 5, CONCAT(s.part, '0'), s.part),
              CONCAT(CASE
                         WHEN COALESCE(m.new_id, s.old_id) < 100 THEN CONCAT('s', LPAD(COALESCE(m.new_id, s.old_id), 2, '0'))
                         WHEN COALESCE(m.new_id, s.old_id) < 200 THEN CONCAT('t', LPAD(COALESCE(m.new_id, s.old_id) - 100, 2, '0'))
                         ELSE CONCAT('s', COALESCE(m.new_id, s.old_id))
                     END, s.colour_position)) AS part
    FROM symbols s
    LEFT JOIN migration_166_ids m ON m.old_id = s.old_id AND LEFT(s.part, 1) <> 'b'
)
SELECT group_id, GROUP_CONCAT(part ORDER BY n SEPARATOR '')
FROM parts
GROUP BY group_id;

-- Badge codes and symbol ids change together, so a retry never maps a badge twice.
START TRANSACTION;

UPDATE `groups` g
JOIN migration_166_badges b ON b.group_id = g.id
SET g.badge = b.badge
WHERE g.badge <> b.badge;

UPDATE groups_items i
JOIN migration_166_ids m ON m.old_id = i.id
SET i.id = -m.new_id
WHERE i.type = 'symbol';

UPDATE groups_items i
JOIN migration_166_ids m ON i.id = -m.new_id
SET i.id = m.new_id
WHERE i.type = 'symbol';

COMMIT;

INSERT INTO groups_items (type, id, firstvalue, secondvalue, enabled)
SELECT 'symbol', s.id, s.firstvalue, s.secondvalue, 1
FROM migration_166_symbols s
WHERE NOT EXISTS (SELECT 1 FROM groups_items i WHERE i.type = 'symbol' AND i.id = s.id);

UPDATE groups_items i
JOIN migration_166_symbols s ON s.id = i.id
SET i.order_num = s.order_num
WHERE i.type = 'symbol';

UPDATE groups_items
SET order_num = id
WHERE type IN ('base', 'color2', 'color3');

UPDATE groups_items
SET order_num = CASE id
    WHEN 1 THEN 1 WHEN 11 THEN 2 WHEN 2 THEN 3 WHEN 3 THEN 4 WHEN 4 THEN 5 WHEN 18 THEN 6 WHEN 5 THEN 7 WHEN 19 THEN 8
    WHEN 6 THEN 9 WHEN 16 THEN 10 WHEN 7 THEN 11 WHEN 8 THEN 12 WHEN 9 THEN 13 WHEN 20 THEN 14 WHEN 21 THEN 15 WHEN 10 THEN 16
    WHEN 22 THEN 17 WHEN 17 THEN 18 WHEN 14 THEN 19 WHEN 23 THEN 20 WHEN 15 THEN 21 WHEN 12 THEN 22 WHEN 24 THEN 23 WHEN 13 THEN 24
    ELSE 0
END
WHERE type = 'color';

DROP TEMPORARY TABLE migration_166_badges;
DROP TEMPORARY TABLE migration_166_ids;
DROP TEMPORARY TABLE migration_166_symbols;
