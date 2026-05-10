using PokemonStorageLibrary.Models;

namespace PokemonStorageLibrary.SaveContent;

public class SaveDataGeneration2 : SaveData
{
    private bool IsCrystal { get; }
    private bool IsJapanese { get; }
    private int BoxSize { get; }
    private int CurrentBoxNumber { get; }
    private int[] BoxOffsets { get; }
    private List<Generation2Box> BoxData = [];

    public SaveDataGeneration2(byte[] content, Game game, string language) : base(content, game, language)
    {
        IsCrystal = game.GameName.Equals("crystal", StringComparison.OrdinalIgnoreCase);
        IsJapanese = string.Equals(Language.Iso3166, "JA", StringComparison.OrdinalIgnoreCase);

        if (!IsJapanese)
        {
            BoxSize = 1102;
            BoxOffsets = [0x4000, 0x4450, 0x48A0, 0x4CF0, 0x5140, 0x5590, 0x6000, 0x6450, 0x68A0, 0x6CF0, 0x7140, 0x5790];
            CurrentBoxNumber = IsCrystal ? Utility.GetByte(ModifiedData, 0x2D10) : Utility.GetByte(ModifiedData, 0x2D6C);
        }
        else
        {
            BoxSize = 1352;
            BoxOffsets = [0x4000, 0x454A, 0xA94, 0x4FDE, 0x5528, 0x5A72, 0x6000, 0x654A, 0x6A94];
            CurrentBoxNumber = Utility.GetByte(ModifiedData, 0x2D10);
        }
        

        for (int i = 0; i < BoxOffsets.Length; i++)
        {
            int thisOffset = BoxOffsets[i];
            if (CurrentBoxNumber == i)
            {
                thisOffset = IsCrystal || IsJapanese ? 0x2D10 : 0x2D6C;
            }
            BoxData.Add(new Generation2Box(Utility.GetBytes(ModifiedData, thisOffset, BoxSize), (byte)i, Game, Language.Iso639));
        }
        ParseOriginalTrainer();
        AreAllChecksumsValid();
        ParsePartyPokemon();
        ParseBoxPokemon();
    }

    #region Override
    protected override void ParseOriginalTrainer()
    {
        string playerName = Utility.GetDecodedString(Utility.GetBytes(ModifiedData, 0x200B, 11), Game, Language.Iso639);
        ushort playerId = Utility.GetUnsignedNumber<ushort>(ModifiedData, 0x2009, 2, true);
        Trainer = new(
            playerName,
            Game.VersionGroupId == 4 && playerId % 1 == 1 ? (int)Gender.FEMALE : (int)Gender.MALE,
            playerId,
            0
        );
    }
    
    protected override void ParsePartyPokemon()
    {
        int partyOffset;
        if (!IsJapanese) partyOffset = IsCrystal ? 0x2865 : 0x288A;
        else partyOffset = IsCrystal ? 0x283E : 0x281A;

        byte[] partyBytes = Utility.GetBytes(ModifiedData, partyOffset, 428);
        Party = GetPokemonFromStorage(partyBytes, Language.Iso639, 6, 48);
    }

    protected override void ParseBoxPokemon()
    {
        for (int i = 0; i < BoxData.Count; i++)
        {
            BoxList[$"{i+1}"] = GetPokemonFromStorage(BoxData[i].OriginalBoxBytes, BoxData[i].Language, IsJapanese ? 30 : 20, 0x20);
        }
    }

