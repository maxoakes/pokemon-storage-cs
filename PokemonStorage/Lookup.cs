using System.Data;
using System.Text.Json;
using Google.Protobuf.WellKnownTypes;
using MySql.Data.MySqlClient;
using PokemonStorage.DatabaseIO;
using PokemonStorage.Models;

namespace PokemonStorage;

public enum Gender
{
    MALE = 0,
    FEMALE = 1,
    GENDERLESS = 2
}

public class Lookup
{
    public static Dictionary<int, Game> Games { get; set; } = [];
    public static Dictionary<int, string> Pokemon { get; set; } = [];
    public static Dictionary<int, string> Moves { get; set; } = [];
    public static Dictionary<int, Item> Items { get; set; } = [];
    public static Dictionary<int, string> Abilities { get; set; } = [];
    public static Dictionary<int, AbilityMapping> AbilityMapping { get; set; } = [];
    public static Dictionary<int, string> Natures { get; set; } = [];
    public static Dictionary<int, int> NatureMapping { get; set; } = [];
    public static Dictionary<int, int> GenderRates { get; set; } = [];
    public static Dictionary<int, int> BaseHappiness { get; set; } = [];
    public static Dictionary<int, string> Languages { get; set; } = [];
    public static Dictionary<int, Location> Locations { get; set; } = [];
    public static Dictionary<int, int> PokemonGen1Index { get; set; } = [];
    public static Dictionary<int, int> PokemonGen3Index { get; set; } = [];
    public static Dictionary<int, int> PokemonGen4Index { get; set; } = [];
    public static Dictionary<int, int> GameGen3Index { get; set; } = [];
    public static Dictionary<int, string> CatchBallIndexGen3 { get; set; } = [];

    public static void Initialize()
    {
        // Fill generation pokedex values

        // Gen 1
        PokemonGen1Index = Read<Dictionary<int, int>>(@"Mappings/pokemon_index_gen1.json") ?? [];

        // Gen 3
        PokemonGen3Index = Read<Dictionary<int, int>>(@"Mappings/pokemon_index_gen3.json") ?? [];

        // Gen 4
        DataTable pokemonSpeciesDataTable = DbInterface.RetrieveTable("SELECT id, generation_id, gender_rate, base_happiness FROM pokemon_species");
        foreach (DataRow row in pokemonSpeciesDataTable.Rows)
        {
            int id = row.Field<int>("id");
            int generation_id = row.Field<int>("generation_id");
            int gender_rate = row.Field<int>("gender_rate");
            int base_happiness = row.Field<int>("base_happiness");

            if (generation_id <= 1 && generation_id >= 4) PokemonGen4Index[id] = id;
            GenderRates[id] = gender_rate;
            BaseHappiness[id] = base_happiness;
        }

        // Special generation 4 mappings
        PokemonGen4Index[496] = 386;
        PokemonGen4Index[497] = 386;
        PokemonGen4Index[498] = 386;
        PokemonGen4Index[499] = 413;
        PokemonGen4Index[500] = 413;
        PokemonGen4Index[501] = 487;
        PokemonGen4Index[502] = 492;
        PokemonGen4Index[503] = 479;
        PokemonGen4Index[504] = 479;
        PokemonGen4Index[505] = 479;
        PokemonGen4Index[506] = 479;
        PokemonGen4Index[507] = 479;

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
                vn.local_language_id = 9
            ORDER BY vg.`order`
        """);

        foreach (DataRow row in gameDataTable.Rows)
        {
            int id = row.Field<int>("id");
            int versionGroup = row.Field<int>("version_group_id");
            int generationId = row.Field<int>("generation_id");
            string name = row.Field<string>("name") ?? "";

            Games[id] = new Game(id, name, versionGroup, generationId);
        }

        GameGen3Index = Read<Dictionary<int, int>>(@"Mappings/game_index_gen3.json") ?? [];

        // Moves
        DataTable moveDataTable = DbInterface.RetrieveTable("SELECT id, identifier, generation_id FROM moves");
        foreach (DataRow row in moveDataTable.Rows)
        {
            int id = row.Field<int>("id");
            string identifier = row.Field<string>("identifier") ?? "???";
            int generationId = row.Field<int>("generation_id");
            Moves[id] = identifier;
        }

        // Pokemon Forms
        DataTable formDataTable = DbInterface.RetrieveTable("SELECT id, identifier FROM pokemon_forms");
        foreach (DataRow row in formDataTable.Rows)
        {
            int id = row.Field<int>("id");
            string identifier = row.Field<string>("identifier") ?? "???";
            Pokemon[id] = identifier;
        }

        // Items
        DataTable itemDataTable = DbInterface.RetrieveTable("""
                SELECT 
                    i.id, 
                    i.identifier, 
                    igi.generation_id, 
                    igi.game_index 
                FROM items i 
                    LEFT JOIN item_game_indices igi ON i.id = igi.item_id 
                ORDER BY igi.generation_id, game_index
            """);

        foreach (DataRow row in itemDataTable.Rows)
        {
            int id = row.Field<int>("id");
            string identifier = row.Field<string>("identifier") ?? "???";
            int generationId = row.Field<int>("generation_id");
            int gameIndex = row.Field<int>("game_index");

            if (Items.ContainsKey(id))
                Items[id].IdMapping[generationId] = gameIndex;
            else
                Items[id] = new Item(id, identifier);
        }

        var gen2ItemMapping = Read<Dictionary<int, int>>(@"Mappings/item_index_gen2.json") ?? [];
        foreach (var pair in gen2ItemMapping)
        {
            Items[pair.Value].IdMapping[2] = pair.Key;
        }

        // Abilities
        DataTable abilityDataTable = DbInterface.RetrieveTable("SELECT id, identifier, generation_id FROM abilities");
        foreach (DataRow row in abilityDataTable.Rows)
        {
            int id = row.Field<int>("id");
            string identifier = row.Field<string>("identifier") ?? "???";
            int generationId = row.Field<int>("generation_id");

            Abilities[id] = identifier;
        }
            

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
            WHERE p.id <= 10000
        """);

