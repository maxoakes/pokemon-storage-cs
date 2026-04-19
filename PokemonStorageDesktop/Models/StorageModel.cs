using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using PokemonStorageLibrary;
using PokemonStorageLibrary.Models;
using PokemonStorageLibrary.SaveContent;

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