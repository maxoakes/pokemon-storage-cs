using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using PokemonStorageLibrary;

namespace PokemonStorageDesktop.UserControls;

public partial class FileOpenDialog : UserControl
{
    public string SelectedFilepath { get; set; }
    public string SelectedLanguage { get; set; }
    public string SelectedVersionGroup { get; set; }
    public Button? FindFileButton { get { return this.FindControl<Button>("BrowseButton"); } }
    public Button? OpenFileButton { get { return this.FindControl<Button>("OpenButton"); } }
    public TextBox? FilePathTextBox { get { return this.GetVisualDescendants().OfType<TextBox>().First(x => x.Name == "OpenFileTextBox"); } }
    public ComboBox? FileVersionCombobox { get { return this.FindControl<ComboBox>("OpenFileVersionDropdown"); } }
    public ComboBox? LanguageCombobox { get { return this.FindControl<ComboBox>("OpenFileLanguageDropdown"); } }

    public FileOpenDialog()
    {
        InitializeComponent();
        if (Design.IsDesignMode) return;

        if (FileVersionCombobox != null)
        {
            FileVersionCombobox.ItemsSource = Lookup.GetVersionNames();
            FileVersionCombobox.SelectedItem = "HeartGold";
        }

        if (LanguageCombobox != null)
        {
            LanguageCombobox.ItemsSource = Lookup.GetLanguageNames("iso639");
            LanguageCombobox.SelectedItem = "en";
        }
    }

    private async void BrowseSaveFile_Click(object? sender, RoutedEventArgs e)
    {
        Console.WriteLine("Button clicked!");
        FilePathTextBox?.Text = await HandleSaveFileOpenClick();
    }

    public async Task<string> HandleSaveFileOpenClick()
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
                return files[0].Path.LocalPath;
            }
        }

        return "";
    }
    
    public void OpenSaveFile_Click(object? sender, RoutedEventArgs e)
    {
        Console.WriteLine("Setting properties");
        SelectedFilepath = this.GetVisualDescendants().OfType<TextBox>().First(x => x.Name == "OpenFileTextBox").Text ?? "";
        SelectedLanguage = this.GetVisualDescendants().OfType<ComboBox>().First(x => x.Name == "OpenFileLanguageDropdown").SelectedItem?.ToString() ?? "";
        SelectedVersionGroup = this.GetVisualDescendants().OfType<ComboBox>().First(x => x.Name == "OpenFileVersionDropdown").SelectedItem?.ToString() ?? "";
    }
}