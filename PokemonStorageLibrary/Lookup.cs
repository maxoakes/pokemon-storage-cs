using System.Data;
using Microsoft.Data.Sqlite;
using PokemonStorageLibrary.Models;

namespace PokemonStorageLibrary;

#region Structs and Enums

public enum LanguageType
{
    Id,
    Iso639,
    Iso3166,
    Identifier
}

public enum DatabaseObject
{
    Abilities,
    Generations,
    Items,
    Languages,
    Locations,
    Moves,
    Natures,
    Types,
    Versions,
    VersionGroups
}

public enum SupplementObject
{
    Balls,
    EncounterMethods,
    GameOrigins,
    Items,
    Languages,
    Locations,
    Pokemon
}

public struct DatabaseIdentity(Int64 id, string identifier, string name)
{
    public ushort Id = (ushort)id;
    public string Identifier = identifier;
    public string Name = name;
}

public enum Gender
{
    MALE = 0,
    FEMALE = 1,
    GENDERLESS = 2
}

public struct Game(long versionId, long generationId, long groupId, string game)
{
    public byte VersionId = (byte)versionId;
    public byte GenerationId = (byte)generationId;
    public byte VersionGroupId = (byte)groupId;
    public string GameName = game;

    public override string ToString()
    {
        return $"{VersionId}: {GameName} (Gen {GenerationId}) (Group {VersionGroupId})";
    }
}

public struct Language(long id, string iso639, string iso3166, string identifier)
{
    public byte Id = (byte)id;
    public string Iso639 = iso639;
    public string Iso3166 = iso3166;
    public string Identifier = identifier;
}

public struct PokemonIdentity(long pokemonId, long speciesId, long formId, string formIdentifier, string speciesIdentifier, string speciesName)
{
    public ushort PokemonId = (ushort)pokemonId;
    public ushort SpeciesId = (ushort)speciesId;
    public ushort FormId = (ushort)formId;
    public string FormIdentifier = formIdentifier;
    public string SpeciesIdentifier = speciesIdentifier;
    public string SpeciesName = speciesName;
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

public struct PokemonType(long slot, long id, string identifier, long generation)
{
    public byte Slot = (byte)slot;
    public byte Id = (byte)id;
    public string Identifier = identifier;
    public int Generation = (int)generation;
}

#endregion

public class Lookup
{
    public static string VeekunConnectionString { get; set; }
    public static string SupplementConnectionString { get; set; }
    public static string DefaultStorageConnectionString { get; set; }
    
    #region Game Char Lookup

    public static string GetDecodedCharacterByGameIndex(int gameIndex, int generation, string language="en", bool standardCharacter=true)
    {
        List<SqliteParameter> parameters = [
            new SqliteParameter("DecValue", SqliteType.Integer) { Value = gameIndex },
            new SqliteParameter("Generation", SqliteType.Integer) { Value = generation },
            new SqliteParameter("Language", SqliteType.Text) { Value = language },
            new SqliteParameter("StandardCharacter", SqliteType.Integer) { Value = standardCharacter ? 1 : 0 }
        ];

        string c = (string)(DbInterface.RetrieveScalar("""
            SELECT character FROM character_encoding 
            WHERE 
                dec_value=@DecValue AND 
                generation=@Generation AND 
                lang_csv LIKE '%' || @Language || '%' 
                AND standard_character=@StandardCharacter
            """, SupplementConnectionString, parameters) ?? "");
        return c;
    }

    public static ushort GetEncodedCharacterByCharacter(char character, int generation, string language="en", bool standardCharacter=true)
    {
        List<SqliteParameter> parameters = [
            new SqliteParameter("Character", SqliteType.Text) { Value = character.ToString() },
            new SqliteParameter("Generation", SqliteType.Integer) { Value = generation },
            new SqliteParameter("Language", SqliteType.Text) { Value = language },
            new SqliteParameter("StandardCharacter", SqliteType.Integer) { Value = standardCharacter ? 1 : 0 }
        ];

        Int64 i = (Int64)DbInterface.RetrieveScalar("""
            SELECT dec_value FROM character_encoding 
            WHERE 
                character=@Character AND 
                generation=@Generation AND 
                lang_csv LIKE '%' || @Language || '%' 
                AND standard_character=@StandardCharacter
            """, SupplementConnectionString, parameters);
        return (ushort)i;
    }

