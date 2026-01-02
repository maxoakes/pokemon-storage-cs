using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PokemonStorage.Models;
using System.Configuration;

namespace PokemonStorage;

public enum Mode
{
    NONE = 0,
    READ = 1,
    WRITE = 2,
}

public struct Settings()
{
    public string Language { get; set; } = "";
    public string VersionName { get; set; } = "";
    public string SaveFilePath { get; set; } = "";
    public Mode Mode { get; set; } = Mode.NONE;
    public bool OutputToConsole { get; set; } = false;
    public bool OutputToFile { get; set; } = false;
    public bool OutputToDatabase { get; set; } = false;
    public string OutputFilePath { get; set; } = "";

    public Settings(IConfiguration config) : this()
    {
        Language = config.GetValue<string>("Settings:Language") ?? "";
        VersionName = config.GetValue<string>("Settings:VersionName") ?? "";
        SaveFilePath = config.GetValue<string>("Settings:SaveFilePath") ?? "";
        Mode = (config.GetValue<string>("Settings:Mode") ?? "").ToUpper() switch
        {
            "READ" => Mode.READ,
            "WRITE" => Mode.WRITE,
            _ => Mode.NONE,
        };
        OutputToConsole = config.GetValue<bool>("Settings:OutputToConsole");
        OutputToFile = config.GetValue<bool>("Settings:OutputToFile");
        OutputToDatabase = config.GetValue<bool>("Settings:OutputToDatabase");
        OutputFilePath = config.GetValue<string>("Settings:OutputFilePath") ?? "";
    }

    public bool AreSettingsValid()
    {
        return !string.IsNullOrWhiteSpace(Language) &&
               !string.IsNullOrWhiteSpace(VersionName) &&
               !string.IsNullOrWhiteSpace(SaveFilePath) &&
               Mode != Mode.NONE;
    }
}

public class Program
{
    public static ILogger? Logger;
    public static Dictionary<string, string> ConnectionStrings = [];
    private static Settings Settings = new();
    private static string OutputFileName = "";
    private static GameState? GameState;

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
        Settings = new Settings(config);

        if (!Settings.AreSettingsValid())
        {
            throw new ConfigurationErrorsException("appsettings.json is not configured correctly");
        }

        byte[] originalSaveData;
        try
        {
            originalSaveData = File.ReadAllBytes(Settings.SaveFilePath);
        }
        catch (Exception ex)
        {
            Logger.LogTrace(ex, "Something went wrong loading the save game file");
            return;
        }

        GameState = new(originalSaveData, Lookup.GetGameByName(Settings.VersionName), Settings.Language);
        GameState.ParseOriginalTrainer();
        Console.WriteLine($"Loaded {GameState.Game} with {originalSaveData.Length} bytes as trainer {GameState.Trainer}");
        OutputFileName = $"{Settings.OutputFilePath}/{GameState.Game.GameName}.{DateTime.Now:s}.json";

        switch (Settings.Mode)
        {
            case Mode.READ:
                GameState.ParsePartyPokemon();
                Console.WriteLine($"Party");
                if (Settings.OutputToDatabase) ReviewPokemonDictionaryForDatabaseWrite(GameState.Party);

                GameState.ParseBoxPokemon();
                foreach ((string box, Dictionary<int, PartyPokemon> boxDictionary) in GameState.BoxList)
                {
                    Console.WriteLine($"Box [{box}]");
                    if (boxDictionary.Count == 0) continue;
                    if (Settings.OutputToDatabase) ReviewPokemonDictionaryForDatabaseWrite(boxDictionary);
                }

                if (Settings.OutputToConsole) Console.WriteLine(SerializeObject(GameState.GetEntireStorageObject()));
                if (Settings.OutputToFile) File.WriteAllText(OutputFileName, SerializeObject(GameState.GetEntireStorageObject()));
                break;
            case Mode.WRITE:
                Console.Write("Enter the primary keys of the database Pokemon to write to first available PC box slots. Separate with commas: ");
                string? input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input)) break;
                List<int> primaryKeys = [.. input.Split(',').Select(x => int.Parse(x.Trim()))];
                List<PartyPokemon> pokemonToStore = [];
                foreach (int pk in primaryKeys)
                {
                    PartyPokemon pokemon = new(GameState.Game.GenerationId);
                    pokemon.LoadFromDatabase(pk);
                    pokemonToStore.Add(pokemon);
                    Console.WriteLine($"Loaded from database {pk}:\t{pokemon.GetSummaryString()}");
                    Console.WriteLine(SerializeObject(pokemon));
                }
                break;
            default:
                Logger.LogError("No valid mode selected");
                break;
        }
    }

    private static void ReviewPokemonDictionaryForDatabaseWrite(Dictionary<int, PartyPokemon> pokemonDictionary)
    {
        bool checkEachPokemonForDatabaseOutput = false;
        if (Settings.OutputToDatabase)
        {
            Console.Write("Review each Pokemon for database insert? (y/n): ");
            string input = Console.ReadLine() ?? "n";
            checkEachPokemonForDatabaseOutput = input.ToLower() == "y";
        }

        foreach ((int slot, PartyPokemon pokemon) in pokemonDictionary)
        {
            if (Settings.OutputToDatabase && checkEachPokemonForDatabaseOutput)
            {
                if (pokemon.PokemonIdentity.SpeciesId == 0) continue;
                Console.Write($"{slot}: {pokemon.GetSummaryString()}");
                string input = Console.ReadLine() ?? "n";
                if (input.ToLower() == "y")
                {
                    int pk = pokemon.InsertIntoDatabase();
                    Console.WriteLine($"Inserted {pokemon.Nickname} as {pk}");
                }
            }
        }
    }

    public static string SerializeObject(object obj)
    {
        return JsonConvert.SerializeObject(obj, Formatting.Indented);
    }
}