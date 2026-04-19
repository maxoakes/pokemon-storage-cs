using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Newtonsoft.Json;
using PokemonStorageDesktop.Models;
using PokemonStorageLibrary.Models;

namespace PokemonStorageDesktop.Views;

public partial class CardViewerContainer : UserControl
{
    public string TabTitle { get; set; }
    public List<PokemonModel> PokemonModels { get; set; }
    public List<PartyPokemon> SelectedPokemon { get { return PokemonModels.Where(x => x.IsChecked).Select(x => x.Pokemon).ToList(); } }
    
    public CardViewerContainer()
    {
        InitializeComponent();
        PokemonModels = [];
    }

    public void AddPokemonGroups(Dictionary<string, List<PokemonModel>> pokemonBoxes)
    {
        foreach ((string boxName, List<PokemonModel> pokemonModels) in pokemonBoxes)
        {
            CardGroup cardGroup = new CardGroup();
            PokemonModels.AddRange(pokemonModels);
            cardGroup.SetCards(pokemonModels);
            cardGroup.SetBannerText(boxName);
            parentPanel.Children.Add(cardGroup);
        }
    }

    private async void ExportSelected_Click(object? sender, RoutedEventArgs e)
    {
        TopLevel? topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null)
        {
            throw new InvalidOperationException("Unable to access TopLevel for file dialog.");
        }

        Control? senderControl = sender as Control;
        switch (senderControl?.Name)
        {
            case "ExportToDatabaseButton":
                Console.WriteLine("Database export");
                foreach (PartyPokemon partyPokemon in SelectedPokemon)
                {
                    int pk = partyPokemon.InsertIntoDatabase();
                    Console.WriteLine($"Inserted {partyPokemon.Nickname} as {pk}");
                }
                // Refresh DB tab
                break;
            case "ExportToJsonButton":
                Console.WriteLine("Json export");
                string suggestedJsonName = $"{TabTitle}.{DateTime.Now:s}.json";
                var jsonFileOutput = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    Title = "Save JSON Export",
                    SuggestedFileName = suggestedJsonName,
                    FileTypeChoices = new[] {
                        new FilePickerFileType("JSON File") { Patterns = ["*.json"] }
                    },
                });
                if (jsonFileOutput?.Path.AbsoluteUri is not null)
                {
                    Console.WriteLine($"Write path: {jsonFileOutput?.Path.AbsolutePath}");
                    File.WriteAllText(
                        jsonFileOutput?.Path.AbsolutePath ?? $"./{suggestedJsonName}", 
                        JsonConvert.SerializeObject(SelectedPokemon, Formatting.Indented)
                    );
                }
                break;
            case "ExportToPokemonShowdownButton":
                Console.WriteLine("Pokemon Showdown export");
                string suggestedShowdownName = $"{TabTitle}.{DateTime.Now:s}.txt";
                var showdownFileOutput = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    Title = "Save JSON Export",
                    SuggestedFileName = suggestedShowdownName,
                    FileTypeChoices = new[] {
                        new FilePickerFileType("Plain text File") { Patterns = ["*.txt"] }
                    },
                });
                if (showdownFileOutput?.Path.AbsoluteUri is not null)
                {
                    Console.WriteLine($"Write path: {showdownFileOutput?.Path.AbsolutePath}");
                    File.WriteAllText(
                        showdownFileOutput?.Path.AbsolutePath ?? $"./{suggestedShowdownName}", 
                        string.Join("\n\n\n", SelectedPokemon.Select(x => x.GetPokemonShowdownString()))
                    );
                }
                break;
        }
        Console.WriteLine($"{SelectedPokemon.Count()} selected");
    }
}