    #endregion

    #region Get by ID

    public static DatabaseIdentity GetDatabaseIdentityById(int id, DatabaseObject databaseObject, int languageId=9)
    {
        List<SqliteParameter> parameters = [
            new SqliteParameter("Id", SqliteType.Integer) { Value = id },
            new SqliteParameter("LanguageId", SqliteType.Integer) { Value = languageId },
        ];
        string query = databaseObject switch
        {
            DatabaseObject.Abilities => 
                "SELECT i.id, i.identifier, n.name FROM abilities i LEFT JOIN ability_names n ON i.id = n.ability_id WHERE n.local_language_id = @LanguageId AND i.id = @Id",
            DatabaseObject.Generations =>
                "SELECT i.id, i.identifier, n.name FROM generations i LEFT JOIN generation_names n ON i.id = n.generation_id WHERE n.local_language_id = @LanguageId AND i.id = @Id",
            DatabaseObject.Items =>
                "SELECT i.id, i.identifier, n.name FROM items i LEFT JOIN item_names n ON i.id = n.item_id WHERE n.local_language_id = @LanguageId AND i.id = @Id",
            DatabaseObject.Languages =>
                "SELECT i.id, i.identifier, n.name FROM languages i LEFT JOIN item_names n WHERE n.local_language_id = @LanguageId AND i.id = @Id",
            DatabaseObject.Locations =>
                "SELECT i.id, i.identifier, n.name FROM locations i LEFT JOIN location_names n ON i.id = n.location_id WHERE n.local_language_id = @LanguageId AND i.id = @Id",
            DatabaseObject.Moves =>
                "SELECT i.id, i.identifier, n.name FROM moves i LEFT JOIN move_names n ON i.id = n.move_id WHERE n.local_language_id = @LanguageId AND i.id = @Id",
            DatabaseObject.Natures =>
                "SELECT i.id, i.identifier, n.name FROM natures i LEFT JOIN nature_names n ON i.id = n.nature_id WHERE n.local_language_id = @LanguageId AND i.id = @Id",
            DatabaseObject.Types =>
                "SELECT i.id, i.identifier, n.name FROM types i LEFT JOIN type_names n ON i.id = n.type_id WHERE n.local_language_id = @LanguageId AND i.id = @Id",
            DatabaseObject.Versions =>
                "SELECT i.id, i.identifier, n.name FROM versions i LEFT JOIN version_names n ON i.id = n.version_id WHERE n.local_language_id = @LanguageId AND i.id = @Id",
            DatabaseObject.VersionGroups =>
                "SELECT i.id, i.identifier, n.name FROM version_groups i LEFT JOIN version_group_names n ON i.id = n.version_group_id WHERE n.local_language_id = @LanguageId AND i.id = @Id",
            _ => 
                "",
        };
        DataRow row = DbInterface.RetrieveSingleRow(query, VeekunConnectionString, parameters);
        if (row == null)
        {
            return new DatabaseIdentity(0, "", "");
        }
        else
        {
            return new DatabaseIdentity(
                row.Field<Int64>("id"),
                row.Field<string>("identifier") ?? "",
                row.Field<string>("name") ?? ""
            );
        }


    }

    #endregion

    #region Get by Identifier

