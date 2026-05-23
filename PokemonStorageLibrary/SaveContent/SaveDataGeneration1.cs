using Microsoft.Extensions.Logging;
using PokemonStorageLibrary.Models;

namespace PokemonStorageLibrary.SaveContent;

public class SaveDataGeneration1 : SaveData
{
    private int BoxSize = 0x462;
    private int CurrentBoxNumber { get; }
    private int[] BoxOffsets =          [0x4000, 0x4462, 0x48C4, 0x4D26, 0x5188, 0x55EA, 0x6000, 0x6462, 0x68C4, 0x6D26, 0x7188, 0x75EA];
    private int[] BoxChecksumOffsets =  [0x5A4D, 0x5A4E, 0x5A4F, 0x5A50, 0x5A51, 0x5A52, 0x7A4D, 0x7A4E, 0x7A4F, 0x7A50, 0x7A51, 0x7A52];
    private List<Generation1Box> BoxData = [];

    public SaveDataGeneration1(byte[] content, Game game, string language) : base(content, game, language)
    {
        CurrentBoxNumber = Utility.GetByte(ModifiedData, 0x284C) & 0x7F;

        for (int i = 0; i < BoxOffsets.Length; i++)
        {
            int thisOffset = CurrentBoxNumber == i ? 0x30C0 : BoxOffsets[i];

            // Skip the box if it is not initialized
            if (ModifiedData[thisOffset] == 0xFF) continue;
            
            BoxData.Add(new Generation1Box(Utility.GetBytes(ModifiedData, thisOffset, BoxSize), (byte)i, Game, Language.Identifier));
        }
        ParseOriginalTrainer();
        AreAllChecksumsValid();
        ParsePartyPokemon();
        ParseBoxPokemon();
    }

    #region Override

    protected override void ParseOriginalTrainer()
    {
        string playerName = Utility.GetDecodedString(Utility.GetBytes(OriginalData, 0x2598, 11), Game, Language.Identifier);
        ushort playerId = Utility.GetUnsignedNumber<ushort>(OriginalData, 0x2605, 2, true);
        Trainer = new(playerName, (int)Gender.MALE, playerId, 0);
    }

    protected override void ParsePartyPokemon()
    {
        byte[] partyBytes = Utility.GetBytes(OriginalData, 0x2F2C, 0x194);
        Party = GetPokemonFromStorage(partyBytes, Language.Iso639, 0x8, 0x2C, 0x110, 0x152);
    }

    protected override void ParseBoxPokemon()
    {
        for (int i = 0; i < BoxData.Count; i++)
        {
            BoxList[$"{i+1}"] = GetPokemonFromStorage(BoxData[i].OriginalBoxBytes, BoxData[i].Language, 0x16, 0x21, 0x2AA, 0x386);
        }
    }

    public override void PrintPokedex()
    {
        byte[] owned = Utility.GetBytes(ModifiedData, 0x25A3, 0x13);
        byte[] seen = Utility.GetBytes(ModifiedData, 0x25B6, 0x13);
        for (int i = 0; i < 151; i++)
        {
            PokemonIdentity pokemon = Lookup.GetPokemonBySpeciesId(i+1, (int)Lookup.GetIdByIdentifier(Language.Identifier, DatabaseObject.Languages));
            int ownedBit = owned[i >> 3] >> (i & 7) & 1;
            int seenBit = seen[i >> 3] >> (i & 7) & 1;
            Console.WriteLine($"{seenBit}/{ownedBit} - {pokemon.SpeciesName}");
        }
    }

    public override void WriteToPokedex(int nationalIndex, bool seen=true, bool owned=true)
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

