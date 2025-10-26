using System;

namespace PokemonStorage.Models;

public class Markings
{
    public bool Circle { get; set; }
    public bool Square { get; set; }
    public bool Triangle { get; set; }
    public bool Heart { get; set; }
    public bool Star { get; set; }
    public bool Diamond { get; set; }

    public Markings(int generation, byte value)
    {
        // if (generation == 3)
        // {
        //     Circle = ByteUtility.GetBit(value, 0);
        //     Square = ByteUtility.GetBit(value, 1);
        //     Triangle = ByteUtility.GetBit(value, 2);
        //     Heart = ByteUtility.GetBit(value, 3);
        //     Star = false;
        //     Diamond = false;
        // }
        // else
        // {
        //     Circle = ByteUtility.GetBit(value, 0);
        //     Triangle = ByteUtility.GetBit(value, 1);
        //     Square = ByteUtility.GetBit(value, 2);
        //     Heart = ByteUtility.GetBit(value, 3);
        //     Star = ByteUtility.GetBit(value, 4);
        //     Diamond = ByteUtility.GetBit(value, 5);
        // }
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