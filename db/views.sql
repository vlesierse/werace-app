-- ============================================
-- WeRace Database Views — AI Foundations
-- Pre-computed views for Phase 2 AI query generation.
-- ============================================

-- v_driver_career_stats: Career summary per driver
CREATE OR REPLACE VIEW v_driver_career_stats AS
SELECT 
    d.id AS driver_id,
    d.driver_ref,
    d.forename,
    d.surname,
    d.nationality,
    COUNT(DISTINCT res.race_id) AS races_entered,
    COUNT(CASE WHEN res.position = 1 THEN 1 END) AS wins,
    COUNT(CASE WHEN res.position <= 3 THEN 1 END) AS podiums,
    COUNT(CASE WHEN res.grid = 1 THEN 1 END) AS pole_positions,
    SUM(res.points) AS total_points,
    MIN(s.year) AS first_season,
    MAX(s.year) AS last_season,
    COUNT(DISTINCT s.year) AS seasons_active
FROM drivers d
JOIN results res ON d.id = res.driver_id
JOIN races r ON res.race_id = r.id
JOIN seasons s ON r.season_id = s.id
GROUP BY d.id, d.driver_ref, d.forename, d.surname, d.nationality;

-- v_constructor_season_stats: Constructor performance per season
CREATE OR REPLACE VIEW v_constructor_season_stats AS
SELECT 
    c.id AS constructor_id,
    c.constructor_ref,
    c.name AS constructor_name,
    s.year AS season_year,
    COUNT(DISTINCT res.race_id) AS races,
    SUM(res.points) AS total_points,
    COUNT(CASE WHEN res.position = 1 THEN 1 END) AS wins,
    COUNT(CASE WHEN res.position <= 3 THEN 1 END) AS podiums
FROM constructors c
JOIN results res ON c.id = res.constructor_id
JOIN races r ON res.race_id = r.id
JOIN seasons s ON r.season_id = s.id
GROUP BY c.id, c.constructor_ref, c.name, s.year;

-- v_race_summary: Complete race summary with winner info
CREATE OR REPLACE VIEW v_race_summary AS
SELECT 
    r.id AS race_id,
    r.name AS race_name,
    r.round,
    r.date AS race_date,
    s.year AS season_year,
    ci.name AS circuit_name,
    ci.country,
    d.forename || ' ' || d.surname AS winner_name,
    c.name AS winner_constructor,
    res.time AS winning_time,
    res.laps AS total_laps
FROM races r
JOIN seasons s ON r.season_id = s.id
JOIN circuits ci ON r.circuit_id = ci.id
LEFT JOIN results res ON r.id = res.race_id AND res.position = 1
LEFT JOIN drivers d ON res.driver_id = d.id
LEFT JOIN constructors c ON res.constructor_id = c.id;

-- v_head_to_head: Head-to-head record between teammates in same constructor/race
CREATE OR REPLACE VIEW v_head_to_head AS
SELECT 
    r1.race_id,
    r1.constructor_id,
    c.name AS constructor_name,
    ra.name AS race_name,
    s.year AS season_year,
    d1.id AS driver1_id,
    d1.forename || ' ' || d1.surname AS driver1_name,
    r1.position_order AS driver1_finish,
    d2.id AS driver2_id,
    d2.forename || ' ' || d2.surname AS driver2_name,
    r2.position_order AS driver2_finish
FROM results r1
JOIN results r2 ON r1.race_id = r2.race_id 
    AND r1.constructor_id = r2.constructor_id 
    AND r1.driver_id < r2.driver_id
JOIN drivers d1 ON r1.driver_id = d1.id
JOIN drivers d2 ON r2.driver_id = d2.id
JOIN constructors c ON r1.constructor_id = c.id
JOIN races ra ON r1.race_id = ra.id
JOIN seasons s ON ra.season_id = s.id;

-- v_circuit_records: Records per circuit (fastest lap, most wins, etc.)
CREATE OR REPLACE VIEW v_circuit_records AS
SELECT 
    ci.id AS circuit_id,
    ci.circuit_ref,
    ci.name AS circuit_name,
    ci.country,
    COUNT(DISTINCT r.id) AS races_held,
    MIN(s.year) AS first_race_year,
    MAX(s.year) AS last_race_year
FROM circuits ci
JOIN races r ON ci.id = r.circuit_id
JOIN seasons s ON r.season_id = s.id
GROUP BY ci.id, ci.circuit_ref, ci.name, ci.country;
