using System;
using System.Collections.Generic;
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

    public MainWindow()
    {
        InitializeComponent();
        FileViewerControl.Content = GetNewOpenFileMenu();
        MainModel = new();
    }

    protected override async void OnOpened(EventArgs e)
    {
        foreach (PokemonModel pokemonModel in MainModel.DatabasePokemon)
        {
            Control card = await pokemonModel.BuildCard();
            (DatabaseViewerControl.Content as WrapPanel).Children.Add(card);
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

    private Grid GetNewOpenFileMenu()
    {
        Grid grid = new Grid
        {
            Name = "OpenFileDialogGrid",
            Background = Brushes.Gainsboro,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            Width = 500,
            Height = 200,
            ColumnDefinitions = new ColumnDefinitions("Auto,Auto,Auto"),
            RowDefinitions = new RowDefinitions("32,Auto,Auto,Auto,64")
        };

        // Row 0 title
        TextBlock title = new TextBlock
        {
            Text = "Find the .sav file you would like to open."
        };
        Grid.SetRow(title, 0);
        Grid.SetColumn(title, 0);
        Grid.SetColumnSpan(title, 3);
        grid.Children.Add(title);

        // Row 1 - Save File label
        TextBlock saveLabel = new TextBlock
        {
            Text = "Save File:"
        };
        Grid.SetRow(saveLabel, 1);
        Grid.SetColumn(saveLabel, 0);
        grid.Children.Add(saveLabel);

        // Row 1 - TextBox
        TextBox savePathBox = new TextBox
        {
            Name = "OpenFileTextBox",
            Text = "",
            
        };
        Grid.SetRow(savePathBox, 1);
        Grid.SetColumn(savePathBox, 1);
        grid.Children.Add(savePathBox);

        // Row 1 - Browse Button
        Button browseButton = new Button
        {
            Content = "Browse...",
        };
        browseButton.Click += BrowseSaveFile_Click;
        Grid.SetRow(browseButton, 1);
        Grid.SetColumn(browseButton, 2);
        grid.Children.Add(browseButton);

        // Row 2 - Version label
        var versionLabel = new TextBlock
        {
            Text = "Version"
        };
        Grid.SetRow(versionLabel, 2);
        Grid.SetColumn(versionLabel, 0);
        grid.Children.Add(versionLabel);

        // Row 2 - Version ComboBox
        var versionCombo = new ComboBox
        {
            Name = "OpenFileVersionDropdown",
            SelectedIndex = 0,
            Width = 200,
            MaxDropDownHeight = 300,
            ItemsSource = Lookup.GetVersionNames(),
            SelectedItem = "HeartGold"
        };

        Grid.SetRow(versionCombo, 2);
        Grid.SetColumn(versionCombo, 1);
        grid.Children.Add(versionCombo);

        // Row 3 - Language label
        var languageLabel = new TextBlock
        {
            Text = "Language"
        };
        Grid.SetRow(languageLabel, 3);
        Grid.SetColumn(languageLabel, 0);
        grid.Children.Add(languageLabel);

        // Row 3 - Language ComboBox
        var languageCombo = new ComboBox
        {
            Name = "OpenFileLanguageDropdown",
            SelectedIndex = 0,
            Width = 200,
            MaxDropDownHeight = 300,
            ItemsSource = Lookup.GetLanguageNames("iso639"),
            SelectedItem = "en"
        };

        Grid.SetRow(languageCombo, 3);
        Grid.SetColumn(languageCombo, 1);
        grid.Children.Add(languageCombo);

        // Row 4 - Open Button
        var openButton = new Button
        {
            Content = "Open"
        };
        openButton.Click += OpenSaveFile_Click;

        Grid.SetRow(openButton, 4);
        Grid.SetColumn(openButton, 2);
        grid.Children.Add(openButton);

        return grid;
    }

    private async void BrowseSaveFile_Click(object? sender, RoutedEventArgs e)
    {
        Console.WriteLine("Button clicked!");
        this.GetVisualDescendants().OfType<TextBox>().First(x => x.Name == "OpenFileTextBox").Text = await HandleSaveFileOpenClick();
    }

    public async Task<string> HandleSaveFileOpenClick()
    {
        TopLevel? topLevel = GetTopLevel(this);

        if (topLevel != null)
        {
            // Start async operation to open the dialog.
            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Open Pokemon Save File",
                AllowMultiple = false
            });

            if (files.Count >= 1)
            {
                return files[0].Path.LocalPath;
            }
        }

        return "";
    }

    private async void OpenSaveFile_Click(object? sender, RoutedEventArgs e)
    {
        Console.WriteLine("Opening file");
        SelectedFilepath = this.GetVisualDescendants().OfType<TextBox>().First(x => x.Name == "OpenFileTextBox").Text ?? "";
        SelectedLanguage = this.GetVisualDescendants().OfType<ComboBox>().First(x => x.Name == "OpenFileLanguageDropdown").SelectedItem?.ToString() ?? "";
        SelectedVersionGroup = this.GetVisualDescendants().OfType<ComboBox>().First(x => x.Name == "OpenFileVersionDropdown").SelectedItem?.ToString() ?? "";
        MainModel.LoadSaveFile(SelectedFilepath, SelectedVersionGroup, SelectedLanguage);
        FileGridParent.Content = GetNewPokemonGrid(TabType.File);

        foreach (PokemonModel pokemonModel in MainModel.SaveFilePokemon)
        {
            Control card = await pokemonModel.BuildCard();
            (FileGridParent.Content as WrapPanel).Children.Add(card);
        }
    }

    private async void ExportSelected_Click(object? sender, RoutedEventArgs e)
    {
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
                var jsonFileOutput = await GetTopLevel(this).StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
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
                var showdownFileOutput = await GetTopLevel(this).StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
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
                var jsonFileOutput = await GetTopLevel(this).StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
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
                var showdownFileOutput = await GetTopLevel(this).StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
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

    public static async Task<Bitmap?> LoadFromWeb(Uri url)
    {
        using var httpClient = new HttpClient();
        try
        {
            var response = await httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var data = await response.Content.ReadAsByteArrayAsync();
            return new Bitmap(new MemoryStream(data));
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"An error occurred while downloading image '{url}' : {ex.Message}");
            return null;
        }
    }
}