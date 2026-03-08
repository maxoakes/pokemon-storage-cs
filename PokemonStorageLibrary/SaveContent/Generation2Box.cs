using System.Dynamic;
using PokemonStorageLibrary;

public class Generation2Box
{
    public Game Game { get; }
    public byte[] OriginalBoxBytes { get; }
    public string Language { get; }
    public byte Id { get; }
    public int BoxSize { get { return string.Equals(Language, "JP", StringComparison.OrdinalIgnoreCase) ? 1352 : 1102; }}
    public byte Count { get; set; }
    public int PokemonSize { get; }
    public int Capacity { get { return string.Equals(Language, "JP", StringComparison.OrdinalIgnoreCase) ? 30 : 20; } }
    public byte[] SpeciesIds { get; private set; }
    public byte[][] PokemonBytes { get; private set; }
    public byte[][] OriginalTrainerNames { get; private set; }
    public byte[][] PokemonNames { get; private set; }

    public Generation2Box(byte[] content, byte id, Game game, string lang)
    {
        Game = game;
        OriginalBoxBytes = content;
        Language = lang;
        Id = id;
        Count = Utility.GetByte(content, 0x00);
        PokemonSize = 0x20;
        SpeciesIds = new byte[Capacity];
        PokemonBytes = new byte[Capacity][];
        OriginalTrainerNames = new byte[Capacity][];
        PokemonNames = new byte[Capacity][];

        int speciesListSize = Capacity + 1;
        int pokemonListByteSize = Capacity * PokemonSize;
        int otNameListByteSize = Capacity * 11;
        int pokemonNameListByteSize = Capacity * 11;
        for (int i = 0; i < Capacity; i++)
        {
            SpeciesIds[i] = Utility.GetByte(content, 0x01 + i);
            PokemonBytes[i] = Utility.GetBytes(content, 1 + speciesListSize + PokemonSize * i, PokemonSize);
            OriginalTrainerNames[i] = Utility.GetBytes(content, 1 + speciesListSize + pokemonListByteSize + 11 * i, 11);
            PokemonNames[i] = Utility.GetBytes(content, 1 + speciesListSize + pokemonListByteSize + otNameListByteSize + 11 * i, 11);
        }
    }

    public string GetOriginalTrainerName(int index)
    {
        return Utility.GetDecodedString(OriginalTrainerNames[index], Game, Language);
    }

    public string GetPokemonName(int index)
    {
        return Utility.GetDecodedString(PokemonNames[index], Game, Language);
    }

    public byte[] GetBoxBytes()
    {
        byte[] content = new byte[BoxSize];
        content[0] = Count;
        for (int i = 0; i < Capacity; i++)
        {
            content[0x01 + i] = SpeciesIds[i];
        }
        content[1  + Count] = 0xFF;
        for (int i = 0; i < Capacity; i++)
        {
            Buffer.BlockCopy(PokemonBytes[i], 0, content, 1 + 1 + Capacity + PokemonSize * i, PokemonSize);
        }
        for (int i = 0; i < Capacity; i++)
        {
            Buffer.BlockCopy(OriginalTrainerNames[i], 0, content, (1 + 1 + Capacity) + (Capacity * PokemonSize) + 11 * i, 11);
        }
        for (int i = 0; i < Capacity; i++)
        {
            Buffer.BlockCopy(PokemonNames[i], 0, content, (1 + 1 + Capacity) + (Capacity * PokemonSize) + (Capacity * 11) + 11 * i, 11);
        }
        return content;
    }
}
