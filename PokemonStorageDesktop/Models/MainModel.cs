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
    public Dictionary<string, Dictionary<int, PartyPokemon>> SaveFilePokemon;
    public Dictionary<int, PartyPokemon> DatabasePokemon;

    public MainModel()
    {
        
    }

    public void LoadDatabase(string connectionString)
    {
        Lookup.StorageConnectionString = connectionString;
    }

    public bool LoadSaveFile(string filePath)
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
            return false;
        }

        Game game = Lookup.GetGameByName("Blue");
        GameState = null;
        switch (game.GenerationId)
        {
            case 1:
                GameState = new SaveDataGeneration1(readData, game, "en");
                break;
            case 2:
                GameState = new SaveDataGeneration2(readData, game, "en");
                break;
            case 3:
                GameState = new SaveDataGeneration3(readData, game, "en");
                break;
            case 4:
                GameState = new SaveDataGeneration4(readData, game, "en");
                break;
            default:
                return false;
        }
        
        return true;
    }
}