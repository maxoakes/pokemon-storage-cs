using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PokemonStorageLibrary;
using PokemonStorageLibrary.Models;
using PokemonStorageLibrary.SaveContent;

namespace PokemonStorageDesktop.Models;

public class SaveFileModel : StorageModel
{
    public string SaveFilePath { get; private set; }
    public override string DisplayTitle { get { return Path.GetFileNameWithoutExtension(SaveFilePath); } }
    public Game Game { get; private set; }
    public SaveData? GameState { get; private set; }

    public SaveFileModel(string filePath, string gameName, string language) : base()
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
                if (PokemonLists.ContainsKey("Party"))
                {
                    PokemonLists["Party"].Add(new PokemonModel(pokemon));
                }
                else
                {
                    PokemonLists.Add("Party", [new PokemonModel(pokemon)]);
                }
            }
            foreach (var box in GameState.BoxList)
            {
                foreach (var pokemon in box.Value.Values)
                {
                    if (PokemonLists.ContainsKey(box.Key))
                    {
                        PokemonLists[box.Key].Add(new PokemonModel(pokemon));
                    }
                    else
                    {
                        PokemonLists.Add(box.Key, [new PokemonModel(pokemon)]);
                    }
                }

            }
        }
    }

    public override int ImportPokemon(List<PartyPokemon> partyPokemonList)
    {
        List<PartyPokemon> confirmedList = partyPokemonList
            .Where(x => Lookup.GetGenerationIntroduced(x.PokemonIdentity.SpeciesId) <= Game.GenerationId).ToList();
        return GameState?.AppendPokemonAndSave(confirmedList, SaveFilePath) ?? 0;
    }
}