    public static long GetIdByIdentifier(string identifier, DatabaseObject databaseObject)
    {
        List<SqliteParameter> parameters = [
            new SqliteParameter("Identifier", SqliteType.Text) { Value = identifier },
        ];
        string query = databaseObject switch
        {
            DatabaseObject.Abilities => 
                "SELECT id FROM abilities WHERE identifier = @Identifier",
            DatabaseObject.Generations =>
                "SELECT id FROM generations WHERE identifier = @Identifier",
            DatabaseObject.Items =>
                "SELECT id FROM items WHERE identifier = @Identifier",
            DatabaseObject.Languages =>
                "SELECT id FROM languages WHERE identifier = @Identifier",
            DatabaseObject.Locations =>
                "SELECT id FROM locations WHERE identifier = @Identifier",
            DatabaseObject.Moves =>
                "SELECT id FROM moves WHERE identifier = @Identifier",
            DatabaseObject.Natures =>
                "SELECT id FROM natures WHERE identifier = @Identifier",
            DatabaseObject.Types =>
                "SELECT id FROM types WHERE identifier = @Identifier",
            DatabaseObject.Versions =>
                "SELECT id FROM versions WHERE identifier = @Identifier",
            DatabaseObject.VersionGroups =>
                "SELECT id FROM version_groups WHERE identifier = @Identifier",
            _ => 
                "",
        };
        DataRow row = DbInterface.RetrieveSingleRow(query, VeekunConnectionString, parameters);
        if (row == null)
        {
            return 0;
        }
        else
        {
            return row.Field<Int64>("id");
        }
    }

    #endregion

    #region Specific Structs

    public static Game GetGameByName(string inputName)
    {
        List<SqliteParameter> parameters = [
            new SqliteParameter("Name", SqliteType.Text) { Value = inputName }
        ];

        DataRow row = DbInterface.RetrieveSingleRow("""
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
        """, VeekunConnectionString, parameters);

        return new Game(
            (byte)row.Field<Int64>("id"),
            (byte)row.Field<Int64>("generation_id"),
            (byte)row.Field<Int64>("version_group_id"),
            row.Field<string>("name") ?? ""
        );
    }

    public static Game GetGameByVersionId(int versionId, int languageId=9)
    {
        List<SqliteParameter> parameters = [
            new SqliteParameter("Id", SqliteType.Integer) { Value = versionId },
            new SqliteParameter("Language", SqliteType.Integer) { Value = languageId }
        ];

        DataRow row = DbInterface.RetrieveSingleRow("""
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
                v.id LIKE @Id AND vn.local_language_id=@Language
        """, VeekunConnectionString, parameters);

        if (row == null) return new Game(0, 0, 0, "");
        else
        {
            return new Game(
                row.Field<Int64>("id"),
                row.Field<Int64>("generation_id"),
                row.Field<Int64>("version_group_id"),
                row.Field<string>("name") ?? ""
            );
        }
    }

    public static DatabaseIdentity GetEncounterTypeByGameIndex(int gameIndex, int languageId=9)
    {
        List<SqliteParameter> parameters = [
            new SqliteParameter("Id", SqliteType.Integer) { Value = gameIndex },
            new SqliteParameter("Language", SqliteType.Integer) { Value = languageId }
        ];

        DataRow row = DbInterface.RetrieveSingleRow(
            "SELECT * FROM encounter_types_game_index WHERE game_index=@Id AND local_language_id=@Language", 
            SupplementConnectionString, 
            parameters
        );

        if (row == null) return new DatabaseIdentity(0, "", "");
        else
        {
            return new DatabaseIdentity(
                row.Field<Int64>("game_index"),
                row.Field<string>("identifier") ?? "",
                row.Field<string>("name") ?? ""
            );
        }
    }

    public static Language GetLanguageById(int id)
    {
        List<SqliteParameter> parameters = [
            new SqliteParameter("Id", SqliteType.Integer) { Value = id }
        ];

        DataRow row = DbInterface.RetrieveSingleRow("SELECT * FROM languages WHERE id=@Id", VeekunConnectionString, parameters);

        return new Language(
            row.Field<Int64>("id"),
            row.Field<string>("iso639") ?? "",
            row.Field<string>("iso3166") ?? "",
            row.Field<string>("identifier") ?? ""
        );
    }

