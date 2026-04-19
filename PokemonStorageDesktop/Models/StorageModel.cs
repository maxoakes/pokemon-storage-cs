using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using PokemonStorageLibrary;
using PokemonStorageLibrary.Models;
using PokemonStorageLibrary.SaveContent;

namespace PokemonStorageDesktop.Models;

public class StorageModel
{
    public string SaveFilePath { get; private set; }
    public Game Game { get; private set; }
    public SaveData? GameState { get; private set; }
    public Dictionary<string, List<PokemonModel>> BoxPokemon;

    public StorageModel(string filePath, string gameName, string language)
    {
        BoxPokemon = [];

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
                if (BoxPokemon.ContainsKey("Party"))
                {
                    BoxPokemon["Party"].Add(new PokemonModel(pokemon));
                }
                else
                {
                    BoxPokemon.Add("Party", [new PokemonModel(pokemon)]);
                }
            }
            foreach (var box in GameState.BoxList)
            {
                foreach (var pokemon in box.Value.Values)
                {
                    if (BoxPokemon.ContainsKey(box.Key))
                    {
                        BoxPokemon[box.Key].Add(new PokemonModel(pokemon));
                    }
                    else
                    {
                        BoxPokemon.Add(box.Key, [new PokemonModel(pokemon)]);
                    }
                }

            }
        }
    }
}