using System.Data;
using Microsoft.Data.Sqlite;

namespace PokemonStorage;

#region Structs and Enums

public struct BasicDatabaseEntry
{
    public int Id;
    public string Identifier;
    public string Name;
}

public struct Game
{
    public byte GenerationId;
    public byte VersionId;
    public byte GameId;
    public string GameName;

    public override string ToString()
    {
        return $"{VersionId}: {GameName} (Gen {GenerationId})";
    }
}

public struct AbilityMapping
{
    public ushort First;
    public ushort Second;
    public ushort Hidden;

    public AbilityMapping()
    {
        First = 0;
        Second = 0;
        Hidden = 0;
    }

    public void Assign(ushort value, ushort slot, bool isHidden)
    {
        if (isHidden) Hidden = value;
        else
        {
            if (slot == 1) First = value;
            else Second = value;
        }
    }
}

public struct Nature
{
    public byte Id;
    public string Identifier;
    public byte DecreaseId;
    public byte IncreaseId;
    public byte GameIndex;
}

public struct PokemonIdentity
{
    public ushort PokemonId;
    public ushort SpeciesId;
    public ushort FormId;
    public string FormIdentifier;
    public string SpeciesIdentifier;
    public string SpeciesName;
}

public enum Gender
{
    MALE = 0,
    FEMALE = 1,
    GENDERLESS = 2
}

#endregion

#region Static Lookup

public class Lookup
{
    public static string GetEncodedCharacterByGameIndex(int gameIndex, int generation, string language="en", bool standardCharacter=true)
    {
        List<SqliteParameter> parameters = [
            new SqliteParameter("DecValue", SqliteType.Integer) { Value = gameIndex },
            new SqliteParameter("Generation", SqliteType.Integer) { Value = generation },
            new SqliteParameter("Language", SqliteType.Text) { Value = language },
            new SqliteParameter("StandardCharacter", SqliteType.Integer) { Value = standardCharacter ? 1 : 0 }
        ];

        string c = (string)DbInterface.RetrieveScalar("""
            SELECT character FROM character_encoding 
            WHERE 
                dec_value=@DecValue AND 
                generation=@Generation AND 
                lang_csv LIKE '%' || @Language || '%' 
                AND standard_character=@StandardCharacter
            """, "supplement", parameters);
        return c;
    }

    public static string GetIdentifierById(string tableName, int id, string connectionString="veekun")
    {
        List<SqliteParameter> parameters = [
            new SqliteParameter("Id", SqliteType.Integer) { Value = id }
        ];

        string value = (string)DbInterface.RetrieveScalar($"SELECT identifier FROM {tableName} WHERE id=@Id", connectionString, parameters);
        return value;
    }

        public static byte GetLanguageIdByIdentifier(string identifier)
    {
        List<SqliteParameter> parameters = [
            new SqliteParameter("Identifier", SqliteType.Text) { Value = identifier }
        ];

        Int64 value = (Int64)DbInterface.RetrieveScalar("SELECT id FROM languages WHERE identifier=@Identifier", "veekun", parameters);
        return (byte)value;
    }

    public static Game GetGameByName(string inputName)
    {
        List<SqliteParameter> parameters = [
            new SqliteParameter("Name", SqliteType.Text) { Value = inputName }
        ];

        // Get basic game ids and names
        DataTable gameDataTable = DbInterface.RetrieveTable("""
            SELECT 
                v.id,
                v.version_group_id,
                vg.generation_id,
                vn.name
            FROM
                versions v 
                LEFT JOIN version_groups vg ON vg.id = v.version_group_id 
                LEFT JOIN version_names vn ON v.id = vn.version_id 
            WHERE 
                vn.name LIKE @Name
        """, "veekun", parameters);

        Game game = new();
        foreach (DataRow row in gameDataTable.Rows)
        {
            game.GameId = (byte)row.Field<Int64>("id");
            game.VersionId = (byte)row.Field<Int64>("version_group_id");
            game.GenerationId = (byte)row.Field<Int64>("generation_id");
            game.GameName = row.Field<string>("name") ?? "";
        }
        return game;
    }

    public static Game GetGameByVersionId(int versionId)
    {
        List<SqliteParameter> parameters = [
            new SqliteParameter("Id", SqliteType.Integer) { Value = versionId }
        ];

        // Get basic game ids and names
        DataTable gameDataTable = DbInterface.RetrieveTable("""
            SELECT 
                vn.name
            FROM
                versions v 
                LEFT JOIN version_names vn ON v.id = vn.version_id 
            WHERE 
                v.id LIKE @Id
        """, "veekun", parameters);

        Game game = new();
        foreach (DataRow row in gameDataTable.Rows)
        {
            game.GameId = (byte)row.Field<Int64>("id");
            game.VersionId = (byte)row.Field<Int64>("version_group_id");
            game.GenerationId = (byte)row.Field<Int64>("generation_id");
            game.GameName = row.Field<string>("name") ?? "";
        }
        return game;
    }

