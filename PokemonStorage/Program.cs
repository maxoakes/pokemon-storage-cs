using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PokemonStorage.Models;
using System.Configuration;
using System.Text.Json;

namespace PokemonStorage;

public class Program
{
    public static ILogger Logger;
    public static string ConnectionString = "";
    public static string SaveFilePath = "";
    public static Trainer ManagerTrainer = new("OAK", (int)Gender.MALE, 1, 0);

    public static void Main()
    {
        // Read Configs
        IConfiguration config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .Build();

        // Init logger
        using ILoggerFactory factory = LoggerFactory.Create(builder =>
            builder.AddSimpleConsole(options =>
            {
                options.IncludeScopes = false;
                options.SingleLine = true;
                options.TimestampFormat = "HH:mm:ss.ffff ";
            })
        );
        Logger = factory.CreateLogger<Program>();

        // Access configuration values
        ConnectionString = config.GetConnectionString("Database") ?? "";
        string language = config.GetValue<string>("Settings:Language") ?? "";
        string mode = config.GetValue<string>("Settings:Mode") ?? "";
        string readOutput = config.GetValue<string>("Settings:ReadOutput") ?? "";
        int gameId = config.GetValue<int>("Settings:GameId");
        SaveFilePath = config.GetValue<string>("Settings:SaveFilePath") ?? "";

        if (string.IsNullOrWhiteSpace(language) || string.IsNullOrWhiteSpace(mode) || gameId == 0 || string.IsNullOrWhiteSpace(SaveFilePath))
        {
            throw new ConfigurationErrorsException("appsettings.json is not configured correctly");
        }

        Logger.LogInformation($"Connection String: {ConnectionString}");
        Console.WriteLine($"Settings: ({language})({gameId}) {Utility.ProperCapitalizeString(mode)} from [{SaveFilePath}]");

        Lookup.Initialize();

        Game game;
        try
        {
            game = Lookup.Games[gameId];
        }
        catch (KeyNotFoundException)
        {
            Console.WriteLine($"Game not valid. Choose from one of the following:");
            foreach (Game availableGame in Lookup.Games.Values.OrderBy(x => x.GameId))
            {
                Console.WriteLine($"\t{availableGame.GameId}: {availableGame.GameName}");
            }
            return;
        }

        byte[] originalSaveData;
        try
        {
            originalSaveData = File.ReadAllBytes(SaveFilePath);
        }
        catch (Exception ex)
        {
            Logger.LogTrace(ex, "Something went wrong loading the save game file");
            return;
        }

        Console.WriteLine($"Loaded {game} with {originalSaveData.Length} bytes.");
        GameState gameState = new(originalSaveData, game, language);

        if (readOutput.ToLower() == "file")
        {
            var pokemonStorageDictionary = new Dictionary<string, Dictionary<string, PartyPokemon>>();
            foreach ((int index, PartyPokemon pokemon) in gameState.Party)
            {
                if (!pokemonStorageDictionary.ContainsKey("Party"))
                    pokemonStorageDictionary["Party"] = new Dictionary<string, PartyPokemon>();

                pokemonStorageDictionary["Party"].Add(index.ToString(), pokemon);
            }

            foreach ((string box, Dictionary<int, PartyPokemon> boxDictionary) in gameState.BoxList)
            {
                if (!pokemonStorageDictionary.ContainsKey(box))
                    pokemonStorageDictionary[box] = new Dictionary<string, PartyPokemon>();

                foreach ((int slot, PartyPokemon pokemon) in boxDictionary)
                {
                    string slotId = slot.ToString();
                    if (!pokemonStorageDictionary[box].ContainsKey(slotId.ToString()))
                        pokemonStorageDictionary[box].Add(slotId.ToString(), pokemon);
                }
            }

            File.WriteAllText($"{SaveFilePath.Split('/').Last()}.json", Program.SerializeObject(pokemonStorageDictionary));
            
        }
        else if (readOutput.ToLower() == "console")
        {
            Console.WriteLine("Party:");
            foreach ((int slot, PartyPokemon pokemon) in gameState.Party)
            {
                Console.WriteLine($"{slot}: {pokemon.PrintRelevant()}");
            }
            foreach ((string box, var boxList) in gameState.BoxList)
            {
                Console.WriteLine($"Box {box}");
                foreach ((int slot, PartyPokemon pokemon) in boxList)
                {
                    Console.WriteLine($"{slot}: {pokemon.PrintRelevant()}");
                }
            }
        }        
    }

    public static string SerializeObject(object obj)
    {
        return JsonConvert.SerializeObject(obj, Formatting.Indented);
    }
}