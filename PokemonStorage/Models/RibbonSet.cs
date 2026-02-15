using System.Data;
using Microsoft.Data.Sqlite;

namespace PokemonStorage.Models;

public class RibbonSet
{
    public uint SinnohSet1 { get; set; }
    public uint SinnohSet2 { get; set; }
    public uint HoennSet { get; set; }

    // Sinnoh Set 1
    public bool SinnohChamp { get { return (SinnohSet1 & 0x01) > 0; } }
    public bool Ability { get { return (SinnohSet1 & 0x02) > 0; } }
    public bool GreatAbility { get { return (SinnohSet1 & 0x04) > 0; } }
    public bool DoubleAbility { get { return (SinnohSet1 & 0x08) > 0; } }
    public bool MultiAbility { get { return (SinnohSet1 & 0x10) > 0; } }
    public bool PairAbility { get { return (SinnohSet1 & 0x20) > 0; } }
    public bool WorldAbility { get { return (SinnohSet1 & 0x40) > 0; } }
    public bool Alert { get { return (SinnohSet1 & 0x80) > 0; } }
    public bool Shock { get { return (SinnohSet1 & 0x100) > 0; } }
    public bool Downcast { get { return (SinnohSet1 & 0x200) > 0; } }
    public bool Careless { get { return (SinnohSet1 & 0x400) > 0; } }
    public bool Relax { get { return (SinnohSet1 & 0x800) > 0; } }
    public bool Snooze { get { return (SinnohSet1 & 0x1000) > 0; } }
    public bool Smile { get { return (SinnohSet1 & 0x2000) > 0; } }
    public bool Gorgeous { get { return (SinnohSet1 & 0x4000) > 0; } }
    public bool Royal { get { return (SinnohSet1 & 0x8000) > 0; } }
    public bool GorgeousRoyal { get { return (SinnohSet1 & 0x10000) > 0; } }
    public bool Footprint { get { return (SinnohSet1 & 0x20000) > 0; } }
    public bool Record { get { return (SinnohSet1 & 0x40000) > 0; } }
    public bool History { get { return (SinnohSet1 & 0x80000) > 0; } }
    public bool Legend { get { return (SinnohSet1 & 0x100000) > 0; } }
    public bool Red { get { return (SinnohSet1 & 0x200000) > 0; } }
    public bool Green { get { return (SinnohSet1 & 0x400000) > 0; } }
    public bool Blue { get { return (SinnohSet1 & 0x800000) > 0; } }
    public bool Festival { get { return (SinnohSet1 & 0x1000000) > 0; } }
    public bool Carnival { get { return (SinnohSet1 & 0x2000000) > 0; } }
    public bool Classic { get { return (SinnohSet1 & 0x4000000) > 0; } }
    public bool Premier { get { return (SinnohSet1 & 0x8000000) > 0; } }

    // Sinnoh Set 2
    public bool SinnohCool { get { return (SinnohSet2 & 0x01) > 0; } }
    public bool SinnohCoolGreat { get { return (SinnohSet2 & 0x02) > 0; } }
    public bool SinnohCoolUltra { get { return (SinnohSet2 & 0x04) > 0; } }
    public bool SinnohCoolMaster { get { return (SinnohSet2 & 0x08) > 0; } } 
    public bool SinnohBeauty { get { return (SinnohSet2 & 0x10) > 0; } }
    public bool SinnohBeautyGreat { get { return (SinnohSet2 & 0x20) > 0; } }
    public bool SinnohBeautyUltra { get { return (SinnohSet2 & 0x40) > 0; } }
    public bool SinnohBeautyMaster { get { return (SinnohSet2 & 0x80) > 0; } }
    public bool SinnohCute { get { return (SinnohSet2 & 0x100) > 0; } }
    public bool SinnohCuteGreat { get { return (SinnohSet2 & 0x200) > 0; } }
    public bool SinnohCuteUltra { get { return (SinnohSet2 & 0x400) > 0; } }
    public bool SinnohCuteMaster { get { return (SinnohSet2 & 0x800) > 0; } }
    public bool SinnohSmart { get { return (SinnohSet2 & 0x1000) > 0; } }
    public bool SinnohSmartGreat { get { return (SinnohSet2 & 0x2000) > 0; } }
    public bool SinnohSmartUltra { get { return (SinnohSet2 & 0x4000) > 0; } }
    public bool SinnohSmartMaster { get { return (SinnohSet2 & 0x8000) > 0; } }
    public bool SinnohTough { get { return (SinnohSet2 & 0x10000) > 0; } }
    public bool SinnohToughGreat { get { return (SinnohSet2 & 0x20000) > 0; } }
    public bool SinnohToughUltra { get { return (SinnohSet2 & 0x40000) > 0; } }
    public bool SinnohToughMaster { get { return (SinnohSet2 & 0x80000) > 0; } }

