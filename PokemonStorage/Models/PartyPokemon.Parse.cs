using System.Data;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace PokemonStorage.Models;

public partial class PartyPokemon
{

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
                    Nickname = Utility.GetDecodedString(nicknameBytes, game, language);
                    Origin.GameVersionId = Lookup.GetVersionIdByGameIndex(Utility.GetUnsignedNumber<byte>(blockBytes, 0x17, 1));
                    Ribbons.ParseRibbonSet(2, Utility.GetBytes(blockBytes, 0x18, 4));
                    break;

                case 'D':
                    byte[] otNameBytes = Utility.GetBytes(blockBytes, 0x0, 16);
                    OriginalTrainer.Name = Utility.GetDecodedString(otNameBytes, game, language);
                    
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

                    if (game.VersionGroupId == 10)
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
        Stats = new(true, iv, ev, PokemonIdentity.SpeciesId, Level, Nature);
    }
    #endregion

}