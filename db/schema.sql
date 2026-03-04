-- ============================================
-- WeRace Phase 1 Schema — PostgreSQL
-- Source of truth. Keep in sync with EF Core configurations.
-- ============================================

-- Seasons
CREATE TABLE seasons (
    id              SERIAL PRIMARY KEY,
    year            INT NOT NULL UNIQUE,
    wikipedia_url   TEXT
);

CREATE INDEX idx_seasons_year ON seasons (year);

-- Circuits
CREATE TABLE circuits (
    id              SERIAL PRIMARY KEY,
    circuit_ref     VARCHAR(255) NOT NULL UNIQUE,
    name            VARCHAR(255) NOT NULL,
    location        VARCHAR(255),
    country         VARCHAR(255),
    latitude        DECIMAL(10, 6),
    longitude       DECIMAL(10, 6),
    altitude        INT,
    wikipedia_url   TEXT
);

CREATE INDEX idx_circuits_circuit_ref ON circuits (circuit_ref);

-- Races
CREATE TABLE races (
    id              SERIAL PRIMARY KEY,
    season_id       INT NOT NULL REFERENCES seasons(id),
    round           INT NOT NULL,
    name            VARCHAR(255) NOT NULL,
    circuit_id      INT NOT NULL REFERENCES circuits(id),
    date            DATE NOT NULL,
    time            TIME,
    fp1_date        DATE,
    fp1_time        TIME,
    fp2_date        DATE,
    fp2_time        TIME,
    fp3_date        DATE,
    fp3_time        TIME,
    quali_date      DATE,
    quali_time      TIME,
    sprint_date     DATE,
    sprint_time     TIME,
    wikipedia_url   TEXT,

    UNIQUE (season_id, round)
);

CREATE INDEX idx_races_season_id ON races (season_id);
CREATE INDEX idx_races_circuit_id ON races (circuit_id);
CREATE INDEX idx_races_date ON races (date);

-- Drivers
CREATE TABLE drivers (
    id              SERIAL PRIMARY KEY,
    driver_ref      VARCHAR(255) NOT NULL UNIQUE,
    number          INT,
    code            VARCHAR(3),
    forename        VARCHAR(255) NOT NULL,
    surname         VARCHAR(255) NOT NULL,
    date_of_birth   DATE,
    nationality     VARCHAR(255),
    wikipedia_url   TEXT
);

CREATE INDEX idx_drivers_driver_ref ON drivers (driver_ref);
CREATE INDEX idx_drivers_code ON drivers (code);

-- Constructors
CREATE TABLE constructors (
    id                  SERIAL PRIMARY KEY,
    constructor_ref     VARCHAR(255) NOT NULL UNIQUE,
    name                VARCHAR(255) NOT NULL,
    nationality         VARCHAR(255),
    wikipedia_url       TEXT
);

CREATE INDEX idx_constructors_constructor_ref ON constructors (constructor_ref);

-- Status (race finish statuses)
CREATE TABLE status (
    id      SERIAL PRIMARY KEY,
    status  VARCHAR(255) NOT NULL
);

-- Results (race results)
CREATE TABLE results (
    id                  SERIAL PRIMARY KEY,
    race_id             INT NOT NULL REFERENCES races(id),
    driver_id           INT NOT NULL REFERENCES drivers(id),
    constructor_id      INT NOT NULL REFERENCES constructors(id),
    number              INT,
    grid                INT NOT NULL,
    position            INT,
    position_text       VARCHAR(255) NOT NULL,
    position_order      INT NOT NULL,
    points              DECIMAL(5, 2) NOT NULL DEFAULT 0,
    laps                INT NOT NULL DEFAULT 0,
    time                VARCHAR(255),
    milliseconds        INT,
    fastest_lap         INT,
    rank                INT,
    fastest_lap_time    VARCHAR(255),
    fastest_lap_speed   VARCHAR(255),
    status_id           INT NOT NULL REFERENCES status(id)
);

CREATE INDEX idx_results_race_id ON results (race_id);
CREATE INDEX idx_results_driver_id ON results (driver_id);
CREATE INDEX idx_results_constructor_id ON results (constructor_id);
CREATE INDEX idx_results_race_driver ON results (race_id, driver_id);

