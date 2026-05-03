using System.Collections.Generic;
using PokemonStorageLibrary.Models;

namespace PokemonStorageDesktop.Models;

public abstract class StorageModel
{
    public abstract string DisplayTitle { get; }
    public Dictionary<string, List<PokemonModel>> PokemonLists;

    public StorageModel()
    {
        PokemonLists = [];
    }

    public abstract int ImportPokemon(List<PartyPokemon> partyPokemonList);
}