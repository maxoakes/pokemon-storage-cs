using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using System.Net.Http;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using PokemonStorageDesktop.Models;
using System.Collections.Generic;
using System.Linq;
using PokemonStorageLibrary;

namespace PokemonStorageDesktop.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public MainModel MainModel { get; set; }
    public List<string> Versions { get; }
    public string? SelectedVersion { get; set; }
    public List<string> Languages { get; }
    public string? SelectedLanguage { get; set; }
    public Task<Bitmap?> ImageFromWebsite { get; } = LoadFromWeb(new Uri("https://veekun.com/dex/media/pokemon/main-sprites/heartgold-soulsilver/1.png"));
    public Task<Bitmap?> ImageFromWebsite2 { get; } = LoadFromWeb(new Uri("https://veekun.com/dex/media/pokemon/main-sprites/heartgold-soulsilver/2.png"));

    public MainWindowViewModel()
    {
        Console.WriteLine("MainWindowViewModel constructor");
        Versions = ["Lookup.GetVersionNames()"];
        Languages = ["Lookup.GetLanguageNames()"];
        MainModel = new();
    }

    public async Task<bool> HandleSaveFileOpenClick(TopLevel topLevel)
    {
        // Start async operation to open the dialog.
        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open Pokemon Save File",
            AllowMultiple = false
        });

        if (files.Count >= 1)
        {
            MainModel.LoadSaveFile(files[0].Path.AbsolutePath);
        }

        return true;
    }

    public static Bitmap LoadFromResource(Uri resourceUri)
    {
        return new Bitmap(AssetLoader.Open(resourceUri));
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
