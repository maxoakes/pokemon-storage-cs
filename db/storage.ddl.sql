CREATE TABLE IF NOT EXISTS original_trainer (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL,
    gender INTEGER NOT NULL DEFAULT 0,
    public_id INTEGER NOT NULL DEFAULT 0,
    secret_id INTEGER NOT NULL DEFAULT 0
);

CREATE TABLE IF NOT EXISTS origin (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    fateful_encounter_id INTEGER NOT NULL DEFAULT 0,
    encounter_type_id INTEGER NOT NULL,
    catch_ball_item_id INTEGER NOT NULL DEFAULT 0,
    origin_version_id INTEGER NOT NULL DEFAULT 0,
    egg_receive_datetime TEXT,
    egg_hatch_location_id INTEGER NOT NULL DEFAULT 0,
    egg_hatch_location_platinum_id INTEGER NOT NULL DEFAULT 0,
    met_level INTEGER NOT NULL DEFAULT 0,
    met_datetime TEXT,
    met_location_id INTEGER NOT NULL DEFAULT 0,
    met_location_platinum_id INTEGER NOT NULL DEFAULT 0
);

CREATE TABLE IF NOT EXISTS stats (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    is_modern INTEGER NOT NULL DEFAULT 0,
    hp_ev INTEGER NOT NULL DEFAULT 0,
    hp_iv INTEGER NOT NULL DEFAULT 0,
    att_ev INTEGER NOT NULL DEFAULT 0,
    att_iv INTEGER NOT NULL DEFAULT 0,
    def_ev INTEGER NOT NULL DEFAULT 0,
    def_iv INTEGER NOT NULL DEFAULT 0,
    spe_ev INTEGER NOT NULL DEFAULT 0,
    spe_iv INTEGER NOT NULL DEFAULT 0,
    spa_ev INTEGER NOT NULL DEFAULT 0,
    spa_iv INTEGER NOT NULL DEFAULT 0,
    spd_ev INTEGER NOT NULL DEFAULT 0,
    spd_iv INTEGER NOT NULL DEFAULT 0
);

CREATE TABLE IF NOT EXISTS pokemon (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    created TEXT DEFAULT (datetime('now','localtime')),
    tag TEXT DEFAULT NULL,
    fk_stats INTEGER REFERENCES stats,
    fk_origin INTEGER REFERENCES origin,
    fk_original_trainer INTEGER REFERENCES original_trainer,    
    language_id INTEGER NOT NULL,
    species_id INTEGER NOT NULL,
    alt_form_id INTEGER NOT NULL DEFAULT 0,
    pv INTEGER NOT NULL DEFAULT 0,
    gender INTEGER NOT NULL,
    is_egg INTEGER NOT NULL DEFAULT 0,
    ability_id INTEGER NOT NULL DEFAULT 0,
    nickname TEXT,
    has_nickname INTEGER NOT NULL DEFAULT 0,
    experience INTEGER NOT NULL DEFAULT 0,
    held_item_id INTEGER NOT NULL DEFAULT 0,
    friendship INTEGER NOT NULL,
    walking_mood INTEGER NOT NULL,
    pokerus_strain INTEGER NOT NULL DEFAULT 0,
    pokerus_days_remaining INTEGER NOT NULL DEFAULT 0,
    coolness INTEGER NOT NULL DEFAULT 0,
    beauty INTEGER NOT NULL DEFAULT 0,
    cuteness INTEGER NOT NULL DEFAULT 0,
    smartness INTEGER NOT NULL DEFAULT 0,
    toughness INTEGER NOT NULL DEFAULT 0,
    sheen INTEGER NOT NULL DEFAULT 0,
    obedience INTEGER NOT NULL DEFAULT 0,
    markings_mask INTEGER NOT NULL DEFAULT 0,
    shiny_leaves_data INTEGER NOT NULL DEFAULT 0,
    gen3_misc_data INTEGER NOT NULL DEFAULT 0,
    ribbon_sinnoh1_data INTEGER NOT NULL DEFAULT 0,
    ribbon_sinnoh2_data INTEGER NOT NULL DEFAULT 0,
    ribbon_hoenn_data INTEGER NOT NULL DEFAULT 0
);

CREATE TABLE IF NOT EXISTS move_set (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    pokemon_id INTEGER REFERENCES pokemon,
    slot_id INTEGER NOT NULL,
    move_id INTEGER NOT NULL,
    move_pp INTEGER NOT NULL,
    times_increased INTEGER NOT NULL DEFAULT 0
);
