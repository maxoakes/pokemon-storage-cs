using System;

namespace PokemonStorageLibrary.Models;

public class Markings
{
    public bool Circle { get; }
    public bool Square { get; }
    public bool Triangle { get; }
    public bool Heart { get; }
    public bool Star { get; }
    public bool Diamond { get; }

    public Markings(int generation, byte value)
    {
        if (generation == 3)
        {
            Circle = Utility.GetBit(value, 0) == 1;
            Square = Utility.GetBit(value, 1) == 1;
            Triangle = Utility.GetBit(value, 2) == 1;
            Heart = Utility.GetBit(value, 3) == 1;
            Star = false;
            Diamond = false;
        }
        else
        {
            Circle = Utility.GetBit(value, 0) == 1;
            Square = Utility.GetBit(value, 2) == 1;
            Triangle = Utility.GetBit(value, 1) == 1;
            Heart = Utility.GetBit(value, 3) == 1;
            Star = Utility.GetBit(value, 4) == 1;
            Diamond = Utility.GetBit(value, 5) == 1;
        }
    }

    public byte AsGen3Byte()
    {
        return (byte)(
            (Circle ? 1 : 0) |
            (Square ? 2 : 0) |
            (Triangle ? 4 : 0) |
            (Heart ? 8 : 0)
        );
    }

    public byte AsGen4Byte()
    {
        return (byte)(
            (Circle ? 1 : 0) |
            (Triangle ? 2 : 0) |
            (Square ? 4 : 0) |
            (Heart ? 8 : 0) |
            (Star ? 16 : 0) |
            (Diamond ? 32 : 0)
        );
    }

    public override string ToString()
    {
        var result = new List<string>();

        if (Circle) result.Add("⏺");
        if (Square) result.Add("⏹");
        if (Triangle) result.Add("▲");
        if (Heart) result.Add("❤");
        if (Star) result.Add("★");
        if (Diamond) result.Add("◆");

        return string.Join(" ", result);
    }
}