        foreach (DataRow row in abilityMappingDataTable.Rows)
        {
            int speciesId = row.Field<int>("species_id");
            int abilityId = row.Field<int>("ability_id");
            int slot = row.Field<int>("slot");
            bool isHidden = Convert.ToBoolean(row.Field<Int64>("is_hidden"));

            if (!AbilityMapping.ContainsKey(speciesId))
                AbilityMapping[speciesId] = new AbilityMapping();

            AbilityMapping[speciesId].Assign(abilityId, slot, isHidden);
        }

        // Natures
        DataTable natureDataTable = DbInterface.RetrieveTable("SELECT id, identifier, game_index FROM natures");
        foreach (DataRow row in natureDataTable.Rows)
        {
            int id = row.Field<int>("id");
            string identifier = row.Field<string>("identifier") ?? "???";
            int gameIndex = row.Field<int>("game_index");
            Natures[id] = identifier;
            NatureMapping[gameIndex] = id;
        }

        // Locations
        DataTable locationDataTable = DbInterface.RetrieveTable("""
            SELECT 
                l.id, l.identifier, lgi.generation_id, lgi.game_index 
            FROM
                locations l 
                LEFT JOIN location_game_indices lgi ON l.id=lgi.location_id 
            WHERE 
                l.region_id IS NOT NULL 
            ORDER BY
                l.id
        """);

        foreach (DataRow row in locationDataTable.Rows)
        {
            int id = row.Field<int>("id");
            string identifier = row.Field<string>("identifier") ?? "???";
            int generation_id = row.Field<int?>("generation_id") ?? 0;
            int gameIndex = row.Field<int?>("game_index") ?? 0;

            if (!Locations.ContainsKey(id)) Locations[id] = new Location(id, identifier);
            Locations[id].IdMapping[generation_id] = gameIndex;
        }

        var gen2LocationMapping = Read<Dictionary<int, int>>(@"Mappings/location_index_gen2.json") ?? [];
        foreach (var pair in gen2LocationMapping)
        {
            if (Locations.ContainsKey(pair.Value)) Locations[pair.Value].IdMapping[2] = pair.Key;
        }

        var gen3LocationMapping = Read<Dictionary<int, int>>(@"Mappings/location_index_gen3.json") ?? [];
        foreach (var pair in gen3LocationMapping)
        {
            if (Locations.ContainsKey(pair.Value)) Locations[pair.Value].IdMapping[3] = pair.Key;
        }