    #region Pokemon

    public static PokemonIdentity GetPokemonBySpeciesId(int speciesId, int languageId)
    {
        List<SqliteParameter> parameters = [
            new SqliteParameter("Id", SqliteType.Integer) { Value = speciesId },
            new SqliteParameter("Lang", SqliteType.Integer) { Value = languageId }
        ];

        Int64 formId = (Int64)DbInterface.RetrieveScalar(""" 
            SELECT 
                pf.id
            FROM 
                pokemon p  
                LEFT JOIN pokemon_forms pf ON p.id=pf.pokemon_id 
                LEFT JOIN pokemon_species ps ON p.species_id=ps.id
            WHERE ps.id = @Id
            ORDER BY ps."order", pf."order" 
        """, "veekun", parameters);

        PokemonIdentity pokemon = GetPokemonByFormId((ushort)formId, languageId);
        return pokemon;
    }

    public static PokemonIdentity GetPokemonByFormId(ushort formId, int languageId)
    {
        List<SqliteParameter> parameters = [
            new SqliteParameter("Id", SqliteType.Integer) { Value = formId },
            new SqliteParameter("Lang", SqliteType.Integer) { Value = languageId }
        ];

        DataTable table = DbInterface.RetrieveTable(""" 
            SELECT 
                p.id AS PokemonId,
                p.identifier AS PokemonIdenfier,
                pf.id AS FormId,
                pf.form_identifier AS FormIdentifier,
                pfn.form_name AS FormName,
                ps.id AS SpeciesId,
                ps.identifier AS SpeciesIdentifier,
                psn.name AS SpeciesName
            FROM 
                pokemon p  
                LEFT JOIN pokemon_forms pf ON p.id=pf.pokemon_id 
                LEFT JOIN pokemon_form_names pfn ON pf.id=pfn.pokemon_form_id 
                LEFT JOIN pokemon_species ps ON p.species_id=ps.id
                LEFT JOIN pokemon_species_names psn ON ps.id=psn.pokemon_species_id 
            WHERE
                (pfn.local_language_id = @Lang OR pfn.local_language_id IS NULL) AND
                (psn.local_language_id = @Lang OR psn.local_language_id IS NULL) AND
                pf.id = @Id
            ORDER BY p."order", pf."order" 
        """, "veekun", parameters);

        PokemonIdentity pokemon = new();
        foreach (DataRow row in table.Rows)
        {
            pokemon.FormId = (ushort)row.Field<Int64>("FormId");
            pokemon.PokemonId = (ushort)row.Field<Int64>("PokemonId");
            pokemon.SpeciesId = (ushort)row.Field<Int64>("SpeciesId");
            pokemon.SpeciesIdentifier = row.Field<string>("SpeciesIdentifier") ?? "???";
            pokemon.FormIdentifier = row.Field<string>("FormIdentifier") ?? "";
            pokemon.SpeciesName = row.Field<string>("SpeciesName") ?? "???";
        }
        return pokemon;
    }

