using System;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace PokemonStorage.Models;

public class PartyPokemon
{
    // Game
    public int Generation { get; set; }
    public string Language { get; set; }

    // Overview
    public int SpeciesId { get; set; }
    public int AlternateFormId { get; set; }
    public int PersonalityValue { get; set; }
    public bool IsEgg { get; set; }
    public Origin Origin { get; set; }
    public Trainer OriginalTrainer { get; set; }
    public string Ability { get; set; }

    // Nickname
    public bool HasNickname { get; set; }
    public string Nickname { get; set; }

    // Status
    public int Level { get; set; }
    public int ExperiencePoints { get; set; }
    public int HeldItem { get; set; }
    public int Friendship { get; set; }
    public int WalkingMood { get; set; }

    // Stats
    public Stat HP { get; set; }
    public Stat Attack { get; set; }
    public Stat Defense { get; set; }
    public Stat Speed { get; set; }
    public Stat SpecialAttack { get; set; }
    public Stat SpecialDefense { get; set; }

    public Dictionary<int, Move> Moves { get; set; }
    public bool Pokerus { get; set; }
    public int PokerusDaysRemaining { get; set; }
    public int PokerusStrain { get; set; }

    // Other
    public int Coolness { get; set; }
    public int Beauty { get; set; }
    public int Cuteness { get; set; }
    public int Smartness { get; set; }
    public int Toughness { get; set; }
    public int Sheen { get; set; }
    public int Obedience { get; set; }
    public RibbonSet Ribbons { get; set; }
    public Markings Markings { get; set; }
    public byte[] Seals { get; set; }
    public byte[] SealCoordinates { get; set; }

    public bool ShinyLeaf1 { get; set; }
    public bool ShinyLeaf2 { get; set; }
    public bool ShinyLeaf3 { get; set; }
    public bool ShinyLeaf4 { get; set; }
    public bool ShinyCrown { get; set; }

    public int Gen3Misc { get; set; }


    public PartyPokemon(int generation)
    {
        // Game
        Generation = generation;
        Language = "EN";

        // Overview
        OriginalTrainer = new Trainer("???", 0, 0, 0);
        SpeciesId = 0;
        AlternateFormId = 0;
        PersonalityValue = 0;
        IsEgg = false;
        Ability = "";

        // Nickname
        HasNickname = false;
        Nickname = "";

        // Status
        Level = 1;
        ExperiencePoints = 0;
        HeldItem = 0;
        Friendship = 0;
        WalkingMood = 0;

        // Stats
        HP = new Stat(0, 0, 0);
        Attack = new Stat(0, 0, 0);
        Defense = new Stat(0, 0, 0);
        Speed = new Stat(0, 0, 0);
        SpecialAttack = new Stat(0, 0, 0);
        SpecialDefense = new Stat(0, 0, 0);
        Moves = [];

        Pokerus = false;
        PokerusDaysRemaining = 0;
        PokerusStrain = 0;

        // Contest attributes
        Coolness = 0;
        Beauty = 0;
        Cuteness = 0;
        Smartness = 0;
        Toughness = 0;
        Sheen = 0;
        Obedience = 0;

        Ribbons = new RibbonSet();
        Markings = new Markings(generation, 0);
        Origin = new Origin();

        Seals = [0x00];
        SealCoordinates = [0x00];

        ShinyLeaf1 = false;
        ShinyLeaf2 = false;
        ShinyLeaf3 = false;
        ShinyLeaf4 = false;
        ShinyCrown = false;

        Gen3Misc = 0;
    }

    #region Decoding

