using System;
using System.Collections.Generic;
using System.IO;
using PokemonStorageLibrary;
using PokemonStorageLibrary.Models;
using PokemonStorageLibrary.SaveContent;

namespace PokemonStorageDesktop.Models;

public class MainModel
{
    public string SaveFilePath { get; private set; }
    public Game Game { get; private set; }
    public SaveData? GameState { get; private set; }
    public string StorageConnectionString { get; private set; }
    public List<PokemonModel> SaveFilePokemon;
    public List<PokemonModel> DatabasePokemon;

    public MainModel()
    {
        SaveFilePokemon = [];
        DatabasePokemon = [];
    }

    public void LoadDatabase(string connectionString)
    {
        Lookup.StorageConnectionString = connectionString;
    }

    public void LoadSaveFile(string filePath, string gameName, string language)
    {
        byte[] readData;
        SaveFilePath = filePath;
        try
        {
            readData = File.ReadAllBytes(SaveFilePath);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return;
        }

        Game = Lookup.GetGameByName(gameName);
        GameState = null;
        switch (Game.GenerationId)
        {
            case 1:
                GameState = new SaveDataGeneration1(readData, Game, language);
                break;
            case 2:
                GameState = new SaveDataGeneration2(readData, Game, language);
                break;
            case 3:
                GameState = new SaveDataGeneration3(readData, Game, language);
                break;
            case 4:
                GameState = new SaveDataGeneration4(readData, Game, language);
                break;
        }

        if (GameState != null)
        {
            foreach (PartyPokemon pokemon in GameState.Party.Values)
            {
                SaveFilePokemon.Add(new PokemonModel(pokemon, Game));
            }
            foreach (var box in GameState.BoxList.Values)
            {
                foreach (PartyPokemon pokemon in box.Values)
                {
                    SaveFilePokemon.Add(new PokemonModel(pokemon, Game));
                }
            }
        }
    }
}