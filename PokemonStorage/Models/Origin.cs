using System;

namespace PokemonStorage.Models;

public class Origin
{
    // General
    public bool FatefulEncounter { get; set; }
    public byte EncounterTypeId { get; set; }
    public string EncounterTypeIdentifier { get { return "???"; } }
    public byte PokeballId { get; set; }
    public string PokeballIdentifier { get { return Lookup.GetCatchBallById(4, PokeballId); } }
    public int OriginGameId { get; set; }
    // public string OriginGameName { get { return Lookup.Games[OriginGameId].GameName; } }

    // Egg
    public DateTime? EggReceiveDate { get; set; }
    public int EggHatchLocationId { get; set; }
    public string EggHatchLocationIdentifier { get { return Lookup.GetLocationNameById(EggHatchLocationId); } }
    
    // Catch
    public int MetLevel { get; set; }
    public DateTime MetDateTime { get; set; }
    public int MetLocationId { get; set; }
    public string MetLocationIdentifier { get { return Lookup.GetLocationNameById(MetLocationId); } }
    

    public Origin()
    {
        FatefulEncounter = false;
        EncounterTypeId = 0;
        PokeballId = 0;
        OriginGameId = 1;
        EggReceiveDate = DateTime.ParseExact("2000/01/01 00:00:00", "yyyy/MM/dd HH:mm:ss", null);
        EggHatchLocationId = 1;
        MetLevel = 0;
        MetLocationId = 260;
        MetDateTime = DateTime.ParseExact("2000/01/01 00:00:00", "yyyy/MM/dd HH:mm:ss", null);
    }

    public override string ToString()
    {
        return $"Met at Lv.{MetLevel} at {MetLocationId} in game {OriginGameId} via {PokeballIdentifier}";
    }
}
