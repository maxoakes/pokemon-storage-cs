using System;
using Avalonia.Controls;
using Avalonia.Input;
using PokemonStorageDesktop.UserControls;
using PokemonStorageLibrary.Models;

namespace PokemonStorageDesktop.Models;

public class PokemonModel
{
    public PartyPokemon Pokemon { get; }
    public SlotCard Card { get; }
    private CheckBox TransferCheckbox { get { return Card.TransferCheckbox; } }
    public bool IsChecked { get { return TransferCheckbox.IsChecked ?? false; }}

    public PokemonModel(PartyPokemon pokemon)
    {
        Pokemon = pokemon;
        Card = new SlotCard();
        Card.UpdateCard(Pokemon);
    }

    public void SetCardClickEvent(EventHandler<PointerPressedEventArgs>? onClicked)
    {
        // Set up click handler
        var border = Card.FindControl<Border>("CardBorder");
        if (border != null && onClicked != null)
        {
            border.PointerPressed += onClicked;
        }
    }

    public static Avalonia.Media.Color GetColorFromTypeString(string identifier)
    {
        return identifier switch
        {
            "normal" => Avalonia.Media.Color.Parse("#b8b786"),
            "fighting" => Avalonia.Media.Color.Parse("#eb8262"),
            "flying" => Avalonia.Media.Color.Parse("#9c9df3"),
            "poison" => Avalonia.Media.Color.Parse("#ca4bc8"),
            "ground" => Avalonia.Media.Color.Parse("#cc8c58"),
            "rock" => Avalonia.Media.Color.Parse("#a5833a"),
            "bug" => Avalonia.Media.Color.Parse("#a5b816"),
            "ghost" => Avalonia.Media.Color.Parse("#7f62a5"),
            "steel" => Avalonia.Media.Color.Parse("#bdbdda"),
            "fire" => Avalonia.Media.Color.Parse("#f05f26"),
            "water" => Avalonia.Media.Color.Parse("#4c85ff"),
            "grass" => Avalonia.Media.Color.Parse("#6bcf32"),
            "electric" => Avalonia.Media.Color.Parse("#ffdc19"),
            "psychic" => Avalonia.Media.Color.Parse("#f37ea1"),
            "ice" => Avalonia.Media.Color.Parse("#bceaf1"),
            "dragon" => Avalonia.Media.Color.Parse("#8556fc"),
            "dark" => Avalonia.Media.Color.Parse("#4b4477"),
            "fairy" => Avalonia.Media.Color.Parse("#ff56c7"),
            _ => Avalonia.Media.Color.Parse("#40000000"),
        };
    }
}