    public override bool AreAllChecksumsValid()
    {
        Checksum bank1 = new()
        {
            Real = Utility.GetByte(ModifiedData, 0x3523),
            Calculated = CalculateChecksum(ModifiedData, 0x2598, 0x3522-0x2598)
        };

        Console.WriteLine($"Main-Real:{Convert.ToString(bank1.Real, 2)}");
        Console.WriteLine($"Main-Calc:{Convert.ToString(bank1.Calculated, 2)}");
        if (!bank1.IsByteSizeValid()) return false;

        Checksum bank2 = new()
        {
            Real = Utility.GetByte(ModifiedData, 0x5A4C),
            Calculated = CalculateChecksum(ModifiedData, 0x4000, 0x462*6)
        };

        Console.WriteLine($"Bank2-Real:{Convert.ToString(bank2.Real, 2)}");
        Console.WriteLine($"Bank2-Calc:{Convert.ToString(bank2.Calculated, 2)}");
        if (!bank2.IsByteSizeValid()) return false;

        Checksum bank3 = new()
        {
            Real = Utility.GetByte(ModifiedData, 0x7A4C),
            Calculated = CalculateChecksum(ModifiedData, 0x6000, 0x462*6)
        };

        Console.WriteLine($"Bank3-Real:{Convert.ToString(bank3.Real, 2)}");
        Console.WriteLine($"Bank3-Calc:{Convert.ToString(bank3.Calculated, 2)}");
        if (!bank3.IsByteSizeValid()) return false;

        Checksum[] boxChecksums = new Checksum[12];
        for (int i = 0; i < boxChecksums.Length; i++)
        {
            boxChecksums[i].Real = Utility.GetByte(ModifiedData, BoxChecksumOffsets[i]);
            boxChecksums[i].Calculated = CalculateChecksum(ModifiedData, BoxOffsets[i], BoxSize);

            Console.WriteLine($"Box{i+1}-Real:{Convert.ToString(boxChecksums[i].Real, 2)}");
            Console.WriteLine($"Box{i+1}-Calc:{Convert.ToString(boxChecksums[i].Calculated, 2)}");
            if (!boxChecksums[i].IsByteSizeValid()) return false;
        }

        return true;
    }

    // https://bulbapedia.bulbagarden.net/wiki/Pok%C3%A9mon_data_structure_(Generation_I)
    public override PartyPokemon GetPartyPokemonFromBoxBytes(byte[] data)
    {
        PartyPokemon p = new(Game)
        {
            Origin = new Origin(Game.VersionId),
            LanguageId = Language.Id
        };
        p.PokemonIdentity = Lookup.GetPokemonByFormId(Lookup.GetIdByGameIndex(Utility.GetByte(data, 0x00), SupplementObject.Pokemon, 1), p.LanguageId); 
        p.ExperiencePoints = Utility.GetUnsignedNumber<uint>(data, 0x0E, 3, true);
        p.Friendship = p.GetBaseHappiness();

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

            byte ppData = Utility.GetUnsignedNumber<byte>(data, movePpOffset, 1, true);
            string ppBinary = Convert.ToString(ppData, 2).PadLeft(8, '0');

            p.Moves[i].TimesIncreased = Convert.ToByte(ppBinary.Substring(0, 2), 2);
            p.Moves[i].Pp = Convert.ToByte(ppBinary.Substring(2, 6), 2);
            p.Moves[i].Id = Utility.GetUnsignedNumber<byte>(data, moveIndexOffset, 1, true);
            p.Moves[i].SlotId = (byte)i;
        }

        // Get Stats
        ushort ivData = Utility.GetUnsignedNumber<ushort>(data, 0x1B, 2, true);
        string ivBinary = Convert.ToString(ivData, 2).PadLeft(16, '0');

        StatHextuple ev = new StatHextuple(
            Utility.GetUnsignedNumber<ushort>(data, 0x11, 2, true),
            Utility.GetUnsignedNumber<ushort>(data, 0x13, 2, true),
            Utility.GetUnsignedNumber<ushort>(data, 0x15, 2, true),
            Utility.GetUnsignedNumber<ushort>(data, 0x19, 2, true),
            Utility.GetUnsignedNumber<ushort>(data, 0x19, 2, true),
            Utility.GetUnsignedNumber<ushort>(data, 0x17, 2, true)
        );

        StatHextuple iv = new StatHextuple(
            Convert.ToByte(string.Join("", ivBinary.Chunk(4).Select(x => x.Last())), 2),
            Convert.ToByte(ivBinary.Substring(0, 4), 2),
            Convert.ToByte(ivBinary.Substring(4, 4), 2),
            Convert.ToByte(ivBinary.Substring(12, 4), 2),
            Convert.ToByte(ivBinary.Substring(12, 4), 2),
            Convert.ToByte(ivBinary.Substring(8, 4), 2)
        );

        p.Stats = new(p, false, iv, ev);

