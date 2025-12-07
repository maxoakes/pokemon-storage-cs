using System.Data;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using PokemonStorage.DatabaseIO;
using PokemonStorage.Models;

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
    public int GenerationId;
    public int VersionId;
    public int GameId;
    public string GameName;

    public override string ToString()
    {
        return $"{VersionId}: {GameName} (Gen {GenerationId})";
    }
}

public struct AbilityMapping
{
    public int First;
    public int Second;
    public int Hidden;

    public AbilityMapping()
    {
        First = 0;
        Second = 0;
        Hidden = 0;
    }

    public void Assign(int value, int slot, bool isHidden)
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
    public int Id;
    public string Identifier;
    public int DecreaseId;
    public int IncreaseId;
    public int GameIndex;
}

public struct PokemonIdentity
{
    public int PokemonId;
    public int SpeciesId;
    public int FormId;
    public string FormIdentifier;
    public string SpeciesIdentifier;
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
                AND special_character=@SpecialCharacter
            """, "supplement", parameters);
        return c;
    }

    public static string GetIdentiferById(string tableName, int id, string connectionString="veekun")
    {
        List<SqliteParameter> parameters = [
            new SqliteParameter("Id", SqliteType.Integer) { Value = id }
        ];

        string value = (string)DbInterface.RetrieveScalar($"SELECT identifer FROM {tableName} WHERE id=@Id", connectionString, parameters);
        return value;
    }

    public static byte GetCatchBallByGameIndex(int gameIndex)
    {
        List<SqliteParameter> parameters = [
            new SqliteParameter("Id", SqliteType.Integer) { Value = gameIndex }
        ];

        byte index = (byte)DbInterface.RetrieveScalar("SELECT item_index FROM catch_ball_game_index WHERE game_index=@Id", "supplement", parameters);
        return index;
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

        byte index = (byte)DbInterface.RetrieveScalar("SELECT version_id FROM game_origin_game_index WHERE game_index=@Id", "supplement", parameters);
        return index;
    }

    public static ushort GetItemIdByGameIndex(int generation, int gameIndex)
    {
        List<SqliteParameter> parameters = [
            new SqliteParameter("GameIndex", SqliteType.Integer) { Value = gameIndex },
            new SqliteParameter("Generation", SqliteType.Integer) { Value = generation },
        ];

        ushort index = (ushort)DbInterface.RetrieveScalar("SELECT item_id FROM item_game_index WHERE game_index=@GameIndex AND generation=@Generation", "supplement", parameters);
        return index;
    }

    public static int GetLanguageIdByGameIndex(int gameIndex)
    {
        List<SqliteParameter> parameters = [
            new SqliteParameter("Id", SqliteType.Integer) { Value = gameIndex }
        ];

        int value = (int)DbInterface.RetrieveScalar("SELECT language_index FROM language_game_index WHERE game_index=@Id", "supplement", parameters);
        return value;
    }

    public static ushort GetLocationIdByGameIndex(int generation, int gameIndex)
    {
        List<SqliteParameter> parameters = [
            new SqliteParameter("GameIndex", SqliteType.Integer) { Value = gameIndex },
            new SqliteParameter("Generation", SqliteType.Integer) { Value = generation },
        ];

        ushort index = (ushort)DbInterface.RetrieveScalar("SELECT location_id FROM location_game_index WHERE game_index=@GameIndex AND generation=@Generation", "supplement", parameters);
        return index;
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

        int value = (int)DbInterface.RetrieveScalar("SELECT gender_rate FROM pokemon_species WHERE id=@Id", "veekun", parameters);
        return value;
    }
            

    public static byte GetBaseHappinessBySpeciesId(uint speciesId)
    {
        List<SqliteParameter> parameters = [
            new SqliteParameter("Id", SqliteType.Integer) { Value = speciesId },
        ];

        byte value = (byte)DbInterface.RetrieveScalar("SELECT base_happiness FROM pokemon_species WHERE id=@Id", "veekun", parameters);
        return value;
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
            int abilityId = row.Field<int>("ability_id");
            int slot = row.Field<int>("slot");
            bool isHidden = Convert.ToBoolean(row.Field<Int64>("is_hidden"));

            abilityMapping.Assign(abilityId, slot, isHidden);
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
            nature.Id = row.Field<int>("id");
            nature.Identifier = row.Field<string>("identifier") ?? "???";
            nature.GameIndex = row.Field<int>("game_index");
            nature.DecreaseId = row.Field<int>("decreased_stat_id");
            nature.IncreaseId = row.Field<int>("increased_stat_id");
        }
        return nature;
    }

    public PokemonIdentity GetPokemonByFormId(int formId, int languageId)
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
            pokemon.FormId = row.Field<int>("FormId");
            pokemon.PokemonId = row.Field<int>("PokemonId");
            pokemon.SpeciesId = row.Field<int>("SpeciesId");
            pokemon.SpeciesIdentifier = row.Field<string>("SpeciesIdentifer") ?? "???";
            pokemon.FormIdentifier = row.Field<string>("FormIdentifier") ?? "";
        }
        return pokemon;
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
            game.GameId = row.Field<int>("id");
            game.VersionId = row.Field<int>("version_group_id");
            game.GenerationId = row.Field<int>("generation_id");
            game.GameName = row.Field<string>("name") ?? "";
        }
        return game;
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
                row.Field<decimal>("hp"),
                row.Field<decimal>("attack"),
                row.Field<decimal>("defense"),
                row.Field<decimal>("special_attack"),
                row.Field<decimal>("special_defense"),
                row.Field<decimal>("speed")
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
            if (row.Field<int>("level") == 100 && row.Field<int>("experience") == experience) return 100;

            return (byte)Math.Max(row.Field<int>("level")-1, 0);
        }
        return 0;
    }
}

#endregion
