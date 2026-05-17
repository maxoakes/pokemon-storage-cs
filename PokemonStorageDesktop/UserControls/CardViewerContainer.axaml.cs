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
    public MainWindow MainWindow { get; }
    public StorageModel StorageModel { get; }
    public List<PartyPokemon> SelectedPokemon { get { return StorageModel.PokemonLists.Values.SelectMany(x => x).Where(x => x.IsChecked).Select(x => x.Pokemon).ToList(); } }
    
    public CardViewerContainer(MainWindow mainWindow, StorageModel storageModel)
    {
        InitializeComponent();
        StorageModel = storageModel;
        MainWindow = mainWindow;
        foreach ((string boxName, List<PokemonModel> pokemonModels) in storageModel.PokemonLists)
        {
            CardGroup cardGroup = new CardGroup();
            cardGroup.SetCards(pokemonModels);
            cardGroup.SetBannerText(boxName);
            parentPanel.Children.Add(cardGroup);
        }
        
        // Add padding at the end to prevent the last card from being cut off
        parentPanel.Children.Add(new Border { Height = 64 });
    }

    public void SetExportOptions(List<StorageModel> storageModels)
    {
        if (btnTransferSelected.Flyout is MenuFlyout flyout)
        {
            flyout.Items.Clear();
            MenuItem showdownItem = new MenuItem 
            { 
                Name = "DEFAULT_Showdown", 
                Header = "Export Selected to PokemonShowdown"
            };
            showdownItem.Click += ExportSelected_Click;
            flyout.Items.Add(showdownItem);

            foreach (StorageModel storageModel in storageModels)
            {
                // Skip the current one
                if (storageModel.DisplayTitle == StorageModel.DisplayTitle) continue;

                MenuItem storageTransferItem = new MenuItem
                {
                    Name = storageModel.DisplayTitle,
                    Header = $"Transfer Selected to {((storageModel is DatabaseModel) ? "Database" : "File")}: {storageModel.DisplayTitle}"
                };
                storageTransferItem.Click += ExportSelected_Click;
                flyout.Items.Add(storageTransferItem);
            }
        }
    }

    private async void ExportSelected_Click(object? sender, RoutedEventArgs e)
    {
        TopLevel? topLevel = TopLevel.GetTopLevel(this) ?? throw new InvalidOperationException("Unable to access TopLevel for file dialog.");

        if (StorageModel is SaveFileModel saveFileModel)
        {
            if (saveFileModel.Game.GenerationId <= 2)
            {
                DialogBoxYesNo applyPersonalityDialog = new DialogBoxYesNo(
                    "You are about to transfer Pokemon from a game that did not use personality values. Would you like to apply personality values to the selected Pokemon?",
                    "Personality Value Application",
                    "Yes, Apply Values",
                    "No, Do Not Change",
                    "Cancel Transfer"
                );

                int? result = await applyPersonalityDialog.ShowDialog<int?>(MainWindow);
                if (result.HasValue && result.Value == 1)
                {
                    SelectedPokemon.ForEach(x => x.AssignRandomPersonalityValue());
                    SelectedPokemon.ForEach(x => x.SetAbilityFromPersonalityValue());
                }
                else if (result.HasValue && result.Value == 0)
                {
                    // Do nothing
                }
                else
                {
                    return;
                }
            }
        }

        Control? senderControl = sender as Control;
        Console.WriteLine(senderControl?.Name);
        switch (senderControl?.Name)
        {
            case "DEFAULT_Json":
                Console.WriteLine("Json export");
                string suggestedJsonName = $"{StorageModel.DisplayTitle}.{DateTime.Now:s}.json";
                var jsonFileOutput = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    Title = "Save JSON Export",
                    SuggestedFileName = suggestedJsonName,
                    FileTypeChoices = [
                        new FilePickerFileType("JSON File") { Patterns = ["*.json"] }
                    ],
                });
                if (jsonFileOutput?.Path.AbsoluteUri is not null)
                {
                    Console.WriteLine($"Write path: {jsonFileOutput?.Path.AbsolutePath}");
                    File.WriteAllText(
                        jsonFileOutput?.Path.AbsolutePath ?? $"./{suggestedJsonName}", 
                        JsonConvert.SerializeObject(SelectedPokemon, Formatting.Indented)
                    );
                    DialogBoxOk jsonTransferDialog = new DialogBoxOk(
                        $"Transfered {SelectedPokemon.Count} Pokemon to:\n{jsonFileOutput?.Path.AbsoluteUri}",
                        "Successful Transfer"
                    );
                    await jsonTransferDialog.ShowDialog<bool>(MainWindow);
                }
                break;
            case "DEFAULT_Showdown":
                Console.WriteLine("Pokemon Showdown export");
                string suggestedShowdownName = $"{StorageModel.DisplayTitle}.{DateTime.Now:s}.txt";
                var showdownFileOutput = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    Title = "Save JSON Export",
                    SuggestedFileName = suggestedShowdownName,
                    FileTypeChoices = [
                        new FilePickerFileType("Plain text File") { Patterns = ["*.txt"] }
                    ],
                });
                if (showdownFileOutput?.Path.AbsoluteUri is not null)
                {
                    Console.WriteLine($"Write path: {showdownFileOutput?.Path.AbsolutePath}");
                    File.WriteAllText(
                        showdownFileOutput?.Path.AbsolutePath ?? $"./{suggestedShowdownName}", 
                        string.Join("\n\n\n", SelectedPokemon.Select(x => x.GetPokemonShowdownString()))
                    );
                    DialogBoxOk showdownTransferDialog = new DialogBoxOk(
                        $"Transfered {SelectedPokemon.Count} Pokemon to\n{showdownFileOutput?.Path.AbsoluteUri}",
                        "Successful Transfer"
                    );
                    await showdownTransferDialog.ShowDialog<bool>(MainWindow);
                }
                break;
            default:
                StorageModel? selectedStorageModel = MainWindow.StorageModels.Find(x => x.DisplayTitle == senderControl?.Name);
                if (selectedStorageModel != null)
                {
                    if (selectedStorageModel is DatabaseModel)
                    {
                        DialogBoxTextField databaseTagDialog = new DialogBoxTextField(
                            "Assign a database tag to the selected Pokemon:",
                            "Database Tag Assignment"
                        );
                        string tag = await databaseTagDialog.ShowDialog<string>(MainWindow);
                        SelectedPokemon.ForEach(x => x.DatabaseTag = tag);
                    }
                    int result = selectedStorageModel.ImportPokemon(SelectedPokemon);
                    string resultMessage = result != SelectedPokemon.Count ?
                        $"Not all Pokemon were transferred to {selectedStorageModel.DisplayTitle}. ({result}/{SelectedPokemon.Count})" :
                        $"Successfully transferred all {result} Pokemon to {selectedStorageModel.DisplayTitle}.";
                        
                    DialogBoxOk storageModelTransferDialog = new DialogBoxOk(resultMessage, "Successful Transfer");
                    await storageModelTransferDialog.ShowDialog<bool>(MainWindow);
                }
                break;
        }
        Console.WriteLine($"{SelectedPokemon.Count()} selected");
    }
}