        // Calculations
        p.Origin.MetLevel = p.Level;
        p.Origin.MetLocationId = 254; // Link Trade Arrive
        p.Gender = p.GetGenderByAttackIv();
        return p;
    }

    public override byte[] GetBoxBytesFromPartyPokemon(PartyPokemon p)
    {
        byte[] bytes = new byte[0x21];
        Array.Fill<byte>(bytes, 0);
        // Index number of species
        bytes[0x00] = (byte)(Lookup.GetGameIndexById(p.PokemonIdentity.FormId, SupplementObject.Pokemon, 1) ?? 0xB5); // Default = Missingno;
        // Level
        bytes[0x03] = p.Level;
        
        byte t1 = GetTypeGameIndexByIndex(p.Type1.Id);
        byte t2 = GetTypeGameIndexByIndex(p.Type2.Id);
        if (t2 == 255) t2 = t1;
        // Type 1
        bytes[0x05] = t1;
        // Type 2
        bytes[0x06] = t2;
        // Catch rate/Held item
        bytes[0x07] = p.GetCatchRate();
        
        // Original trainer ID
        byte[] otid = [.. BitConverter.GetBytes(p.OriginalTrainer.PublicId).Reverse()];
        Buffer.BlockCopy(otid, 0, bytes, 0x0C, 2);

        // Experience points
        byte[] exp = [.. BitConverter.GetBytes(p.ExperiencePoints).Reverse()];
        Buffer.BlockCopy(exp, 1, bytes, 0x0E, 3);

        // EV stats
        byte[] hp_ev = [.. BitConverter.GetBytes(p.Stats.Old.HP.Ev).Reverse()];
        Buffer.BlockCopy(hp_ev, 0, bytes, 0x11, 2);
        byte[] attack_ev = [.. BitConverter.GetBytes(p.Stats.Old.Attack.Ev).Reverse()];
        Buffer.BlockCopy(attack_ev, 0, bytes, 0x13, 2);
        byte[] defense_ev = [.. BitConverter.GetBytes(p.Stats.Old.Defense.Ev).Reverse()];
        Buffer.BlockCopy(defense_ev, 0, bytes, 0x15, 2);
        byte[] speed_ev = [.. BitConverter.GetBytes(p.Stats.Old.Speed.Ev).Reverse()];
        Buffer.BlockCopy(speed_ev, 0, bytes, 0x17, 2);
        byte[] special_ev = [.. BitConverter.GetBytes(p.Stats.Old.SpecialAttack.Ev).Reverse()];
        Buffer.BlockCopy(special_ev, 0, bytes, 0x19, 2);

        // IV data: one nibble for attack, defense, special, speed
        bytes[0x1B] = (byte)((p.Stats.Old.Attack.Iv << 4) + p.Stats.Old.Defense.Iv);
        bytes[0x1C] = (byte)((p.Stats.Old.Speed.Iv << 4) + p.Stats.Old.SpecialAttack.Iv);

        // Move ID, max PP and times increased
        List<Move> validMoves = p.Moves.OrderBy(x => x.Key).Select(x => x.Value).Where(x => x.GetGenerationId() == 1).ToList();
        int moveIdOffset = 0x08;
        int movePpOffset = 0x1D;
        for (int i = 0; i < validMoves.Count; i++)
        {
            bytes[moveIdOffset+i] = (byte)validMoves[i].Id;
            bytes[movePpOffset+i] = (byte)((validMoves[i].TimesIncreased << 6) + Math.Min((byte)63, validMoves[i].Pp));
        }

        // Current HP (assume full)
        byte[] hp = [.. BitConverter.GetBytes(p.Stats.Old.HP.Value).Reverse()];
        Buffer.BlockCopy(hp, 0, bytes, 0x01, 2);
        return bytes;
    }

    public override int AddPokemonToNextOpenBox(PartyPokemon pokemon)
    {
        Generation1Box? targetBox = BoxData.FirstOrDefault(x => x.Count < 20);
        if (targetBox == null) return -1;

        int boxId = targetBox.Id;
        int targetSlot = targetBox.Count;

        if (targetSlot > 20) return -1;
        
        BoxData[boxId].SpeciesIds[targetSlot] = (byte)(Lookup.GetGameIndexById(pokemon.PokemonIdentity.FormId, SupplementObject.Pokemon, 1) ?? 0xBE);
        try
        {
            BoxData[boxId].SpeciesIds[targetSlot+1] = 0xFF;
        }
        catch
        {
            Console.WriteLine($"Box {boxId} is full. Will not write terminator 0xFF.");
        }
        
        BoxData[boxId].OriginalTrainerNames[targetSlot] = Utility.GetEncodedString(pokemon.OriginalTrainer.Name, 11, Game, Language.Identifier);
        BoxData[boxId].PokemonNames[targetSlot] = Utility.GetEncodedString(pokemon.Nickname, 11, Game, Language.Identifier);
        BoxData[boxId].PokemonBytes[targetSlot] = GetBoxBytesFromPartyPokemon(pokemon);
        BoxData[boxId].Count++;
        return boxId;
    }

    public override int AppendPokemonAndSave(List<PartyPokemon> partyPokemonList, string filepath, bool overwriteBackup=true)
    {
        int i = 0;
        foreach (PartyPokemon partyPokemon in partyPokemonList)
        {
            int boxId = AddPokemonToNextOpenBox(partyPokemon);
            ApplyBoxData(boxId);

            WriteToPokedex((int)partyPokemon.GetNationalDexNumber());
            i++;

            WriteRecalculatedChecksums();
            bool isValidWrite = AreAllChecksumsValid();
            
            if (!isValidWrite) throw new InvalidDataException("Checksum after Pokemon write was not valid!");
        }

        File.Copy(filepath, filepath + ".original", overwriteBackup);
        File.WriteAllBytes(filepath, ModifiedData);
        return i;
    }

    #endregion

    #region Helpers

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

    public void CopyToCurrentBoxData(int boxId)
    {
        Array.Copy(BoxData[boxId].GetBoxBytes(), 0, ModifiedData, 0x30C0, BoxData[boxId].GetBoxBytes().Length);
    }

    public void ApplyBoxData(int boxId)
    {
        if (boxId < 0) return;
        byte[] newBoxData = BoxData[boxId].GetBoxBytes();
        if (boxId == CurrentBoxNumber) CopyToCurrentBoxData(boxId);
        Array.Copy(newBoxData, 0, ModifiedData, BoxOffsets[boxId], newBoxData.Length);
    }

    private Dictionary<int, PartyPokemon> GetPokemonFromStorage(byte[] storageBytes, string lang, int pokemonOffset, int pokemonSize, int trainerNameOffset, int nicknamesOffset)
    {
        Dictionary<int, PartyPokemon> box = [];
        byte boxCount = Utility.GetUnsignedNumber<byte>(storageBytes, 0x00, 1, true);

        for (int i = 0; i < boxCount; i++)
        {
            byte[] nicknameBytes = Utility.GetBytes(storageBytes, nicknamesOffset + (0xB * i), 0xB);
            string nickname = Utility.GetDecodedString(nicknameBytes, Game, lang);

            byte[] originalTrainerNameBytes = Utility.GetBytes(storageBytes, trainerNameOffset + (0xB * i), 0xB);
            string originalTrainerName = Utility.GetDecodedString(originalTrainerNameBytes, Game, lang);
            if (string.IsNullOrWhiteSpace(originalTrainerName)) originalTrainerName = "TRAINER";

            byte[] pokemonBytes = Utility.GetBytes(storageBytes, pokemonOffset + (pokemonSize * i), pokemonSize);
            
            // Check for invalid Pokemon
            if (pokemonBytes[0] > 0xBE) continue;

            PartyPokemon pokemon = GetPartyPokemonFromBoxBytes(pokemonBytes);
            pokemon.Nickname = nickname;
            pokemon.HasNickname = pokemon.DoesNicknameExist();
            pokemon.OriginalTrainer = new Trainer(originalTrainerName, 0, Utility.GetUnsignedNumber<ushort>(pokemonBytes, 0x0C, 2, true), 0);
            box[i] = pokemon;
        }
        return box;
    }

    private static byte CalculateChecksum(byte[] content, int offset, int length)
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

    #endregion

    #region Static

    public static byte GetTypeGameIndexByIndex(byte index)
    {
        switch (index)
        {
            case 1: return 0; // Normal
            case 2: return 1; // Fighting
            case 3: return 2; // Flying
            case 4: return 3; // Poison
            case 5: return 4; // Ground
            case 6: return 5; // Rock
            case 7: return 7; // Bug
            case 8: return 8; // Ghost
            case 10: return 20; // Fire
            case 11: return 21; // Water
            case 12: return 22; // Grass
            case 13: return 23; // Electric
            case 14: return 24; // Psychic
            case 15: return 25; // Ice
            case 16: return 26; // Dragon
            default: return 255;
        }
    } 

    #endregion
}