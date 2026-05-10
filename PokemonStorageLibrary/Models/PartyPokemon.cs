using System.Data;
using Microsoft.Data.Sqlite;

namespace PokemonStorageLibrary.Models;

public partial class PartyPokemon
{
    // Game
    public byte LanguageId { get; set; }

    // Overview
    public PokemonIdentity PokemonIdentity { get; set; }
    public ushort AlternateFormId { get; set; }
    public uint PersonalityValue { get; set; }
    public Gender Gender { get; set; }
    public bool IsEgg { get; set; }
    public Origin Origin { get; set; }
    public Trainer OriginalTrainer { get; set; }
    public byte AbilityNumber { get; set; }
    public ushort AbilityId { get; set; }
    public DatabaseIdentity AbilityIdentity { get { return Lookup.GetDatabaseIdentityById(AbilityId, DatabaseObject.Abilities); } }
    public Nature Nature { get { return GetNatureFromPersonalityValue(); } }
    public PokemonType Type1 { get { return Lookup.GetPokemonType(PokemonIdentity.PokemonId, 1); } }
    public PokemonType Type2 { get { return Lookup.GetPokemonType(PokemonIdentity.PokemonId, 2); } }
    

    // Nickname
    public bool HasNickname { get; set; }
    public string Nickname { get; set; }

    // Status
    public byte Level { get { return GetLevelFromExperience(); } }
    public uint ExperiencePoints { get; set; }
    public ushort HeldItemId { get; set; }
    public DatabaseIdentity HeldItemIdentity { get { return Lookup.GetDatabaseIdentityById(HeldItemId, DatabaseObject.Items); } }
    public byte Friendship { get; set; }
    public byte WalkingMood { get; set; }
    public bool IsShinyPersonalityValue { get {return GetShinyFromPersonalityValue(); } }
    public bool IsShinyAttackIv { get {return GetShinyByIv(); } }

    // Stats
    public StatStructure Stats { get; set; }

    // Moves
    public Dictionary<int, Move> Moves { get; set; }
    public uint PokerusDaysRemaining { get; set; }
    public uint PokerusStrain { get; set; }

    // Other
    public uint Coolness { get; set; }
    public uint Beauty { get; set; }
    public uint Cuteness { get; set; }
    public uint Smartness { get; set; }
    public uint Toughness { get; set; }
    public uint Sheen { get; set; }
    public bool Obedience { get; set; }
    public RibbonSet Ribbons { get; set; }
    public Markings Markings { get; set; }
    public byte ShinyLeaves { get; set; }
    public byte Gen3Misc { get; set; }

    // Database Items
    public long DatabasePrimaryKey { get; set; }
    public string DatabaseTag { get; set; }

    #region Constructors

    public PartyPokemon(Game game)
    {
        // Overview
        Origin = new Origin(game.GenerationId);
        LanguageId = (byte)Lookup.GetIdByIdentifier("en", DatabaseObject.Languages);
        OriginalTrainer = new Trainer("???", 0, 0, 0);
        PokemonIdentity = new PokemonIdentity();
        AlternateFormId = 0;
        PersonalityValue = 0;
        IsEgg = false;

        // Nickname
        HasNickname = false;
        Nickname = "";

        // Status
        ExperiencePoints = 0;
        HeldItemId = 0;
        Friendship = 0;
        WalkingMood = 0;

        // Stats
        Stats = new(this, true, new StatHextuple(), new StatHextuple());
        
        Moves = [];
        for (int i = 0; i < 4; i++)
        {
            Moves[i] = new Move(0, 0, 0, (byte)i);
        }

        PokerusDaysRemaining = 0;
        PokerusStrain = 0;

        // Contest attributes
        Coolness = 0;
        Beauty = 0;
        Cuteness = 0;
        Smartness = 0;
        Toughness = 0;
        Sheen = 0;
        Obedience = false;

        Ribbons = new RibbonSet();
        Markings = new Markings(Origin.Game.GenerationId, 0);

        ShinyLeaves = 0;
        Gen3Misc = 0;
        DatabasePrimaryKey = 0;
        DatabaseTag = "";
    }

