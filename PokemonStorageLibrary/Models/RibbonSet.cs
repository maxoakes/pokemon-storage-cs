using System.Data;
using Microsoft.Data.Sqlite;

namespace PokemonStorageLibrary.Models;

public class RibbonSet
{
    public uint SinnohSet1 { get; set; }
    public uint SinnohSet2 { get; set; }
    public uint HoennSet { get; set; }

    public Dictionary<string, bool> Ribbons
    {
        get
        {
            return new Dictionary<string, bool>
            {
                // Sinnoh Set 1
                ["Sinnoh Champion"] = (SinnohSet1 & 0x01) > 0,
                ["Ability"] = (SinnohSet1 & 0x02) > 0,
                ["Great Ability"] = (SinnohSet1 & 0x04) > 0,
                ["Double Ability"] = (SinnohSet1 & 0x08) > 0,
                ["Multi Ability"] = (SinnohSet1 & 0x10) > 0,
                ["Pair Ability"] = (SinnohSet1 & 0x20) > 0,
                ["World Ability"] = (SinnohSet1 & 0x40) > 0,
                ["Alert"] = (SinnohSet1 & 0x80) > 0,
                ["Shock"] = (SinnohSet1 & 0x100) > 0,
                ["Downcast"] = (SinnohSet1 & 0x200) > 0,
                ["Careless"] = (SinnohSet1 & 0x400) > 0,
                ["Relax"] = (SinnohSet1 & 0x800) > 0,
                ["Snooze"] = (SinnohSet1 & 0x1000) > 0,
                ["Smile"] = (SinnohSet1 & 0x2000) > 0,
                ["Gorgeous"] = (SinnohSet1 & 0x4000) > 0,
                ["Royal"] = (SinnohSet1 & 0x8000) > 0,
                ["Gorgeous Royal"] = (SinnohSet1 & 0x10000) > 0,
                ["Footprint"] = (SinnohSet1 & 0x20000) > 0,
                ["Record"] = (SinnohSet1 & 0x40000) > 0,
                ["History"] = (SinnohSet1 & 0x80000) > 0,
                ["Legend"] = (SinnohSet1 & 0x100000) > 0,
                ["Red"] = (SinnohSet1 & 0x200000) > 0,
                ["Green"] = (SinnohSet1 & 0x400000) > 0,
                ["Blue"] = (SinnohSet1 & 0x800000) > 0,
                ["Festival"] = (SinnohSet1 & 0x1000000) > 0,
                ["Carnival"] = (SinnohSet1 & 0x2000000) > 0,
                ["Classic"] = (SinnohSet1 & 0x4000000) > 0,
                ["Premier"] = (SinnohSet1 & 0x8000000) > 0,

                // Sinnoh Set 2
                ["Sinnoh Cool"] = (SinnohSet2 & 0x01) > 0,
                ["Sinnoh Cool Great"] = (SinnohSet2 & 0x02) > 0,
                ["Sinnoh Cool Ultra"] = (SinnohSet2 & 0x04) > 0,
                ["Sinnoh Cool Master"] = (SinnohSet2 & 0x08) > 0,
                ["Sinnoh Beauty"] = (SinnohSet2 & 0x10) > 0,
                ["Sinnoh Beauty Great"] = (SinnohSet2 & 0x20) > 0,
                ["Sinnoh Beauty Ultra"] = (SinnohSet2 & 0x40) > 0,
                ["Sinnoh Beauty Master"] = (SinnohSet2 & 0x80) > 0,
                ["Sinnoh Cute"] = (SinnohSet2 & 0x100) > 0,
                ["Sinnoh Cute Great"] = (SinnohSet2 & 0x200) > 0,
                ["Sinnoh Cute Ultra"] = (SinnohSet2 & 0x400) > 0,
                ["Sinnoh Cute Master"] = (SinnohSet2 & 0x800) > 0,
                ["Sinnoh Smart"] = (SinnohSet2 & 0x1000) > 0,
                ["Sinnoh Smart Great"] = (SinnohSet2 & 0x2000) > 0,
                ["Sinnoh Smart Ultra"] = (SinnohSet2 & 0x4000) > 0,
                ["Sinnoh Smart Master"] = (SinnohSet2 & 0x8000) > 0,
                ["Sinnoh Tough"] = (SinnohSet2 & 0x10000) > 0,
                ["Sinnoh Tough Great"] = (SinnohSet2 & 0x20000) > 0,
                ["Sinnoh Tough Ultra"] = (SinnohSet2 & 0x40000) > 0,
                ["Sinnoh Tough Master"] = (SinnohSet2 & 0x80000) > 0,

                // Hoenn Set
                ["Hoenn Cool"] = (HoennSet & 0x01) > 0,
                ["Hoenn Cool Super"] = (HoennSet & 0x02) > 0,
                ["Hoenn Cool Hyper"] = (HoennSet & 0x04) > 0,
                ["Hoenn Cool Master"] = (HoennSet & 0x08) > 0,
                ["Hoenn Beauty"] = (HoennSet & 0x10) > 0,
                ["Hoenn Beauty Super"] = (HoennSet & 0x20) > 0,
                ["Hoenn Beauty Hyper"] = (HoennSet & 0x40) > 0,
                ["Hoenn Beauty Master"] = (HoennSet & 0x80) > 0,
                ["Hoenn Cute"] = (HoennSet & 0x100) > 0,
                ["Hoenn Cute Super"] = (HoennSet & 0x200) > 0,
                ["Hoenn Cute Hyper"] = (HoennSet & 0x400) > 0,
                ["Hoenn Cute Master"] = (HoennSet & 0x800) > 0,
                ["Hoenn Smart"] = (HoennSet & 0x1000) > 0,
                ["Hoenn Smart Super"] = (HoennSet & 0x2000) > 0,
                ["Hoenn Smart Hyper"] = (HoennSet & 0x4000) > 0,
                ["Hoenn Smart Master"] = (HoennSet & 0x8000) > 0,
                ["Hoenn Tough"] = (HoennSet & 0x10000) > 0,
                ["Hoenn Tough Super"] = (HoennSet & 0x20000) > 0,
                ["Hoenn Tough Hyper"] = (HoennSet & 0x40000) > 0,
                ["Hoenn Tough Master"] = (HoennSet & 0x80000) > 0,
                ["Champion"] = (HoennSet & 0x100000) > 0,
                ["Winning"] = (HoennSet & 0x200000) > 0,
                ["Victory"] = (HoennSet & 0x400000) > 0,
                ["Artist"] = (HoennSet & 0x800000) > 0,
                ["Effort"] = (HoennSet & 0x1000000) > 0,
                ["Marine"] = (HoennSet & 0x2000000) > 0,
                ["Land"] = (HoennSet & 0x4000000) > 0,
                ["Sky"] = (HoennSet & 0x8000000) > 0,
                ["Country"] = (HoennSet & 0x10000000) > 0,
                ["National"] = (HoennSet & 0x20000000) > 0,
                ["Earth"] = (HoennSet & 0x40000000) > 0,
                ["World"] = (HoennSet & 0x80000000) > 0
            };
        }
    }
        
    public override string ToString()
    {
        return "";
    }
}