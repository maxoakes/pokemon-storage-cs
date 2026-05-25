using System.Collections.Generic;
using PokemonStorageDesktop.Views;
using PokemonStorageLibrary.Models;

namespace PokemonStorageDesktop.Models;

public abstract class StorageModel
{
    public abstract string DisplayTitle { get; }
    public Dictionary<string, List<PokemonModel>> PokemonLists;
    public CardViewerContainer CardViewerContainer { get; set; }

    public StorageModel()
    {
        PokemonLists = [];
    }

    /// <summary>
    /// Read from the storage file that contains the Pokemon and add them to the PokemonLists property
    /// </summary>
    /// <returns>
    /// true if successful, false if not
    /// </returns>
    public abstract bool ParseFile();

    /// <summary>
    /// Add a list of Pokemon to this storage file
    /// </summary>
    /// <param name="partyPokemonList">List of Pokemon to add</param>
    /// <returns>Number of Pokemon successfully added</returns>
    public abstract int ImportPokemon(List<PartyPokemon> partyPokemonList);

    /// <summary>
    /// Wipes the PokemonLists property
    /// </summary>
    public void ClearPokemonLists()
    {
        PokemonLists.Clear();
    }
}