    // Hoenn Set
    public bool HeonnCool { get { return (HoennSet & 0x01) > 0; } }
    public bool HoennCoolSuper { get { return (HoennSet & 0x02) > 0; } }
    public bool HeonnCoolHyper { get { return (HoennSet & 0x04) > 0; } }
    public bool HeonnCoolMaster { get { return (HoennSet & 0x08) > 0; } }
    public bool HeonnBeauty { get { return (HoennSet & 0x10) > 0; } }
    public bool HeonnBeautySuper { get { return (HoennSet & 0x20) > 0; } }
    public bool HeonnBeautyHyper { get { return (HoennSet & 0x40) > 0; } }
    public bool HeonnBeautyMaster { get { return (HoennSet & 0x80) > 0; } }
    public bool HeonnCute { get { return (HoennSet & 0x100) > 0; } }
    public bool HeonnCuteSuper { get { return (HoennSet & 0x200) > 0; } }
    public bool HeonnCuteHyper { get { return (HoennSet & 0x400) > 0; } }
    public bool HeonnCuteMaster { get { return (HoennSet & 0x800) > 0; } }
    public bool HeonnSmart { get { return (HoennSet & 0x1000) > 0; } }
    public bool HeonnSmartSuper { get { return (HoennSet & 0x2000) > 0; } }
    public bool HeonnSmartHyper { get { return (HoennSet & 0x4000) > 0; } }
    public bool HeonnSmartMaster { get { return (HoennSet & 0x8000) > 0; } }
    public bool HeonnTough { get { return (HoennSet & 0x10000) > 0; } }
    public bool HeonnToughSuper { get { return (HoennSet & 0x20000) > 0; } }
    public bool HeonnToughHyper { get { return (HoennSet & 0x40000) > 0; } }
    public bool HeonnToughMaster { get { return (HoennSet & 0x80000) > 0; } }
    public bool Champion { get { return (HoennSet & 0x100000) > 0; } }
    public bool Winning { get { return (HoennSet & 0x200000) > 0; } }
    public bool Victory { get { return (HoennSet & 0x400000) > 0; } }
    public bool Artist { get { return (HoennSet & 0x800000) > 0; } }
    public bool Effort { get { return (HoennSet & 0x1000000) > 0; } }
    public bool Marine { get { return (HoennSet & 0x2000000) > 0; } }
    public bool Land { get { return (HoennSet & 0x4000000) > 0; } }
    public bool Sky { get { return (HoennSet & 0x8000000) > 0; } }
    public bool Country { get { return (HoennSet & 0x10000000) > 0; } }
    public bool National { get { return (HoennSet & 0x20000000) > 0; } }
    public bool Earth { get { return (HoennSet & 0x40000000) > 0; } }
    public bool World { get { return (HoennSet & 0x80000000) > 0; } }

    public RibbonSet()
    {

    }

    public int InsertIntoDatabase()
    {
        List<SqliteParameterPair> parameterPairs =
        [
            new SqliteParameterPair("sinnoh_set_1", SqliteType.Integer, SinnohSet1),
            new SqliteParameterPair("sinnoh_set_2", SqliteType.Integer, SinnohSet2),
            new SqliteParameterPair("hoenn_set", SqliteType.Integer, HoennSet)
        ];

        return DbInterface.InsertIntoDatabase("ribbon", parameterPairs, "storage");
    }

        
    public override string ToString()
    {
        return "";
    }
}