    public override void PrintPokedex()
    {
        int ownedOffset;
        int seenOffset;
        if (IsCrystal)
        {
            if (IsJapanese)
            {
                ownedOffset = 0x29AA;
                seenOffset = 0x29CA;
            }
            else
            {
                ownedOffset = 0x2A27;
                seenOffset = 0x2A47;
            }
        }
        else
        {
            if (IsJapanese)
            {
                ownedOffset = 0x29CE;
                seenOffset = 0x29EE;
            }
            else
            {
                ownedOffset = 0x2A4C;
                seenOffset = 0x2A6C;
            }
        }
        byte[] owned = Utility.GetBytes(ModifiedData, ownedOffset, 0x32);
        byte[] seen = Utility.GetBytes(ModifiedData, seenOffset, 0x32);
        for (int i = 0; i < 251; i++)
        {
            PokemonIdentity pokemon = Lookup.GetPokemonBySpeciesId(i+1, Language.Id);
            int ownedBit = owned[i >> 3] >> (i & 7) & 1;
            int seenBit = seen[i >> 3] >> (i & 7) & 1;
            Console.WriteLine($"{seenBit}/{ownedBit} - {pokemon.SpeciesName}");
        }
    }

    public override void WriteToPokedex(int nationalIndex, bool seen=true, bool owned=true)
    {
        int ownedOffset;
        int seenOffset;
        if (IsCrystal)
        {
            if (IsJapanese)
            {
                ownedOffset = 0x29AA;
                seenOffset = 0x29CA;
            }
            else
            {
                ownedOffset = 0x2A27;
                seenOffset = 0x2A47;
            }
        }
        else
        {
            if (IsJapanese)
            {
                ownedOffset = 0x29CE;
                seenOffset = 0x29EE;
            }
            else
            {
                ownedOffset = 0x2A4C;
                seenOffset = 0x2A6C;
            }
        }

        int bitIndex = nationalIndex - 1;
        int byteOffset = bitIndex / 8;
        int maskedBitDecimal = nationalIndex % 8;
        int mask = 1 << maskedBitDecimal - 1; 
        if (seen)
        {
            ModifiedData[seenOffset + byteOffset] |= (byte)mask;
        }
        if (owned)
        {
            ModifiedData[ownedOffset + byteOffset] |= (byte)mask;
        }
    }

    public override bool AreAllChecksumsValid()
    {
        int checksum1Offset;
        int checksum2Offset;
        Checksum checksum1 = new();
        Checksum checksum2 = new();

        // Gold and Silver
        if (!IsCrystal && !IsJapanese)
        {
            checksum1Offset = 0x2D69;
            checksum2Offset = 0x7E6D;
            checksum1 = new()
            {
                Real = Utility.GetByte(ModifiedData, checksum1Offset),
                Calculated = CalculateChecksum(ModifiedData, 0x2009, 0x2D68-0x2009)
            };
            checksum2 = new()
            {
                Real = Utility.GetByte(ModifiedData, checksum2Offset),
                Calculated = (byte)(CalculateChecksum(ModifiedData, 0x0C6B, 0x17EC-0x0C6B) + CalculateChecksum(ModifiedData, 0x3D96, 0x3F3F-0x3D96) + CalculateChecksum(ModifiedData, 0x7E39, 0x7E6C-0x7E39))
            };
        }
        else if (!IsCrystal && IsJapanese)
        {
            checksum1Offset = 0x2D0D;
            checksum2Offset = 0x7F0D;
            checksum1 = new()
            {
                Real = Utility.GetByte(ModifiedData, checksum1Offset),
                Calculated = CalculateChecksum(ModifiedData, 0x2009, 0x2C8B-0x2009)
            };
            checksum2 = new()
            {
                Real = Utility.GetByte(ModifiedData, checksum2Offset),
                Calculated = CalculateChecksum(ModifiedData, 0x7209, 0x7E8B-0x7209)
            };
        }
        // Crystal
        else if (IsCrystal && !IsJapanese)
        {
            checksum1Offset = 0x2D0D;
            checksum2Offset = 0x1F0D;
            checksum1 = new()
            {
                Real = Utility.GetByte(ModifiedData, checksum1Offset),
                Calculated = CalculateChecksum(ModifiedData, 0x2009, 0x2B82-0x2009)
            };
            checksum2 = new()
            {
                Real = Utility.GetByte(ModifiedData, checksum2Offset),
                Calculated = CalculateChecksum(ModifiedData, 0x1209, 0x1D82-0x1209)
            };
        }
        else if (IsCrystal && IsJapanese)
        {
            checksum1Offset = 0x2D0D;
            checksum2Offset = 0x1F0D;
            checksum1 = new()
            {
                Real = Utility.GetByte(ModifiedData, checksum1Offset),
                Calculated = CalculateChecksum(ModifiedData, 0x2009, 0x2AE2-0x2009)
            };
            checksum2 = new()
            {
                Real = Utility.GetByte(ModifiedData, checksum2Offset),
                Calculated = CalculateChecksum(ModifiedData, 0x7209, 0x7CE2-0x7209)
            };
        }

        Console.WriteLine($"1-Real:{Convert.ToString(checksum1.Real, 2)}");
        Console.WriteLine($"1-Calc:{Convert.ToString(checksum1.Calculated, 2)}");
        if (!checksum1.IsByteSizeValid()) return false;

        Console.WriteLine($"2-Real:{Convert.ToString(checksum2.Real, 2)}");
        Console.WriteLine($"2-Calc:{Convert.ToString(checksum2.Calculated, 2)}");
        if (!checksum2.IsByteSizeValid()) return false;

        return true;
    }

