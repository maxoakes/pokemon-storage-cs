using System;
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

    public List<SqliteParameter> GetSqliteParameters()
    {
        return new List<SqliteParameter>
        {
            new SqliteParameter("FatefulEncounter", SqliteType.Integer) { Value = FatefulEncounter ? 1 : 0 },
            new SqliteParameter("EncounterTypeId", SqliteType.Integer) { Value = EncounterTypeId },
            new SqliteParameter("PokeballId", SqliteType.Integer) { Value = PokeballId },
            new SqliteParameter("GameVersionId", SqliteType.Integer) { Value = GameVersionId },
            new SqliteParameter("EggReceiveDate", SqliteType.Text) { Value = EggReceiveDate?.ToString("yyyy-MM-dd HH:mm:ss") ?? "" },
            new SqliteParameter("EggHatchLocationId", SqliteType.Integer) { Value = EggHatchLocationId },
            new SqliteParameter("EggHatchLocationPlatinumId", SqliteType.Integer) { Value = EggHatchLocationPlatinumId },
            new SqliteParameter("MetLevel", SqliteType.Integer) { Value = MetLevel },
            new SqliteParameter("MetDateTime", SqliteType.Text) { Value = MetDateTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "" },
            new SqliteParameter("MetLocationId", SqliteType.Integer) { Value = MetLocationId },
            new SqliteParameter("MetLocationPlatinumId", SqliteType.Integer) { Value = MetLocationPlatinumId }
        };
    }

    public override string ToString()
    {
        return $"Met at Lv.{MetLevel} at {MetLocationIdentifier}/{MetLocationPlatinumIdentifier} in game {GameVersionId} via {PokeballIdentifier}";
    }
}