-- Qualifying (separate entity)
CREATE TABLE qualifying (
    id                  SERIAL PRIMARY KEY,
    race_id             INT NOT NULL REFERENCES races(id),
    driver_id           INT NOT NULL REFERENCES drivers(id),
    constructor_id      INT NOT NULL REFERENCES constructors(id),
    number              INT NOT NULL,
    position            INT NOT NULL,
    q1                  VARCHAR(255),
    q2                  VARCHAR(255),
    q3                  VARCHAR(255)
);

CREATE INDEX idx_qualifying_race_id ON qualifying (race_id);
CREATE INDEX idx_qualifying_race_driver ON qualifying (race_id, driver_id);

-- Sprint Results (separate table for 2021+ sprint races)
CREATE TABLE sprint_results (
    id                  SERIAL PRIMARY KEY,
    race_id             INT NOT NULL REFERENCES races(id),
    driver_id           INT NOT NULL REFERENCES drivers(id),
    constructor_id      INT NOT NULL REFERENCES constructors(id),
    number              INT,
    grid                INT NOT NULL,
    position            INT,
    position_text       VARCHAR(255) NOT NULL,
    position_order      INT NOT NULL,
    points              DECIMAL(5, 2) NOT NULL DEFAULT 0,
    laps                INT NOT NULL DEFAULT 0,
    time                VARCHAR(255),
    milliseconds        INT,
    fastest_lap         INT,
    fastest_lap_time    VARCHAR(255),
    status_id           INT NOT NULL REFERENCES status(id)
);

CREATE INDEX idx_sprint_results_race_id ON sprint_results (race_id);
CREATE INDEX idx_sprint_results_driver_id ON sprint_results (driver_id);

-- Pit Stops
CREATE TABLE pit_stops (
    race_id         INT NOT NULL REFERENCES races(id),
    driver_id       INT NOT NULL REFERENCES drivers(id),
    stop            INT NOT NULL,
    lap             INT NOT NULL,
    time            TIME,
    duration        VARCHAR(255),
    milliseconds    INT,

    PRIMARY KEY (race_id, driver_id, stop)
);

CREATE INDEX idx_pit_stops_race_id ON pit_stops (race_id);
CREATE INDEX idx_pit_stops_driver_id ON pit_stops (driver_id);

-- Lap Times
CREATE TABLE lap_times (
    race_id         INT NOT NULL REFERENCES races(id),
    driver_id       INT NOT NULL REFERENCES drivers(id),
    lap             INT NOT NULL,
    position        INT,
    time            VARCHAR(255),
    milliseconds    INT,

    PRIMARY KEY (race_id, driver_id, lap)
);

CREATE INDEX idx_lap_times_race_id ON lap_times (race_id);
CREATE INDEX idx_lap_times_driver_id ON lap_times (driver_id);

-- Driver Standings
CREATE TABLE driver_standings (
    id              SERIAL PRIMARY KEY,
    race_id         INT NOT NULL REFERENCES races(id),
    driver_id       INT NOT NULL REFERENCES drivers(id),
    points          DECIMAL(5, 2) NOT NULL DEFAULT 0,
    position        INT,
    position_text   VARCHAR(255),
    wins            INT NOT NULL DEFAULT 0
);

CREATE INDEX idx_driver_standings_race_id ON driver_standings (race_id);
CREATE INDEX idx_driver_standings_driver_id ON driver_standings (driver_id);
CREATE INDEX idx_driver_standings_race_driver ON driver_standings (race_id, driver_id);

-- Constructor Standings
CREATE TABLE constructor_standings (
    id                  SERIAL PRIMARY KEY,
    race_id             INT NOT NULL REFERENCES races(id),
    constructor_id      INT NOT NULL REFERENCES constructors(id),
    points              DECIMAL(5, 2) NOT NULL DEFAULT 0,
    position            INT,
    position_text       VARCHAR(255),
    wins                INT NOT NULL DEFAULT 0
);

CREATE INDEX idx_constructor_standings_race_id ON constructor_standings (race_id);
CREATE INDEX idx_constructor_standings_constructor_id ON constructor_standings (constructor_id);

-- Constructor Results (aggregate race-level constructor data from Jolpica)
CREATE TABLE constructor_results (
    id                  SERIAL PRIMARY KEY,
    race_id             INT NOT NULL REFERENCES races(id),
    constructor_id      INT NOT NULL REFERENCES constructors(id),
    points              DECIMAL(5, 2),
    status              VARCHAR(255)
);

CREATE INDEX idx_constructor_results_race_id ON constructor_results (race_id);
CREATE INDEX idx_constructor_results_constructor_id ON constructor_results (constructor_id);
