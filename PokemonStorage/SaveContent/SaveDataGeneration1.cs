using Microsoft.Extensions.Logging;
using PokemonStorage.Models;

namespace PokemonStorage.SaveContent;

public class SaveDataGeneration1 : SaveData
{
    private bool IsYellow { get; }
    private int BoxSize = 0x462;
    private int[] BoxOffsets =          [0x4000, 0x4462, 0x48C4, 0x4D26, 0x5188, 0x55EA, 0x6000, 0x6462, 0x68C4, 0x6D26, 0x7188, 0x75EA];
    private int[] BoxChecksumOffsets =  [0x5A4D, 0x5A4E, 0x5A4F, 0x5A50, 0x5A51, 0x5A52, 0x7A4D, 0x7A4E, 0x7A4F, 0x7A50, 0x7A51, 0x7A52];

    public SaveDataGeneration1(byte[] content, Game game, string language) : base(content, game, language)
    {
        IsYellow = game.GameName.Equals("yellow", StringComparison.OrdinalIgnoreCase);
        AreAllChecksumsValid();
        CheckPokedex();
    }


    #region Override

    public override Trainer ParseOriginalTrainer()
    {
        string playerName = Utility.GetEncodedString(Utility.GetBytes(OriginalData, 0x2598, 11), Game, Language);
        ushort playerId = Utility.GetUnsignedNumber<ushort>(OriginalData, 0x2605, 2, true);
        Trainer = new(playerName, (int)Gender.MALE, playerId, 0);
        return Trainer;
    }

    public override void ParsePartyPokemon()
    {
        byte[] partyBytes = Utility.GetBytes(OriginalData, 0x2F2C, 0x194);
        Party = GetPokemonFromStorageGen1(partyBytes, Language, 0x8, 0x2C, 0x110, 0x152);
    }

    public override void ParseBoxPokemon()
    {
        for (int i = 0; i < BoxOffsets.Length; i++)
        {
            byte[] boxBytes = Utility.GetBytes(OriginalData, BoxOffsets[i], BoxSize);
            BoxList[$"{i+1}"] = GetPokemonFromStorageGen1(boxBytes, Language, 0x16, 0x21, 0x2AA, 0x386);
        }
    }

    #endregion

    #region Read Data
    private void CheckPokedex()
    {
        byte[] owned = Utility.GetBytes(ModifiedData, 0x25A3, 0x13);
        byte[] seen = Utility.GetBytes(ModifiedData, 0x25B6, 0x13);
        for (int i = 0; i < 151; i++)
        {
            PokemonIdentity pokemon = Lookup.GetPokemonBySpeciesId(i+1, Lookup.GetLanguageIdByIdentifier(Language));
            int ownedBit = owned[i >> 3] >> (i & 7) & 1;
            int seenBit = seen[i >> 3] >> (i & 7) & 1;
            Program.Logger.LogInformation($"{seenBit}/{ownedBit} - {pokemon.SpeciesName}");
        }
    }

