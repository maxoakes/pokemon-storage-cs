using Microsoft.Data.Sqlite;

namespace PokemonStorage.Models;

public class Origin
{
    // General
    public bool FatefulEncounter { get; set; }
    public byte EncounterTypeId { get; set; }
    public string EncounterTypeIdentifier { get { return Lookup.GetEncounterTypeGameIndex(EncounterTypeId); } }
    public byte PokeballId { get; set; }
    public string PokeballIdentifier { get { return Lookup.GetIdentifierById("items", PokeballId, "veekun"); } }
    public int GameVersionId { get; set; }
    public string GameVersionIdentifier { get { return Lookup.GetIdentifierById("versions", GameVersionId); } }

    // Egg
    public DateTime? EggReceiveDate { get; set; }
    public int EggHatchLocationId { get; set; }
    public string EggHatchLocationIdentifier { get { return Lookup.GetIdentifierById("locations", EggHatchLocationId, "veekun"); } }
    public int EggHatchLocationPlatinumId { get; set; }
    public string EggHatchLocationPlatinumIdentifier { get { return Lookup.GetIdentifierById("locations", EggHatchLocationPlatinumId, "veekun"); } }
    
    // Catch
    public int MetLevel { get; set; }
    public DateTime? MetDateTime { get; set; }
    public int MetLocationId { get; set; }
    public string MetLocationIdentifier { get { return Lookup.GetIdentifierById("locations", MetLocationId, "veekun"); } }
    public int MetLocationPlatinumId { get; set; }
    public string MetLocationPlatinumIdentifier { get { return Lookup.GetIdentifierById("locations", MetLocationPlatinumId, "veekun"); } }
    

    public Origin()
    {
        FatefulEncounter = false;
        EncounterTypeId = 0;
        PokeballId = 0;
        GameVersionId = 0;
        EggReceiveDate = null;
        EggHatchLocationId = 0;
        EggHatchLocationPlatinumId = 0;
        MetLevel = 0;
        MetLocationId = 0;
        MetLocationPlatinumId = 0;
        MetDateTime = null;
    }

    public int InsertIntoDatabase()
    {
        List<SqliteParameterPair> parameterPairs =
        [
            new SqliteParameterPair("fateful_encounter_id", SqliteType.Integer, FatefulEncounter ? 1 : 0),
            new SqliteParameterPair("encounter_type_id", SqliteType.Integer, EncounterTypeId),
            new SqliteParameterPair("catch_ball_item_id", SqliteType.Integer, PokeballId),
            new SqliteParameterPair("origin_version_id", SqliteType.Integer, GameVersionId),
            new SqliteParameterPair("egg_receive_datetime", SqliteType.Text, EggReceiveDate?.ToString("yyyy-MM-dd HH:mm:ss")),
            new SqliteParameterPair("egg_hatch_location_id", SqliteType.Integer, EggHatchLocationId),
            new SqliteParameterPair("egg_hatch_location_platinum_id", SqliteType.Integer, EggHatchLocationPlatinumId),
            new SqliteParameterPair("met_level", SqliteType.Integer, MetLevel),
            new SqliteParameterPair("met_datetime", SqliteType.Text, MetDateTime?.ToString("yyyy-MM-dd HH:mm:ss")),
            new SqliteParameterPair("met_location_id", SqliteType.Integer, MetLocationId),
            new SqliteParameterPair("met_location_platinum_id", SqliteType.Integer, MetLocationPlatinumId)
        ];

        return DbInterface.InsertIntoDatabase("origin", parameterPairs, "storage");
    }

    public override string ToString()
    {
        return $"Met at Lv.{MetLevel} at {MetLocationIdentifier}/{MetLocationPlatinumIdentifier} in game {GameVersionId} via {PokeballIdentifier}";
    }
}
