using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace PokemonStorage.Models;

public class PartyPokemon
{
    // Game
    public int Generation { get; set; }
    public string Language { get; set; }

    // Overview
    public ushort SpeciesId { get; set; }
    public string SpeciesIdentifier { get { return Lookup.GetSpeciesName(SpeciesId); } }
    public uint AlternateFormId { get; set; }
    public uint PersonalityValue { get; set; }
    public bool IsEgg { get; set; }
    public Origin Origin { get; set; }
    public Trainer OriginalTrainer { get; set; }
    public ushort AbilityId { get; set; }
    public string AbilityIdentifier { get { return Lookup.GetAbilityName(AbilityId); } }
    

    // Nickname
    public bool HasNickname { get; set; }
    public string Nickname { get; set; }

    // Status
    public byte Level { get { return Lookup.GetLevelFromExperience(SpeciesId, ExperiencePoints); } }
    public uint ExperiencePoints { get; set; }
    public ushort HeldItemId { get; set; }
    public string HeldItemIdentifier { get { return Lookup.GetItemName(HeldItemId); } }
    public byte Friendship { get; set; }
    public byte WalkingMood { get; set; }

    // Stats
    public Stat HP { get; set; }
    public Stat Attack { get; set; }
    public Stat Defense { get; set; }
    public Stat Speed { get; set; }
    public Stat SpecialAttack { get; set; }
    public Stat SpecialDefense { get; set; }

    public Dictionary<int, Move> Moves { get; set; }
    public uint PokerusDaysRemaining { get; set; }
    public uint PokerusStrain { get; set; }

    // Other
    public uint Coolness { get; set; }
    public uint Beauty { get; set; }
    public uint Cuteness { get; set; }
    public uint Smartness { get; set; }
    public uint Toughness { get; set; }
    public uint Sheen { get; set; }
    public bool Obedience { get; set; }
    public RibbonSet Ribbons { get; set; }
    public Markings Markings { get; set; }

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

        // Nickname
        HasNickname = false;
        Nickname = "";

        // Status
        ExperiencePoints = 0;
        HeldItemId = 0;
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
        for (int i = 0; i < 4; i++)
        {
            Moves[i] = new Move(0, 0, 0);
        }

        PokerusDaysRemaining = 0;
        PokerusStrain = 0;

        // Contest attributes
        Coolness = 0;
        Beauty = 0;
        Cuteness = 0;
        Smartness = 0;
        Toughness = 0;
        Sheen = 0;
        Obedience = false;

        Ribbons = new RibbonSet();
        Markings = new Markings(generation, 0);
        Origin = new Origin();

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
        OriginalTrainer = new Trainer(trainerName, 0, Utility.GetUnsignedNumber<ushort>(content, 0x0C, 2, true), 0);
        SpeciesId = Lookup.GetSpeciesIdByIndex(1, Utility.GetByte(content, 0x00));
        ExperiencePoints = Utility.GetUnsignedNumber<uint>(content, 0x0E, 3, true);
        HasNickname = NicknameExists();
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

            byte ppData = Utility.GetUnsignedNumber<byte>(content, movePpOffset, 1, true);
            string ppBinary = Convert.ToString(ppData, 2).PadLeft(8, '0');

