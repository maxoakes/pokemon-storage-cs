using System;
using Avalonia;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using PokemonStorageDesktop.Models;
using PokemonStorageLibrary;
using PokemonStorageLibrary.Models;
using Newtonsoft.Json;
using PokemonStorageDesktop.UserControls;
using System.Collections.Generic;

namespace PokemonStorageDesktop.Views;

public enum TabType
{
    File,
    Database
}

public partial class MainWindow : Window
{
    // public DatabaseModel MainModel { get; set; }
    public List<DatabaseModel> DatabaseModels;
    public List<StorageModel> StorageModels;

    public MainWindow()
    {
        InitializeComponent();
    }

    public void AddFileTab(StorageModel storageModel)
    {
        TabItem tabItem = new TabItem();
        
        // Create the header
        StackPanel headerPanel = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            Spacing = 8
        };
        
        TextBlock tabText = new TextBlock
        {
            Text = storageModel.Game.GameName
        };
        headerPanel.Children.Add(tabText);
        
        tabItem.Header = headerPanel;
        CardViewerContainer cardViewerContainer = new CardViewerContainer
        {
            TabTitle = storageModel.Game.GameName
        };
        cardViewerContainer.AddPokemonGroups(storageModel.BoxPokemon);
        tabItem.Content = cardViewerContainer;
        
        foreach (var boxes in storageModel.BoxPokemon.Values)
        {
            foreach (PokemonModel pokemonModel in boxes)
            {
                pokemonModel.SetCardClickEvent((s, e) => AboutPanel.OnNewSelection(pokemonModel.Pokemon));
            }
        }

        // Add to TabControl
        tabControl.Items.Add(tabItem);
    }

    public void AddDatabaseTab(DatabaseModel databaseModel, string tabTitle = "Database")
    {
        TabItem tabItem = new TabItem();
        
        // Create the header
        StackPanel headerPanel = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            Spacing = 8
        };
        
        TextBlock tabText = new TextBlock
        {
            Text = tabTitle
        };
        headerPanel.Children.Add(tabText);
        
        tabItem.Header = headerPanel;
        CardViewerContainer cardViewerContainer = new CardViewerContainer
        {
            TabTitle = tabTitle
        };
        Dictionary<string, List<PokemonModel>> databasePokemon = [];
        databasePokemon.Add(tabTitle, databaseModel.DatabasePokemon);

        cardViewerContainer.AddPokemonGroups(databasePokemon);
        tabItem.Content = cardViewerContainer;
        
        foreach (PokemonModel pokemonModel in databaseModel.DatabasePokemon)
        {
            pokemonModel.SetCardClickEvent((s, e) => AboutPanel.OnNewSelection(pokemonModel.Pokemon));
        }

        // Add to TabControl
        tabControl.Items.Add(tabItem);
    }

    protected override async void OnOpened(EventArgs e)
    {
        if (Design.IsDesignMode) return;

        // Automatically load database
        AddDatabaseTab(new DatabaseModel());
    }

    private async void OpenSource_Click(object? sender, RoutedEventArgs e)
    {
        Control? senderControl = sender as Control;
        switch (senderControl?.Name)
        {
            case "OpenSaveFileDefault":
            case "OpenSaveFile":
                
                SaveFileSelectDialog saveFileSelectDialog = new SaveFileSelectDialog();
                bool result = await saveFileSelectDialog.ShowDialog<bool>(this);
                Console.WriteLine($"Open file {result}");

                if (result)
                {
                    AddFileTab(new StorageModel(
                        saveFileSelectDialog.SelectedFilepath, 
                        saveFileSelectDialog.SelectedVersionGroup, 
                        saveFileSelectDialog.SelectedLanguage)
                    );
                }
                break;
            case "OpenDatabase":
                Console.WriteLine("Open database");
                break;
            case "DeleteSelectedSource":
                tabControl.Items.Remove(tabControl.SelectedItem);
                break;
        }
    }
}