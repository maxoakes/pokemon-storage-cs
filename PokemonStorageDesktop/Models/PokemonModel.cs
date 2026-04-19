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
}