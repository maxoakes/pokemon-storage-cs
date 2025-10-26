using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PokemonStorage.Models;
using System.Configuration;
using System.IO;
// import os
// import sys
// from dotenv import load_dotenv
// from Database import Database
// from Extractor import Extractor
// from Lookup import Lookup
// from Models.GameState import GameState
// from Models.Pokemon import Pokemon
// from Models.Trainer import Trainer

// def main():

namespace PokemonStorage;


public class Program
{
    public static string ConnectionString = "";
    public static string Language = "";
    public static string Mode = "";
    public static int GameId = 0;
    public static string SaveFilePath = "";

    public static Trainer OriginalTrainer = new("OAK", (int)Gender.MALE, 1, 0);

    public static void Main()
    {
        // Read Configs
        IConfiguration config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        // Init logger
        using ILoggerFactory factory = LoggerFactory.Create(builder => builder.AddConsole());
        ILogger logger = factory.CreateLogger<Program>();

        // Access configuration values
        ConnectionString = config.GetConnectionString("Database") ?? "";
        Language = config.GetValue<string>("Settings:Language") ?? "";
        Mode = config.GetValue<string>("Settings:Mode") ?? "";
        GameId = config.GetValue<int>("Settings:GameId");
        SaveFilePath = config.GetValue<string>("Settings:SaveFilePath") ?? "";

        if (string.IsNullOrWhiteSpace(Language) || string.IsNullOrWhiteSpace(Mode) || GameId == 0 || string.IsNullOrWhiteSpace(SaveFilePath))
        {
            throw new ConfigurationErrorsException("appsettings.json is not configured correctly");
        }

        logger.LogInformation($"Connection String: {ConnectionString}");
        logger.LogInformation($"Settings: ({Language})({GameId}) {Utility.ProperCapitalizeString(Mode)} from [{SaveFilePath}]");

        Lookup.Initialize();

        Game game;
        try
        {
            game = Lookup.Games[GameId];
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
            logger.LogTrace(ex, "Something went wrong loading the save game file");
            return;
        }

        Console.WriteLine($"Loaded {game} with {originalSaveData.Length} bytes.");

        //     print(f"Reading {game} for version {game.version_id} in '{lang}' with length {len(content)}...")
        //     game_state = GameState(content, game, lang)

        //     print("Party:")
        //     for (i, p) in game_state.party.items():
        //         print(f"  Slot {i+1}: {p.get_one_liner_description()}")
        //     for (b_id, box) in game_state.box_lists.items():
        //         print(f"Box {b_id}")
        //         for (i, p) in box.items():
        //             print(f"  Slot {i+1}: {p.get_one_liner_description()}")


        }
}