using PokemonStorageLibrary.Models;

namespace PokemonStorageLibrary.SaveContent;

/// <summary>
/// Simple storage of two values where one is true and the other is asserted
/// </summary>
public struct Checksum
{
    public int Real { get; set; }
    public int Calculated { get; set; }

    /// <summary>
    /// Compares the real and asserted values when cast to bytes
    /// </summary>
    /// <returns>True if they are the same, false if they are not</returns>
    public bool IsByteSizeValid()
    {
        return (byte)Real == (byte)Calculated;
    }

    public bool IsShortSizeValid()
    {
        return (ushort)Real == (ushort)Calculated;
    }
}

public abstract class SaveData
{
    public byte[] OriginalData { get; set; }
    public byte[] ModifiedData { get; set; }
    public Game Game { get; }
    public Language Language { get; }
    public Trainer Trainer { get; set; }
    public Dictionary<int, PartyPokemon> Party { get; set; } = [];
    public Dictionary<string, Dictionary<int, PartyPokemon>> BoxList { get; set; } = [];

    public SaveData(byte[] content, Game game, string language)
    {
        OriginalData = new byte[content.Length];
        ModifiedData = new byte[content.Length];
        Array.Copy(content, OriginalData, content.Length);
        Array.Copy(content, ModifiedData, content.Length);
        Game = game;
        Language = Lookup.GetLanguageByIdentifier(language, LanguageType.Iso3166);
    }

    #region Abstract Declarations

    /// <summary>
    /// Fills GameState.Trainer with the original trainer parsed from the save file content.
    /// </summary>
    /// <returns></returns>
    protected abstract void ParseOriginalTrainer();

    /// <summary>
    /// Fills GameState.Party with the party Pokemon parsed from the save file content.
    /// </summary>
    protected abstract void ParsePartyPokemon();

    /// <summary>
    /// Fills GameState.BoxList with the box Pokemon parsed from the save file content.
    /// </summary>
    protected abstract void ParseBoxPokemon();

    /// <summary>
    /// Look at all save section's checksums and validate it against actual data of ModifiedData
    /// </summary>
    /// <returns>True if all checksums are valid, false if any are incorrect.</returns>
    public abstract bool AreAllChecksumsValid();

    /// <summary>
    /// Print the seen/owned status of all Pokemon of ModifiedData
    /// </summary>
    public abstract void PrintPokedex();

    /// <summary>
    /// Set the ModifiedData content to state that the Pokemon of a certain index has been seen and/or owned
    /// </summary>
    /// <param name="nationalIndex">Index of the Pokemon to alter</param>
    /// <param name="seen">Has the Pokemon been seen</param>
    /// <param name="owned">Is the Pokemon owned</param>
    public abstract void WriteToPokedex(int nationalIndex, bool seen=true, bool owned=true);

    /// <summary>
    /// Read byte data and create a standard Pokemon object from that data
    /// </summary>
    /// <returns>Standard PartyPokemon that is universal across all generations</returns>
    public abstract PartyPokemon GetPartyPokemonFromBoxBytes(byte[] data);

    /// <summary>
    /// Get the bytes of the Party Pokemon in the format of the appropriate game's PC box Pokemon format
    /// </summary>
    /// <param name="p">Party Pokemon</param>
    /// <returns>Save data bytes of Party Pokemon</returns>
    public abstract byte[] GetBoxBytesFromPartyPokemon(PartyPokemon p);

    /// <summary>
    /// Write Pokemon data to the next available PC slot
    /// </summary>
    /// <param name="pokemon">Pokemon object to write</param>
    /// <param name="targetBox">Box that the Pokemon should go in. If -1, target first available slot
    /// <returns></returns>
    public abstract int AddPokemonToNextOpenBox(PartyPokemon pokemon);
    
    /// <summary>
    /// The complete process of writing Pokemon to a file, updating the Pokedex, and saving the file.
    /// </summary>
    /// <param name="partyPokemonList"></param>
    /// <returns>Number of Pokemon appended to file</returns>
    public abstract int AppendPokemonAndSave(List<PartyPokemon> partyPokemonList, string filepath, bool overwriteBackup=true);

    #endregion

    /// <summary>
    /// Returns a single object that contains a dictionary of party Pokemon and all box Pokemon.
    /// </summary>
    /// <returns></returns>
    public object GetEntireStorageObject()
    {
        var pokemonStorageDictionary = new Dictionary<string, Dictionary<string, PartyPokemon>>();
        foreach ((int index, PartyPokemon pokemon) in Party)
        {
            if (!pokemonStorageDictionary.ContainsKey("Party"))
                pokemonStorageDictionary["Party"] = [];

            pokemonStorageDictionary["Party"].Add(index.ToString(), pokemon);
        }

        foreach ((string box, Dictionary<int, PartyPokemon> boxDictionary) in BoxList)
        {
            if (!pokemonStorageDictionary.ContainsKey(box))
                pokemonStorageDictionary[box] = [];

            foreach ((int slot, PartyPokemon pokemon) in boxDictionary)
            {
                string slotId = slot.ToString();
                if (!pokemonStorageDictionary[box].ContainsKey(slotId.ToString()))
                    pokemonStorageDictionary[box].Add(slotId.ToString(), pokemon);
            }
        }
        return pokemonStorageDictionary;
    }
}
