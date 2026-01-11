using System.Data;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace PokemonStorage.Models;

public partial class PartyPokemon
{
    // Game
    public byte Generation { get; set; }
    public byte LanguageId { get; set; }

    // Overview
    public PokemonIdentity PokemonIdentity { get; set; }
    public ushort AlternateFormId { get; set; }
    public uint PersonalityValue { get; set; }
    public Gender Gender { get; set; }
    public bool IsEgg { get; set; }
    public Origin Origin { get; set; }
    public Trainer OriginalTrainer { get; set; }
    public ushort AbilityId { get; set; }
    public string AbilityIdentifier { get { return Lookup.GetIdentifierById("abilities", AbilityId); } }
    public Nature Nature {get { return GetNatureFromPersonalityValue(); } }
    

    // Nickname
    public bool HasNickname { get; set; }
    public string Nickname { get; set; }

    // Status
    public byte Level { get { return Lookup.GetLevelFromExperience(PokemonIdentity.SpeciesId, ExperiencePoints); } }
    public uint ExperiencePoints { get; set; }
    public ushort HeldItemId { get; set; }
    public string HeldItemIdentifier { get { return Lookup.GetIdentifierById("items", HeldItemId); } }
    public byte Friendship { get; set; }
    public byte WalkingMood { get; set; }
    public bool IsShinyPersonalityValue { get {return GetShinyFromPersonalityValue(); } }
    public bool IsShinyAttackIv { get {return GetShinyByIv(); } }

    // Stats
    public StatSet Stats { get; set; }
    public StatSet ModernStatSystem { get; set; }

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


    public PartyPokemon(Game game)
    {
        // Game
        Generation = game.GenerationId;
        LanguageId = Lookup.GetLanguageIdByIdentifier("en");

        // Overview
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
        Stats = new(true, new StatHextuple(), new StatHextuple());
        
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
        Markings = new Markings(Generation, 0);
        Origin = new Origin(game.VersionId);

        ShinyLeaves = 0;
        Gen3Misc = 0;
    }

    #region Getters

    private bool GetShinyByIv()
    {
        return Stats.Attack.Iv > 1 &&
            Stats.Defense.Iv == 10 &&
            Stats.Speed.Iv == 10 &&
            Stats.SpecialAttack.Iv == 10;
    }
    
    private bool GetShinyFromPersonalityValue()
    {
        UInt32 p1 = PersonalityValue / 65536;
        UInt32 p2 = PersonalityValue % 65536;
        UInt32 shinyValue = (uint)(OriginalTrainer.PublicId ^ OriginalTrainer.SecretId ^ p1 ^ p2);
        return shinyValue < 8;
    }

    private Nature GetNatureFromPersonalityValue()
    {
        int pNature = (int)(PersonalityValue % 25);
        return Lookup.GetNatureByGameIndex(pNature);
    }

    private ushort GetAbilityFromSlotId(int slotId)
    {
        var speciesAbilities = Lookup.GetAbilitiesByPokemonId(PokemonIdentity.PokemonId);
        return slotId == 0 ? speciesAbilities.First : speciesAbilities.Second;
    }

    private string GetPersonalityString()
    {
        string binary = Convert.ToString(PersonalityValue, 2).PadLeft(32, '0');
        string[] bytes = Enumerable.Range(0, 4).Select(i => binary.Substring(i * 8, 8)).ToArray();
        return string.Join(" ", bytes);
    }

    public void AssignGenderByAttackIv()
    {
        int ratio = Lookup.GetGenderRateBySpeciesId(PokemonIdentity.SpeciesId);
        Gender = ratio switch
        {
            0 => Gender.MALE,
            8 => Gender.FEMALE,
            -1 => Gender.GENDERLESS,
            _ => Stats.AsOldSystem(Lookup.GetBaseStats(PokemonIdentity.SpeciesId), Level).Attack.Iv <= ratio ? Gender.FEMALE : Gender.MALE,
        };
    }