    public PartyPokemon(Int64 primaryKey, string connectionString)
    {
        List<SqliteParameter> parameters = 
        [
            new SqliteParameter("PokemonId", SqliteType.Integer) { Value = primaryKey }
        ];

        DataTable pokemonDataTable = DbInterface.RetrieveTable("SELECT * FROM pokemon WHERE id = @PokemonId", connectionString, parameters);
        if (pokemonDataTable.Rows.Count == 0)
        {
            throw new KeyNotFoundException($"No Pokemon found with primary key {primaryKey}");
        }

        foreach (DataRow row in pokemonDataTable.Rows)
        {
            LanguageId = (byte)row.Field<Int64>("language_id");
            PokemonIdentity = Lookup.GetPokemonByFormId((ushort)row.Field<Int64>("species_id"), LanguageId);
            AlternateFormId = (ushort)row.Field<Int64>("alt_form_id");
            PersonalityValue = (uint)row.Field<Int64>("pv");
            Gender = (Gender)row.Field<Int64>("gender");
            IsEgg = row.Field<Int64>("is_egg") == 1;
            AbilityId = (ushort)row.Field<Int64>("ability_id");
            AbilityNumber = (byte)GetAbilityNumberFromAbilityId();
            Nickname = row.Field<string>("nickname") ?? PokemonIdentity.SpeciesName;
            HasNickname = row.Field<Int64>("has_nickname") == 1;
            ExperiencePoints = (uint)row.Field<Int64>("experience");
            HeldItemId = (ushort)row.Field<Int64>("held_item_id");
            Friendship = (byte)row.Field<Int64>("friendship");
            WalkingMood = (byte)row.Field<Int64>("walking_mood");
            PokerusStrain = (byte)row.Field<Int64>("pokerus_strain");
            PokerusDaysRemaining = (byte)row.Field<Int64>("pokerus_days_remaining");
            Coolness = (byte)row.Field<Int64>("coolness");
            Beauty = (byte)row.Field<Int64>("beauty");
            Cuteness = (byte)row.Field<Int64>("cuteness");
            Smartness = (byte)row.Field<Int64>("smartness");
            Toughness = (byte)row.Field<Int64>("toughness");
            Sheen = (byte)row.Field<Int64>("sheen");
            Obedience = row.Field<Int64>("obedience") == 1;
            Markings = new(4, (byte)row.Field<Int64>("markings_mask"));
            ShinyLeaves = (byte)row.Field<Int64>("shiny_leaves_data");
            Gen3Misc = (byte)row.Field<Int64>("gen3_misc_data");
            Ribbons = new()
            {
                SinnohSet1 = (uint)row.Field<Int64>("ribbon_sinnoh1_data"),
                SinnohSet2 = (uint)row.Field<Int64>("ribbon_sinnoh2_data"),
                HoennSet = (uint)row.Field<Int64>("ribbon_hoenn_data")
            };
            Origin = new(row.Field<Int64>("fk_origin"));
            OriginalTrainer = new((Int64)row.Field<Int64>("fk_original_trainer"));
            Stats = new(this, row.Field<Int64>("fk_stats"));
            DatabasePrimaryKey = row.Field<Int64>("id");
            DatabaseTag = row.Field<string>("tag") ?? "";
        };

        DataTable movesDataTable = DbInterface.RetrieveTable($"SELECT * FROM move_set WHERE pokemon_id = @PokemonId", connectionString, parameters);
        Moves = [];
        foreach (DataRow row in movesDataTable.Rows)
        {
            int slot = (int)row.Field<Int64>("slot_id");
            Moves[slot] = new(
                (ushort)row.Field<Int64>("move_id"),
                (byte)row.Field<Int64>("move_pp"),
                (byte)row.Field<Int64>("times_increased"),
                (byte)row.Field<Int64>("slot_id")
            );
        }
    }

