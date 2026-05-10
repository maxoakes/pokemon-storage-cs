using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using PokemonStorageLibrary;
using PokemonStorageLibrary.Models;
using Avalonia.Interactivity;
using PokemonStorageDesktop.Models;

namespace PokemonStorageDesktop.UserControls;

public partial class SlotCard : UserControl
{
    public PartyPokemon Pokemon { get; private set; }
    public bool IsChecked => cbTransfer?.IsChecked ?? false;

    public SlotCard()
    {
        InitializeComponent();
    }

    public void UpdateCard(PartyPokemon pokemon)
    {
        Pokemon = pokemon;
        tbNickname.Text = Pokemon.Nickname;
        tbOriginalTrainer.Text = $"O/T: {Pokemon.OriginalTrainer.Name}";
        tbInformation1.Text = $"Lv. {Pokemon.Level} {Pokemon.GetGenderCharacter()}\n{Lookup.GetDatabaseIdentityById(Pokemon.Nature.Id, DatabaseObject.Natures).Name}";
        tbInformation2.Text = Pokemon.DatabaseTag;

        borderCard.Background = new SolidColorBrush(PokemonModel.GetColorFromTypeString(Pokemon.Type1.Identifier));
        borderName.Background = new SolidColorBrush(PokemonModel.GetColorFromTypeString(Pokemon.Type2.Identifier));
    }

    protected override async void OnLoaded(RoutedEventArgs e)
    {
        if (Design.IsDesignMode) return;

        RenderOptions.SetBitmapInterpolationMode(imageSprite, BitmapInterpolationMode.None);
        int targetVersionId = Pokemon.Origin.Game.VersionId;
        while (true)
        {
            imageSprite.Source = await GetImageFromUrl(BuildSpriteUrl(targetVersionId));
            if (imageSprite.Source != null || targetVersionId < 1) break;
            else 
            {
                Console.WriteLine("Attempting next lowest version group for sprite");
                targetVersionId--;
            }
        }
        
    }

    public string BuildSpriteUrl(int versionId)
    {
        //https://img.pokemondb.net/sprites/heartgold-soulsilver/normal/pikachu-f.png
        string baseUrl = "https://img.pokemondb.net/sprites";
        string versionRoot = GetVersionRootName(versionId);
        string shiny = Pokemon.IsShinyPersonalityValue ? "shiny" : "normal";
        string female = 
            Pokemon.DoesSpeciesHaveGenderDifference() && 
            Pokemon.Gender == Gender.FEMALE && 
            Pokemon.Origin.Game.VersionGroupId > 7 
                ? "-f" 
                : "";

        return $"{baseUrl}/{versionRoot}/{shiny}/{Pokemon.PokemonIdentity.SpeciesIdentifier}{female}.png";
    }

    private string GetVersionRootName(int versionId)
    {
        switch (versionId)
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