using System;

namespace PokemonStorage.Models;

public class Origin
{
    // General
    public bool FatefulEncounter { get; set; }
    public byte EncounterTypeId { get; set; }
    public string EncounterTypeIdentifier { get { return "???"; } }
    public string PokeballIdentifier { get; set; }
    public int OriginGameId { get; set; }
    // public string OriginGameName { get { return Lookup.Games[OriginGameId].GameName; } }

    // Egg
    public DateTime? EggReceiveDate { get; set; }
    public int EggHatchLocation { get; set; }
    public string EggHatchLocationIdentifier { get { return Lookup.GetLocationNameById(EggHatchLocation); } }
    
    // Catch
    public int MetLevel { get; set; }
    public DateTime MetDateTime { get; set; }
    public int MetLocation { get; set; }
    public string MetLocationIdentifier { get { return Lookup.GetLocationNameById(MetLocation); } }
    

    public Origin()
    {
        FatefulEncounter = false;
        EncounterTypeId = 0;
        PokeballIdentifier = "poke-ball";
        OriginGameId = 1;
        EggReceiveDate = DateTime.ParseExact("2000/01/01 00:00:00", "yyyy/MM/dd HH:mm:ss", null);
        EggHatchLocation = 1;
        MetLevel = 0;
        MetLocation = 260;
        MetDateTime = DateTime.ParseExact("2000/01/01 00:00:00", "yyyy/MM/dd HH:mm:ss", null);
    }

    public override string ToString()
    {
        return $"Met at Lv.{MetLevel} at {MetLocation} in game {OriginGameId} via {PokeballIdentifier}";
    }
}
