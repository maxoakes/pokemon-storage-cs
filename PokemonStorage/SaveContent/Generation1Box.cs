using System.Dynamic;
using PokemonStorage;
using PokemonStorage.Models;
using PokemonStorage.SaveContent;

public class Generation1Box
{
    public Game Game { get; }
    public string Language { get; }
    public byte Id { get; }
    public byte Count { get; private set; }
    public byte[] SpeciesIds { get; private set; }
    public byte[][] PokemonBytes { get; private set; }
    public byte[][] OriginalTrainerNames { get; private set; }
    public byte[][] PokemonNames { get; private set; }

    public Generation1Box(byte[] content, byte id, Game game, string lang)
    {
        Game = game;
        Language = lang;
        Id = id;
        Count = Utility.GetByte(content, 0x00);
        SpeciesIds = new byte[20];
        PokemonBytes = new byte[20][];
        OriginalTrainerNames = new byte[20][];
        PokemonNames = new byte[20][];
        for (int i = 0; i < 20; i++)
        {
            SpeciesIds[i] = Utility.GetByte(content, 0x01 + (i*0x1));
            PokemonBytes[i] = Utility.GetBytes(content, 0x16 + (i * 0x21), 0x21);
            OriginalTrainerNames[i] = Utility.GetBytes(content, 0x2AA + (i * 0xB), 0xB);
            PokemonNames[i] = Utility.GetBytes(content, 0x386 + (i * 0xB), 0xB);
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
        byte[] content = new byte[0x462];
        content[0] = Count;
        for (int i = 0; i < 20; i++)
        {
            content[0x01 + i] = SpeciesIds[i];
        }
        content[0x15] = 0;
        for (int i = 0; i < 20; i++)
        {
            Buffer.BlockCopy(PokemonBytes[i], 0, content, 0x16+(i*0x21), 0x21);
        }
        for (int i = 0; i < 20; i++)
        {
            Buffer.BlockCopy(OriginalTrainerNames[i], 0, content, 0x2AA+(i*0xB), 0xB);
        }
        for (int i = 0; i < 20; i++)
        {
            Buffer.BlockCopy(PokemonNames[i], 0, content, 0x386+(i*0xB), 0xB);
        }
        return content;
    }

    /// <summary>
    /// Put all Pokemon bytes in the correct places in a box
    /// </summary>
    /// <param name="pokemon">Normalized Party Pokemon data</param>
    /// <param name="slot">Target slot starting at 0</param>
    /// <returns>True if placement was done, false if it was not done</returns>
    public bool AssignPokemon(PartyPokemon pokemon, int slot)
    {
        if (slot >= 20) return false;
        SpeciesIds[slot] = (byte)Lookup.GetPokemonGameIndexByFormId(1, pokemon.PokemonIdentity.FormId);
        OriginalTrainerNames[slot] = Utility.GetEncodedString(pokemon.OriginalTrainer.Name, 11, Game, Language);
        PokemonNames[slot] = Utility.GetEncodedString(pokemon.Nickname, 11, Game, Language);
        PokemonBytes[slot] = SaveDataGeneration1.GetBoxBytesFromPartyPokemon(pokemon);
        Count++;
        return true;
    }
}