    public static Language GetLanguageByIdentifier(string identifier, LanguageType languageType)
    {
        List<SqliteParameter> parameters = [
            new SqliteParameter("Id", SqliteType.Text) { Value = identifier }
        ];

        string columnName = languageType switch
        {
            LanguageType.Id => "id",
            LanguageType.Iso639 => "iso639",
            LanguageType.Iso3166 => "iso3166",
            LanguageType.Identifier => "identifier",
            _ => identifier
        };

        DataRow row = DbInterface.RetrieveSingleRow($"SELECT * FROM languages WHERE {columnName}=@Id", VeekunConnectionString, parameters);

        return new Language(
            row.Field<Int64>("id"),
            row.Field<string>("iso639") ?? "",
            row.Field<string>("iso3166") ?? "",
            row.Field<string>("identifier") ?? ""
        );
    }

    public static PokemonType GetPokemonType(ushort pokemonId, int slot)
    {
        List<SqliteParameter> parameters = [
            new SqliteParameter("Id", SqliteType.Integer) { Value = pokemonId },
            new SqliteParameter("Slot", SqliteType.Integer) { Value = slot }
        ];

        DataRow row = DbInterface.RetrieveSingleRow("SELECT pt.pokemon_id, pt.slot, t.id, t.identifier, t.generation_id  FROM pokemon_types pt LEFT JOIN types t ON pt.type_id = t.id WHERE pt.pokemon_id=@Id AND pt.slot=@Slot", VeekunConnectionString, parameters);

        if (row == null) return new PokemonType(slot, 255, "", 1);
        else
        {
            return new PokemonType(
                row.Field<Int64>("slot"),
                row.Field<Int64>("id"),
                row.Field<string>("identifier") ?? "",
                row.Field<Int64>("generation_id")
            );
        }
    }

    public static byte GetGenerationIntroduced(ushort speciesId)
    {
        List<SqliteParameter> parameters = [
            new SqliteParameter("Id", SqliteType.Integer) { Value = speciesId }
        ];

        return (byte)(Int64)DbInterface.RetrieveScalar("SELECT generation_id FROM pokemon_species WHERE id=@Id", VeekunConnectionString, parameters);
    }

    #endregion

    #region Get List

    public static List<string> GetVersionNames(int languageId=9)
    {
        List<SqliteParameter> parameters = [
            new SqliteParameter("LanguageId", SqliteType.Integer) { Value = languageId }
        ];

        DataTable dataTable = DbInterface.RetrieveTable("""
            SELECT 
                v.id,
                v.version_group_id,
                vg.generation_id,
                vn.name
            FROM
                versions v 
                LEFT JOIN version_groups vg ON vg.id = v.version_group_id 
                LEFT JOIN version_names vn ON v.id = vn.version_id
            WHERE vn.local_language_id = @LanguageId AND v.version_group_id < 11
        """, VeekunConnectionString, parameters);

        return dataTable.AsEnumerable().Select(x => x.Field<string>("name") ?? "").ToList();
    }

    public static List<string> GetLanguageNames(string columnName = "name", int languageId=9)
    {
        List<SqliteParameter> parameters = [
            new SqliteParameter("LanguageId", SqliteType.Integer) { Value = languageId }
        ];

        DataTable dataTable = DbInterface.RetrieveTable($"""
            SELECT 
                {columnName}
            FROM
                languages l 
                LEFT JOIN language_names t ON l.id = t.language_id 
            WHERE t.local_language_id = @LanguageId
        """, VeekunConnectionString, parameters);

        return dataTable.AsEnumerable().Select(x => x.Field<string>(columnName) ?? "").ToList();
    }

    #endregion

    #region Get Game Index