            Moves[i].TimesIncreased = Convert.ToInt32(ppBinary.Substring(0, 2), 2);
            Moves[i].Pp = Convert.ToInt32(ppBinary.Substring(2, 6), 2);
            Moves[i].Id = Utility.GetUnsignedNumber<byte>(content, moveIndexOffset, 1, true);
        }

        // Get Stats
        ushort ivData = Utility.GetUnsignedNumber<ushort>(content, 0x1B, 2, true);
        string ivBinary = Convert.ToString(ivData, 2).PadLeft(16, '0');

        HP = new Stat(
            Utility.GetUnsignedNumber<ushort>(content, 0x11, 2, true),
            Convert.ToByte(string.Join("", ivBinary.Chunk(4).Select(x => x.Last())), 2));

        Attack = new Stat(
            Utility.GetUnsignedNumber<ushort>(content, 0x13, 2, true),
            Convert.ToByte(ivBinary.Substring(0, 4), 2));

        Defense = new Stat(
            Utility.GetUnsignedNumber<ushort>(content, 0x15, 2, true),
            Convert.ToByte(ivBinary.Substring(4, 4), 2));

        Speed = new Stat(
            Utility.GetUnsignedNumber<ushort>(content, 0x17, 2, true),
            Convert.ToByte(ivBinary.Substring(8, 4), 2));

        SpecialAttack = new Stat(
            Utility.GetUnsignedNumber<ushort>(content, 0x19, 2, true),
            Convert.ToByte(ivBinary.Substring(12, 4), 2));

        SpecialDefense = new Stat(
            Utility.GetUnsignedNumber<ushort>(content, 0x19, 2, true),
            Convert.ToByte(ivBinary.Substring(12, 4), 2));

        StatHextuple calculated = GetCalculatedStats(1);
        HP.Value = calculated.HP;
        Attack.Value = calculated.Attack;
        Defense.Value = calculated.Defense;
        SpecialAttack.Value = calculated.SpecialAttack;
        SpecialDefense.Value = calculated.SpecialDefense;
        Speed.Value = calculated.Speed;

        PrintFullDetails();
    }

    public void LoadFromGen2Bytes(byte[] content, int version, string nickname, string trainerName)
    {
        Origin = new Origin
        {
            OriginGameId = version
        };

        Nickname = nickname;
        HasNickname = NicknameExists();
        SpeciesId = Utility.GetByte(content, 0x00);
        HeldItemId = Lookup.GetItemIdByIndex(2, Utility.GetUnsignedNumber<byte>(content, 0x01, 1, true));
        OriginalTrainer = new Trainer(trainerName, 0, Utility.GetUnsignedNumber<ushort>(content, 0x06, 2, true), 0);
        ExperiencePoints = Utility.GetUnsignedNumber<uint>(content, 0x08, 3, true);
        Friendship = Utility.GetUnsignedNumber<byte>(content, 0x1B, 1, true);

        // Get Moves
        (int moveIndexOffset, int movePpOffset)[] moveDataOffsets = [
            (0x02, 0x17),
            (0x03, 0x18),
            (0x04, 0x19),
            (0x05, 0x1A)
        ];

        for (int i = 0; i < moveDataOffsets.Length; i++)
        {
            int moveIndexOffset = moveDataOffsets[i].moveIndexOffset;
            int movePpOffset = moveDataOffsets[i].movePpOffset;

            byte ppData = Utility.GetUnsignedNumber<byte>(content, movePpOffset, 1, true);
            string ppBinary = Convert.ToString(ppData, 2).PadLeft(8, '0');

            Moves[i].Id = Utility.GetUnsignedNumber<byte>(content, moveIndexOffset, 1, true);
            Moves[i].Pp = Convert.ToInt32(ppBinary.Substring(2, 6), 2);
            Moves[i].TimesIncreased = Convert.ToInt32(ppBinary.Substring(0, 2), 2);
        }

        // Get Stats
        ushort ivData = Utility.GetUnsignedNumber<ushort>(content, 0x15, 2, true);
        string ivBinary = Convert.ToString(ivData, 2).PadLeft(16, '0');
        Program.Logger.LogInformation($"IV Binary: {string.Join('_', ivBinary.Chunk(4).Select(x => new string(x)))}");

        HP = new Stat(
            Utility.GetUnsignedNumber<ushort>(content, 0x0B, 2, true),
            Convert.ToByte(string.Join("", ivBinary.Chunk(4).Select(x => x.Last())), 2));

        Attack = new Stat(
            Utility.GetUnsignedNumber<ushort>(content, 0x0D, 2, true),
            Convert.ToByte(ivBinary.Substring(0, 4), 2));

        Defense = new Stat(
            Utility.GetUnsignedNumber<ushort>(content, 0x0F, 2, true),
            Convert.ToByte(ivBinary.Substring(4, 4), 2));

        Speed = new Stat(
            Utility.GetUnsignedNumber<ushort>(content, 0x11, 2, true),
            Convert.ToByte(ivBinary.Substring(8, 4), 2));

        SpecialAttack = new Stat(
            Utility.GetUnsignedNumber<ushort>(content, 0x13, 2, true),
            Convert.ToByte(ivBinary.Substring(12, 4), 2));

        SpecialDefense = new Stat(
            Utility.GetUnsignedNumber<ushort>(content, 0x13, 2, true),
            Convert.ToByte(ivBinary.Substring(12, 4), 2));

        StatHextuple calculated = GetCalculatedStats(2);
        HP.Value = calculated.HP;
        Attack.Value = calculated.Attack;
        Defense.Value = calculated.Defense;
        SpecialAttack.Value = calculated.SpecialAttack;
        SpecialDefense.Value = calculated.SpecialDefense;
        Speed.Value = calculated.Speed;

        // Pokerus
        byte pokerusData = Utility.GetUnsignedNumber<byte>(content, 0x1C, 1, true);
        PokerusStrain = (uint)(pokerusData >> 4);
        PokerusDaysRemaining = (uint)(pokerusData % 16);

        // Caught Data
        ushort caughtData = Utility.GetUnsignedNumber<ushort>(content, 0x1D, 2, true);
        string caughtBinary = Convert.ToString(caughtData, 2).PadLeft(16, '0');
        byte timeframe = Convert.ToByte(ivBinary.Substring(0, 2));
        switch (timeframe)
        {
            case 1:
                Origin.MetDateTime = DateTime.Now.Date + TimeSpan.FromHours(9);
                break;
            case 2:
                Origin.MetDateTime = DateTime.Now.Date + TimeSpan.FromHours(13);
                break;
            case 3:
                Origin.MetDateTime = DateTime.Now.Date + TimeSpan.FromHours(21);
                break;
            default:
                Origin.MetDateTime = DateTime.Now.Date + TimeSpan.FromHours(12);
                break;
        }
        Origin.MetLevel = Convert.ToByte(caughtBinary.Substring(2, 6), 2);
        OriginalTrainer.Gender = Convert.ToByte(caughtBinary.Substring(8, 1), 2) == 1 ? Gender.FEMALE : Gender.MALE;
        Origin.MetLocation = Lookup.GetLocationIdByIndex(2, Convert.ToUInt16(caughtBinary.Substring(9, 7), 2));

        PrintFullDetails();
    }
    
    public void LoadFromGen3Bytes(byte[] content, int version, string language)
    {
        Origin = new Origin
        {
            OriginGameId = version
        };

        PersonalityValue = Utility.GetUnsignedNumber<uint>(content, 0x00, 4);
        uint otId = Utility.GetUnsignedNumber<uint>(content, 0x04, 4);
        OriginalTrainer = new Trainer(
            Utility.GetEncodedString(Utility.GetBytes(content, 0x14, 7), version, language),
            0,
            Utility.GetUnsignedNumber<ushort>(content, 0x04, 2),
            Utility.GetUnsignedNumber<ushort>(content, 0x06, 2)
        );

        Nickname = Utility.GetEncodedString(Utility.GetBytes(content, 0x08, 10), version, language);
        HasNickname = NicknameExists();
        Language = Lookup.GetLanguageById(Utility.GetUnsignedNumber<byte>(content, 0x12, 1));
        Gen3Misc = Utility.GetUnsignedNumber<byte>(content, 0x13, 1);
        Markings = new Markings(3, Utility.GetUnsignedNumber<byte>(content, 0x1B, 1));

        // Data Section Decryption

        ushort checksum = Utility.GetUnsignedNumber<ushort>(content, 0x1C, 2);
        byte[] fullEncryptedData = Utility.GetBytes(content, 0x20, 48);
        int order_index = (int)(PersonalityValue % 24);

        Dictionary<int, string> order = new()
        {
            {0, "GAEM"}, {1,"GAME"}, {2,"GEAM"}, {3,"GEMA"},{4, "GMAE"}, {5,"GMEA"}, {6,"AGEM"}, {7,"AGME"},
            {8, "AEGM"}, {9,"AEMG"},{10,"AMGE"},{11,"AMEG"},{12,"AGAM"},{13,"EGMA"},{14,"EAGM"},{15,"EAMG"},
            {16,"EMGA"},{17,"EMAG"},{18,"MGAE"},{19,"MGEA"},{20,"MAGE"},{21,"MAEG"},{22,"MEGA"},{23,"MEAG"}
        };

        string orderString = order[order_index];
        Program.Logger.LogInformation($"Decryption Order: {order_index}:{orderString}");
        uint decryptionKey = PersonalityValue ^ otId;
        byte[] decryptedData = [];
        for (int i = 0; i < 48; i += 4)
        {
            byte[] y = Utility.GetBytes(fullEncryptedData, 0x1 * i, 4);
            byte[] unencryptedBytes = y.Zip(BitConverter.GetBytes(decryptionKey)).Select(x => Convert.ToByte(x.First ^ x.Second)).ToArray();
            decryptedData = decryptedData.Concat(unencryptedBytes).ToArray();
        }

        uint calculatedChecksum = 0;
        for (int i = 0; i < 48; i += 2)
        {
            calculatedChecksum += Utility.GetUnsignedNumber<ushort>(decryptedData, 0x1 * i, 2);
        }

        Program.Logger.LogInformation($"{checksum & 0xffff} ?== {calculatedChecksum & 0xffff}");
        Program.Logger.LogInformation($"{Convert.ToString(calculatedChecksum, 2).PadLeft(17, '0')}");
        Program.Logger.LogInformation($"{Convert.ToString(checksum, 2).PadLeft(17, '0')}");

        foreach ((char c, int i) in orderString.Select((c, i) => (c, i)))
        {
            int offset = i * 12;
            byte[] substructureBytes = Utility.GetBytes(decryptedData, offset, 12);
            Program.Logger.LogInformation($"{c} ==> {BitConverter.ToString(substructureBytes)}");
            switch (c)
            {
                case 'G':
                    SpeciesId = Lookup.GetSpeciesIdByIndex(3, Utility.GetUnsignedNumber<ushort>(substructureBytes, 0x00, 2));
                    HeldItemId = Lookup.GetItemIdByIndex(3, Utility.GetUnsignedNumber<ushort>(substructureBytes, 0x02, 2));
                    ExperiencePoints = Utility.GetUnsignedNumber<uint>(substructureBytes, 0x04, 4);

                    byte ppBonuses = Utility.GetUnsignedNumber<byte>(substructureBytes, 0x08, 1);
                    string ppBinary = Convert.ToString(ppBonuses, 2).PadLeft(8, '0');
                    for (int j = 0; j < 4; j++)
                    {
                        int bonuses = Convert.ToInt32(ppBinary.Substring(j * 2, 2), 2);
                        Moves[j].TimesIncreased = bonuses;
                    }
                    Friendship = Utility.GetUnsignedNumber<byte>(substructureBytes, 0x09, 1);
                    break;

                case 'A':
                    for (int j = 0; j < 4; j++)
                    {
                        Moves[j].Id = Utility.GetUnsignedNumber<ushort>(substructureBytes, j * 2, 2);
                    }

                    for (int j = 0; j < 4; j++)
                    {
                        Moves[j].Pp = Utility.GetUnsignedNumber<byte>(substructureBytes, 0x08 + j, 1);
                    }
                    break;

                case 'E':
                    HP.Ev = Utility.GetUnsignedNumber<byte>(substructureBytes, 0x00, 1);
                    Attack.Ev = Utility.GetUnsignedNumber<byte>(substructureBytes, 0x01, 1);
                    Defense.Ev = Utility.GetUnsignedNumber<byte>(substructureBytes, 0x02, 1);
                    Speed.Ev = Utility.GetUnsignedNumber<byte>(substructureBytes, 0x03, 1);
                    SpecialAttack.Ev = Utility.GetUnsignedNumber<byte>(substructureBytes, 0x04, 1);
                    SpecialDefense.Ev = Utility.GetUnsignedNumber<byte>(substructureBytes, 0x05, 1);

                    Coolness = Utility.GetUnsignedNumber<byte>(substructureBytes, 0x06, 1);
                    Beauty = Utility.GetUnsignedNumber<byte>(substructureBytes, 0x07, 1);
                    Cuteness = Utility.GetUnsignedNumber<byte>(substructureBytes, 0x08, 1);
                    Smartness = Utility.GetUnsignedNumber<byte>(substructureBytes, 0x09, 1);
                    Toughness = Utility.GetUnsignedNumber<byte>(substructureBytes, 0x0A, 1);
                    Sheen = Utility.GetUnsignedNumber<byte>(substructureBytes, 0x0B, 1);
                    break;

                case 'M':
                    // Pokerus
                    byte pokerusData = Utility.GetUnsignedNumber<byte>(substructureBytes, 0x00, 1);
                    string pokerusBinary = Convert.ToString(pokerusData, 2).PadLeft(8, '0');

                    // Stra Days
                    // 0000 0000
                    Program.Logger.LogInformation($"Pokerus Data: {pokerusBinary}");
                    PokerusStrain = Convert.ToByte(pokerusBinary.Substring(0, 4), 2);
                    PokerusDaysRemaining = Convert.ToByte(pokerusBinary.Substring(4, 4), 2);

                    // Origin
                    Origin.MetLocation = Lookup.GetLocationIdByIndex(3, Utility.GetUnsignedNumber<ushort>(substructureBytes, 0x01, 1));
                    ushort originData = Utility.GetUnsignedNumber<ushort>(substructureBytes, 0x02, 2);
                    string originBinary = Convert.ToString(originData, 2).PadLeft(16, '0');

                    // G Poke Game Level
                    // 0 0000 0000 0000000
                    Program.Logger.LogInformation($"Origin Data: {originBinary}");

                    OriginalTrainer.Gender = originBinary[0] == '1' ? Gender.FEMALE : Gender.MALE;
                    Origin.PokeballIdentifier = Lookup.GetCatchBallById(3, Convert.ToUInt16(originBinary.Substring(1, 4), 2));
                    Origin.OriginGameId = Lookup.GetGameOfOrigin(3, Convert.ToUInt16(originBinary.Substring(5, 4), 2));
                    Origin.MetLevel = Convert.ToByte(originBinary.Substring(9, 7), 2);

                    // IVs, Egg, Ability
                    uint miscData = Utility.GetUnsignedNumber<uint>(substructureBytes, 0x04, 4);
                    string miscBinary = Convert.ToString(miscData, 2).PadLeft(32, '0');

                    // A E SpD   SpA   Spe   Def   Atk   HP
                    // 0 0 00000 00000 00000 00000 00000 00000
                    Program.Logger.LogInformation($"Misc Data: {miscBinary}");

                    AbilityId = Convert.ToByte(miscBinary[0]);
                    IsEgg = miscBinary[1] == '1';
                    SpecialDefense.Iv = Convert.ToByte(miscBinary.Substring(2, 5), 2);
                    SpecialAttack.Iv = Convert.ToByte(miscBinary.Substring(7, 5), 2);
                    Speed.Iv = Convert.ToByte(miscBinary.Substring(12, 5), 2);
                    Defense.Iv = Convert.ToByte(miscBinary.Substring(17, 5), 2);
                    Attack.Iv = Convert.ToByte(miscBinary.Substring(22, 5), 2);
                    HP.Iv = Convert.ToByte(miscBinary.Substring(27, 5), 2);

                    // Ribbons, Obedience
                    string ribbonBinary = Convert.ToString(Utility.GetUnsignedNumber<uint>(substructureBytes, 0x08, 4), 2).PadLeft(32, '0');

                    // O ---- W E N C n r b E A V W C Tou Sma Cut Bea Coo
                    // 0 0000 0 0 0 0 0 0 0 0 0 0 0 0 000 000 000 000 000
                    Program.Logger.LogInformation($"Ribbon Data: {ribbonBinary}");
                    Obedience = ribbonBinary[0] == '1';
                    Ribbons.World = ribbonBinary[5] == '1';
                    Ribbons.Earth = ribbonBinary[6] == '1';
                    Ribbons.National = ribbonBinary[7] == '1';
                    Ribbons.Country = ribbonBinary[8] == '1';
                    Ribbons.Sky = ribbonBinary[9] == '1';
                    Ribbons.Land = ribbonBinary[10] == '1';
                    Ribbons.Marine = ribbonBinary[11] == '1';
                    Ribbons.Effort = ribbonBinary[12] == '1';
                    Ribbons.Artist = ribbonBinary[13] == '1';
                    Ribbons.Victory = ribbonBinary[14] == '1';
                    Ribbons.Winning = ribbonBinary[15] == '1';
                    Ribbons.Champion = ribbonBinary[16] == '1';
                    Ribbons.HeonnTough = Convert.ToByte(ribbonBinary.Substring(17, 3), 2);
                    Ribbons.HeonnSmart = Convert.ToByte(ribbonBinary.Substring(20, 3), 2);
                    Ribbons.HeonnCute = Convert.ToByte(ribbonBinary.Substring(23, 3), 2);
                    Ribbons.HeonnBeauty = Convert.ToByte(ribbonBinary.Substring(26, 3), 2);
                    Ribbons.HeonnCool = Convert.ToByte(ribbonBinary.Substring(29, 3), 2);
                    break;

                default:
                    break;
            }
        }

        PrintFullDetails();
    }

    #endregion

    #region Console Print

    public string PrintRelevant(int inputGeneration = 0)
    {
        int displayGeneration = inputGeneration;
        if (displayGeneration == 0) displayGeneration = Generation;

        StringBuilder returnString = new();

        returnString.AppendLine($"\t{SpeciesId}: {SpeciesIdentifier} ({Nickname}) OT:{OriginalTrainer}");
        returnString.AppendLine($"\tLv.{Level} Exp:{ExperiencePoints}");
        if (displayGeneration > 1)
        {
            returnString.AppendLine($"\tHeld Item:{HeldItemIdentifier}");
        }
        if (displayGeneration == 2)
        {
            returnString.AppendLine($"\tGender:{GetGenderByAttackIv()} Happiness:{Friendship} Shiny:{GetShinyByIv()}");
        }
        else if (displayGeneration > 2)
        {
            returnString.AppendLine($"\t\tGender:{GetGenderByPersonalityValue()} Ability: {AbilityIdentifier} Happiness:{Friendship} Shiny:{GetShinyFromPersonalityValue()}");
        }

        returnString.AppendLine($"\tM1:{(Moves.TryGetValue(0, out var m1) ? m1 : "")}");
        returnString.AppendLine($"\tM2:{(Moves.TryGetValue(1, out var m2) ? m2 : "")}");
        returnString.AppendLine($"\tM3:{(Moves.TryGetValue(2, out var m3) ? m3 : "")}");
        returnString.AppendLine($"\tM4:{(Moves.TryGetValue(3, out var m4) ? m4 : "")}");
        returnString.AppendLine($"\tHP:      {HP}");
        returnString.AppendLine($"\tAttack:  {Attack}");
        returnString.AppendLine($"\tDefense: {Defense}");
        returnString.AppendLine($"\tSpeed:   {Speed}");
        returnString.AppendLine($"\tSpA:     {SpecialAttack}");
        returnString.AppendLine($"\tSpD:     {SpecialDefense}");

        if (displayGeneration == 2)
        {
            returnString.AppendLine($"\tMeet Time: {Origin.MetDateTime.TimeOfDay.Hours} Location: {Lookup.GetLocationNameById(Origin.MetLocation)} Level: {Origin.MetLevel}");
        }

        return returnString.ToString();
    }
    public void PrintFullDetails()
    {
        Console.WriteLine(string.Join(Environment.NewLine,
        [
            $"{SpeciesId}: {Lookup.GetSpeciesName(SpeciesId)} " +
                $"Form:{AlternateFormId} Gen:{Generation} Lang:{Language}",

            $"\tHasNickname:{HasNickname} Nickname:[{Nickname}]",

            $"\tOT:{OriginalTrainer} Item:{Lookup.GetItemName(HeldItemId)}",

            $"\tLv.{Level} Exp:{ExperiencePoints} Frnd:{Friendship} " +
                $"WalkMood:{WalkingMood} Ob:{Obedience} IsEgg:{IsEgg}",

            $"\tPv:{GetPersonalityString()} : {PersonalityValue}",

            $"\t\tGender:{GetGenderByPersonalityValue()} " +
                $"Ability:{AbilityIdentifier} " +
                $"Nature:{Lookup.GetNatureName(GetNatureFromPersonalityValue())} " +
                $"Shiny:{GetShinyFromPersonalityValue()}",

            $"\tPokerus:{PokerusStrain != 0} Rem:{PokerusDaysRemaining} Str:{PokerusStrain}",

            $"\tOrigin:{Origin}",

            $"\tContest: Cool:{Coolness} Beauty:{Beauty} Cute:{Cuteness} " +
                $"Smart:{Smartness} Tough:{Toughness} Sheen:{Sheen}",

            $"\tRibbons:{Ribbons}",

            $"\tShinyLeafs:{ShinyLeaf1}:{ShinyLeaf2}:{ShinyLeaf3}:{ShinyLeaf4} Crown:{ShinyCrown}",

            $"\tMarkings:{Markings}",

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
        ]));
    }

    #endregion

    #region Getters
    private StatHextuple GetCalculatedStats(int generation)
    {
        StatHextuple baseStats = Lookup.GetBaseStats(SpeciesId);

        if (generation > 2)
        {
            var (increased, decreased) = Lookup.GetNatureStats(GetNatureFromPersonalityValue());
            double modifiedAttack = 1;
            double modifiedDefense = 1;
            double modifiedSpecialAttack = 1;
            double modifiedSpecialDefense = 1;
            double modifiedSpeed = 1;

            switch (increased)
            {
                case 2:
                    modifiedAttack = 1.1;
                    break;
                case 3:
                    modifiedDefense = 1.1;
                    break;
                case 4:
                    modifiedSpecialAttack = 1.1;
                    break;
                case 5:
                    modifiedSpecialDefense = 1.1;
                    break;
                case 6:
                    modifiedSpeed = 1.1;
                    break;
                default:
                    break;
            }

            switch (decreased)
            {
                case 2:
                    modifiedAttack = 0.9;
                    break;
                case 3:
                    modifiedDefense = 0.9;
                    break;
                case 4:
                    modifiedSpecialAttack = 0.9;
                    break;
                case 5:
                    modifiedSpecialDefense = 0.9;
                    break;
                case 6:
                    modifiedSpeed = 0.9;
                    break;
                default:
                    break;
            }

            return new StatHextuple(
                Math.Floor((2 * baseStats.HP + HP.Iv + Math.Floor(HP.Ev / 4.0)) * Level / 100.0) + Level + 10,
                Math.Floor((Math.Floor((2 * baseStats.Attack + Attack.Iv + Math.Floor(Attack.Ev / 4.0)) * Level / 100.0) + 5) * modifiedAttack),
                Math.Floor((Math.Floor((2 * baseStats.Defense + Defense.Iv + Math.Floor(Defense.Ev / 4.0)) * Level / 100.0) + 5) * modifiedDefense),
                Math.Floor((Math.Floor((2 * baseStats.SpecialAttack + SpecialAttack.Iv + Math.Floor(SpecialAttack.Ev / 4.0)) * Level / 100.0) + 5) * modifiedSpecialAttack),
                Math.Floor((Math.Floor((2 * baseStats.SpecialDefense + SpecialDefense.Iv + Math.Floor(SpecialDefense.Ev / 4.0)) * Level / 100.0) + 5) * modifiedSpecialDefense),
                Math.Floor((Math.Floor((2 * baseStats.Speed + Speed.Iv + Math.Floor(Speed.Ev / 4.0)) * Level / 100.0) + 5) * modifiedSpeed)
            );
            
        }
        else
        {
            return new StatHextuple(
                Math.Floor(((baseStats.HP + HP.Iv) * 2 + Math.Floor(Math.Ceiling(Math.Sqrt(HP.Ev)) / 4)) * Level / 100) + Level + 10,
                Math.Floor(((baseStats.Attack + Attack.Iv) * 2 + Math.Floor(Math.Ceiling(Math.Sqrt(Attack.Ev)) / 4)) * Level / 100) + 5,
                Math.Floor(((baseStats.Defense + Defense.Iv) * 2 + Math.Floor(Math.Ceiling(Math.Sqrt(Defense.Ev)) / 4)) * Level / 100) + 5,
                Math.Floor(((baseStats.SpecialAttack + SpecialAttack.Iv) * 2 + Math.Floor(Math.Ceiling(Math.Sqrt(SpecialAttack.Ev)) / 4)) * Level / 100) + 5,
                Math.Floor(((baseStats.SpecialDefense + SpecialDefense.Iv) * 2 + Math.Floor(Math.Ceiling(Math.Sqrt(SpecialDefense.Ev)) / 4)) * Level / 100) + 5,
                Math.Floor(((baseStats.Speed + Speed.Iv) * 2 + Math.Floor(Math.Ceiling(Math.Sqrt(Speed.Ev)) / 4)) * Level / 100) + 5
            );
        }


    }

    private bool GetShinyByIv()
    {
        return Attack.Iv > 1 &&
            Defense.Iv == 10 &&
            Speed.Iv == 10 &&
            SpecialAttack.Iv == 10;
    }
    
    private bool GetShinyFromPersonalityValue()
    {
        UInt32 p1 = PersonalityValue / 65536;
        UInt32 p2 = PersonalityValue % 65536;
        UInt32 shinyValue = (uint)(OriginalTrainer.PublicId ^ OriginalTrainer.SecretId ^ p1 ^ p2);
        return shinyValue < 8;
    }

    private int GetNatureFromPersonalityValue()
    {
        int pNature = (int)(PersonalityValue % 25);
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
            int choice = (int)(PersonalityValue % 1);
            return abilities[choice];
        }
    }

    private string GetPersonalityString()
    {
        string binary = Convert.ToString(PersonalityValue, 2).PadLeft(32, '0');
        string[] bytes = Enumerable.Range(0, 4).Select(i => binary.Substring(i * 8, 8)).ToArray();
        return string.Join(" ", bytes);
    }

    private Gender GetGenderByAttackIv()
    {
        int ratio = Lookup.GetGenderRate(SpeciesId);
        switch (ratio)
        {
            case 0:
                return Gender.MALE;
            case 8:
                return Gender.FEMALE;
            case -1:
                return Gender.GENDERLESS;
            default:
                return Attack.Iv <= ratio ? Gender.FEMALE : Gender.MALE;
        }

    }
    private Gender GetGenderByPersonalityValue()
    {
        int pGender = (int)(PersonalityValue % 256);
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

    #endregion

    private static UInt32 GeneratePersonalityValue()
    {
        Random random = new();
        return (uint)random.NextInt64(UInt32.MaxValue);
    }

    private bool NicknameExists()
    {
        return !string.Equals(Regex.Replace(Nickname.ToLower().Replace(" ", "-"), @"[^0-9a-zA-Z\w]+", ""), Lookup.GetSpeciesName(SpeciesId).ToLower());
    }
}