using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using PokemonStorageDesktop.Models;
using PokemonStorageLibrary;
using PokemonStorageDesktop.UserControls;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PokemonStorageDesktop.Views;

public partial class MainWindow : Window
{
    public List<StorageModel> StorageModels;
    public List<CardViewerContainer> CardViewerContainers;

    public MainWindow()
    {
        InitializeComponent();
        StorageModels = [];
        CardViewerContainers = [];
    }

    public async Task AddTab(StorageModel storageModel)
    {
        if (StorageModels.Any(x => x.DisplayTitle == storageModel.DisplayTitle))
        {
            DialogBoxOk dialogBoxOk = new DialogBoxOk(
                "A connection to this source already exists!",
                "Connection Already Exists"
            );
            await dialogBoxOk.ShowDialog<bool>(this);
            return;
        };

        CardViewerContainer cardViewerContainer = new CardViewerContainer(this, storageModel);
        TextBlock tabText = new TextBlock
        {
            Text = storageModel.DisplayTitle
        };
        StackPanel headerPanel = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal
        };
        headerPanel.Children.Add(tabText);
        TabItem tabItem = new TabItem
        {
            Name = storageModel.DisplayTitle,
            Header = headerPanel,
            Content = cardViewerContainer
        };      

        foreach (var boxes in storageModel.PokemonLists.Values)
        {
            foreach (PokemonModel pokemonModel in boxes)
            {
                pokemonModel.SetCardClickEvent((s, e) => AboutPanel.OnNewSelection(pokemonModel.Pokemon));
            }
        }

        tabControl.Items.Add(tabItem);
        StorageModels.Add(storageModel);
        CardViewerContainers.Add(cardViewerContainer);
        
        CardViewerContainers.ForEach(x => x.SetExportOptions(StorageModels));

        // Automatically switch to new tab
        tabControl.SelectedIndex = tabControl.Items.Count-1;
    }

    protected override async void OnOpened(EventArgs e)
    {
        if (Design.IsDesignMode) return;

        // Automatically load database
        await AddTab(new DatabaseModel(Lookup.DefaultStorageConnectionString));
    }

    private async void OpenSource_Click(object? sender, RoutedEventArgs e)
    {
        Control? senderControl = sender as Control;
        switch (senderControl?.Name)
        {
            case "OpenSaveFileDefault":
            case "OpenSaveFile":
                SaveFileSelectDialog saveFileSelectDialog = new SaveFileSelectDialog
                {
                    WindowStartupLocation = WindowStartupLocation.CenterScreen
                };

                if (await saveFileSelectDialog.ShowDialog<bool>(this))
                {
                    try
                    {
                        await AddTab(new SaveFileModel(
                            saveFileSelectDialog.SelectedFilepath,
                            saveFileSelectDialog.SelectedVersionGroup,
                            saveFileSelectDialog.SelectedLanguage
                        ));
                    }
                    catch (Exception ex)
                    {
                        DialogBoxOk failedOpenFileDialog = new (
                            $"Failed to open save file. Was the correct version and language selected?\n\n{ex.Message}\n\n{ex.StackTrace}",
                            "File Parse Failed"
                        );
                        await failedOpenFileDialog.ShowDialog<bool>(this);
                        return;
                    }

                }
                break;
            case "OpenDatabase":
                DatabaseSelectDialog databaseSelectDialog = new DatabaseSelectDialog
                {
                    WindowStartupLocation = WindowStartupLocation.CenterScreen
                };

                if (await databaseSelectDialog.ShowDialog<bool>(this))
                {
                    await AddTab(new DatabaseModel(databaseSelectDialog.ConnectionString));
                }
                break;
            case "DeleteSelectedSource":
                StorageModels.RemoveAll(x => x.DisplayTitle == (tabControl.SelectedItem as TabItem)?.Name);
                tabControl.Items.Remove(tabControl.SelectedItem);
                break;
        }
    }

    private void Exit_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}