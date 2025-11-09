using System;

namespace PokemonStorage.Models;

public class Stat
{
    public ushort Value { get; set; }
    public ushort Ev { get; set; }
    public byte Iv { get; set; }

    public Stat(int value, int ev, int iv)
    {
        Value = (ushort)value;
        Ev = (ushort)ev;
        Iv = (byte)iv;
    }

    public Stat(ushort ev, byte iv)
    {
        Ev = ev;
        Iv = iv;
    }

    public override string ToString()
    {
        return $"[SV:{Value}/EV:{Ev}/IV:{Iv}]";
    }
}