    #endregion

    #region Getters

    private bool GetShinyByIv()
    {
        return Stats.Old.Attack.Iv > 1 &&
            Stats.Old.Defense.Iv == 10 &&
            Stats.Old.Speed.Iv == 10 &&
            Stats.Old.SpecialAttack.Iv == 10;
    }
    
    private bool GetShinyFromPersonalityValue()
    {
        UInt32 p1 = PersonalityValue / 65536;
        UInt32 p2 = PersonalityValue % 65536;
        UInt32 shinyValue = (uint)(OriginalTrainer.PublicId ^ OriginalTrainer.SecretId ^ p1 ^ p2);
        return shinyValue < 8;
    }

    public Nature GetNatureFromPersonalityValue()
    {
        int pNature = (int)(PersonalityValue % 25);
        return Lookup.GetNatureByGameIndex(pNature);
    }

    public ushort GetAbilityIdFromAbilityNumber()
    {
        var speciesAbilities = GetPossibleAbilities();
        return AbilityNumber == 0 ? speciesAbilities.First : speciesAbilities.Second;
    }

    public ushort GetAbilityNumberFromAbilityId()
    {
        return GetAbilityIdByAbilityNumber(AbilityNumber);
    }

    public void AssignGenderByAttackIv()
    {
        int ratio = GetGenderRate();
        Gender = ratio switch
        {
            0 => Gender.MALE,
            8 => Gender.FEMALE,
            -1 => Gender.GENDERLESS,
            _ => Stats.Old.Attack.Iv <= ratio ? Gender.FEMALE : Gender.MALE,
        };
    }

    public Gender GetGenderByPersonalityValue()
    {
        int pGender = (int)(PersonalityValue % 256);
        int threshold = GetGenderThreshold();
        if (threshold == 0)
            return Gender.MALE;
        else if (threshold == 254)
            return Gender.FEMALE;
        else if (threshold == 255)
            return Gender.GENDERLESS;
        else
        {
            if (pGender >= threshold)
                return Gender.MALE;
            else
                return Gender.FEMALE;
        }
    }

    public int GetGenderThreshold()
    {
        var threshold = new Dictionary<int, int>
        {
            [0] = 0,
            [1] = 31,
            [2] = 63,
            [4] = 127,
            [6] = 191,
            [7] = 225,
            [8] = 254,
            [-1] = 255
        };

        // key/8 chance of being female
        var genderRate = GetGenderRate();
        return threshold.GetValueOrDefault(genderRate, 255);
    }


    public int GetGenderRate()
    {
        List<SqliteParameter> parameters = [
            new SqliteParameter("Id", SqliteType.Integer) { Value = PokemonIdentity.SpeciesId },
        ];

        Int64 value = (Int64)DbInterface.RetrieveScalar("SELECT gender_rate FROM pokemon_species WHERE id=@Id", Lookup.VeekunConnectionString, parameters);
        return (int)value;
    }
            
    public byte GetBaseHappiness()
    {
        List<SqliteParameter> parameters = [
            new SqliteParameter("Id", SqliteType.Integer) { Value = PokemonIdentity.SpeciesId },
        ];

        Int64 value = (Int64)DbInterface.RetrieveScalar("SELECT base_happiness FROM pokemon_species WHERE id=@Id", Lookup.VeekunConnectionString, parameters);
        return (byte)value;
    }
        
