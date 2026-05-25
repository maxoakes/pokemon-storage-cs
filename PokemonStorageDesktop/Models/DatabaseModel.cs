using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using PokemonStorageLibrary;
using PokemonStorageLibrary.Models;

namespace PokemonStorageDesktop.Models;

public class DatabaseModel : StorageModel
{
    private string ConnectionString { get; set; }
    public override string DisplayTitle { get { return Path.GetFileNameWithoutExtension(Utility.GetConnectionStringPath(ConnectionString)); } }
    
    public DatabaseModel(string connectionString) : base()
    {
        ConnectionString = connectionString;
        ParseFile();
    }

    public override bool ParseFile()
    {
        List<Int64> primaryKeys = DbInterface.RetrieveTable("SELECT id FROM pokemon", ConnectionString).AsEnumerable().Select(x => x.Field<Int64>("id")).ToList();
        List<PartyPokemon> databasePokemon = primaryKeys.Select(x => new PartyPokemon(x, ConnectionString)).ToList();
        PokemonLists.Add("Database", databasePokemon.Select(x => new PokemonModel(x)).ToList());
        return true;
    }

    public override int ImportPokemon(List<PartyPokemon> partyPokemonList)
    {
        int i = 0;
        foreach (PartyPokemon partyPokemon in partyPokemonList)
        {
            int pk = partyPokemon.InsertIntoDatabase(ConnectionString);
            Console.WriteLine($"Inserted {partyPokemon.Nickname} as {pk}");
            i++;
        }
        return i;
    }
}