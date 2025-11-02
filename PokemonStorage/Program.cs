using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PokemonStorage.Models;
using System.Configuration;

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
        using ILoggerFactory factory = LoggerFactory.Create(builder => builder.AddConsole());
        Logger = factory.CreateLogger<Program>();

        // Access configuration values
        ConnectionString = config.GetConnectionString("Database") ?? "";
        string language = config.GetValue<string>("Settings:Language") ?? "";
        string mode = config.GetValue<string>("Settings:Mode") ?? "";
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
        catch (KeyNotFoundException ex)
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

        Console.WriteLine("Party:");
        foreach ((int slot, PartyPokemon pokemon) in gameState.Party)
        {
            Console.WriteLine($"{slot}: {pokemon.PrintRelevant()}");
            pokemon.PrintFullDetails();
        }
        foreach ((int boxId, var boxList) in gameState.BoxList)
        {
            Console.WriteLine($"Box {boxId}");
            foreach ((int slot, PartyPokemon pokemon) in boxList)
            {
                Console.WriteLine($"{slot}: {pokemon.PrintRelevant()}");
                pokemon.PrintFullDetails();
            }
        }
    }
}