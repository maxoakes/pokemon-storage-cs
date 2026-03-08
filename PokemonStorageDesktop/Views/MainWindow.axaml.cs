using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Media;
using PokemonStorageLibrary;

namespace PokemonStorageDesktop.Views;

public enum TabType
{
    File,
    Database
}

public partial class MainWindow : Window
{    
    public MainWindow()
    {
        InitializeComponent();
    }

    private void OpenSaveFile_OnClick(object? sender, RoutedEventArgs e)
    {
        Console.WriteLine("Click!");
    }

    private Grid GetNewPokemonGrid(TabType tabType)
    {
        return new Grid
        {
            Name = Enum.GetName(tabType),
            Background = Brushes.Red,
            ColumnDefinitions = new ColumnDefinitions("Auto,Auto,Auto,Auto,Auto,Auto")
        };
    }

    private Grid GetNewOpenFileMenu()
    {
        var grid = new Grid
        {
            Background = Brushes.Gainsboro,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
            Width = 500,
            Height = 200,
            ColumnDefinitions = new ColumnDefinitions("Auto,Auto,Auto"),
            RowDefinitions = new RowDefinitions("32,Auto,Auto,Auto,64")
        };

        // Row 0 title
        var title = new TextBlock
        {
            Text = "Find the .sav file you would like to open."
        };
        Grid.SetRow(title, 0);
        Grid.SetColumn(title, 0);
        Grid.SetColumnSpan(title, 3);
        grid.Children.Add(title);

        // Row 1 - Save File label
        var saveLabel = new TextBlock
        {
            Text = "Save File:"
        };
        Grid.SetRow(saveLabel, 1);
        Grid.SetColumn(saveLabel, 0);
        grid.Children.Add(saveLabel);

        // Row 1 - TextBox
        var savePathBox = new TextBox
        {
            Text = "File path"
        };
        Grid.SetRow(savePathBox, 1);
        Grid.SetColumn(savePathBox, 1);
        grid.Children.Add(savePathBox);

        // Row 1 - Browse Button
        var browseButton = new Button
        {
            Content = "Browse..."
        };
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
            Name = "dropdownVersions",
            SelectedIndex = 0,
            Width = 200,
            MaxDropDownHeight = 300
        };

        versionCombo.Bind(ComboBox.ItemsSourceProperty, new Binding("Versions"));
        versionCombo.Bind(ComboBox.SelectedValueProperty, new Binding("SelectedVersion"));

        versionCombo.ItemTemplate = new FuncDataTemplate<object>((item, _) =>
            new TextBlock { Text = item?.ToString() });

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
            Name = "dropdownLanguage",
            SelectedIndex = 0,
            Width = 200,
            MaxDropDownHeight = 300
        };

        languageCombo.Bind(ComboBox.ItemsSourceProperty, new Binding("Languages"));
        languageCombo.Bind(ComboBox.SelectedValueProperty, new Binding("SelectedLanguage"));

        languageCombo.ItemTemplate = new FuncDataTemplate<object>((item, _) =>
            new TextBlock { Text = item?.ToString() });

        Grid.SetRow(languageCombo, 3);
        Grid.SetColumn(languageCombo, 1);
        grid.Children.Add(languageCombo);

        // Row 4 - Open Button
        var openButton = new Button
        {
            Content = "Open"
        };
        Grid.SetRow(openButton, 4);
        Grid.SetColumn(openButton, 2);
        grid.Children.Add(openButton);

        return grid;
    }

}