    // https://bulbapedia.bulbagarden.net/wiki/Pok%C3%A9mon_data_structure_(Generation_I)
    public void LoadFromGen1Bytes(byte[] content, int version, string nickname, string trainerName)
    {
        Origin = new Origin
        {
            OriginGameId = version
        };
        
        Nickname = nickname;
        OriginalTrainer = new Trainer(trainerName, 0, Utility.GetUnsignedShort(content, 0x0C, true), 0);
        SpeciesId = Lookup.GetSpeciesIdByIndex(1, Utility.GetByte(content, 0x00));
        ExperiencePoints = Utility.GetInt(content, 0x0E, 24, true);
        Level = Lookup.GetLevelFromExperience(SpeciesId, ExperiencePoints);
        HasNickname = CheckIfNickname();
        PersonalityValue = GeneratePersonalityValue();
        Friendship = Lookup.GetBaseHappiness(SpeciesId);

        // Get Moves
        (int moveIndexOffset, int movePpOffset)[] moveDataOffsets = [
            (0x08, 0x1D),
            (0x09, 0x1E),
            (0x0A, 0x1F),
            (0x0B, 0x20)
        ];

        for (int i = 0; i < moveDataOffsets.Length; i++)
        {
            int moveIndexOffset = moveDataOffsets[i].moveIndexOffset;
            int movePpOffset = moveDataOffsets[i].movePpOffset;

            byte ppData = Utility.GetByte(content, movePpOffset);
            string ppBinary = Convert.ToString(ppData, 2);
            Program.Logger.LogInformation(ppBinary);

            int powerUps = Convert.ToInt32(ppBinary.Substring(0, 2), 2);
            int currentPp = Convert.ToInt32(ppBinary.Substring(2, 6), 2);
            int moveIndex = Utility.GetByte(content, moveIndexOffset);

            Moves[i] = new Move(moveIndex, currentPp, powerUps);
        }

        // Get Stats
        ushort ivData = Utility.GetUnsignedShort(content, 0x1B, true);
        string ivBinary = Convert.ToString(ivData, 2);

        HP = new Stat(
            Utility.GetUnsignedShort(content, 0x22, true),
            Utility.GetUnsignedShort(content, 0x11, true),
            0);

        Attack = new Stat(
            Utility.GetUnsignedShort(content, 0x24, true),
            Utility.GetUnsignedShort(content, 0x13, true),
            Convert.ToInt32(ivBinary.Substring(0, 2)));

        Defense = new Stat(
            Utility.GetUnsignedShort(content, 0x26, true),
            Utility.GetUnsignedShort(content, 0x15, true),
            Convert.ToInt32(ivBinary.Substring(2, 2)));

        Speed = new Stat(
            Utility.GetUnsignedShort(content, 0x28, true),
            Utility.GetUnsignedShort(content, 0x17, true),
            Convert.ToInt32(ivBinary.Substring(4, 2)));
            
        SpecialAttack = new Stat(
            Utility.GetUnsignedShort(content, 0x2A, true),
            Utility.GetUnsignedShort(content, 0x19, true),
            Convert.ToInt32(ivBinary.Substring(6, 2)));

        SpecialDefense = new Stat(
            Utility.GetUnsignedShort(content, 0x2A, true),
            Utility.GetUnsignedShort(content, 0x19, true),
            Convert.ToInt32(ivBinary.Substring(6, 2)));

        //(self.hp.value, self.attack.value, self.defense.value, self.special_attack.value, self.special_defense.value, self.speed.value) = self.get_calculated_stats(self.generation)
    }

    #endregion

    public string GetOneLinerDescription()
    {
        string speciesName = Lookup.GetSpeciesName(SpeciesId);
        string gender = GetGenderByPersonalityValue().ToString();

        return $"{speciesName}({gender})({SpeciesId})/{Nickname} " +
                $"Lv.{Level} " +
                $"HP->{HP} " +
                $"Att->{Attack} " +
                $"Def->{Defense} " +
                $"SpA->{SpecialAttack} " +
                $"SpD->{SpecialDefense} " +
                $"Spe->{Speed}";
    }



    //     def print_full_details(self):
    //         print(
    //             f"{self.species_id}: {Lookup.get_species_name(self.species_id)} Form:{self.alternate_form_id} Gen:{self.generation} Lang:{self.language}",
    //             f"\tHasNickname:{self.has_nickname} Nickname:[{self.nickname}]",
    //             f"\tOT:{self.original_trainer} Item:{Lookup.get_item_name(self.held_item)}",
    //             f"\tLv.{self.level} Exp:{self.experience_points} Frnd:{self.friendship} WalkMood:{self.walking_mood} Ob:{self.obedience} IsEgg:{self.is_egg}",
    //             f"\tPv:{self.get_personality_string()} : {self.personality_value}",
    //             f"\t\tGender:{self.get_gender_by_personality_value().name} Ability:{Lookup.get_ability_name(self.get_ability_from_personality_value())} Nature:{Lookup.get_nature_name(self.get_nature_from_personality_value())} Shiny:{self.get_shiny_from_personality_value()}",
    //             f"\tPokerus:{self.pokerus} Rem:{self.pokerus_days_remaining} Str:{self.pokerus_strain}",
    //             f"\tOrigin:{self.origin}",
    //             f"\tContest: Cool:{self.coolness} Beauty:{self.beauty} Cute:{self.cuteness} Smart:{self.smartness} Tough:{self.toughness} Sheen{self.sheen}",
    //             f"\tRibbons:{self.ribbons}",
    //             f"\tSeals: {bin(ByteUtility.get_int(self.seals, 0, len(self.seals), True))[2:].zfill(len(self.seals)*8)} Coordinates: {bin(ByteUtility.get_int(self.seal_coordinates, 0, len(self.seal_coordinates), True))[2:].zfill(len(self.seal_coordinates)*8)}",
    //             f"\tShinyLeafs:{self.shiny_leaf_1}:{self.shiny_leaf_2}:{self.shiny_leaf_3}:{self.shiny_leaf_4} Crown:{self.shiny_crown}",
    //             f"\tMarkings:{self.markings}",
    //             f"\tCalculated:{self.get_calculated_stats(self.generation)}",
    //             f"\tHp:{self.hp}",
    //             f"\tAtk:{self.attack}",
    //             f"\tDef:{self.defense}",
    //             f"\tSpe:{self.speed}",
    //             f"\tSpA:{self.special_attack}",
    //             f"\tSpD:{self.special_defense}",
    //             f"\tM1:{self.moves.get(0, '')}",
    //             f"\tM2:{self.moves.get(1, '')}",
    //             f"\tM3:{self.moves.get(2, '')}",
    //             f"\tM4:{self.moves.get(3, '')}",
    //         sep=os.linesep)

