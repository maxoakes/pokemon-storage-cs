using System;

namespace PokemonStorageLibrary.Models;

public class Nature
{
    public byte Id;
    public byte DecreaseId;
    public byte IncreaseId;
    public byte GameIndex;

    public Nature(int id, int decrease, int increase, int gameIndex)
    {
        Id = (byte)id;
        DecreaseId = (byte)decrease;
        IncreaseId = (byte)increase;
        GameIndex = (byte)gameIndex;
    }
}
