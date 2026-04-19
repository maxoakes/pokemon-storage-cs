using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.VisualTree;
using PokemonStorageDesktop.Models;
using PokemonStorageDesktop.UserControls;
using PokemonStorageLibrary.Models;

namespace PokemonStorageDesktop.Views;

public partial class CardGroup : UserControl
{
    public List<PartyPokemon> SelectedPokemon { get { return panelCardContainer.GetVisualDescendants().OfType<SlotCard>().Where(x => x.IsChecked).Select(x => x.Pokemon).ToList(); } }
    
    public CardGroup()
    {
        InitializeComponent();
    }

    public void SetBannerText(string text)
    {
        textBlockTitle.Text = text;
    }

    public void AddCard(PokemonModel pokemonModel)
    {
        panelCardContainer.Children.Add(pokemonModel.Card);
    }

    public void SetCards(List<PokemonModel> pokemonModels)
    {
        panelCardContainer.Children.Clear();
        panelCardContainer.Children.AddRange(pokemonModels.Select(x => x.Card));
    }
}