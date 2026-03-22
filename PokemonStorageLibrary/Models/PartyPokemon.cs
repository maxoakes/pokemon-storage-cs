using System.Data;
using System.Text;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

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
    public string AbilityIdentifier { get { return Lookup.GetIdentifierById("abilities", AbilityId, Lookup.VeekunConnectionString); } }
    public Nature Nature { get { return GetNatureFromPersonalityValue(); } }
    

    // Nickname
    public bool HasNickname { get; set; }
    public string Nickname { get; set; }

    // Status
    public byte Level { get { return Lookup.GetLevelFromExperience(PokemonIdentity.SpeciesId, ExperiencePoints); } }
    public uint ExperiencePoints { get; set; }
    public ushort HeldItemId { get; set; }
    public string HeldItemIdentifier { get { return Lookup.GetIdentifierById("items", HeldItemId, Lookup.VeekunConnectionString); } }
    public byte Friendship { get; set; }
    public byte WalkingMood { get; set; }
    public bool IsShinyPersonalityValue { get {return GetShinyFromPersonalityValue(); } }
    public bool IsShinyAttackIv { get {return GetShinyByIv(); } }

    // Stats
    public StatStructure Stats { get; set; }

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

    #region Constructors

    public PartyPokemon(Game game)
    {
        // Overview
        Origin = new Origin(game.GenerationId);
        LanguageId = Lookup.GetLanguageIdByIdentifier("en");
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
        Stats = new(true, new StatHextuple(), new StatHextuple(), PokemonIdentity.SpeciesId, Level);
        
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
    }

    public PartyPokemon(Int64 primaryKey)
    {
        List<SqliteParameter> parameters = 
        [
            new SqliteParameter("PrimaryKey", SqliteType.Integer) { Value = primaryKey }
        ];

        DataTable pokemonDataTable = DbInterface.RetrieveTable($"SELECT * FROM pokemon WHERE id = @PrimaryKey", Lookup.StorageConnectionString, parameters);
        if (pokemonDataTable.Rows.Count == 0)
        {
            throw new Exception($"No Pokemon found with primary key {primaryKey}");
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
            Stats = new(row.Field<Int64>("fk_stats"), PokemonIdentity.SpeciesId, Level, Nature);
        };

        List<SqliteParameterPair> moveParameters = 
        [
            new SqliteParameterPair("PrimaryKey", SqliteType.Integer, primaryKey)
        ];

        DataTable movesDataTable = DbInterface.RetrieveTable($"SELECT * FROM move_set WHERE pokemon_id = @PrimaryKey", Lookup.StorageConnectionString, parameters);
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
        var speciesAbilities = Lookup.GetAbilitiesByPokemonId(PokemonIdentity.PokemonId);
        return AbilityNumber == 0 ? speciesAbilities.First : speciesAbilities.Second;
    }

    public ushort GetAbilityNumberFromAbilityId()
    {
        return Lookup.GetAbilityIdByAbilityNumber(PokemonIdentity.PokemonId, AbilityNumber);
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
            _ => Stats.Old.Attack.Iv <= ratio ? Gender.FEMALE : Gender.MALE,
        };
    }

    public Gender GetGenderByPersonalityValue()
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
        return !string.Equals(PokemonIdentity.SpeciesName.ToLower(), Nickname.ToLower(), StringComparison.OrdinalIgnoreCase);
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

    public string GetPokemonShowdownString()
    {
        string genderString = Gender == Gender.GENDERLESS ? "" : $" ({Gender.ToString().Substring(0,1).ToUpper()})";
        string itemString = HeldItemId > 0 ? $" @ {Lookup.GetNameById("item_names", "item_id", HeldItemId, 9, Lookup.VeekunConnectionString)}" : "";
        string basicLine = $"{PokemonIdentity.SpeciesName}{genderString}{itemString}";
        string abilityLine = $"Ability: {Lookup.GetNameById("ability_names", "ability_id", AbilityId, 9, Lookup.VeekunConnectionString)}";
        string levelLine = $"Level: {Level}";
        string shinyLine = $"Shiny: {(IsShinyPersonalityValue ? "Yes": "No")}";
        string evLine = $"EVs: {Stats.Modern.HP.Ev} HP / {Stats.Modern.Attack.Ev} Atk / {Stats.Modern.Defense.Ev} Def / {Stats.Modern.SpecialAttack.Ev} SpA / {Stats.Modern.SpecialDefense.Ev} SpD / {Stats.Modern.Speed.Ev} Spe";
        string ivLine = $"IVs: {Stats.Modern.HP.Iv} HP / {Stats.Modern.Attack.Iv} Atk / {Stats.Modern.Defense.Iv} Def / {Stats.Modern.SpecialAttack.Iv} SpA / {Stats.Modern.SpecialDefense.Iv} SpD / {Stats.Modern.Speed.Iv} Spe";
        string natureLine = $"{Lookup.GetNameById("nature_names", "nature_id", Nature.Id, 9, Lookup.VeekunConnectionString)} Nature";
        List<string>moves = Moves.Values.Select(x => $"- {Lookup.GetNameById("move_names", "move_id", x.Id, 9, Lookup.VeekunConnectionString)}").ToList();
        return $"{basicLine}\n{abilityLine}\n{levelLine}\n{shinyLine}\n{evLine}\n{ivLine}\n{natureLine}\n{string.Join("\n", moves)}";
    }

    #endregion
    
    #region Database

    public int InsertIntoDatabase()
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
        ];

        int primaryKey = DbInterface.InsertIntoDatabase("pokemon", parameters, Lookup.StorageConnectionString);
        Moves.Values.ToList().ForEach(x => x.InsertIntoDatabase(primaryKey));
        return primaryKey;
    }

    #endregion
}