using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using PokemonStorageDesktop.Models;
using PokemonStorageLibrary;
using PokemonStorageLibrary.Models;
using Newtonsoft.Json;
using PokemonStorageDesktop.UserControls;

namespace PokemonStorageDesktop.Views;

public enum TabType
{
    File,
    Database
}

public partial class MainWindow : Window
{
    public MainModel MainModel { get; set; }
    public string SelectedFilepath { get; set; }
    public string SelectedLanguage { get; set; }
    public string SelectedVersionGroup { get; set; }
    public ScrollViewer FileViewerControl { get { return this.GetControl<ScrollViewer>("FileGridParent"); } }
    public ScrollViewer DatabaseViewerControl { get { return this.GetControl<ScrollViewer>("DatabaseGridParent"); } }
    public AboutPanel AboutPanelControl { get { return this.GetControl<AboutPanel>("AboutPanel"); } }
    public FileOpenDialog? FileOpenDialog { get { return this.GetVisualDescendants().OfType<FileOpenDialog>().First(); } }

    public MainWindow()
    {
        InitializeComponent();
        
        MainModel = new();
    }

    protected override async void OnOpened(EventArgs e)
    {
        if (Design.IsDesignMode) return;

        FileViewerControl.Content = new FileOpenDialog();
        FileOpenDialog.OpenFileButton.Click += OpenSaveFile_Click;
        
        foreach (PokemonModel pokemonModel in MainModel.DatabasePokemon)
        {
            if (DatabaseViewerControl.Content is WrapPanel wrapPanel)
            {
                pokemonModel.SetCardClickEvent((s, e) => AboutPanelControl.OnNewSelection(pokemonModel.Pokemon));
                wrapPanel.Children.Add(pokemonModel.Card);
            }
        }
    }

    private WrapPanel GetNewPokemonGrid(TabType tabType)
    {
        WrapPanel grid = new WrapPanel
        {
            Name = Enum.GetName(tabType),
            Background = Brushes.Red,
        };
        return grid;
    }

    private async void OpenSaveFile_Click(object? sender, RoutedEventArgs e)
    {
        Console.WriteLine("Opening file");
        SelectedFilepath = this.GetVisualDescendants().OfType<TextBox>().First(x => x.Name == "OpenFileTextBox").Text ?? "";
        SelectedLanguage = this.GetVisualDescendants().OfType<ComboBox>().First(x => x.Name == "OpenFileLanguageDropdown").SelectedItem?.ToString() ?? "";
        SelectedVersionGroup = this.GetVisualDescendants().OfType<ComboBox>().First(x => x.Name == "OpenFileVersionDropdown").SelectedItem?.ToString() ?? "";
        MainModel.LoadSaveFile(SelectedFilepath, SelectedVersionGroup, SelectedLanguage);
        FileGridParent.Content = GetNewPokemonGrid(TabType.File);

        var aboutPanel = this.GetControl<AboutPanel>("AboutPanel");
        foreach (PokemonModel pokemonModel in MainModel.SaveFilePokemon)
        {
            if (FileGridParent.Content is WrapPanel wrapPanel)
            {
                pokemonModel.SetCardClickEvent((s, e) => AboutPanelControl.OnNewSelection(pokemonModel.Pokemon));
                wrapPanel.Children.Add(pokemonModel.Card);
            }
        }
    }

    private async void ExportSelected_Click(object? sender, RoutedEventArgs e)
    {
        TopLevel? topLevel = GetTopLevel(this);
        if (topLevel == null)
        {
            throw new InvalidOperationException("Unable to access TopLevel for file dialog.");
        }

        Control? senderControl = sender as Control;
        switch (senderControl?.Name)
        {
            case "ExportToDatabaseButton":
                Console.WriteLine("Database export");
                foreach (PartyPokemon partyPokemon in MainModel.SaveFilePokemon.Where(x => x.IsChecked).Select(x => x.Pokemon))
                {
                    int pk = partyPokemon.InsertIntoDatabase();
                    Console.WriteLine($"Inserted {partyPokemon.Nickname} as {pk}");
                }
                // Refresh DB tab
                break;
            case "ExportToJsonButton":
                Console.WriteLine("Json export");
                string suggestedJsonName = $"{MainModel.Game.GameName}.{DateTime.Now:s}.json";
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
                        JsonConvert.SerializeObject(MainModel.SaveFilePokemon.Where(x => x.IsChecked).Select(x => x.Pokemon), Formatting.Indented)
                    );
                }
                break;
            case "ExportToPokemonShowdownButton":
                Console.WriteLine("Pokemon Showdown export");
                string suggestedShowdownName = $"{MainModel.Game.GameName}.{DateTime.Now:s}.txt";
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
                        string.Join("\n\n\n", MainModel.SaveFilePokemon.Where(x => x.IsChecked).Select(x => x.Pokemon.GetPokemonShowdownString()))
                    );
                }
                break;
        }
        Console.WriteLine($"{MainModel.SaveFilePokemon.Count(x => x.IsChecked)} selected");
    }

    private async void ImportSelected_Click(object? sender, RoutedEventArgs e)
    {
        TopLevel? topLevel = GetTopLevel(this);
        if (topLevel == null)
        {
            throw new InvalidOperationException("Unable to access TopLevel for file dialog.");
        }
        if (MainModel.GameState == null)
        {
            Console.WriteLine("No game loaded, cannot import");
            return;
        }
        Control? senderControl = sender as Control;
        switch (senderControl?.Name)
        {
            case "ImportFromDatabaseButton":
                Console.WriteLine("Database export");
                foreach (PartyPokemon partyPokemon in MainModel.DatabasePokemon.Where(x => x.IsChecked).Select(x => x.Pokemon))
                {
                    int slot = MainModel.GameState.AddPokemonToNextOpenBox(partyPokemon);
                    Console.WriteLine($"Wrote {partyPokemon.Nickname} into slot {slot}");
                }
                // Refresh Save file tab
                break;
            case "DatabaseExportToJsonButton":
                Console.WriteLine("Json export");
                string suggestedJsonName = $"{MainModel.Game.GameName}.{DateTime.Now:s}.json";
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
                        JsonConvert.SerializeObject(MainModel.DatabasePokemon.Where(x => x.IsChecked).Select(x => x.Pokemon), Formatting.Indented)
                    );
                }
                break;
            case "DatabaseExportToPokemonShowdownButton":
                Console.WriteLine("Pokemon Showdown export");
                string suggestedShowdownName = $"{MainModel.Game.GameName}.{DateTime.Now:s}.txt";
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
                        string.Join("\n\n\n", MainModel.DatabasePokemon.Where(x => x.IsChecked).Select(x => x.Pokemon.GetPokemonShowdownString()))
                    );
                }
                break;
        }
        Console.WriteLine($"{MainModel.DatabasePokemon.Count(x => x.IsChecked)} selected");
    }
}