    public AbilityMapping GetPossibleAbilities()
    {
        List<SqliteParameter> parameters = [
            new SqliteParameter("Id", SqliteType.Integer) { Value = PokemonIdentity.PokemonId },
        ];

        DataTable abilityMappingDataTable = DbInterface.RetrieveTable("""
            SELECT 
                ps.id AS species_id, 
                pa.ability_id, 
                pa.slot, 
                pa.is_hidden 
            FROM 
                pokemon p 
                LEFT JOIN pokemon_species ps ON p.species_id = ps.id 
                LEFT JOIN pokemon_abilities pa ON p.id=pa.pokemon_id 
            WHERE p.id=@Id
        """, Lookup.VeekunConnectionString, parameters);

        AbilityMapping abilityMapping = new();
        foreach (DataRow row in abilityMappingDataTable.Rows)
        {
            Int64 abilityId = row.Field<Int64>("ability_id");
            Int64 slot = row.Field<Int64>("slot");
            bool isHidden = Convert.ToBoolean(row.Field<Int64>("is_hidden"));

            abilityMapping.Assign((ushort)abilityId, (ushort)slot, isHidden);
        }

        return abilityMapping;
    }

    public ushort GetAbilityIdByAbilityNumber(byte slotId)
    {
        List<SqliteParameter> parameters = [
            new SqliteParameter("PokemonId", SqliteType.Integer) { Value = PokemonIdentity.PokemonId },
            new SqliteParameter("SlotId", SqliteType.Integer) { Value = slotId + 1 },
        ];

        try
        {
            Int64 index = (Int64)DbInterface.RetrieveScalar("SELECT ability_id FROM pokemon_abilities WHERE pokemon_id=@PokemonId AND slot=@SlotId", Lookup.VeekunConnectionString, parameters);
            return (ushort)index;
        }
        catch (NullReferenceException)
        {
            return 0;
        }
    }

    public StatHextuple GetBaseStats()
    {
        List<SqliteParameter> parameters = [
            new SqliteParameter("Id", SqliteType.Integer) { Value = PokemonIdentity.SpeciesId }
        ];

        DataTable statDataTable = DbInterface.RetrieveTable("""
                SELECT 
                    ps.id,
                    SUM(CASE WHEN ps2.stat_id=1 THEN ps2.base_stat ELSE 0 END) AS hp,
                    SUM(CASE WHEN ps2.stat_id=2 THEN ps2.base_stat ELSE 0 END) AS attack,
                    SUM(CASE WHEN ps2.stat_id=3 THEN ps2.base_stat ELSE 0 END) AS defense,
                    SUM(CASE WHEN ps2.stat_id=4 THEN ps2.base_stat ELSE 0 END) AS special_attack,
                    SUM(CASE WHEN ps2.stat_id=5 THEN ps2.base_stat ELSE 0 END) AS special_defense,
                    SUM(CASE WHEN ps2.stat_id=6 THEN ps2.base_stat ELSE 0 END) AS speed
                FROM 
                    pokemon p 
                    LEFT JOIN pokemon_species ps ON ps.id=p.species_id 
                    LEFT JOIN pokemon_stats ps2 ON ps2.pokemon_id=p.id
                WHERE p.id < 10000 AND ps.id = @Id
                GROUP BY ps.id
            """, Lookup.VeekunConnectionString, parameters);

        foreach (DataRow row in statDataTable.Rows)
        {
            return new StatHextuple(
                (ushort)row.Field<Int64>("hp"),
                (ushort)row.Field<Int64>("attack"),
                (ushort)row.Field<Int64>("defense"),
                (ushort)row.Field<Int64>("special_attack"),
                (ushort)row.Field<Int64>("special_defense"),
                (ushort)row.Field<Int64>("speed")
            );
        }
        return new StatHextuple();
    }

    public byte GetLevelFromExperience()
    {
        List<SqliteParameter> parameters = [
            new SqliteParameter("Id", SqliteType.Integer) { Value = PokemonIdentity.SpeciesId },
            new SqliteParameter("Experience", SqliteType.Integer) { Value = ExperiencePoints }
        ];

        DataTable statDataTable = DbInterface.RetrieveTable("""
                SELECT 
                    e.level,
                    e.experience,
                    ps.id,
                    ps.identifier 
                FROM 
                    experience e 
                    LEFT JOIN pokemon_species ps ON ps.growth_rate_id=e.growth_rate_id
                WHERE 
                    ps.id = @Id AND
                    e.experience >= @Experience
                ORDER BY ps.id, `level` 
        """, Lookup.VeekunConnectionString, parameters);

        foreach (DataRow row in statDataTable.Rows)
        {
            // Edge case, level 0 and 0 experience
            if (ExperiencePoints == 0) return 1;

            // Edge case, level 100
            if (row.Field<Int64>("level") == 100 && row.Field<Int64>("experience") == ExperiencePoints) return 100;

            return (byte)Math.Max(row.Field<Int64>("level")-1, 0);
        }
        return 0;
    }