    // https://bulbapedia.bulbagarden.net/wiki/Pok%C3%A9mon_data_structure_(Generation_II)
    public override PartyPokemon GetPartyPokemonFromBoxBytes(byte[] data)
    {
        PartyPokemon p = new(Game)
        {
            Origin = new Origin(Game.VersionId),
            LanguageId = Language.Id
        };

        p.PokemonIdentity = Lookup.GetPokemonByFormId(Lookup.GetIdByGameIndex(Utility.GetByte(data, 0x00), SupplementObject.Pokemon, 2), p.LanguageId);
        p.HeldItemId = Lookup.GetIdByGameIndex(Utility.GetUnsignedNumber<byte>(data, 0x01, 1, true), SupplementObject.Items, 2);
        p.ExperiencePoints = Utility.GetUnsignedNumber<uint>(data, 0x08, 3, true);
        p.Friendship = Utility.GetUnsignedNumber<byte>(data, 0x1B, 1, true);

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

            byte ppData = Utility.GetUnsignedNumber<byte>(data, movePpOffset, 1, true);
            string ppBinary = Convert.ToString(ppData, 2).PadLeft(8, '0');

            p.Moves[i].Id = Utility.GetUnsignedNumber<byte>(data, moveIndexOffset, 1, true);
            p.Moves[i].Pp = Convert.ToByte(ppBinary.Substring(2, 6), 2);
            p.Moves[i].TimesIncreased = Convert.ToByte(ppBinary.Substring(0, 2), 2);
            p.Moves[i].SlotId = (byte)i;
        }

        // Get Stats
        ushort ivData = Utility.GetUnsignedNumber<ushort>(data, 0x15, 2, true);
        string ivBinary = Convert.ToString(ivData, 2).PadLeft(16, '0');
        // Console.WriteLine($"IV Binary: {string.Join('_', ivBinary.Chunk(4).Select(x => new string(x)))}");

        StatHextuple ev = new StatHextuple(
            Utility.GetUnsignedNumber<ushort>(data, 0x0B, 2, true),
            Utility.GetUnsignedNumber<ushort>(data, 0x0D, 2, true),
            Utility.GetUnsignedNumber<ushort>(data, 0x0F, 2, true),
            Utility.GetUnsignedNumber<ushort>(data, 0x11, 2, true),
            Utility.GetUnsignedNumber<ushort>(data, 0x13, 2, true),
            Utility.GetUnsignedNumber<ushort>(data, 0x13, 2, true)
        );

        StatHextuple iv = new StatHextuple(
            Convert.ToByte(string.Join("", ivBinary.Chunk(4).Select(x => x.Last())), 2),
            Convert.ToByte(ivBinary.Substring(0, 4), 2),
            Convert.ToByte(ivBinary.Substring(4, 4), 2),
            Convert.ToByte(ivBinary.Substring(8, 4), 2),
            Convert.ToByte(ivBinary.Substring(12, 4), 2),
            Convert.ToByte(ivBinary.Substring(12, 4), 2)
        );

