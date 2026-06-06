using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Mysqlx.Session;
using Newtonsoft.Json;
using PokemonStorageDesktop.Models;
using PokemonStorageLibrary.Models;

namespace PokemonStorageDesktop.Views;

public partial class CardViewerContainer : UserControl
{
    public MainWindow MainWindow { get; }
    public StorageModel StorageModel { get; private set; }
    public List<PartyPokemon> SelectedPokemon { get { return StorageModel.PokemonLists.Values.SelectMany(x => x).Where(x => x.IsChecked).Select(x => x.Pokemon).ToList(); } }
    
    public CardViewerContainer(MainWindow mainWindow, StorageModel storageModel)
    {
        InitializeComponent();
        MainWindow = mainWindow;
        StorageModel = storageModel;
        StorageModel.CardViewerContainer = this;
        InstantiateCards();
    }

    public void InstantiateCards()
    {

        foreach ((string boxName, List<PokemonModel> pokemonModels) in StorageModel.PokemonLists)
        {
            // Apply the on-click event function to update the AboutPanel
            pokemonModels.ForEach(x => x.SetCardClickEvent((s, e) => MainWindow.AboutPanel.OnNewSelection(x.Pokemon)));
            CardGroup cardGroup = new CardGroup();
            cardGroup.SetCards(pokemonModels);
            cardGroup.SetBannerText(boxName);
            parentPanel.Children.Add(cardGroup);
        }
        
        // Add padding at the end to prevent the last card from being cut off
        parentPanel.Children.Add(new Border { Height = 64 });
    }
    
    public void ResetCards()
    {
        parentPanel.Children.Clear();
    }

    public void UncheckAll()
    {
        StorageModel.PokemonLists.Values.ToList().ForEach(x => x.ForEach(y => y.Card.TransferCheckbox.IsChecked = false));
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

            if (StorageModel is DatabaseModel)
            {
                MenuItem storageDeleteItem = new MenuItem
                {
                    Name = "DatabaseDelete",
                    Header = $"Delete Selected from {StorageModel.DisplayTitle}"
                };
                storageDeleteItem.Click += ExportSelected_Click;
                flyout.Items.Add(storageDeleteItem);
            }

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
            case "DatabaseDelete":
                if (StorageModel is DatabaseModel databaseModel)
                {
                    DialogBoxYesNo deleteDialog = new DialogBoxYesNo(
                        $"Are you sure you would like to delete the following selected Pokemon?\n\n{string.Join("\n", SelectedPokemon.Select(x => x.Nickname))}",
                        "Deletion Confirmation",
                        $"Yes, Delete {SelectedPokemon.Count}",
                        "No, Do Not Delete",
                        ""
                    );

                    int? deleteDialogResult = await deleteDialog.ShowDialog<int?>(MainWindow);
                    if (deleteDialogResult.HasValue && deleteDialogResult.Value == 1)
                    {
                        int pokemonCount = 0;
                        long rowCount = 0;
                        foreach (PartyPokemon pokemon in SelectedPokemon)
                        {
                            rowCount += pokemon.DeleteFromDatabase(databaseModel.ConnectionString);
                            pokemonCount++;
                        }

                        DialogBoxOk deleteConfirmationDialog = new DialogBoxOk(
                            $"{pokemonCount} Pokemon {(pokemonCount == 1 ? "was" : "were")} removed from {databaseModel.DisplayTitle}. ({rowCount} total rows affected)", 
                            "Successful Deletion"
                        );
                        await deleteConfirmationDialog.ShowDialog<bool>(MainWindow);

                        databaseModel.ClearPokemonLists();
                        databaseModel.ParseFile();
                        databaseModel.CardViewerContainer.ResetCards();
                        databaseModel.CardViewerContainer.InstantiateCards();
                        UncheckAll();
                    }
                }

                break;
            default:
                StorageModel? selectedStorageModel = MainWindow.StorageModels.Find(x => x.DisplayTitle == senderControl?.Name);
                if (selectedStorageModel != null)
                {
                    if (SelectedPokemon.Any(x => x.PersonalityValue == 0))
                    {
                        DialogBoxYesNo applyPersonalityDialog = new DialogBoxYesNo(
                            "Some of the Pokemon you are about to transfer do not have a personality value. Would you like to apply one to those that do not have one?",
                            "Personality Value Application",
                            "Yes, Apply Values",
                            "No, Do Not Change",
                            "Cancel Transfer"
                        );

                        int? pvDialog = await applyPersonalityDialog.ShowDialog<int?>(MainWindow);
                        if (pvDialog.HasValue && pvDialog.Value == 1)
                        {
                            SelectedPokemon.ForEach(x => x.AssignRandomPersonalityValue());
                            SelectedPokemon.ForEach(x => x.SetAbilityFromPersonalityValue());
                        }
                        else if (pvDialog.HasValue && pvDialog.Value == 0)
                        {
                            // Do nothing
                        }
                        else
                        {
                            return;
                        }
                    }

                    if (selectedStorageModel is DatabaseModel)
                    {
                        DialogBoxTextField databaseTagDialog = new DialogBoxTextField(
                            "Assign a database tag to the selected Pokemon:",
                            "Database Tag Assignment"
                        );
                        string tag = await databaseTagDialog.ShowDialog<string>(MainWindow);

                        // Special return marker
                        if (tag=="$RESET") return;
                        
                        SelectedPokemon.ForEach(x => x.DatabaseTag = tag);
                    }
                    
                    int successfulTransfers = selectedStorageModel.ImportPokemon(SelectedPokemon);
                    string resultMessage = successfulTransfers != SelectedPokemon.Count ?
                        $"Not all Pokemon were transferred to {selectedStorageModel.DisplayTitle}. ({successfulTransfers}/{SelectedPokemon.Count})" :
                        $"Successfully transferred all {successfulTransfers} Pokemon to {selectedStorageModel.DisplayTitle}.";
                        
                    DialogBoxOk storageModelTransferDialog = new DialogBoxOk(resultMessage, "Successful Transfer");
                    await storageModelTransferDialog.ShowDialog<bool>(MainWindow);

                    selectedStorageModel.ClearPokemonLists();
                    selectedStorageModel.ParseFile();
                    selectedStorageModel.CardViewerContainer.ResetCards();
                    selectedStorageModel.CardViewerContainer.InstantiateCards();
                    UncheckAll();
                }
                break;
        }
        Console.WriteLine($"{SelectedPokemon.Count()} selected");
    }
}