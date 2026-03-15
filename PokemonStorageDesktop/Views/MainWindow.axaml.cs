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
    public ScrollViewer DatabaseViewerControl { get; set; }
    public Task<Bitmap?> ImageFromWebsite { get; } = LoadFromWeb(new Uri("https://veekun.com/dex/media/pokemon/main-sprites/heartgold-soulsilver/1.png"));

    public MainWindow()
    {
        InitializeComponent();
        FileViewerControl.Content = GetNewOpenFileMenu();
        MainModel = new();
    }

    private void OpenSaveFile_OnClick(object? sender, RoutedEventArgs e)
    {
        Console.WriteLine("Click!");
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
            Control card = await pokemonModel.BuildCard(MainModel.Game);
            (FileGridParent.Content as WrapPanel).Children.Add(card);
        }
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