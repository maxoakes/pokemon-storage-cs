using System;

namespace PokemonStorage.Models;

public class AbilityMapping
{
    public int First { get; set; }
    public int Second { get; set; }
    public int Hidden { get; set; }

    public AbilityMapping()
    {
        First = 0;
        Second = 0;
        Hidden = 0;
    }

    public void Assign(int value, int slot, bool isHidden)
    {
        if (isHidden) Hidden = value;
        else
        {
            if (slot == 1) First = value;
            else Second = value;
        }
    }

    public (int first, int second) GetAbilities()
    {
        return (First, Second);
    } 
}
