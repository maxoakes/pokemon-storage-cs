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
        WrapPanel? overviewBadgePanel = this.FindControl<WrapPanel>("OverviewBadges");
        if (overviewBadgePanel != null)
        {
            List<Border> overviewBadges = overviewBadgePanel.Children.OfType<Border>().ToList() ?? new List<Border>();
            foreach (var badge in overviewBadges)
            {
                overviewBadges.Remove(badge);
            }

            // Add badges to the overview section
            overviewBadgePanel.Children.Add(NewBadge("Language", pokemon.LanguageId.ToString(), "Language"));
            overviewBadgePanel.Children.Add(NewBadge("Gender", pokemon.Gender.ToString(), pokemon.Gender.ToString()));
            if ((pokemon.IsShinyPersonalityValue && pokemon.Origin.Game.GenerationId > 2) 
                || (pokemon.IsShinyAttackIv && pokemon.Origin.Game.GenerationId < 3))
            {
                overviewBadgePanel.Children.Add(NewBadge("ShinyValue", "Shiny", "Badge Shiny"));
            }
            if (pokemon.AlternateFormId > 0)
            {
                overviewBadgePanel.Children.Add(NewBadge("FormValue", pokemon.AlternateFormId.ToString("X"), "Form"));
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
            List<Border> ribbonBadges = ribbonBadgePanel?.Children.OfType<Border>().ToList() ?? new List<Border>();
            foreach (var badge in ribbonBadges)
            {
                ribbonBadges.Remove(badge);
            }

            List<Border> newRibbonBadges = new List<Border>();
            foreach (var item in pokemon.Ribbons.Ribbons)
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
        SetTextBlockText("HeaderTitle", pokemon.PokemonIdentity.SpeciesName);
        SetTextBlockText("HeaderSubtitle", pokemon.Nickname);
        SetTextBlockText("OriginalTrainer", $"{pokemon.OriginalTrainer.Name} ({pokemon.OriginalTrainer.PublicId}-{pokemon.OriginalTrainer.PublicId})");

        // Overview Section
        SetTextBlockText("PersonalityValue", pokemon.PersonalityValue.ToString());
        SetTextBlockText("NatureValue", pokemon.Nature.Identifier);
        SetTextBlockText("AbilityValue", pokemon.AbilityIdentifier);
        SetTextBlockText("LevelValue", pokemon.Level.ToString());
        SetTextBlockText("ExperienceValue", pokemon.ExperiencePoints.ToString());
        SetTextBlockText("FriendshipValue", pokemon.Friendship.ToString());
        SetTextBlockText("HeldItemValue", pokemon.HeldItemIdentifier ?? "None");
        string pokerusString = 
            pokemon.PokerusStrain == 0 
            ? "No" 
            : $"Strain {pokemon.PokerusStrain} ({pokemon.PokerusDaysRemaining} days remain)";
        SetTextBlockText("PokerusValue", pokerusString);

        // Origin Section
        SetTextBlockText("OriginGameValue", pokemon.Origin.Game.GameName);
        SetTextBlockText("OriginMetLevelValue", pokemon.Origin.MetLevel.ToString());
        SetTextBlockText("OriginMetDateValue", pokemon.Origin.MetDateTime?.ToString("yyyy-MM-dd") ?? "");
        SetTextBlockText("OriginMetLocationValue", pokemon.Origin.MetLocationIdentifier);
        SetTextBlockText("OriginPokeballValue", pokemon.Origin.PokeballIdentifier);
        SetTextBlockText("OriginEncounterTypeValue", pokemon.Origin.EncounterTypeIdentifier);
        SetTextBlockText("OriginEggReceiveValue", pokemon.Origin.EggReceiveDate?.ToString("yyyy-MM-dd") ?? "");
        SetTextBlockText("OriginEggHatchLocationValue", pokemon.Origin.EggHatchLocationIdentifier);
        
        // Stats Section - use Modern stats
        SetTextBlockText("StatsSectionTitle", $"Stats {(pokemon.Stats.IsModernSystemByDefault ? "Modern" : "Old")}");
        var statSet = pokemon.Stats.Modern;
        if (statSet != null)
        {
            SetTextBlockText("HPCalcValue", statSet.HP?.Value.ToString() ?? "");
            SetTextBlockText("HPIVValue", statSet.HP?.Iv.ToString() ?? "");
            SetTextBlockText("HPEVValue", statSet.HP?.Ev.ToString() ?? "");

            SetTextBlockText("AttackCalcValue", statSet.Attack?.Value.ToString() ?? "");
            SetTextBlockText("AttackIVValue", statSet.Attack?.Iv.ToString() ?? "");
            SetTextBlockText("AttackEVValue", statSet.Attack?.Ev.ToString() ?? "");

            SetTextBlockText("DefenseCalcValue", statSet.Defense?.Value.ToString() ?? "");
            SetTextBlockText("DefenseIVValue", statSet.Defense?.Iv.ToString() ?? "");
            SetTextBlockText("DefenseEVValue", statSet.Defense?.Ev.ToString() ?? "");

            SetTextBlockText("SpAtkCalcValue", statSet.SpecialAttack?.Value.ToString() ?? "");
            SetTextBlockText("SpAtkIVValue", statSet.SpecialAttack?.Iv.ToString() ?? "");
            SetTextBlockText("SpAtkEVValue", statSet.SpecialAttack?.Ev.ToString() ?? "");

            SetTextBlockText("SpDefCalcValue", statSet.SpecialDefense?.Value.ToString() ?? "");
            SetTextBlockText("SpDefIVValue", statSet.SpecialDefense?.Iv.ToString() ?? "");
            SetTextBlockText("SpDefEVValue", statSet.SpecialDefense?.Ev.ToString() ?? "");

            SetTextBlockText("SpeedCalcValue", statSet.Speed?.Value.ToString() ?? "");
            SetTextBlockText("SpeedIVValue", statSet.Speed?.Iv.ToString() ?? "");
            SetTextBlockText("SpeedEVValue", statSet.Speed?.Ev.ToString() ?? "");
        }

        // Moves Section
        // Clear all move slots first
        SetTextBlockText("Move1Name", "");
        SetTextBlockText("Move1PP", "");
        SetTextBlockText("Move1PowerUps", "");
        SetTextBlockText("Move2Name", "");
        SetTextBlockText("Move2PP", "");
        SetTextBlockText("Move2PowerUps", "");
        SetTextBlockText("Move3Name", "");
        SetTextBlockText("Move3PP", "");
        SetTextBlockText("Move3PowerUps", "");
        SetTextBlockText("Move4Name", "");
        SetTextBlockText("Move4PP", "");
        SetTextBlockText("Move4PowerUps", "");

        // Populate moves
        int moveIndex = 1;
        foreach (Move move in pokemon.Moves.Values)
        {
            if (move.Id == 0) continue;

            switch (moveIndex)
            {
                case 1:
                    SetTextBlockText("Move1Name", move.Identifier);
                    SetTextBlockText("Move1PP", move.Pp.ToString());
                    SetTextBlockText("Move1PowerUps", move.TimesIncreased.ToString());
                    break;
                case 2:
                    SetTextBlockText("Move2Name", move.Identifier);
                    SetTextBlockText("Move2PP", move.Pp.ToString());
                    SetTextBlockText("Move2PowerUps", move.TimesIncreased.ToString());
                    break;
                case 3:
                    SetTextBlockText("Move3Name", move.Identifier);
                    SetTextBlockText("Move3PP", move.Pp.ToString());
                    SetTextBlockText("Move3PowerUps", move.TimesIncreased.ToString());
                    break;
                case 4:
                    SetTextBlockText("Move4Name", move.Identifier);
                    SetTextBlockText("Move4PP", move.Pp.ToString());
                    SetTextBlockText("Move4PowerUps", move.TimesIncreased.ToString());
                    break;
            }
            moveIndex++;
        }

        // Additional Info Section
        SetTextBlockText("MarkingsValue", pokemon.Coolness.ToString());
        SetTextBlockText("ShinyLeavesValue", pokemon.Coolness.ToString());
        SetTextBlockText("Gen3MiscValue", pokemon.Coolness.ToString());
        SetTextBlockText("WalkingMoodValue", pokemon.Coolness.ToString());
        SetTextBlockText("CoolnessValue", pokemon.Coolness.ToString());
        SetTextBlockText("BeautyValue", pokemon.Beauty.ToString());
        SetTextBlockText("CutenessValue", pokemon.Cuteness.ToString());
        SetTextBlockText("SmartinessValue", pokemon.Smartness.ToString());
        SetTextBlockText("ToughnessValue", pokemon.Toughness.ToString());
        SetTextBlockText("SheenValue", pokemon.Sheen.ToString());
    }

    private void SetTextBlockText(string componentName, string content)
    {
        this.FindControl<TextBlock>(componentName)?.Text = content;
    }

    private Border NewBadge(string name, string content, string className)
    {
        Border border = new Border();
        border.Classes.AddRange(className.Split(" "));
        TextBlock text = new TextBlock
        {
            Name = name,
            Text = content
        };
        border.Child = text;
        return border;
    }
}