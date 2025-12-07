using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PokemonStorage.Models;
using System.Configuration;
using System.Text.Json;

namespace PokemonStorage;

public class Program
{
    public static ILogger? Logger;
    public static Dictionary<string, string> ConnectionStrings = [];
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
        ConnectionStrings["veekun"] = config.GetConnectionString("veekun") ?? "";
        ConnectionStrings["supplement"] = config.GetConnectionString("supplement") ?? "";
        ConnectionStrings["storage"] = config.GetConnectionString("storage") ?? "";
        string language = config.GetValue<string>("Settings:Language") ?? "";
        string mode = config.GetValue<string>("Settings:Mode") ?? "";
        string readOutput = config.GetValue<string>("Settings:ReadOutput") ?? "";
        string gameName = config.GetValue<string>("Settings:GameName") ?? "";
        SaveFilePath = config.GetValue<string>("Settings:SaveFilePath") ?? "";

        if (string.IsNullOrWhiteSpace(language) || string.IsNullOrWhiteSpace(mode) || string.IsNullOrWhiteSpace(gameName) || string.IsNullOrWhiteSpace(SaveFilePath))
        {
            throw new ConfigurationErrorsException("appsettings.json is not configured correctly");
        }

        Console.WriteLine($"Settings: ({language})({gameName}) {Utility.ProperCapitalizeString(mode)} from [{SaveFilePath}]");
        Game game = Lookup.GetGameByName(gameName);

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
                Console.WriteLine($"{slot}: {SerializeObject(pokemon)}");
            }
            foreach ((string box, var boxList) in gameState.BoxList)
            {
                Console.WriteLine($"Box {box}");
                foreach ((int slot, PartyPokemon pokemon) in boxList)
                {
                    Console.WriteLine($"{slot}: {SerializeObject(pokemon)}");
                }
            }
        }        
    }

    public static string SerializeObject(object obj)
    {
        return JsonConvert.SerializeObject(obj, Formatting.Indented);
    }
}