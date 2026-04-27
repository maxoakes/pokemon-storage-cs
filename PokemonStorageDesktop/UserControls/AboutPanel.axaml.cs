using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using PokemonStorageLibrary;
using PokemonStorageLibrary.Models;

namespace PokemonStorageDesktop.UserControls;

public partial class AboutPanel : UserControl
{
    public AboutPanel()
    {
        InitializeComponent();
    }

    public void OnNewSelection(PartyPokemon? pokemon)
    {
        if (pokemon == null)
        {
            return;
        }

        // Clear the overview badges
        WrapPanel? overviewBadgePanel = this.FindControl<WrapPanel>("OverviewBadgeBanner");
        if (overviewBadgePanel != null)
        {
            overviewBadgePanel.Children.RemoveAll(overviewBadgePanel.Children.OfType<Border>());

            // Add badges to the overview section
            overviewBadgePanel.Children.Add(NewBadge("Language", pokemon.LanguageId.ToString(), "Badge Language"));
            overviewBadgePanel.Children.Add(NewBadge("Gender", pokemon.Gender.ToString(), $"Badge {pokemon.Gender.ToString()}"));
            if ((pokemon.IsShinyPersonalityValue && pokemon.Origin.Game.GenerationId > 2) 
                || (pokemon.IsShinyAttackIv && pokemon.Origin.Game.GenerationId < 3))
            {
                overviewBadgePanel.Children.Add(NewBadge("ShinyValue", "Shiny", "Badge Shiny"));
            }
            if (pokemon.AlternateFormId > 0)
            {
                overviewBadgePanel.Children.Add(NewBadge("FormValue", pokemon.AlternateFormId.ToString("X"), "Badge Form"));
            }
            if (pokemon.IsEgg)
            {
                overviewBadgePanel.Children.Add(NewBadge("EggValue", "Egg", "Badge Egg"));
            }
            if (pokemon.Origin.FatefulEncounter)
            {
                overviewBadgePanel.Children.Add(NewBadge("FatefulValue", "Fateful Encounter", "Badge Fateful"));
            }
            if (pokemon.Obedience)
            {
                overviewBadgePanel.Children.Add(NewBadge("ObedientValue", "Obedient", "Badge Obedient"));
            }
        }


        // Clear the ribbon badges
        WrapPanel? ribbonBadgePanel = this.FindControl<WrapPanel>("RibbonBadgeBanner");
        if (ribbonBadgePanel != null)
        {
            ribbonBadgePanel?.Children.RemoveAll(ribbonBadgePanel.Children.OfType<Border>());

            List<Border> newRibbonBadges = new List<Border>();
            foreach (var item in pokemon.Ribbons.Ribbons.Where(x => x.Value))
            {
                bool isHoenn = item.Key.Contains("Hoenn") || 
                    item.Key.Equals("Champion") || 
                    item.Key.Equals("Winning") || 
                    item.Key.Equals("Victory") || 
                    item.Key.Equals("Artist") ||
                    item.Key.Equals("Effort") ||
                    item.Key.Equals("Marine") ||
                    item.Key.Equals("Land") ||
                    item.Key.Equals("Sky") ||
                    item.Key.Equals("Country") ||
                    item.Key.Equals("National") ||
                    item.Key.Equals("Earth") ||
                    item.Key.Equals("World");
                newRibbonBadges.Add(NewBadge(item.Key.Replace(" ","")+"Value", item.Key, $"Badge {(isHoenn ? "Hoenn" : "Sinnoh")}"));
            }
            ribbonBadgePanel?.Children.AddRange(newRibbonBadges);
        }

        // Header
        tbHeaderTitle.Text = pokemon.PokemonIdentity.SpeciesName;
        tbHeaderSubtitle.Text = pokemon.Nickname;
        tbOriginalTrainer.Text = $"{pokemon.OriginalTrainer.Name} ({pokemon.OriginalTrainer.PublicId}-{pokemon.OriginalTrainer.PublicId})";

        // Overview Section
        tbPersonalityValue.Text = pokemon.PersonalityValue.ToString();
        tbNatureValue.Text = Lookup.GetDatabaseIdentityById(pokemon.Nature.Id, DatabaseObject.Natures).Name;
        tbAbilityValue.Text = pokemon.AbilityIdentity.Name;
        tbLevelValue.Text = pokemon.Level.ToString();
        tbExperienceValue.Text = pokemon.ExperiencePoints.ToString();
        tbFriendshipValue.Text = pokemon.Friendship.ToString();
        tbHeldItemValue.Text = pokemon.HeldItemIdentity.Name ?? "None";
        string pokerusString =
            pokemon.PokerusStrain == 0 
            ? "No" 
            : $"Strain {pokemon.PokerusStrain} ({pokemon.PokerusDaysRemaining} days remain)";
        tbPokerusValue.Text = pokerusString;

        // Origin Section
        tbOriginGameValue.Text = pokemon.Origin.Game.GameName;
        tbOriginMetLevelValue.Text = pokemon.Origin.MetLevel.ToString();
        tbOriginMetDateValue.Text = pokemon.Origin.MetDateTime?.ToString("yyyy-MM-dd") ?? "";
        tbOriginMetLocationValue.Text = pokemon.Origin.MetLocationIdentity.Name;
        tbOriginPokeballValue.Text = pokemon.Origin.CatchBallIdentity.Name;
        tbOriginEncounterTypeValue.Text = pokemon.Origin.EncounterMethodIdentity.Name;
        tbOriginEggReceiveValue.Text = pokemon.Origin.EggReceiveDate?.ToString("yyyy-MM-dd") ?? "";
        tbOriginEggHatchLocationValue.Text = pokemon.Origin.EggHatchLocationIdentity.Name;
        
        // Stats Section - use Modern stats
        tbStatsSectionTitle.Text = $"Stats {(pokemon.Stats.IsModernSystemByDefault ? "Modern" : "Old")}";
        var statSet = pokemon.Stats.Modern;
        if (statSet != null)
        {
            tbHPCalcValue.Text = statSet.HP?.Value.ToString() ?? "";
            tbHPIVValue.Text = statSet.HP?.Iv.ToString() ?? "";
            tbHPEVValue.Text = statSet.HP?.Ev.ToString() ?? "";

            tbAttackCalcValue.Text = statSet.Attack?.Value.ToString() ?? "";
            tbAttackIVValue.Text = statSet.Attack?.Iv.ToString() ?? "";
            tbAttackEVValue.Text = statSet.Attack?.Ev.ToString() ?? "";

            tbDefenseCalcValue.Text = statSet.Defense?.Value.ToString() ?? "";
            tbDefenseIVValue.Text = statSet.Defense?.Iv.ToString() ?? "";
            tbDefenseEVValue.Text = statSet.Defense?.Ev.ToString() ?? "";

            tbSpAtkCalcValue.Text = statSet.SpecialAttack?.Value.ToString() ?? "";
            tbSpAtkIVValue.Text = statSet.SpecialAttack?.Iv.ToString() ?? "";
            tbSpAtkEVValue.Text = statSet.SpecialAttack?.Ev.ToString() ?? "";

            tbSpDefCalcValue.Text = statSet.SpecialDefense?.Value.ToString() ?? "";
            tbSpDefIVValue.Text = statSet.SpecialDefense?.Iv.ToString() ?? "";
            tbSpDefEVValue.Text = statSet.SpecialDefense?.Ev.ToString() ?? "";

            tbSpeedCalcValue.Text = statSet.Speed?.Value.ToString() ?? "";
            tbSpeedIVValue.Text = statSet.Speed?.Iv.ToString() ?? "";
            tbSpeedEVValue.Text = statSet.Speed?.Ev.ToString() ?? "";
        }

        // Moves Section
        // Clear all move slots first
        tbMove1Name.Text = "";
        tbMove1PP.Text = "";
        tbMove1PowerUps.Text = "";
        tbMove2Name.Text = "";
        tbMove2PP.Text = "";
        tbMove2PowerUps.Text = "";
        tbMove3Name.Text = "";
        tbMove3PP.Text = "";
        tbMove3PowerUps.Text = "";
        tbMove4Name.Text = "";
        tbMove4PP.Text = "";
        tbMove4PowerUps.Text = "";

        // Populate moves
        int moveIndex = 1;
        foreach (Move move in pokemon.Moves.Values)
        {
            if (move.Id == 0) continue;

            switch (moveIndex)
            {
                case 1:
                    tbMove1Name.Text = move.Identity.Name;
                    tbMove1PP.Text = move.Pp.ToString();
                    tbMove1PowerUps.Text = move.TimesIncreased.ToString();
                    break;
                case 2:
                    tbMove2Name.Text = move.Identity.Name;
                    tbMove2PP.Text = move.Pp.ToString();
                    tbMove2PowerUps.Text = move.TimesIncreased.ToString();
                    break;
                case 3:
                    tbMove3Name.Text = move.Identity.Name;
                    tbMove3PP.Text = move.Pp.ToString();
                    tbMove3PowerUps.Text = move.TimesIncreased.ToString();
                    break;
                case 4:
                    tbMove4Name.Text = move.Identity.Name;
                    tbMove4PP.Text = move.Pp.ToString();
                    tbMove4PowerUps.Text = move.TimesIncreased.ToString();
                    break;
            }
            moveIndex++;
        }

        // Additional Info Section
        tbMarkingsValue.Text = pokemon.Coolness.ToString();
        tbShinyLeavesValue.Text = pokemon.Coolness.ToString();
        tbGen3MiscValue.Text = pokemon.Coolness.ToString();
        tbWalkingMoodValue.Text = pokemon.Coolness.ToString();
        tbCoolnessValue.Text = pokemon.Coolness.ToString();
        tbBeautyValue.Text = pokemon.Beauty.ToString();
        tbCutenessValue.Text = pokemon.Cuteness.ToString();
        tbSmartinessValue.Text = pokemon.Smartness.ToString();
        tbToughnessValue.Text = pokemon.Toughness.ToString();
        tbSheenValue.Text = pokemon.Sheen.ToString();
    }

    private static Border NewBadge(string name, string content, string className)
    {
        Border border = new Border
        {
            Name=$"{name}Badge",
        };
        border.Classes.AddRange(className.Split(" "));
        TextBlock text = new TextBlock
        {
            Name = $"{name}Value",
            Text = content
        };
        border.Child = text;
        return border;
    }
}