    public override bool AreAllChecksumsValid()
    {
        Checksum bank1 = new()
        {
            Real = Utility.GetByte(ModifiedData, 0x3523),
            Calculated = CalculateChecksum(ModifiedData, 0x2598, 0x3522-0x2598)
        };

        Program.Logger.LogInformation($"Main-Real:{Convert.ToString(bank1.Real, 2)}");
        Program.Logger.LogInformation($"Main-Calc:{Convert.ToString(bank1.Calculated, 2)}");
        if (!IsChecksumValid(bank1)) return false;

        Checksum bank2 = new()
        {
            Real = Utility.GetByte(ModifiedData, 0x5A4C),
            Calculated = CalculateChecksum(ModifiedData, 0x4000, 0x462*6)
        };

        Program.Logger.LogInformation($"Bank2-Real:{Convert.ToString(bank2.Real, 2)}");
        Program.Logger.LogInformation($"Bank2-Calc:{Convert.ToString(bank2.Calculated, 2)}");
        if (!IsChecksumValid(bank2)) return false;

        Checksum bank3 = new()
        {
            Real = Utility.GetByte(ModifiedData, 0x7A4C),
            Calculated = CalculateChecksum(ModifiedData, 0x6000, 0x462*6)
        };

        Program.Logger.LogInformation($"Bank3-Real:{Convert.ToString(bank3.Real, 2)}");
        Program.Logger.LogInformation($"Bank3-Calc:{Convert.ToString(bank3.Calculated, 2)}");
        if (!IsChecksumValid(bank3)) return false;

        Checksum[] boxChecksums = new Checksum[12];
        for (int i = 0; i < boxChecksums.Length; i++)
        {
            boxChecksums[i].Real = Utility.GetByte(ModifiedData, BoxChecksumOffsets[i]);
            boxChecksums[i].Calculated = CalculateChecksum(ModifiedData, BoxOffsets[i], BoxSize);

            Program.Logger.LogInformation($"Box{i+1}-Real:{Convert.ToString(boxChecksums[i].Real, 2)}");
            Program.Logger.LogInformation($"Box{i+1}-Calc:{Convert.ToString(boxChecksums[i].Calculated, 2)}");
            if (!IsChecksumValid(boxChecksums[i])) return false;
        }

        return true;
    }

    #endregion

    #region Write Data


    public void WriteToPokedex(int nationalIndex, bool seen=true, bool owned=true)
    {
        int bitIndex = nationalIndex - 1;
        int byteOffset = bitIndex / 8;
        int maskedBitDecimal = nationalIndex % 8;
        int mask = 1 << maskedBitDecimal - 1; 
        if (seen)
        {
            ModifiedData[0x25B6 + byteOffset] |= (byte)mask;
        }
        if (owned)
        {
            ModifiedData[0x25A3 + byteOffset] |= (byte)mask;
        }
    }

    public void WriteRecalculatedChecksums()
    {
        ModifiedData[0x3523] = CalculateChecksum(ModifiedData, 0x2598, 0x3522-0x2598);
        ModifiedData[0x5A4C] = CalculateChecksum(ModifiedData, 0x4000, 0x462*6);
        ModifiedData[0x7A4C] = CalculateChecksum(ModifiedData, 0x6000, 0x462*6);

        for (int i = 0; i < BoxChecksumOffsets.Length; i++)
        {
            ModifiedData[BoxChecksumOffsets[i]] = CalculateChecksum(ModifiedData, BoxOffsets[i], BoxSize);
        }
    }

    #endregion

    #region Helpers

    private bool IsChecksumValid(Checksum checksum)
    {
        return (byte)checksum.Real == (byte)checksum.Calculated;
    }

    private byte CalculateChecksum(byte[] content, int offset, int length)
    {
        byte sum = 0;
        foreach (byte b in Utility.GetBytes(content, offset, length))
        {
            unchecked
            {
                sum += b;
            }
        }
        return (byte)~sum;
    }

    private Dictionary<int, PartyPokemon> GetPokemonFromStorageGen1(byte[] storageBytes, string lang, int pokemonOffset, int pokemonSize, int trainerNameOffset, int nicknamesOffset)
    {
        Dictionary<int, PartyPokemon> box = [];
        byte boxCount = Utility.GetUnsignedNumber<byte>(storageBytes, 0x00, 1, true);

        for (int i = 0; i < boxCount; i++)
        {
            byte[] nicknameBytes = Utility.GetBytes(storageBytes, nicknamesOffset + (0xB * i), 0xB);
            string nickname = Utility.GetEncodedString(nicknameBytes, Game, lang);

            byte[] originalTrainerNameBytes = Utility.GetBytes(storageBytes, trainerNameOffset + (0xB * i), 0xB);
            string originalTrainerName = Utility.GetEncodedString(originalTrainerNameBytes, Game, lang);

            byte[] pokemonBytes = Utility.GetBytes(storageBytes, pokemonOffset + (pokemonSize * i), pokemonSize);
            PartyPokemon pokemon = new(1);
            pokemon.LoadFromGen1Bytes(pokemonBytes, Game, nickname, originalTrainerName, lang);
            box[i] = pokemon;
        }
        return box;
    }

    #endregion
}