using System.Data;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace PokemonStorage.Models;

public partial class PartyPokemon
{
    #region Gen 2
    // https://bulbapedia.bulbagarden.net/wiki/Pok%C3%A9mon_data_structure_(Generation_II)
    public void LoadFromGen2Bytes(byte[] content, Game game, string nickname, string trainerName, string language)
    {
        Origin = new Origin(game.VersionId);
        LanguageId = Lookup.GetLanguageIdByIdentifier(language);

        Nickname = nickname;
        PokemonIdentity = Lookup.GetPokemonByFormId(Lookup.GetPokemonFormIdByGameIndex(2, Utility.GetByte(content, 0x00)), LanguageId);
        HeldItemId = Lookup.GetItemIdByGameIndex(2, Utility.GetUnsignedNumber<byte>(content, 0x01, 1, true));
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
            Moves[i].Pp = Convert.ToByte(ppBinary.Substring(2, 6), 2);
            Moves[i].TimesIncreased = Convert.ToByte(ppBinary.Substring(0, 2), 2);
            Moves[i].SlotId = (byte)i;
        }

        // Get Stats
        ushort ivData = Utility.GetUnsignedNumber<ushort>(content, 0x15, 2, true);
        string ivBinary = Convert.ToString(ivData, 2).PadLeft(16, '0');
        // Program.Logger.LogInformation($"IV Binary: {string.Join('_', ivBinary.Chunk(4).Select(x => new string(x)))}");

        StatHextuple ev = new StatHextuple(
            Utility.GetUnsignedNumber<ushort>(content, 0x0B, 2, true),
            Utility.GetUnsignedNumber<ushort>(content, 0x0D, 2, true),
            Utility.GetUnsignedNumber<ushort>(content, 0x0F, 2, true),
            Utility.GetUnsignedNumber<ushort>(content, 0x11, 2, true),
            Utility.GetUnsignedNumber<ushort>(content, 0x13, 2, true),
            Utility.GetUnsignedNumber<ushort>(content, 0x13, 2, true)
        );

        StatHextuple iv = new StatHextuple(
            Convert.ToByte(string.Join("", ivBinary.Chunk(4).Select(x => x.Last())), 2),
            Convert.ToByte(ivBinary.Substring(0, 4), 2),
            Convert.ToByte(ivBinary.Substring(4, 4), 2),
            Convert.ToByte(ivBinary.Substring(8, 4), 2),
            Convert.ToByte(ivBinary.Substring(12, 4), 2),
            Convert.ToByte(ivBinary.Substring(12, 4), 2)
        );

        Stats = new(false, ev, iv);

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
        Origin.MetLocationId = Lookup.GetLocationIdByGameIndex(2, Convert.ToUInt16(caughtBinary.Substring(9, 7), 2));

