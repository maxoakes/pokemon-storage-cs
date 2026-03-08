using Avalonia;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PokemonStorageLibrary;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;

namespace PokemonStorageDesktop;

public struct Settings()
{
    public string Language { get; set; } = "";
    public string VersionName { get; set; } = "";
    public string SaveFilePath { get; set; } = "";
    public bool OutputToConsole { get; set; } = false;
    public bool OutputToFile { get; set; } = false;
    public bool OutputToDatabase { get; set; } = false;
    public string OutputFilePath { get; set; } = "";

    public Settings(IConfiguration config) : this()
    {
        Language = config.GetValue<string>("Settings:Language") ?? "";
        VersionName = config.GetValue<string>("Settings:VersionName") ?? "";
        SaveFilePath = config.GetValue<string>("Settings:SaveFilePath") ?? "";
        OutputToConsole = config.GetValue<bool>("Settings:OutputToConsole");
        OutputToFile = config.GetValue<bool>("Settings:OutputToFile");
        OutputToDatabase = config.GetValue<bool>("Settings:OutputToDatabase");
        OutputFilePath = config.GetValue<string>("Settings:OutputFilePath") ?? "";
    }

    public bool AreSettingsValid()
    {
        return !string.IsNullOrWhiteSpace(Language) &&
               !string.IsNullOrWhiteSpace(VersionName) &&
               !string.IsNullOrWhiteSpace(SaveFilePath);
    }
}

sealed class Program
{
    public static ILogger Logger;
    public static Dictionary<string, string> ConnectionStrings = [];
    private static Settings Settings = new();

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
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
        Lookup.VeekunConnectionString = config.GetConnectionString("veekun") ?? "";
        Lookup.SupplementConnectionString = config.GetConnectionString("supplement") ?? "";
        Lookup.StorageConnectionString = config.GetConnectionString("storage") ?? "";
        Settings = new Settings(config);

        if (!Settings.AreSettingsValid())
        {
            throw new ConfigurationErrorsException("appsettings.json is not configured correctly");
        }


        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
