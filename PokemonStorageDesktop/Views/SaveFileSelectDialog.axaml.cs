using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using PokemonStorageLibrary;

namespace PokemonStorageDesktop.Views;

public partial class SaveFileSelectDialog : Window
{
    public string SelectedFilepath { get; set; }
    public string SelectedLanguage { get; set; }
    public string SelectedVersionGroup { get; set; }

    public SaveFileSelectDialog()
    {
        InitializeComponent();
        CanResize = false;

        if (Design.IsDesignMode) return;

        if (ddVersion != null)
        {
            ddVersion.ItemsSource = Lookup.GetVersionNames();
            ddVersion.SelectedItem = "HeartGold";
        }

        if (ddLanguage != null)
        {
            ddLanguage.ItemsSource = Lookup.GetLanguageNames("iso639");
            ddLanguage.SelectedItem = "en";
        }
    }

    private async void OpenFile_Click(object? sender, RoutedEventArgs e)
    {
        Console.WriteLine($"Open Button");
        SelectedFilepath = tbFilepath.Text ?? "";
        SelectedLanguage = ddLanguage.SelectedItem?.ToString() ?? "";
        SelectedVersionGroup = ddVersion.SelectedItem?.ToString() ?? "";

        if (!string.IsNullOrWhiteSpace(SelectedFilepath))
        {
            Console.WriteLine($"Got {SelectedFilepath}");
            this.Close(true);    
        }
    }

    private async void BrowseSaveFile_Click(object? sender, RoutedEventArgs e)
    {
        TopLevel? topLevel = TopLevel.GetTopLevel(this);

        if (topLevel != null)
        {
            // Start async operation to open the dialog.
            var filePickerOpenOptions = new FilePickerOpenOptions
            {
                Title = "Open Pokemon Save File",
                AllowMultiple = false
            };
            var files = await topLevel.StorageProvider.OpenFilePickerAsync(filePickerOpenOptions);

            if (files.Count >= 1)
            {
                tbFilepath?.Text = files[0].Path.LocalPath;
            }
        }
    }
}