    public void PrintFullDetails()
    {
        Console.WriteLine(string.Join(Environment.NewLine, new[]
        {
            $"{SpeciesId}: {Lookup.GetSpeciesName(SpeciesId)} " +
                $"Form:{AlternateFormId} Gen:{Generation} Lang:{Language}",

            $"\tHasNickname:{HasNickname} Nickname:[{Nickname}]",

            $"\tOT:{OriginalTrainer} Item:{Lookup.GetItemName(HeldItem)}",

            $"\tLv.{Level} Exp:{ExperiencePoints} Frnd:{Friendship} " +
                $"WalkMood:{WalkingMood} Ob:{Obedience} IsEgg:{IsEgg}",

            $"\tPv:{GetPersonalityString()} : {PersonalityValue}",

            $"\t\tGender:{GetGenderByPersonalityValue()} " +
                $"Ability:{Lookup.GetAbilityName(GetAbilityFromPersonalityValue())} " +
                $"Nature:{Lookup.GetNatureName(GetNatureFromPersonalityValue())} " +
                $"Shiny:{GetShinyFromPersonalityValue()}",

            $"\tPokerus:{Pokerus} Rem:{PokerusDaysRemaining} Str:{PokerusStrain}",

            $"\tOrigin:{Origin}",

            $"\tContest: Cool:{Coolness} Beauty:{Beauty} Cute:{Cuteness} " +
                $"Smart:{Smartness} Tough:{Toughness} Sheen:{Sheen}",

            $"\tRibbons:{Ribbons}",

            $"\tSeals: {Convert.ToString(Utility.GetInt(Seals, 0, Seals.Length, true), 2).PadLeft(Seals.Length * 8, '0')} " +
                $"Coordinates: {Convert.ToString(Utility.GetInt(SealCoordinates, 0, SealCoordinates.Length, true), 2).PadLeft(SealCoordinates.Length * 8, '0')}",

            $"\tShinyLeafs:{ShinyLeaf1}:{ShinyLeaf2}:{ShinyLeaf3}:{ShinyLeaf4} Crown:{ShinyCrown}",

            $"\tMarkings:{Markings}",

            $"\tCalculated:{GetCalculatedStats(Generation)}",

            $"\tHp:{HP}",
            $"\tAtk:{Attack}",
            $"\tDef:{Defense}",
            $"\tSpe:{Speed}",
            $"\tSpA:{SpecialAttack}",
            $"\tSpD:{SpecialDefense}",

            $"\tM1:{(Moves.TryGetValue(0, out var m1) ? m1 : "")}",
            $"\tM2:{(Moves.TryGetValue(1, out var m2) ? m2 : "")}",
            $"\tM3:{(Moves.TryGetValue(2, out var m3) ? m3 : "")}",
            $"\tM4:{(Moves.TryGetValue(3, out var m4) ? m4 : "")}"
        }));
    }

    private object GetCalculatedStats(int generation)
    {
        throw new NotImplementedException();
    }

    private bool GetShinyFromPersonalityValue()
    {
        int p1 = PersonalityValue / 65536;
        int p2 = PersonalityValue % 65536;
        int shinyValue = OriginalTrainer.PublicId ^ OriginalTrainer.SecretId ^ p1 ^ p2;
        return shinyValue < 8;
    }

    private int GetNatureFromPersonalityValue()
    {
        int pNature = PersonalityValue % 25;
        return Lookup.GetNatureIdByIndex(pNature);
    }

    private int GetAbilityFromPersonalityValue()
    {
        var (first, second) = Lookup.GetAbilities(SpeciesId);
        List<int> abilities = [];
        if (first != 0) abilities.Add(first);
        if (second != 0) abilities.Add(second);

        if (abilities.Count == 1) return abilities.First();
        else if (abilities.Count == 0) return 0;
        else
        {
            int choice = PersonalityValue % 1;
            return abilities[choice];
        }
    }

    private object GetPersonalityString()
    {
        string binary = Convert.ToString(PersonalityValue, 2).PadLeft(32, '0');
        string[] bytes = Enumerable.Range(0, 4).Select(i => binary.Substring(i * 8, 8)).ToArray();
        return string.Join(" ", bytes);
    }

    private Gender GetGenderByPersonalityValue()
    {
        int pGender = PersonalityValue % 256;
        int threshold = Lookup.GetGenderThreshold(SpeciesId);
        if (threshold == 0)
            return Gender.MALE;
        else if (threshold == 254)
            return Gender.FEMALE;
        else if (threshold == 255)
            return Gender.GENDERLESS;
        else
        {
            if (pGender >= threshold)
                return Gender.MALE;
            else
                return Gender.FEMALE;
        }
    }

    private static int GeneratePersonalityValue()
    {
        Random random = new();
        return random.Next(0, (int)Math.Pow(2,32));
    }

    private bool CheckIfNickname()
    {
        return !string.Equals(Regex.Replace(Nickname.ToLower().Replace(" ", "-"), @"[^0-9a-zA-Z\w]+", ""), Lookup.GetSpeciesName(SpeciesId).ToLower());
    }
}
