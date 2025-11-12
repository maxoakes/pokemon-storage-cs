using System;

namespace PokemonStorage.Models;

public class Markings
{
    public byte Bits { get; set; }
    public bool Circle { get { return (Bits & 0x01) > 0; } }
    public bool Square { get { return (Bits & 0x04) > 0; } }
    public bool Triangle { get { return (Bits & 0x02) > 0; } }
    public bool Heart { get { return (Bits & 0x08) > 0; } }
    public bool Star { get { return (Bits & 0x10) > 0; } }
    public bool Diamond { get { return (Bits & 0x20) > 0; } }

    public Markings(int generation, byte value)
    {
        if (generation == 3)
        {
            Bits |= (byte)(Utility.GetBit(value, 0) == 1 ? 1 : 0); // circle
            Bits |= (byte)(Utility.GetBit(value, 1) == 1 ? 4 : 0); // square
            Bits |= (byte)(Utility.GetBit(value, 2) == 1 ? 2 : 0); // trinagle
            Bits |= (byte)(Utility.GetBit(value, 3) == 1 ? 8 : 0); // heart
        }
        else
        {
            Bits = value;
        }
    }

    public override string ToString()
    {
        var result = new List<string>();

        if (Circle) result.Add("CIRCLE");
        if (Square) result.Add("SQUARE");
        if (Triangle) result.Add("TRIANGLE");
        if (Heart) result.Add("HEART");
        if (Star) result.Add("STAR");
        if (Diamond) result.Add("DIAMOND");

        return string.Join(";", result);
    }
}