    public static ushort GetGameIndexById(int id, SupplementObject supplementObject, int generation = 0)
    {
        List<SqliteParameter> parameters = [
            new SqliteParameter("Id", SqliteType.Integer) { Value = id },
            new SqliteParameter("Generation", SqliteType.Integer) { Value = generation },
        ];

        string query = supplementObject switch
        {
            SupplementObject.Balls => 
                "SELECT game_index FROM catch_ball_game_index WHERE item_index = @Id",
            SupplementObject.GameOrigins =>
                "SELECT game_index FROM game_origin_game_index WHERE version_id = @Id",
            SupplementObject.Items =>
                "SELECT game_index FROM item_game_index WHERE item_id = @Id AND generation = @Generation",
            SupplementObject.Languages =>
                "SELECT game_index FROM language_game_index WHERE language_index = @Id",
            SupplementObject.Locations =>
                "SELECT game_index FROM location_game_index WHERE location_id = @Id AND generation = @Generation",
            SupplementObject.Pokemon =>
                "SELECT game_index FROM pokemon_game_index WHERE form_id = @Id AND generation = @Generation",
            _ => 
                "",
        };
        return Convert.ToUInt16(DbInterface.RetrieveScalar(query, SupplementConnectionString, parameters));
    }

    #endregion

    #region Get By Game Index

    public static ushort GetIdByGameIndex(int id, SupplementObject supplementObject, int generation = 0)
    {
        List<SqliteParameter> parameters = [
            new SqliteParameter("GameIndex", SqliteType.Integer) { Value = id },
            new SqliteParameter("Generation", SqliteType.Integer) { Value = generation },
        ];

        string query = supplementObject switch
        {
            SupplementObject.Balls => 
                "SELECT item_index FROM catch_ball_game_index WHERE game_index=@GameIndex",
            SupplementObject.EncounterMethods =>
                "",
            SupplementObject.GameOrigins =>
                "SELECT version_id FROM game_origin_game_index WHERE game_index=@GameIndex",
            SupplementObject.Items =>
                "SELECT item_id FROM item_game_index WHERE game_index=@GameIndex AND generation=@Generation",
            SupplementObject.Languages =>
                "SELECT language_index FROM language_game_index WHERE game_index=@GameIndex",
            SupplementObject.Locations =>
                "SELECT location_id FROM location_game_index WHERE game_index=@GameIndex AND generation=@Generation",
            SupplementObject.Pokemon =>
                "SELECT form_id FROM pokemon_game_index WHERE game_index=@GameIndex AND generation=@Generation",
            _ => 
                "",
        };
        return Convert.ToUInt16(DbInterface.RetrieveScalar(query, SupplementConnectionString, parameters));
    }

    public static Nature GetNatureByGameIndex(int index)
    {
        List<SqliteParameter> parameters = [
            new SqliteParameter("Id", SqliteType.Integer) { Value = index },
        ];

        DataRow row = DbInterface.RetrieveSingleRow("SELECT * FROM natures WHERE game_index=@Id", VeekunConnectionString, parameters);
        
        return new Nature(
            (int)row.Field<Int64>("id"),
            (int)row.Field<Int64>("decreased_stat_id"),
            (int)row.Field<Int64>("increased_stat_id"),
            (int)row.Field<Int64>("game_index")
        );
    }

    #endregion
    
    #region PokemonIdentity

    public static PokemonIdentity GetPokemonByFormId(ushort formId, int languageId)
    {
        List<SqliteParameter> parameters = [
            new SqliteParameter("Id", SqliteType.Integer) { Value = formId },
            new SqliteParameter("Lang", SqliteType.Integer) { Value = languageId }
        ];

        DataRow row = DbInterface.RetrieveSingleRow(""" 
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
        """, VeekunConnectionString, parameters);

        return new PokemonIdentity(
            row.Field<Int64>("PokemonId"),
            row.Field<Int64>("SpeciesId"),
            row.Field<Int64>("FormId"),
            row.Field<string>("FormIdentifier") ?? "",
            row.Field<string>("SpeciesIdentifier") ?? "",
            row.Field<string>("SpeciesName") ?? "??"
        );
    }

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
        """, VeekunConnectionString, parameters);

        return GetPokemonByFormId((ushort)formId, languageId);
    }

    #endregion
}
