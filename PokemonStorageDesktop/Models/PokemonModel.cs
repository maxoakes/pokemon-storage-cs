using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using PokemonStorageLibrary;
using PokemonStorageLibrary.Models;

namespace PokemonStorageDesktop.Models;

public class PokemonModel
{
    public PartyPokemon Pokemon { get; }
    public Control Card { get; }
    private CheckBox TransferCheckbox { get; set; }
    public bool IsChecked { get { return TransferCheckbox.IsChecked ?? false; }}

    public PokemonModel(PartyPokemon pokemon, Game game)
    {
        Pokemon = pokemon;
    }

    public async Task<StackPanel> BuildCard(Game game)
    {
        var userControl = new UserControl
        {
            Width = 135,
            Height = 250
        };

        var border = new Border
        {
            BorderThickness = new Thickness(2),
            BorderBrush = Brushes.Yellow,
            Background = Brushes.Green,
            Padding = new Thickness(2)
        };

        var grid = new Grid
        {
            RowDefinitions = new RowDefinitions("*,*")
        };

        // Top panel
        var panel = new Panel
        {
            ClipToBounds = false
        };
        Grid.SetRow(panel, 0);

        var mainSprite = new Image
        {
            Height = 128,
            Width = 128,
            Stretch = Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Source = await GetImageFromUrl(BuildSpriteUrl(game))
        };
        // mainSprite.Bind(Image.SourceProperty, new Avalonia.Data.Binding(GetImageFromUrl(BuildSpriteUrl(game))));
        RenderOptions.SetBitmapInterpolationMode(mainSprite, BitmapInterpolationMode.None);

        // var itemSprite = new Image
        // {
        //     Height = 48,
        //     Width = 48,
        //     Stretch = Stretch.Uniform,
        //     HorizontalAlignment = HorizontalAlignment.Left,
        //     VerticalAlignment = VerticalAlignment.Bottom
        // };
        // itemSprite.Bind(Image.SourceProperty, new Avalonia.Data.Binding("ImageFromWebsite2"));
        // panel.Children.Add(itemSprite);

        panel.Children.Add(mainSprite);
        

        // Bottom stack
        var stackPanel = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            Background = Brushes.Blue
        };
        Grid.SetRow(stackPanel, 1);

        stackPanel.Children.Add(new TextBlock
        {
            Text = Pokemon.Nickname,
            FontWeight = FontWeight.Bold,
            FontSize = 12,
            HorizontalAlignment = HorizontalAlignment.Left,
            Background = Brushes.Gray
        });

        stackPanel.Children.Add(new TextBlock
        {
            Text = $"Lv. {Pokemon.Level} ({Pokemon.Gender})",
            FontSize = 12,
            HorizontalAlignment = HorizontalAlignment.Left,
            Background = Brushes.Gray
        });

        stackPanel.Children.Add(new TextBlock
        {
            Text = $"Exp: {Pokemon.ExperiencePoints}",
            FontSize = 12,
            HorizontalAlignment = HorizontalAlignment.Left,
            Background = Brushes.Gray
        });

        stackPanel.Children.Add(new TextBlock
        {
            Text = $"{Pokemon.Nature.Identifier}",
            FontSize = 12,
            HorizontalAlignment = HorizontalAlignment.Left,
            Background = Brushes.Gray
        });

        stackPanel.Children.Add(new TextBlock
        {
            Text = $"O/T: {Pokemon.OriginalTrainer.Name}",
            FontSize = 12,
            HorizontalAlignment = HorizontalAlignment.Left,
            Background = Brushes.Gray
        });

        CheckBox checkBox = new CheckBox
        {
            HorizontalAlignment = HorizontalAlignment.Right
        };

        stackPanel.Children.Add(checkBox);
        TransferCheckbox = checkBox;

        // Assemble tree
        grid.Children.Add(panel);
        grid.Children.Add(stackPanel);

        border.Child = grid;
        userControl.Content = border;

        StackPanel parentControl = new StackPanel();
        parentControl.Children.Add(userControl);

        return parentControl;
    }

    public string BuildSpriteUrl(Game game)
    {
        //https://img.pokemondb.net/sprites/heartgold-soulsilver/normal/pikachu-f.png
        string baseUrl = "https://img.pokemondb.net/sprites";
        string versionRoot = Lookup.GetVersionGroupIdentifierByVersionGroupId(game.VersionGroupId);
        string shiny = Pokemon.IsShinyPersonalityValue ? "shiny" : "normal";
        string female = Lookup.DoesSpeciesHaveGenderDifference(Pokemon.PokemonIdentity.SpeciesId) ? "-f" : "";

        return $"{baseUrl}/{versionRoot}/{shiny}/{Pokemon.PokemonIdentity.SpeciesIdentifier}{female}.png";
    }

    private async Task<IImage?> GetImageFromUrl(string url)
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