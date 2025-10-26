using System;

namespace PokemonStorage.Models;

public class Origin
{
    public bool FatefulEncounter { get; set; }
    public int EggHatchLocation { get; set; }
    public int EggReceiveDate { get; set; }
    public int MetDate { get; set; }
    public int EncounterType { get; set; }
    public string Pokeball { get; set; }
    public int OriginGameId { get; set; }
    public int MetLevel { get; set; }
    public DateTime MetDateTime { get; set; }
    public int CatchLevel { get; set; }
    public string CatchLocation { get; set; }
    public int MetLocation { get; set; }

    public Origin()
    {
        FatefulEncounter = false;
        EggHatchLocation = 1;
        EggReceiveDate = 0;
        EncounterType = 1;
        Pokeball = "poke-ball";
        OriginGameId = 1;
        MetLevel = 1;
        MetLocation = 1;
        MetDateTime = DateTime.ParseExact("2000/01/01 00:00:00", "yyyy/MM/dd HH:mm:ss", null);
    }

    public override string ToString()
    {
        // string locationName = Lookup.GetLocationNameById(MetLocation);
        // string gameName = Lookup.Games[OriginGameId];
        // return $"Met at Lv.{MetLevel} at {locationName} in game {gameName} via {Pokeball}";
        return "";
    }
}