    public static int GetGenderThreshold(int speciesId)
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
        var genderRate = GetGenderRateBySpeciesId(speciesId);
        return threshold.GetValueOrDefault(genderRate, 255);
    }

    public static int GetGenderRateBySpeciesId(int speciesId)
    {
        List<SqliteParameter> parameters = [
            new SqliteParameter("Id", SqliteType.Integer) { Value = speciesId },
        ];

        Int64 value = (Int64)DbInterface.RetrieveScalar("SELECT gender_rate FROM pokemon_species WHERE id=@Id", "veekun", parameters);
        return (int)value;
    }
            
    public static byte GetBaseHappinessBySpeciesId(uint speciesId)
    {
        List<SqliteParameter> parameters = [
            new SqliteParameter("Id", SqliteType.Integer) { Value = speciesId },
        ];

        Int64 value = (Int64)DbInterface.RetrieveScalar("SELECT base_happiness FROM pokemon_species WHERE id=@Id", "veekun", parameters);
        return (byte)value;
    }
        
    public static AbilityMapping GetAbilitiesByPokemonId(int pokemonId)
    {
        List<SqliteParameter> parameters = [
            new SqliteParameter("Id", SqliteType.Integer) { Value = pokemonId },
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
        """, "veekun", parameters);

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

    public static Nature GetNatureByGameIndex(int index)
    {
        List<SqliteParameter> parameters = [
            new SqliteParameter("Id", SqliteType.Integer) { Value = index },
        ];

        DataTable table = DbInterface.RetrieveTable("SELECT * FROM natures WHERE game_index=@Id", "veekun", parameters);

        Nature nature = new();
        foreach (DataRow row in table.Rows)
        {
            nature.Id = (byte)row.Field<Int64>("id");
            nature.Identifier = row.Field<string>("identifier") ?? "???";
            nature.GameIndex = (byte)row.Field<Int64>("game_index");
            nature.DecreaseId = (byte)row.Field<Int64>("decreased_stat_id");
            nature.IncreaseId = (byte)row.Field<Int64>("increased_stat_id");
        }
        return nature;
    }

    public static StatHextuple GetBaseStats(int speciesId)
    {
        List<SqliteParameter> parameters = [
            new SqliteParameter("Id", SqliteType.Integer) { Value = speciesId }
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
            """, "veekun", parameters);

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

    public static byte GetLevelFromExperience(uint speciesId, uint experience)
    {
        List<SqliteParameter> parameters = [
            new SqliteParameter("Id", SqliteType.Integer) { Value = speciesId },
            new SqliteParameter("Experience", SqliteType.Integer) { Value = experience }
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
                    e.experience > @Experience
                ORDER BY ps.id, `level` 
        """, "veekun", parameters);

        foreach (DataRow row in statDataTable.Rows)
        {
            // Edge case, level 0 and 0 experience
            if (experience == 0) return 1;

            // Edge case, level 100
            if (row.Field<Int64>("level") == 100 && row.Field<Int64>("experience") == experience) return 100;

            return (byte)Math.Max(row.Field<Int64>("level")-1, 0);
        }
        return 0;
    }

    #endregion

    #region Index Mappings

    public static ushort GetPokemonFormIdByGameIndex(int generation, int gameIndex)
    {
        List<SqliteParameter> parameters = [
            new SqliteParameter("GameIndex", SqliteType.Integer) { Value = gameIndex },
            new SqliteParameter("Generation", SqliteType.Integer) { Value = generation },
        ];

        try
        {
            Int64 index = (Int64)DbInterface.RetrieveScalar("SELECT form_id FROM pokemon_game_index WHERE game_index=@GameIndex AND generation=@Generation", "supplement", parameters);
            return (ushort)index;
        }
        catch (NullReferenceException)
        {
            return 0;
        }
    }

    public static byte GetCatchBallByGameIndex(int gameIndex)
    {
        List<SqliteParameter> parameters = [
            new SqliteParameter("Id", SqliteType.Integer) { Value = gameIndex }
        ];

        try
        {
            Int64 index = (Int64)DbInterface.RetrieveScalar("SELECT item_index FROM catch_ball_game_index WHERE game_index=@Id", "supplement", parameters);
            return (byte)index;
        }
        catch (NullReferenceException)
        {
            return 0;
        }
    }

    public static string GetEncounterTypeGameIndex(int gameIndex)
    {
        List<SqliteParameter> parameters = [
            new SqliteParameter("Id", SqliteType.Integer) { Value = gameIndex }
        ];

        string value = (string)DbInterface.RetrieveScalar("SELECT identifier FROM encounter_types_game_index WHERE game_index=@Id", "supplement", parameters);
        return value;
    }

    public static byte GetVersionIdByGameIndex(int gameIndex)
    {
        List<SqliteParameter> parameters = [
            new SqliteParameter("Id", SqliteType.Integer) { Value = gameIndex }
        ];

        try
        {
            Int64 index = (Int64)DbInterface.RetrieveScalar("SELECT version_id FROM game_origin_game_index WHERE game_index=@Id", "supplement", parameters);
            return (byte)index;
        }
        catch (NullReferenceException)
        {
            return 0;
        }
    }

    public static ushort GetItemIdByGameIndex(int generation, int gameIndex)
    {
        List<SqliteParameter> parameters = [
            new SqliteParameter("GameIndex", SqliteType.Integer) { Value = gameIndex },
            new SqliteParameter("Generation", SqliteType.Integer) { Value = generation },
        ];

        try
        {
            Int64 index = (Int64)DbInterface.RetrieveScalar("SELECT item_id FROM item_game_index WHERE game_index=@GameIndex AND generation=@Generation", "supplement", parameters);
            return (ushort)index;
        }
        catch (NullReferenceException)
        {
            return 0;
        }
    }

    public static byte GetLanguageIdByGameIndex(int gameIndex)
    {
        List<SqliteParameter> parameters = [
            new SqliteParameter("Id", SqliteType.Integer) { Value = gameIndex }
        ];

        try
        {
            Int64 value = (Int64)DbInterface.RetrieveScalar("SELECT language_index FROM language_game_index WHERE game_index=@Id", "supplement", parameters);
            return (byte)value;
        }
        catch (NullReferenceException)
        {
            return 0;
        }
    }

    public static ushort GetLocationIdByGameIndex(int generation, int gameIndex)
    {
        List<SqliteParameter> parameters = [
            new SqliteParameter("GameIndex", SqliteType.Integer) { Value = gameIndex },
            new SqliteParameter("Generation", SqliteType.Integer) { Value = generation },
        ];

        try
        {
            Int64 index = (Int64)DbInterface.RetrieveScalar("SELECT location_id FROM location_game_index WHERE game_index=@GameIndex AND generation=@Generation", "supplement", parameters);
            return (ushort)index;
        }
        catch (NullReferenceException)
        {
            return 0;
        }
    }

    #endregion
}
#endregion
