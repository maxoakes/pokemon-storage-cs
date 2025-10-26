using System;

namespace PokemonStorage.Models;

public class Stat
{
    public int Value { get; set; }
    public int Ev { get; set; }
    public int Iv { get; set; }

    public Stat(int value, int ev, int iv)
    {
        Value = value;
        Ev = ev;
        Iv = iv;
    }

    public override string ToString()
    {
        return $"[SV:{Value}/EV:{Ev}/IV:{Iv}]";
    }
}
