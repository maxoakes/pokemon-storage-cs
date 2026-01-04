using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PokemonStorage.Models;
using PokemonStorage.SaveContent;
using System.Configuration;
using System.Diagnostics;

namespace PokemonStorage;

public enum Mode
{
    NONE = 0,
    READ = 1,
    WRITE = 2,
    DEBUG = 3,
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
            "DEBUG" => Mode.DEBUG,
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
    private static SaveData? GameState;

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

        Game game = Lookup.GetGameByName(Settings.VersionName);
        GameState = null;
        switch (game.GenerationId)
        {
            case 1:
                GameState = new SaveDataGeneration1(originalSaveData, game, Settings.Language);
                break;
            case 2:
                GameState = new SaveDataGeneration2(originalSaveData, game, Settings.Language);
                break;
            case 3:
                GameState = new SaveDataGeneration3(originalSaveData, game, Settings.Language);
                break;
            case 4:
                GameState = new SaveDataGeneration4(originalSaveData, game, Settings.Language);
                break;
            default:
                throw new ConfigurationErrorsException($"Invalid game version generation: {game}");
        }
        GameState.ParseOriginalTrainer();
        Console.WriteLine($"Loaded {GameState.Game} with {originalSaveData.Length} bytes as trainer {GameState.Trainer}");
        OutputFileName = $"{Settings.OutputFilePath}/{GameState.Game.GameName}.{DateTime.Now:s}.json";

        switch (Settings.Mode)
        {
            case Mode.READ:
                GameState.ParsePartyPokemon();
                if (Settings.OutputToDatabase) ReviewPokemonDictionaryForDatabaseWrite(GameState.Party);

                GameState.ParseBoxPokemon();
                foreach ((string box, Dictionary<int, PartyPokemon> boxDictionary) in GameState.BoxList)
                {
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
                    PartyPokemon pokemon = new(GameState.Game);
                    pokemon.LoadFromDatabase(pk);
                    pokemonToStore.Add(pokemon);
                    Console.WriteLine($"Loaded from database {pk}:\t{pokemon.GetSummaryString()}");
                    Console.WriteLine(SerializeObject(pokemon));
                }
                break;
            case Mode.DEBUG:
                SaveDataGeneration1 debugSaveData = (SaveDataGeneration1)GameState;
                PartyPokemon debugPokemon = new(GameState.Game)
                {
                    PokemonIdentity = Lookup.GetPokemonBySpeciesId(151, Lookup.GetLanguageIdByIdentifier(Settings.Language)),
                    Nickname = "MEW1",
                    ExperiencePoints = 1027103-666, // lvl 98
                    OriginalTrainer = new Trainer("Scoot", 0, 12345, 54321),
                    Moves = new Dictionary<int, Move>
                    {
                        {1, new Move(1, 35, 0, 0)}, // pound
                        {2, new Move(118, 10, 1, 0)}, // metronome
                        {3, new Move(94, 10, 2, 0)}, // psychic
                        {4, new Move(142, 10, 3, 0)}, // lovely kiss
                    },
                    Stats = new StatSet(false,
                        new StatHextuple(
                            15, 14, 13, 12, 11, 10
                        ),
                        new StatHextuple(
                            0xFFFF - 1, 0xFFFF - 2, 0xFFFF - 3, 0xFFFF - 4, 0xFFFF - 5, 0xFFFF - 6
                        )
                    )
                };
                debugSaveData.WriteToPokedex(151);
                byte[] debugPokemonBytes = SaveDataGeneration1.GetBoxBytesFromPartyPokemon(debugPokemon);
                debugSaveData.WriteRecalculatedChecksums();
                bool isValidWrite = debugSaveData.AreAllChecksumsValid();
                if (isValidWrite)
                {
                    Logger.LogInformation("Checksum was valid!");
                    File.Copy(Settings.SaveFilePath, Settings.SaveFilePath + ".original");
                    File.WriteAllBytes(Settings.SaveFilePath, debugSaveData.ModifiedData);
                }
                else
                {
                    Logger.LogError("Checksum of modified save file was not valid!");
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