    public Int64 GetNationalDexNumber()
    {
        List<SqliteParameter> parameters = [
            new SqliteParameter("Id", SqliteType.Integer) { Value = PokemonIdentity.SpeciesId }
        ];

        return (long)DbInterface.RetrieveScalar("SELECT * FROM pokemon_dex_numbers WHERE pokedex_id=1 AND species_id=@Id", Lookup.VeekunConnectionString, parameters);
    }

    public byte GetCatchRate()
    {
        List<SqliteParameter> parameters = [
            new SqliteParameter("Id", SqliteType.Integer) { Value = PokemonIdentity.SpeciesId }
        ];

        return (byte)(Int64)DbInterface.RetrieveScalar("SELECT capture_rate FROM pokemon_species WHERE id=@Id", Lookup.VeekunConnectionString, parameters);
    }

    public bool DoesSpeciesHaveGenderDifference()
    {
        List<SqliteParameter> parameters = [
            new SqliteParameter("Id", SqliteType.Integer) { Value = PokemonIdentity.SpeciesId }
        ];

        return (Int64)DbInterface.RetrieveScalar("SELECT has_gender_differences FROM pokemon_species WHERE id=@Id", Lookup.VeekunConnectionString, parameters) == 1;
    }

    public bool DoesNicknameExist()
    {
        return !string.Equals(PokemonIdentity.SpeciesName.ToLower(), Nickname.ToLower(), StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Setters

    public void AssignRandomPersonalityValue()
    {
        Random random = new();
        PersonalityValue = (uint)random.NextInt64(UInt32.MaxValue);
    }

    public void SetAbilityFromPersonalityValue()
    {
        AbilityNumber = (byte)(PersonalityValue % 1);
        AbilityId = GetAbilityIdByAbilityNumber(AbilityNumber);
    }

    #endregion

    #region Print

    public string GetGenderCharacter()
    {
        return Gender switch
        {
            Gender.MALE => "♂",
            Gender.FEMALE => "♀",
            _ => "",
        };
    }

    public string GetPokemonShowdownString()
    {
        string genderString = Gender == Gender.GENDERLESS ? "" : $" ({Gender.ToString().Substring(0,1).ToUpper()})";
        string itemString = HeldItemId > 0 ? $" @ {HeldItemIdentity.Name}" : "";
        string basicLine = $"{PokemonIdentity.SpeciesName}{genderString}{itemString}";
        string abilityLine = $"Ability: {AbilityIdentity.Name}";
        string levelLine = $"Level: {Level}";
        string shinyLine = $"Shiny: {(IsShinyPersonalityValue ? "Yes": "No")}";
        string evLine = $"EVs: {Stats.Modern.HP.Ev} HP / {Stats.Modern.Attack.Ev} Atk / {Stats.Modern.Defense.Ev} Def / {Stats.Modern.SpecialAttack.Ev} SpA / {Stats.Modern.SpecialDefense.Ev} SpD / {Stats.Modern.Speed.Ev} Spe";
        string ivLine = $"IVs: {Stats.Modern.HP.Iv} HP / {Stats.Modern.Attack.Iv} Atk / {Stats.Modern.Defense.Iv} Def / {Stats.Modern.SpecialAttack.Iv} SpA / {Stats.Modern.SpecialDefense.Iv} SpD / {Stats.Modern.Speed.Iv} Spe";
        string natureLine = Nature != null ? $"{Lookup.GetDatabaseIdentityById(Nature.Id, DatabaseObject.Natures)} Nature" : "Serious";
        List<string>moves = Moves.Values.Select(x => $"- {x.Identity.Name}").ToList();
        return $"{basicLine}\n{abilityLine}\n{levelLine}\n{shinyLine}\n{evLine}\n{ivLine}\n{natureLine}\n{string.Join("\n", moves)}";
    }

    #endregion
    
    #region Database

    public int InsertIntoDatabase(string connectionString)
    {
        List<SqliteParameterPair> parameters = 
        [
            new SqliteParameterPair("language_id", SqliteType.Integer, LanguageId),
            new SqliteParameterPair("species_id", SqliteType.Integer, PokemonIdentity.SpeciesId),
            new SqliteParameterPair("alt_form_id", SqliteType.Integer, AlternateFormId),
            new SqliteParameterPair("pv", SqliteType.Integer, PersonalityValue),
            new SqliteParameterPair("gender", SqliteType.Integer, (int)Gender),
            new SqliteParameterPair("is_egg", SqliteType.Integer, IsEgg ? 1 : 0),
            new SqliteParameterPair("ability_id", SqliteType.Integer, AbilityId),
            new SqliteParameterPair("nickname", SqliteType.Text, Nickname),
            new SqliteParameterPair("has_nickname", SqliteType.Integer, HasNickname ? 1 : 0),
            new SqliteParameterPair("experience", SqliteType.Integer, ExperiencePoints),
            new SqliteParameterPair("held_item_id", SqliteType.Integer, HeldItemId),
            new SqliteParameterPair("friendship", SqliteType.Integer, Friendship),
            new SqliteParameterPair("walking_mood", SqliteType.Integer, WalkingMood),
            new SqliteParameterPair("pokerus_strain", SqliteType.Integer, PokerusStrain),
            new SqliteParameterPair("pokerus_days_remaining", SqliteType.Integer, PokerusDaysRemaining),
            new SqliteParameterPair("coolness", SqliteType.Integer, Coolness),
            new SqliteParameterPair("beauty", SqliteType.Integer, Beauty),
            new SqliteParameterPair("cuteness", SqliteType.Integer, Cuteness),
            new SqliteParameterPair("smartness", SqliteType.Integer, Smartness),
            new SqliteParameterPair("toughness", SqliteType.Integer, Toughness),
            new SqliteParameterPair("sheen", SqliteType.Integer, Sheen),
            new SqliteParameterPair("obedience", SqliteType.Integer, Obedience ? 1 : 0),
            new SqliteParameterPair("markings_mask", SqliteType.Integer, Markings.AsGen4Byte()),
            new SqliteParameterPair("shiny_leaves_data", SqliteType.Integer, ShinyLeaves),
            new SqliteParameterPair("gen3_misc_data", SqliteType.Integer, Gen3Misc),
            new SqliteParameterPair("ribbon_sinnoh1_data", SqliteType.Integer, Ribbons.SinnohSet1),
            new SqliteParameterPair("ribbon_sinnoh2_data", SqliteType.Integer, Ribbons.SinnohSet2),
            new SqliteParameterPair("ribbon_hoenn_data", SqliteType.Integer, Ribbons.HoennSet),
            new SqliteParameterPair("fk_stats", SqliteType.Integer, Stats.InsertIntoDatabase()),
            new SqliteParameterPair("fk_origin", SqliteType.Integer, Origin.InsertIntoDatabase()),
            new SqliteParameterPair("fk_original_trainer", SqliteType.Integer, OriginalTrainer.GetDatabasePrimaryKeyAndInsertIfNotExist()),
            new SqliteParameterPair("tag", SqliteType.Text, DatabaseTag),
        ];

        int primaryKey = DbInterface.InsertIntoDatabase("pokemon", parameters, connectionString);
        Moves.Values.ToList().ForEach(x => x.InsertIntoDatabase(primaryKey));
        return primaryKey;
    }

    #endregion
}