        // Calculations
        AssignGenderByAttackIv();
        HasNickname = DoesNicknameExist();
    }
    #endregion

    #region Gen 3
    // https://bulbapedia.bulbagarden.net/wiki/Pok%C3%A9mon_data_structure_(Generation_III)
    public void LoadFromGen3Bytes(byte[] content, Game game, string language)
    {
        Origin = new Origin(game.VersionId);
        PersonalityValue = Utility.GetUnsignedNumber<uint>(content, 0x00, 4);
        uint otId = Utility.GetUnsignedNumber<uint>(content, 0x04, 4);
        OriginalTrainer = new Trainer(
            Utility.GetEncodedString(Utility.GetBytes(content, 0x14, 7), game, language),
            0,
            Utility.GetUnsignedNumber<ushort>(content, 0x04, 2),
            Utility.GetUnsignedNumber<ushort>(content, 0x06, 2)
        );

        Nickname = Utility.GetEncodedString(Utility.GetBytes(content, 0x08, 10), game, language);
        LanguageId = Lookup.GetLanguageIdByIdentifier(language);
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
        // Program.Logger.LogInformation($"Decryption Order: {order_index}:{orderString}");
        uint decryptionKey = PersonalityValue ^ otId;
        byte[] decryptedData = [];
        for (int i = 0; i < 48; i += 4)
        {
            byte[] y = Utility.GetBytes(fullEncryptedData, 0x1 * i, 4);
            byte[] unencryptedBytes = y.Zip(BitConverter.GetBytes(decryptionKey)).Select(x => Convert.ToByte(x.First ^ x.Second)).ToArray();
            decryptedData = decryptedData.Concat(unencryptedBytes).ToArray();
        }

        uint calculated = 0;
        for (int i = 0; i < 48; i += 2)
        {
            calculated += Utility.GetUnsignedNumber<ushort>(decryptedData, 0x1 * i, 2);
        }

        // Program.Logger.LogInformation($"{checksum & 0xffff} ?== {calculated & 0xffff}");
        // Program.Logger.LogInformation($"CHSM:{Convert.ToString(checksum, 2).PadLeft(17, '0')}");
        // Program.Logger.LogInformation($"CALC:{Convert.ToString(calculated, 2).PadLeft(17, '0')}");
        bool checksumResult = (checksum & 0xffff) == (calculated & 0xffff);
        if (!checksumResult)
        {
            throw new Exception($"Bad checksum result. Expected {checksum & 0xffff} and got {calculated & 0xffff}");
        }

        int abilitySlotId = 0;
        StatHextuple ev = new StatHextuple();
        StatHextuple iv = new StatHextuple();
        foreach ((char c, int i) in orderString.Select((c, i) => (c, i)))
        {
            int offset = i * 12;
            byte[] substructureBytes = Utility.GetBytes(decryptedData, offset, 12);
            // Program.Logger.LogInformation($"{c} ==> {BitConverter.ToString(substructureBytes)}");
            switch (c)
            {
                case 'G':
                    PokemonIdentity = Lookup.GetPokemonByFormId(Lookup.GetPokemonFormIdByGameIndex(3, Utility.GetUnsignedNumber<ushort>(substructureBytes, 0x00, 2)), LanguageId); 
                    HeldItemId = Lookup.GetItemIdByGameIndex(3, Utility.GetUnsignedNumber<ushort>(substructureBytes, 0x02, 2));
                    ExperiencePoints = Utility.GetUnsignedNumber<uint>(substructureBytes, 0x04, 4);

                    byte ppBonuses = Utility.GetUnsignedNumber<byte>(substructureBytes, 0x08, 1);
                    string ppBinary = Convert.ToString(ppBonuses, 2).PadLeft(8, '0');
                    for (int j = 0; j < 4; j++)
                    {
                        byte bonuses = Convert.ToByte(ppBinary.Substring(j * 2, 2), 2);
                        Moves[j].TimesIncreased = bonuses;
                    }
                    Friendship = Utility.GetUnsignedNumber<byte>(substructureBytes, 0x09, 1);
                    break;

                case 'A':
                    for (int j = 0; j < 4; j++)
                    {
                        Moves[j].Id = Utility.GetUnsignedNumber<ushort>(substructureBytes, j * 2, 2);
                        Moves[j].SlotId = (byte)j;
                    }

                    for (int j = 0; j < 4; j++)
                    {
                        Moves[j].Pp = Utility.GetUnsignedNumber<byte>(substructureBytes, 0x08 + j, 1);
                    }
                    break;

                case 'E':
                    ev.HP = Utility.GetUnsignedNumber<byte>(substructureBytes, 0x00, 1);
                    ev.Attack = Utility.GetUnsignedNumber<byte>(substructureBytes, 0x01, 1);
                    ev.Defense = Utility.GetUnsignedNumber<byte>(substructureBytes, 0x02, 1);
                    ev.Speed = Utility.GetUnsignedNumber<byte>(substructureBytes, 0x03, 1);
                    ev.SpecialAttack = Utility.GetUnsignedNumber<byte>(substructureBytes, 0x04, 1);
                    ev.SpecialDefense = Utility.GetUnsignedNumber<byte>(substructureBytes, 0x05, 1);

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
                    // Program.Logger.LogInformation($"Pokerus Data: {pokerusBinary}");
                    PokerusStrain = Convert.ToByte(pokerusBinary.Substring(0, 4), 2);
                    PokerusDaysRemaining = Convert.ToByte(pokerusBinary.Substring(4, 4), 2);

                    // Origin
                    Origin.MetLocationId = Lookup.GetLocationIdByGameIndex(3, Utility.GetUnsignedNumber<ushort>(substructureBytes, 0x01, 1));
                    ushort originData = Utility.GetUnsignedNumber<ushort>(substructureBytes, 0x02, 2);
                    string originBinary = Convert.ToString(originData, 2).PadLeft(16, '0');

                    // G Poke Game Level
                    // 0 0000 0000 0000000
                    // Program.Logger.LogInformation($"Origin Data: {originBinary}");

                    OriginalTrainer.Gender = originBinary[0] == '1' ? Gender.FEMALE : Gender.MALE;
                    Origin.PokeballId = Convert.ToByte(originBinary.Substring(1, 4), 2);
                    Origin.GameVersionId = Lookup.GetVersionIdByGameIndex(Convert.ToUInt16(originBinary.Substring(5, 4), 2));
                    Origin.MetLevel = Convert.ToByte(originBinary.Substring(9, 7), 2);

                    // IVs, Egg, Ability
                    uint miscData = Utility.GetUnsignedNumber<uint>(substructureBytes, 0x04, 4);
                    string miscBinary = Convert.ToString(miscData, 2).PadLeft(32, '0');

                    // A E SpD   SpA   Spe   Def   Atk   HP
                    // 0 0 00000 00000 00000 00000 00000 00000
                    // Program.Logger.LogInformation($"Misc Data: {miscBinary}");

                    abilitySlotId = Int32.Parse(miscBinary[0].ToString());
                    IsEgg = miscBinary[1] == '1';
                    iv.SpecialDefense = Convert.ToByte(miscBinary.Substring(2, 5), 2);
                    iv.SpecialAttack = Convert.ToByte(miscBinary.Substring(7, 5), 2);
                    iv.Speed = Convert.ToByte(miscBinary.Substring(12, 5), 2);
                    iv.Defense = Convert.ToByte(miscBinary.Substring(17, 5), 2);
                    iv.Attack = Convert.ToByte(miscBinary.Substring(22, 5), 2);
                    iv.HP = Convert.ToByte(miscBinary.Substring(27, 5), 2);

                    // Ribbons, Obedience
                    string ribbonBinary = Convert.ToString(Utility.GetUnsignedNumber<uint>(substructureBytes, 0x08, 4), 2).PadLeft(32, '0');

                    // O ---- W E N C n r b E A V W C Tou Sma Cut Bea Coo
                    // 0 0000 0 0 0 0 0 0 0 0 0 0 0 0 000 000 000 000 000
                    // Program.Logger.LogInformation($"Ribbon Data: {ribbonBinary}");
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

        // Calculations
        // Program.Logger.LogInformation($"Done reading: {PokemonIdentity.SpeciesIdentifier}");
        Stats = new(true, iv, ev);
        Gender = GetGenderByPersonalityValue();
        AbilityId = GetAbilityFromSlotId(abilitySlotId);
        HasNickname = DoesNicknameExist();
    }
    #endregion

    #region Gen 4
    // https://bulbapedia.bulbagarden.net/wiki/Pok%C3%A9mon_data_structure_(Generation_IV)
    public void LoadFromGen4Bytes(byte[] content, Game game, string language)
    {        
        // decryption
        const int WORD_SIZE = 2;
        const uint DECRYPTION_FACTOR = 0x41C64E6D;
        const uint DECRYPTION_CONST = 0x6073;

        uint checksum = Utility.GetUnsignedNumber<uint>(content, 0x06, 2);
        byte[] encrypted = Utility.GetBytes(content, 0x08, 128);
        uint prng = checksum;
        byte[] unencrypted = new byte[128];

        for (int i = 0; i < 64 * WORD_SIZE; i += WORD_SIZE)
        {
            unchecked
            {
                prng = (DECRYPTION_FACTOR * prng + DECRYPTION_CONST) & 0xFFFFFFFFu;
            }

            uint rand = prng >> 16;
            uint y = Utility.GetUnsignedNumber<uint>(encrypted, i, WORD_SIZE);
            ushort decryptedWord = (ushort)((y ^ (int)rand) & 0xFFFF);

            // write little-endian two bytes to decrypted buffer
            unencrypted[i] = (byte)(decryptedWord & 0xFF);
            unencrypted[i + 1] = (byte)((decryptedWord >> 8) & 0xFF);
        }

        // checksum calculation
        uint calculated = 0;
        for (int i = 0; i < 64 * WORD_SIZE; i += 2)
        {
            calculated += Utility.GetUnsignedNumber<ushort>(unencrypted, i, 2);
        }
        calculated &= 0xFFFF;

        // Program.Logger.LogInformation($"{checksum & 0xffff} ?== {calculated & 0xffff}");
        bool checksumResult = (checksum & 0xffff) == (calculated & 0xffff);
        if (!checksumResult)
        {
            throw new Exception($"Bad checksum result. Expected {checksum & 0xffff} and got {calculated & 0xffff}");
        }

        // block order maps
        var shuffle = new Dictionary<int, string>
        {
            {0,"ABCD"}, {1,"ABDC"}, {2,"ACBD"}, {3,"ACDB"},
            {4,"ADBC"}, {5,"ADCB"}, {6,"BACD"}, {7,"BADC"},
            {8,"BCAD"}, {9,"BCDA"}, {10,"BDAC"}, {11,"BDCA"},
            {12,"CABD"}, {13,"CADB"}, {14,"CBAD"}, {15,"CBDA"},
            {16,"CDAB"}, {17,"CDBA"}, {18,"DABC"}, {19,"DACB"},
            {20,"DBAC"}, {21,"DBCA"}, {22,"DCAB"}, {23,"DCBA"}
        };

        var unshuffle = new Dictionary<int, string>
        {
            {0,"ABCD"}, {1,"ABDC"}, {2,"ACBD"}, {3,"ADBC"},
            {4,"ACDB"}, {5,"ADCB"}, {6,"BACD"}, {7,"BADC"},
            {8,"CABD"}, {9,"DABC"}, {10,"CADB"}, {11,"DACB"},
            {12,"BCAD"}, {13,"BDAC"}, {14,"CBAD"}, {15,"DBAC"},
            {16,"CDAB"}, {17,"DCAB"}, {18,"BCDA"}, {19,"BDCA"},
            {20,"CBDA"}, {21,"DBCA"}, {22,"CDBA"}, {23,"DCBA"}
        };

        PersonalityValue = Utility.GetUnsignedNumber<uint>(content, 0x00, 4);
        int s = (int)(((PersonalityValue & 0x3E000) >> 0xD) % 24);
        string shuffledOrder = shuffle[s];
        // Program.Logger.LogInformation($"Decryption Order: {s}:{shuffledOrder}");

        StatHextuple ev = new StatHextuple();
        StatHextuple iv = new StatHextuple();
        for (int i = 0; i < shuffledOrder.Length; i++)
        {
            int thisOffset = i * 0x20;
            char c = shuffledOrder[i];
            byte[] blockBytes = Utility.GetBytes(unencrypted, thisOffset, 0x20);

            switch (c)
            {
                case 'A':
                    PokemonIdentity = Lookup.GetPokemonByFormId(Lookup.GetPokemonFormIdByGameIndex(4, Utility.GetUnsignedNumber<ushort>(blockBytes, 0x00, 2)), LanguageId); 
                    HeldItemId = Lookup.GetItemIdByGameIndex(4, Utility.GetUnsignedNumber<ushort>(blockBytes, 0x02, 2));
                    OriginalTrainer.PublicId = Utility.GetUnsignedNumber<ushort>(blockBytes, 0x04, 2);
                    OriginalTrainer.SecretId = Utility.GetUnsignedNumber<ushort>(blockBytes, 0x06, 2);
                    ExperiencePoints = Utility.GetUnsignedNumber<uint>(blockBytes, 0x08, 4);
                    Friendship = Utility.GetUnsignedNumber<byte>(blockBytes, 0x0C, 1);
                    AbilityId = Utility.GetUnsignedNumber<byte>(blockBytes, 0x0D, 1);
                    Markings = new(4, Utility.GetUnsignedNumber<byte>(blockBytes, 0x0E, 1));
                    LanguageId = Lookup.GetLanguageIdByGameIndex(Utility.GetUnsignedNumber<byte>(blockBytes, 0x0F, 1));
                    ev.HP = Utility.GetUnsignedNumber<byte>(blockBytes, 0x10, 1);
                    ev.Attack = Utility.GetUnsignedNumber<byte>(blockBytes, 0x11, 1);
                    ev.Defense = Utility.GetUnsignedNumber<byte>(blockBytes, 0x12, 1);
                    ev.Speed = Utility.GetUnsignedNumber<byte>(blockBytes, 0x13, 1);
                    ev.SpecialAttack = Utility.GetUnsignedNumber<byte>(blockBytes, 0x14, 1);
                    ev.SpecialDefense = Utility.GetUnsignedNumber<byte>(blockBytes, 0x15, 1);
                    Coolness = Utility.GetUnsignedNumber<byte>(blockBytes, 0x16, 1);
                    Beauty = Utility.GetUnsignedNumber<byte>(blockBytes, 0x17, 1);
                    Cuteness = Utility.GetUnsignedNumber<byte>(blockBytes, 0x18, 1);
                    Smartness = Utility.GetUnsignedNumber<byte>(blockBytes, 0x19, 1);
                    Toughness = Utility.GetUnsignedNumber<byte>(blockBytes, 0x1A, 1);
                    Sheen = Utility.GetUnsignedNumber<byte>(blockBytes, 0x1B, 1);
                    Ribbons.ParseRibbonSet(1, Utility.GetBytes(blockBytes, 0x1C, 4));
                    break;

                case 'B':
                    // Moves
                    Moves[0].Id = Utility.GetUnsignedNumber<ushort>(blockBytes, 0x0, 2);
                    Moves[1].Id = Utility.GetUnsignedNumber<ushort>(blockBytes, 0x2, 2);
                    Moves[2].Id = Utility.GetUnsignedNumber<ushort>(blockBytes, 0x4, 2);
                    Moves[3].Id = Utility.GetUnsignedNumber<ushort>(blockBytes, 0x6, 2);
                    Moves[0].Pp = Utility.GetUnsignedNumber<byte>(blockBytes, 0x8, 1);
                    Moves[1].Pp = Utility.GetUnsignedNumber<byte>(blockBytes, 0x9, 1);
                    Moves[2].Pp = Utility.GetUnsignedNumber<byte>(blockBytes, 0xA, 1);
                    Moves[3].Pp = Utility.GetUnsignedNumber<byte>(blockBytes, 0xB, 1);
                    Moves[0].TimesIncreased = Utility.GetUnsignedNumber<byte>(blockBytes, 0xC, 1);
                    Moves[1].TimesIncreased = Utility.GetUnsignedNumber<byte>(blockBytes, 0xD, 1);
                    Moves[2].TimesIncreased = Utility.GetUnsignedNumber<byte>(blockBytes, 0xE, 1);
                    Moves[3].TimesIncreased = Utility.GetUnsignedNumber<byte>(blockBytes, 0xF, 1);
                    Moves[0].SlotId = 0;
                    Moves[1].SlotId = 1;
                    Moves[2].SlotId = 2;
                    Moves[3].SlotId = 3;

                    // IVs and more
                    uint ivData = Utility.GetUnsignedNumber<uint>(blockBytes, 0x10, 4);
                    string ivBinary = Utility.ReverseString(Convert.ToString(ivData, 2).PadLeft(32, '0'));
                    iv.HP = Convert.ToByte(Utility.ReverseString(ivBinary.Substring(0, 5)), 2);
                    iv.Attack = Convert.ToByte(Utility.ReverseString(ivBinary.Substring(5, 5)), 2);
                    iv.Defense = Convert.ToByte(Utility.ReverseString(ivBinary.Substring(10, 5)), 2);
                    iv.Speed = Convert.ToByte(Utility.ReverseString(ivBinary.Substring(15, 5)), 2);
                    iv.SpecialAttack = Convert.ToByte(Utility.ReverseString(ivBinary.Substring(20, 5)), 2);
                    iv.SpecialDefense = Convert.ToByte(Utility.ReverseString(ivBinary.Substring(25, 5)), 2);
                    IsEgg = ivBinary[30] == '1';
                    HasNickname = ivBinary[31] == '1';

                    // Heonn Ribbons
                    Ribbons.ParseRibbonSet(0, Utility.GetBytes(blockBytes, 0x14, 4));

                    // Flags
                    int flagsData = Utility.GetUnsignedNumber<byte>(blockBytes, 0x18, 1);
                    string flagsBinary = Utility.ReverseString(Convert.ToString(flagsData, 2).PadLeft(8, '0'));

                    Origin.FatefulEncounter = flagsBinary[0] == '1';
                    if (flagsBinary[2] == '1') Gender = Gender.GENDERLESS;
                    else
                    {
                        Gender = flagsBinary[1] == '1' ? Gender.FEMALE : Gender.MALE;
                    }
                    
                    AlternateFormId = (ushort)Convert.ToInt16(Utility.ReverseString(flagsBinary.Substring(3, 5)), 2);
                    ShinyLeaves = Utility.GetUnsignedNumber<byte>(blockBytes, 0x19, 1);

                    Origin.EggHatchLocationPlatinumId = Lookup.GetLocationIdByGameIndex(4, Utility.GetUnsignedNumber<ushort>(blockBytes, 0x1C, 2));
                    Origin.MetLocationPlatinumId = Lookup.GetLocationIdByGameIndex(4, Utility.GetUnsignedNumber<ushort>(blockBytes, 0x1E, 2));
                    break;

                case 'C':
                    byte[] nicknameBytes = Utility.GetBytes(blockBytes, 0x0, 20);
                    Nickname = Utility.GetEncodedString(nicknameBytes, game, language);
                    Origin.GameVersionId = Lookup.GetVersionIdByGameIndex(Utility.GetUnsignedNumber<byte>(blockBytes, 0x17, 1));
                    Ribbons.ParseRibbonSet(2, Utility.GetBytes(blockBytes, 0x18, 4));
                    break;

                case 'D':
                    byte[] otNameBytes = Utility.GetBytes(blockBytes, 0x0, 16);
                    OriginalTrainer.Name = Utility.GetEncodedString(otNameBytes, game, language);
                    
                    byte eggYear = Utility.GetUnsignedNumber<byte>(blockBytes, 0x10, 1);
                    byte eggMonth = Utility.GetUnsignedNumber<byte>(blockBytes, 0x11, 1);
                    byte eggDay = Utility.GetUnsignedNumber<byte>(blockBytes, 0x12, 1);
                    if (eggDay > 0)
                    {
                        Origin.EggReceiveDate = new DateTime(eggYear + 2000, eggMonth, eggDay);
                    }

                    byte metYear = Utility.GetUnsignedNumber<byte>(blockBytes, 0x13, 1);
                    byte metMonth = Utility.GetUnsignedNumber<byte>(blockBytes, 0x14, 1);
                    byte metDay = Utility.GetUnsignedNumber<byte>(blockBytes, 0x15, 1);
                    if (metDay > 0)
                    {
                        Origin.MetDateTime = new DateTime(metYear + 2000, metMonth, metDay);
                    }
                    
                    Origin.EggHatchLocationId = Lookup.GetLocationIdByGameIndex(4, Utility.GetUnsignedNumber<ushort>(blockBytes, 0x16, 2));
                    Origin.MetLocationId = Lookup.GetLocationIdByGameIndex(4, Utility.GetUnsignedNumber<ushort>(blockBytes, 0x18, 2));

                    byte pokerusData = Utility.GetUnsignedNumber<byte>(blockBytes, 0x1A, 1);
                    string pokerusBinary = Convert.ToString(pokerusData, 2).PadLeft(8, '0');
                    PokerusStrain = Convert.ToByte(pokerusBinary.Substring(0, 4), 2);
                    PokerusDaysRemaining = Convert.ToByte(pokerusBinary.Substring(4, 4), 2);

                    Origin.PokeballId = Utility.GetUnsignedNumber<byte>(blockBytes, 0x1B, 1);

                    int originData = Utility.GetUnsignedNumber<byte>(blockBytes, 0x1C, 1);
                    string originBinary = Utility.ReverseString(Convert.ToString(originData, 2).PadLeft(8, '0'));
                    Origin.MetLevel = Convert.ToByte(Utility.ReverseString(originBinary.Substring(0, 6)), 2);
                    OriginalTrainer.Gender = originBinary[7] == '1' ? Gender.FEMALE : Gender.MALE;
                    Origin.EncounterTypeId = Utility.GetUnsignedNumber<byte>(blockBytes, 0x1D, 1);

                    if (game.VersionId == 15 || game.VersionId == 16)
                    {
                        if (Origin.PokeballId == 0)
                        {
                            Origin.PokeballId = Utility.GetUnsignedNumber<byte>(blockBytes, 0x1E, 1);
                        }
                        WalkingMood = Utility.GetUnsignedNumber<byte>(blockBytes, 0x1F, 1);
                    }
                    break;
                default:
                    break;
            }
        }

        // Calculations        
        // Program.Logger.LogInformation($"Done reading: {PokemonIdentity.SpeciesIdentifier}");
        Stats = new(true, iv, ev);
    }
    #endregion

}