        p.Stats = new(p, false, iv, ev);

        // Pokerus
        byte pokerusData = Utility.GetUnsignedNumber<byte>(data, 0x1C, 1, true);
        p.PokerusStrain = (uint)(pokerusData >> 4);
        p.PokerusDaysRemaining = (uint)(pokerusData % 16);

        // Caught Data
        ushort caughtData = Utility.GetUnsignedNumber<ushort>(data, 0x1D, 2, true);
        string caughtBinary = Convert.ToString(caughtData, 2).PadLeft(16, '0');
        byte timeframe = Convert.ToByte(caughtBinary.Substring(0, 2));
        p.Origin.MetDateTime = timeframe switch
        {
            1 => (DateTime?)(DateTime.Now.Date + TimeSpan.FromHours(9)),
            2 => (DateTime?)(DateTime.Now.Date + TimeSpan.FromHours(13)),
            3 => (DateTime?)(DateTime.Now.Date + TimeSpan.FromHours(21)),
            _ => (DateTime?)(DateTime.Now.Date + TimeSpan.FromHours(12)),
        };
        p.Origin.MetLevel = Convert.ToByte(caughtBinary.Substring(2, 6), 2);
        p.OriginalTrainer.Gender = Convert.ToByte(caughtBinary.Substring(8, 1), 2) == 1 ? Gender.FEMALE : Gender.MALE;
        p.Origin.MetLocationId = Lookup.GetIdByGameIndex(Convert.ToUInt16(caughtBinary.Substring(9, 7), 2), SupplementObject.Locations, 2);