    private Gender GetGenderByPersonalityValue()
    {
        int pGender = (int)(PersonalityValue % 256);
        int threshold = Lookup.GetGenderThreshold(PokemonIdentity.SpeciesId);
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

    public bool DoesNicknameExist()
    {
        return !string.Equals(PokemonIdentity.SpeciesName.ToLower(), Nickname.ToLower());
    }

    #endregion

    #region Setters

    private static UInt32 GeneratePersonalityValue()
    {
        Random random = new();
        return (uint)random.NextInt64(UInt32.MaxValue);
    }

    #endregion

    #region Print

    public string GetSummaryString()
    {
        return $"{PokemonIdentity.SpeciesName}: Lv.{Level} ({Gender}) ({Nickname}) Nature: {Nature.Identifier}, Item: {HeldItemIdentifier}";
    }

    #endregion
    
    #region Database

    public int InsertIntoDatabase()
    {
        List<SqliteParameterPair> parameters = 
        [
            new SqliteParameterPair("language_id", SqliteType.Integer, LanguageId),
            new SqliteParameterPair("generation_id", SqliteType.Integer, Generation),
            new SqliteParameterPair("species_id", SqliteType.Integer, PokemonIdentity.SpeciesId),
            new SqliteParameterPair("form_id", SqliteType.Integer, AlternateFormId),
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
            new SqliteParameterPair("hp_iv", SqliteType.Integer, Stats.HP.Iv),
            new SqliteParameterPair("hp_ev", SqliteType.Integer, Stats.HP.Ev),
            new SqliteParameterPair("att_iv", SqliteType.Integer, Stats.Attack.Iv),
            new SqliteParameterPair("att_ev", SqliteType.Integer, Stats.Attack.Ev),
            new SqliteParameterPair("def_iv", SqliteType.Integer, Stats.Defense.Iv),
            new SqliteParameterPair("def_ev", SqliteType.Integer, Stats.Defense.Ev),
            new SqliteParameterPair("spe_iv", SqliteType.Integer, Stats.Speed.Iv),
            new SqliteParameterPair("spe_ev", SqliteType.Integer, Stats.Speed.Ev),
            new SqliteParameterPair("spa_iv", SqliteType.Integer, Stats.SpecialAttack.Iv),
            new SqliteParameterPair("spa_ev", SqliteType.Integer, Stats.SpecialAttack.Ev),
            new SqliteParameterPair("spd_iv", SqliteType.Integer, Stats.SpecialDefense.Iv),
            new SqliteParameterPair("spd_ev", SqliteType.Integer, Stats.SpecialDefense.Ev),
            new SqliteParameterPair("coolness", SqliteType.Integer, Coolness),
            new SqliteParameterPair("beauty", SqliteType.Integer, Beauty),
            new SqliteParameterPair("cuteness", SqliteType.Integer, Cuteness),
            new SqliteParameterPair("smartness", SqliteType.Integer, Smartness),
            new SqliteParameterPair("toughness", SqliteType.Integer, Toughness),
            new SqliteParameterPair("sheen", SqliteType.Integer, Sheen),
            new SqliteParameterPair("obedience", SqliteType.Integer, Obedience ? 1 : 0),
            new SqliteParameterPair("markings_mask", SqliteType.Integer, Markings.Bits),
            new SqliteParameterPair("shiny_leaves", SqliteType.Integer, ShinyLeaves),
            new SqliteParameterPair("gen3_misc", SqliteType.Integer, Gen3Misc),
            new SqliteParameterPair("fk_ribbon", SqliteType.Integer, Ribbons.InsertIntoDatabase()),
            new SqliteParameterPair("fk_origin", SqliteType.Integer, Origin.InsertIntoDatabase()),
            new SqliteParameterPair("fk_original_trainer", SqliteType.Integer, OriginalTrainer.GetDatabasePrimaryKey()),          
        ];

        int primaryKey = DbInterface.InsertIntoDatabase("pokemon", parameters, "storage");

        foreach (var move in Moves)
        {
            move.Value.InsertIntoDatabase(primaryKey);
        }
        return primaryKey;
    }

    public void LoadFromDatabase(int primaryKey)
    {
        List<SqliteParameter> parameters = 
        [
            new SqliteParameter("PrimaryKey", SqliteType.Integer) { Value = primaryKey }
        ];

        DataTable dataTable = DbInterface.RetrieveTable($"SELECT * FROM pokemon WHERE id = @PrimaryKey", "storage", parameters);
        if (dataTable.Rows.Count == 0)
        {
            throw new Exception($"No Pokemon found with primary key {primaryKey}");
        }

        foreach (DataRow row in dataTable.Rows)
        {
            LanguageId = (byte)row.Field<Int64>("language_id");
            Generation = (byte)row.Field<Int64>("generation_id");
            PokemonIdentity = Lookup.GetPokemonByFormId((ushort)row.Field<Int64>("species_id"), LanguageId);
            AlternateFormId = (ushort)row.Field<Int64>("form_id");
            PersonalityValue = (uint)row.Field<Int64>("pv");
            Gender = (Gender)row.Field<Int64>("gender");
            IsEgg = row.Field<Int64>("is_egg") == 1;
            AbilityId = (ushort)row.Field<Int64>("ability_id");
            Nickname = row.Field<string>("nickname") ?? PokemonIdentity.SpeciesName;
            HasNickname = row.Field<Int64>("has_nickname") == 1;
            ExperiencePoints = (uint)row.Field<Int64>("experience");
            HeldItemId = (ushort)row.Field<Int64>("held_item_id");
            Friendship = (byte)row.Field<Int64>("friendship");
            WalkingMood = (byte)row.Field<Int64>("walking_mood");
            PokerusStrain = (byte)row.Field<Int64>("pokerus_strain");
            PokerusDaysRemaining = (byte)row.Field<Int64>("pokerus_days_remaining");
            Stats = new(
                row.Field<Int64>("is_modern_stats") == 1, 
                new StatHextuple(
                    (ushort)row.Field<Int64>("hp_iv"),
                    (ushort)row.Field<Int64>("att_iv"),
                    (ushort)row.Field<Int64>("def_iv"),
                    (ushort)row.Field<Int64>("spe_iv"),
                    (ushort)row.Field<Int64>("spa_iv"),
                    (ushort)row.Field<Int64>("spd_iv")
                ), 
                new StatHextuple(
                    (ushort)row.Field<Int64>("hp_ev"),
                    (ushort)row.Field<Int64>("att_ev"),
                    (ushort)row.Field<Int64>("def_ev"),
                    (ushort)row.Field<Int64>("spe_ev"),
                    (ushort)row.Field<Int64>("spa_ev"),
                    (ushort)row.Field<Int64>("spd_ev")
                )
            );
            Coolness = (byte)row.Field<Int64>("coolness");
            Beauty = (byte)row.Field<Int64>("beauty");
            Cuteness = (byte)row.Field<Int64>("cuteness");
            Smartness = (byte)row.Field<Int64>("smartness");
            Toughness = (byte)row.Field<Int64>("toughness");
            Sheen = (byte)row.Field<Int64>("sheen");
            Obedience = row.Field<Int64>("obedience") == 1;
            Markings = new(4, 0)
            {
                Bits = (byte)row.Field<Int64>("markings_mask")
            };
            ShinyLeaves = (byte)row.Field<Int64>("shiny_leaves");
            Gen3Misc = (byte)row.Field<Int64>("gen3_misc");
            Ribbons.LoadFromDatabase((int)row.Field<Int64>("fk_ribbon"));
            Origin.LoadFromDatabase((int)row.Field<Int64>("fk_origin"));
            OriginalTrainer.LoadFromDatabase((int)row.Field<Int64>("fk_original_trainer"));
        };

        List<SqliteParameterPair> moveParameters = 
        [
            new SqliteParameterPair("PrimaryKey", SqliteType.Integer, primaryKey)
        ];

        Int64[] movePrimaryKeys = DbInterface.RetrieveTable($"SELECT id FROM move_set WHERE pokemon_id = @PrimaryKey", "storage", parameters).AsEnumerable().Select(x => x.Field<Int64>("id")).ToArray();
        Moves.Clear();
        for (int i = 0; i < movePrimaryKeys.Length; i++)
        {   
            Move move = new(0, 0, 0, (byte)i);
            move.LoadFromDatabase(movePrimaryKeys[i]);
            Moves.Add(i, move);
        }
    }

    #endregion
}