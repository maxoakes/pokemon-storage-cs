using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using PokemonStorageLibrary.Models;

namespace PokemonStorageDesktop.Models;

public class PokemonModel
{
    public PartyPokemon Pokemon { get; }
    public Control Card { get; }
    private CheckBox TransferCheckbox { get; set; }
    public bool IsChecked { get { return TransferCheckbox.IsChecked ?? false; }}

    public PokemonModel(PartyPokemon pokemon)
    {
        Pokemon = pokemon;
        Card = BuildCard();
    }

    private StackPanel BuildCard()
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
            VerticalAlignment = VerticalAlignment.Center
        };
        mainSprite.Bind(Image.SourceProperty, new Avalonia.Data.Binding("ImageFromWebsite"));
        RenderOptions.SetBitmapInterpolationMode(mainSprite, BitmapInterpolationMode.None);

        var itemSprite = new Image
        {
            Height = 48,
            Width = 48,
            Stretch = Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Bottom
        };
        itemSprite.Bind(Image.SourceProperty, new Avalonia.Data.Binding("ImageFromWebsite2"));

        panel.Children.Add(mainSprite);
        panel.Children.Add(itemSprite);

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
}