        Languages = Read<Dictionary<int, string>>(@"Mappings/language_index.json") ?? [];
        CatchBallIndexGen3 = Read<Dictionary<int, string>>(@"Mappings/catch_ball_index_gen3.json") ?? [];
    }

    public static T? Read<T>(string filePath)
    {
        string text = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<T>(text);
    }

    public static int GetItemIdByIndex(int generation, int gameIndex)
    {
        if (gameIndex != 0)
        {
            foreach (var item in Items.Values)
            {
                if (item.IdMapping.ContainsKey(generation) && item.IdMapping[generation] == gameIndex) return item.Id;
            }
        }
        return 0;
    }

    public static string GetItemName(ushort id)
    {
        if (id == 0) return "";
        return Items.TryGetValue(id, out var item) ? item.Name : "??";
    }

    public static ushort GetSpeciesIdByIndex(int generation, int gameIndex)
    {
        return generation switch
        {
            1 => (ushort)PokemonGen1Index.GetValueOrDefault(gameIndex, 0),
            3 => (ushort)PokemonGen3Index.GetValueOrDefault(gameIndex, 0),
            4 => (ushort)PokemonGen4Index.GetValueOrDefault(gameIndex, 0),
            _ => (ushort)gameIndex
        };
    }

    public static string GetSpeciesName(int id)
    {
        return Pokemon.GetValueOrDefault(id, "???");
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
        var genderRate = GetGenderRate(speciesId);
        return threshold.GetValueOrDefault(genderRate, 255);
    }

    public static int GetGenderRate(int speciesId)
    {
        return GenderRates.GetValueOrDefault(speciesId, -1);
    }
            

    public static byte GetBaseHappiness(uint speciesId)
    {
        return (byte)BaseHappiness.GetValueOrDefault((int)speciesId, 0);
    }
        

    public static (int first, int second) GetAbilities(int speciesId)
    {
        return AbilityMapping.TryGetValue(speciesId, out var mapping) ? mapping.GetAbilities() : (0,0);
    }

    public static string GetAbilityName(int abilityId)
    {
        return Abilities.GetValueOrDefault(abilityId, "???");
    }
        

    public static string GetNatureName(int natureId)
    {
        return Natures.GetValueOrDefault(natureId, "???");
    }
        

    public static int GetNatureIdByIndex(int index)
    {
        return NatureMapping.GetValueOrDefault(index, 0);
    }

    public static string GetLanguageById(int languageId)
    {
        return Languages.GetValueOrDefault(languageId, string.Empty);
    }

    public static string GetLocationNameById(int locationId)
    {
        Location? location = Locations.GetValueOrDefault(locationId);
        if (location != null)
        {
            return location.Name;
        }
        return "???";
    }

    public static int GetLocationIdByIndex(int generation, int gameIndex)
    {
        if (gameIndex != 0)
        {
            foreach (var location in Locations.Values)
            {
                if (location.IdMapping.ContainsKey(generation) && location.IdMapping[generation] == gameIndex)
                {
                    return location.Id;
                }
            }
        }
        return 0;
    }

    public static string GetCatchBallById(int generationId, int index)
    {
        return generationId switch
        {
            3 => CatchBallIndexGen3.GetValueOrDefault(index, "???"),
            _ => "poke-ball"
        };
    }

    public static int GetGameOfOrigin(int generationId, int index)
    {
        return generationId switch
        {
            3 => GameGen3Index.GetValueOrDefault(index, 19),
            _ => 1
        };
    }

    public static StatHextuple GetBaseStats(int speciesId)
    {
        List<MySqlParameter> parameters = [
            new MySqlParameter("Id", MySqlDbType.Int16) { Value = speciesId }
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
            """, parameters);

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

    public static (int increased, int decreased) GetNatureStats(int natureId)
    {
        List<MySqlParameter> parameters = [
            new MySqlParameter("Id", MySqlDbType.Int16) { Value = natureId }
        ];
        DataTable statDataTable = DbInterface.RetrieveTable("SELECT increased_stat_id, decreased_stat_id FROM natures WHERE id = @Id", parameters);

        foreach (DataRow row in statDataTable.Rows)
        {
            return (
                row.Field<int>("increased_stat_id"),
                row.Field<int>("decreased_stat_id")
            );
        }
        return (0, 0);
    }

    public static int GetGrowthRateId(int speciesId)
    {
        List<MySqlParameter> parameters = [
            new MySqlParameter("Id", MySqlDbType.Int16) { Value = speciesId }
        ];
        DataTable statDataTable = DbInterface.RetrieveTable("SELECT growth_rate_id FROM pokemon_species WHERE id=@Id", parameters);

        foreach (DataRow row in statDataTable.Rows)
        {
            return row.Field<int>("growth_rate_id");
        }
        return 0;
    }

    public static byte GetLevelFromExperience(uint speciesId, uint experience)
    {
        List<MySqlParameter> parameters = [
            new MySqlParameter("Id", MySqlDbType.Int16) { Value = speciesId },
            new MySqlParameter("Experience", MySqlDbType.Int32) { Value = experience }
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
        """, parameters);

        foreach (DataRow row in statDataTable.Rows)
        {
            return (byte)Math.Max(row.Field<int>("level")-1, 0);
        }
        return 0;
    }
}
