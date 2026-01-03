using PokemonStorage.Models;

namespace PokemonStorage.SaveContent;

public struct Checksum
{
    public int Real { get; set; }
    public int Calculated { get; set; }
}

public abstract class SaveData
{
    public byte[] OriginalData { get; set; }
    public byte[] ModifiedData { get; set; }
    public Game Game { get; set; }
    public string Language { get; set; }
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
        Language = language;
    }

    /// <summary>
    /// Fills GameState.Trainer with the original trainer parsed from the save file content.
    /// </summary>
    /// <returns></returns>
    public abstract Trainer ParseOriginalTrainer();

    /// <summary>
    /// Fills GameState.Party with the party Pokemon parsed from the save file content.
    /// </summary>
    public abstract void ParsePartyPokemon();

    /// <summary>
    /// Fills GameState.BoxList with the box Pokemon parsed from the save file content.
    /// </summary>
    public abstract void ParseBoxPokemon();

    /// <summary>
    /// Look at all save section's checksums and validate it against actual data of ModifiedData
    /// </summary>
    /// <returns>True if all checksums are valid, false if any are incorrect.</returns>
    public abstract bool AreAllChecksumsValid();
    
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
