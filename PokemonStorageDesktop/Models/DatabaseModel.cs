using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using PokemonStorageLibrary;
using PokemonStorageLibrary.Models;
using PokemonStorageLibrary.SaveContent;

namespace PokemonStorageDesktop.Models;

public class DatabaseModel
{
    public string StorageConnectionString { get; private set; }
    public List<PokemonModel> DatabasePokemon;

    public DatabaseModel()
    {
        DatabasePokemon = [];
        
        List<Int64> primaryKeys = DbInterface.RetrieveTable("SELECT id FROM pokemon", Lookup.StorageConnectionString).AsEnumerable().Select(x => x.Field<Int64>("id")).ToList();
        primaryKeys.ForEach(x => DatabasePokemon.Add(new PokemonModel(new PartyPokemon(x))));
    }
}