        // Calculations
        p.AssignGenderByAttackIv();
        return p;
    }

    public override byte[] GetBoxBytesFromPartyPokemon(PartyPokemon p)
    {
        byte[] bytes = new byte[0x20];
        Array.Fill<byte>(bytes, 0);
        bytes[0x00] = (byte)Lookup.GetGameIndexById(p.PokemonIdentity.FormId, SupplementObject.Pokemon, 2);
        bytes[0x01] = (byte)Lookup.GetGameIndexById(p.HeldItemId, SupplementObject.Items, 2);
        bytes[0x02] = (byte)p.Moves[0].Id;
        bytes[0x03] = (byte)p.Moves[1].Id;
        bytes[0x04] = (byte)p.Moves[2].Id;
        bytes[0x05] = (byte)p.Moves[3].Id;
        
        byte[] otid = [.. BitConverter.GetBytes(p.OriginalTrainer.PublicId).Reverse()];
        Buffer.BlockCopy(otid, 0, bytes, 0x06, 2);

        byte[] exp = [.. BitConverter.GetBytes(p.ExperiencePoints).Reverse()];
        Buffer.BlockCopy(exp, 1, bytes, 0x08, 3);

        byte[] hp_ev = [.. BitConverter.GetBytes(p.Stats.Old.HP.Ev).Reverse()];
        Buffer.BlockCopy(hp_ev, 0, bytes, 0x0B, 2);
        byte[] attack_ev = [.. BitConverter.GetBytes(p.Stats.Old.Attack.Ev).Reverse()];
        Buffer.BlockCopy(attack_ev, 0, bytes, 0x0D, 2);
        byte[] defense_ev = [.. BitConverter.GetBytes(p.Stats.Old.Defense.Ev).Reverse()];
        Buffer.BlockCopy(defense_ev, 0, bytes, 0x0F, 2);
        byte[] speed_ev = [.. BitConverter.GetBytes(p.Stats.Old.Speed.Ev).Reverse()];
        Buffer.BlockCopy(speed_ev, 0, bytes, 0x11, 2);
        byte[] special_ev = [.. BitConverter.GetBytes(p.Stats.Old.SpecialAttack.Ev).Reverse()];
        Buffer.BlockCopy(special_ev, 0, bytes, 0x13, 2);

        bytes[0x15] = (byte)((p.Stats.Old.Attack.Iv << 4) + p.Stats.Old.Defense.Iv);
        bytes[0x16] = (byte)((p.Stats.Old.Speed.Iv << 4) + p.Stats.Old.SpecialAttack.Iv);
        bytes[0x17] = (byte)((p.Moves[0].TimesIncreased << 6) + Math.Min((byte)63, p.Moves[0].Pp));
        bytes[0x18] = (byte)((p.Moves[1].TimesIncreased << 6) + Math.Min((byte)63, p.Moves[1].Pp));
        bytes[0x19] = (byte)((p.Moves[2].TimesIncreased << 6) + Math.Min((byte)63, p.Moves[2].Pp));
        bytes[0x1A] = (byte)((p.Moves[3].TimesIncreased << 6) + Math.Min((byte)63, p.Moves[3].Pp));
        bytes[0x1B] = p.Friendship;
        bytes[0x1C] = (byte)((int)(p.PokerusStrain << 4) + p.PokerusDaysRemaining);
        byte timeOfDay = 2;
        if (p.Origin.MetDateTime.HasValue)
        {
            if (p.Origin.MetDateTime.Value.TimeOfDay.Hours < 4 || p.Origin.MetDateTime.Value.TimeOfDay.Hours > 5) timeOfDay = 3;
            else if (p.Origin.MetDateTime.Value.TimeOfDay.Hours > 3 || p.Origin.MetDateTime.Value.TimeOfDay.Hours < 10) timeOfDay = 1;
        }
        bytes[0x1D] = (byte)((timeOfDay << 6) + p.Level);
        bytes[0x1E] = (byte)(((int)p.OriginalTrainer.Gender << 7) + Lookup.GetGameIndexById(p.Origin.MetLocationId, SupplementObject.Locations, 2));        
        bytes[0x1F] = p.Level;
        return bytes;
    }

    public override int AddPokemonToNextOpenBox(PartyPokemon pokemon)
    {
        int boxCapacity = IsJapanese ? 30 : 20;
        Generation2Box? targetBox = BoxData.FirstOrDefault(x => x.Count < boxCapacity);
            
        if (targetBox == null) return -1;

        int boxId = targetBox.Id;
        int targetSlot = targetBox.Count;

        if (targetSlot > boxCapacity) return -1;
        
        BoxData[boxId].SpeciesIds[targetSlot] = (byte)Lookup.GetGameIndexById(pokemon.PokemonIdentity.FormId, SupplementObject.Pokemon, 2);
        BoxData[boxId].OriginalTrainerNames[targetSlot] = Utility.GetEncodedString(pokemon.OriginalTrainer.Name, 11, Game, Language.Iso639);
        BoxData[boxId].PokemonNames[targetSlot] = Utility.GetEncodedString(pokemon.Nickname, 11, Game, Language.Iso639);
        BoxData[boxId].PokemonBytes[targetSlot] = GetBoxBytesFromPartyPokemon(pokemon);
        BoxData[boxId].Count++;
        return boxId;
    }

    public override int AppendPokemonAndSave(List<PartyPokemon> partyPokemonList, string filepath, bool overwriteBackup=true)
    {
        int i = 0;
        foreach (PartyPokemon partyPokemon in partyPokemonList)
        {
            AddPokemonToNextOpenBox(partyPokemon);
            WriteToPokedex((int)partyPokemon.GetNationalDexNumber());
            WriteRecalculatedChecksums();
            bool isValidWrite = AreAllChecksumsValid();
            
            if (!isValidWrite)
            {
                throw new InvalidDataException("Checksum after Pokemon write was not valid!");
            }
            i++;
        }

        File.Copy(filepath, filepath + ".original", overwriteBackup);
        File.WriteAllBytes(filepath, ModifiedData);
        return i;
    }

    #endregion

    #region Helpers

    public void WriteRecalculatedChecksums()
    {
        int checksum1Offset;
        int checksum2Offset;

        // Gold and Silver
        if (!IsCrystal && !IsJapanese)
        {
            checksum1Offset = 0x2D69;
            checksum2Offset = 0x7E6D;
            ModifiedData[checksum1Offset] = CalculateChecksum(ModifiedData, 0x2009, 0x2D68-0x2009);
            ModifiedData[checksum2Offset] = (byte)(CalculateChecksum(ModifiedData, 0x0C6B, 0x17EC - 0x0C6B) + CalculateChecksum(ModifiedData, 0x3D96, 0x3F3F - 0x3D96) + CalculateChecksum(ModifiedData, 0x7E39, 0x7E6C - 0x7E39));
        }
        else if (!IsCrystal && IsJapanese)
        {
            checksum1Offset = 0x2D0D;
            checksum2Offset = 0x7F0D;
            ModifiedData[checksum1Offset] = CalculateChecksum(ModifiedData, 0x2009, 0x2C8B-0x2009);
            ModifiedData[checksum2Offset] = CalculateChecksum(ModifiedData, 0x7209, 0x7E8B-0x7209);
        }
        // Crystal
        else if (IsCrystal && !IsJapanese)
        {
            checksum1Offset = 0x2D0D;
            checksum2Offset = 0x1F0D;
            ModifiedData[checksum1Offset] = CalculateChecksum(ModifiedData, 0x2009, 0x2B82-0x2009);
            ModifiedData[checksum2Offset] = CalculateChecksum(ModifiedData, 0x1209, 0x1D82-0x1209);
        }
        else if (IsCrystal && IsJapanese)
        {
            checksum1Offset = 0x2D0D;
            checksum2Offset = 0x1F0D;
            ModifiedData[checksum1Offset] = CalculateChecksum(ModifiedData, 0x2009, 0x2AE2-0x2009);
            ModifiedData[checksum2Offset] = CalculateChecksum(ModifiedData, 0x7209, 0x7CE2-0x7209);
        }
    }

    public void ApplyBoxData(int boxId)
    {
        if (boxId < 0) return;
        byte[] newBoxData = BoxData[boxId].GetBoxBytes();
        int currentBoxOffset = IsCrystal || IsJapanese ? 0x2D10 : 0x2D6C;
        int thisOffset = CurrentBoxNumber == boxId ? currentBoxOffset : BoxOffsets[boxId];
        Array.Copy(newBoxData, 0, ModifiedData, thisOffset, newBoxData.Length);
    }

    private Dictionary<int, PartyPokemon> GetPokemonFromStorage(byte[] storageBytes, string lang, int capacity, int pokemonSize)
    {
        Dictionary<int, PartyPokemon> box = [];
        byte boxCount = Utility.GetUnsignedNumber<byte>(storageBytes, 0x00, 1, true);
        int pokemonOffset = 2 + capacity;
        int originalTrainerNameOffset = pokemonOffset + (pokemonSize * capacity);
        int nicknameOffset = originalTrainerNameOffset + (capacity * 0xB);

        for (int i = 0; i < boxCount; i++)
        {
            byte[] nicknameBytes = Utility.GetBytes(storageBytes, nicknameOffset + (0xB * i), 0xB);
            string nickname = Utility.GetDecodedString(nicknameBytes, Game, lang);

            byte[] originalTrainerNameBytes = Utility.GetBytes(storageBytes, originalTrainerNameOffset + (0xB * i), 0xB);
            string originalTrainerName = Utility.GetDecodedString(originalTrainerNameBytes, Game, lang);

            byte[] pokemonBytes = Utility.GetBytes(storageBytes, pokemonOffset + (pokemonSize * i), 32);
            PartyPokemon pokemon = GetPartyPokemonFromBoxBytes(pokemonBytes);
            pokemon.Nickname = nickname;
            pokemon.HasNickname = pokemon.DoesNicknameExist();
            pokemon.OriginalTrainer = new Trainer(originalTrainerName, 0, Utility.GetUnsignedNumber<ushort>(pokemonBytes, 0x06, 2, true), 0);
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
        return (byte)sum;
    }

    #endregion
}