using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Layout;
using PokemonStorageLibrary;
using PokemonStorageLibrary.Models;
using Avalonia.Interactivity;

namespace PokemonStorageDesktop.UserControls;

public partial class SlotCard : UserControl
{
    public PartyPokemon Pokemon { get; private set; }
    public CheckBox? TransferCheckBox => this.FindControl<CheckBox>("TransferCheckbox");
    public Image? SpriteImage => this.FindControl<Image>("PokemonSprite");
    public bool IsChecked => TransferCheckBox?.IsChecked ?? false;

    public SlotCard()
    {
        InitializeComponent();
    }

    public void UpdateCard(PartyPokemon pokemon)
    {
        Pokemon = pokemon;
        SetTextBlockText("NicknameValue", Pokemon.Nickname);
        SetTextBlockText("OriginalTrainerValue", $"O/T: {Pokemon.OriginalTrainer.Name}");
        SetTextBlockText("LevelGenderValue", $"Lv. {Pokemon.Level} ({Pokemon.Gender})");
        SetTextBlockText("NatureValue", Pokemon.Nature.Identifier);
    }

    private void SetTextBlockText(string componentName, string content)
    {
        this.FindControl<TextBlock>(componentName)?.Text = content;
    }

    protected override async void OnLoaded(RoutedEventArgs e)
    {
        if (Design.IsDesignMode) return;

        RenderOptions.SetBitmapInterpolationMode(SpriteImage, BitmapInterpolationMode.None);
        SpriteImage.Source = await GetImageFromUrl(BuildSpriteUrl());
    }

    public string BuildSpriteUrl()
    {
        //https://img.pokemondb.net/sprites/heartgold-soulsilver/normal/pikachu-f.png
        string baseUrl = "https://img.pokemondb.net/sprites";
        string versionRoot = GetVersionRootName();
        string shiny = Pokemon.IsShinyPersonalityValue ? "shiny" : "normal";
        string female = Lookup.DoesSpeciesHaveGenderDifference(Pokemon.PokemonIdentity.SpeciesId) && Pokemon.Origin.Game.VersionId > 6 ? "-f" : "";

        return $"{baseUrl}/{versionRoot}/{shiny}/{Pokemon.PokemonIdentity.SpeciesIdentifier}{female}.png";
    }

    private string GetVersionRootName()
    {
        switch (Pokemon.Origin.Game.VersionId)
        {
            case 1: return "red-blue";
            case 2: return "red-blue";
            case 3: return "yellow";
            case 4: return "gold";
            case 5: return "silver";
            case 6: return "crystal";
            case 7: return "ruby-sapphire";
            case 8: return "ruby-sapphire";
            case 9: return "emerald";
            case 10: return "firered-leafgreen";
            case 11: return "firered-leafgreen";
            case 12: return "diamond-pearl";
            case 13: return "diamond-pearl";
            case 14: return "platinum";
            case 15: return "heartgold-soulsilver";
            case 16: return "heartgold-soulsilver";
            default: return "heartgold-soulsilver";
        }
    }

    private static async Task<IImage?> GetImageFromUrl(string url)
    {
        try
        {
            var response = await Program.HttpClient.GetAsync(url);
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