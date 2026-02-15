using System;
using Microsoft.Extensions.Logging;
using PokemonStorage.Models;

namespace PokemonStorage.SaveContent;

public class SaveDataGeneration4 : SaveData
{
    private List<Generation4Block> GeneralBlocks { get; set; }
    private List<Generation4Block> StorageBlocks { get; set; }
    private int GeneralBlockIndex = 0;
    private int StorageBlockIndex = 0;

    public SaveDataGeneration4(byte[] content, Game game, string language) : base(content, game, language)
    {
        GeneralBlocks =
        [
            new(content, game, true),
            new(content, game, true, true),
        ];
        StorageBlocks =
        [
            new(content, game, false),
            new(content, game, false, true),
        ];

        // https://projectpokemon.org/home/docs/gen-4/dp-save-structure-r74/
        // Choose the general block to write to
        GeneralBlockIndex = GeneralBlocks[0].GeneralSaveCount > GeneralBlocks[1].GeneralSaveCount ? 0 : 1;
        if (!GeneralBlocks[GeneralBlockIndex].IsChecksumValid) GeneralBlockIndex = (GeneralBlockIndex + 1) % 2;
        if (!GeneralBlocks[GeneralBlockIndex].IsChecksumValid) throw new Exception("Bad checksum");

        // Choose the storage block to write to
        if (StorageBlocks[0].StorageSaveCount > StorageBlocks[1].StorageSaveCount)
        {
            StorageBlockIndex = 0;
        }
        else if (StorageBlocks[0].StorageSaveCount < StorageBlocks[1].StorageSaveCount)
        {
            StorageBlockIndex = 1;
        }
        else
        {
            StorageBlockIndex = GeneralBlocks[0].StorageSaveCount > GeneralBlocks[1].StorageSaveCount ? 0 : 1;
        }
        
        if (!(StorageBlocks[StorageBlockIndex].GeneralSaveCount == GeneralBlocks[GeneralBlockIndex].GeneralSaveCount && StorageBlocks[StorageBlockIndex].IsChecksumValid))
        {
            GeneralBlockIndex = (GeneralBlockIndex + 1) % 2;
            StorageBlockIndex = (StorageBlockIndex + 1) % 2;
            if (!StorageBlocks[StorageBlockIndex].IsChecksumValid && !GeneralBlocks[GeneralBlockIndex].IsChecksumValid)
            {
                throw new Exception("Gen 4 load error");
            }
        }
        
        foreach (var block in GeneralBlocks)
        {
            Program.Logger.LogInformation($"{block.Checksum} ?== {block.GetCalculatedChecksum()}");
        }
        foreach (var block in StorageBlocks)
        {
            Program.Logger.LogInformation($"{block.Checksum} ?== {block.GetCalculatedChecksum()}");
        }
        ParseOriginalTrainer();
        AreAllChecksumsValid();
        PrintPokedex();
        ParsePartyPokemon();
        ParseBoxPokemon();
    }

    public override bool AreAllChecksumsValid()
    {
        return true;
    }

    protected override void ParseOriginalTrainer()
    {
        int trainerNameOffset = Game.VersionGroupId == 9 ? 0x68 : 0x64;
        int trainerPublicIdOffset = Game.VersionGroupId == 9 ? 0x78 : 0x74;
        int trainerSecretIdOffset = Game.VersionGroupId == 9 ? 0x7A : 0x76;
        int trainerGenderOffset = Game.VersionGroupId == 9 ? 0x80 : 0x7C;

        Trainer = new(
            Utility.GetDecodedString(Utility.GetBytes(GeneralBlocks[GeneralBlockIndex].Data, trainerNameOffset, 16), Game, Language),
            Utility.GetUnsignedNumber<byte>(GeneralBlocks[GeneralBlockIndex].Data, trainerPublicIdOffset, 1),
            Utility.GetUnsignedNumber<ushort>(GeneralBlocks[GeneralBlockIndex].Data, trainerSecretIdOffset, 2),
            Utility.GetUnsignedNumber<ushort>(GeneralBlocks[GeneralBlockIndex].Data, trainerGenderOffset, 2)
        );
    }

    /// <summary>
    /// Fills GameState.Party with the party Pokemon parsed from the save file content.
    /// </summary>
    protected override void ParsePartyPokemon()
    {
        int partySizeOffset = Game.VersionGroupId == 9 ? 0x9C : 0x94;
        int partyOffset = Game.VersionGroupId == 9 ? 0xA0 : 0x98;

        int partySize = Utility.GetUnsignedNumber<byte>(GeneralBlocks[GeneralBlockIndex].Data, partySizeOffset, 1);
        byte[] partyBytes = Utility.GetBytes(GeneralBlocks[GeneralBlockIndex].Data, partyOffset, 1416);

        for (int i = 0; i < partySize; i++)
        {
            byte[] pokemonBytes = Utility.GetBytes(partyBytes, i * 236, 236);
            Party[i] = GetPartyPokemonFromBoxBytes(pokemonBytes);
        }
    }

    /// <summary>
    /// Fills GameState.BoxList with the box Pokemon parsed from the save file content.
    /// </summary>
    protected override void ParseBoxPokemon()
    {
        int boxSize = Game.VersionGroupId == 10 ? 0x1000 : 0xFF0;
        for (int i = 0; i < 18; i++)
        {
            int pokemonOffset = (Game.VersionGroupId == 10) ? 0x00 : 0x04;
            int boxNameOffset = (Game.VersionGroupId == 10) ? 0x12008 : 0x11EE4;
            
            byte[] boxNameBytes = Utility.GetBytes(StorageBlocks[StorageBlockIndex].Data, boxNameOffset + (i * 40), 40);
            string boxName = Utility.GetDecodedString(boxNameBytes, Game, Language);
            if (!BoxList.ContainsKey(boxName)) BoxList.Add(boxName, []);
            byte[] thisBoxBytes = Utility.GetBytes(StorageBlocks[StorageBlockIndex].Data, pokemonOffset + (boxSize * i), 136 * 30);

            for (int j = 0; j < 30; j++)
            {
                uint thisPv = Utility.GetUnsignedNumber<uint>(thisBoxBytes, (j * 136) + 0, 4);
                ushort thisCs = Utility.GetUnsignedNumber<ushort>(thisBoxBytes, (j * 136) + 6, 2);
                if (thisPv == 0 && thisCs == 0) continue;
                
                byte[] pokemonBytes = Utility.GetBytes(thisBoxBytes, j * 136, 136);
                BoxList[boxName][j] = GetPartyPokemonFromBoxBytes(pokemonBytes);
            }
        }
    }

    // https://bulbapedia.bulbagarden.net/wiki/Pok%C3%A9mon_data_structure_(Generation_IV)
    public override PartyPokemon GetPartyPokemonFromBoxBytes(byte[] data)
    {
        PartyPokemon p = new(Game);

        // decryption
        const int WORD_SIZE = 2;
        const uint DECRYPTION_FACTOR = 0x41C64E6D;
        const uint DECRYPTION_CONST = 0x6073;

        uint checksum = Utility.GetUnsignedNumber<uint>(data, 0x06, 2);
        byte[] encrypted = Utility.GetBytes(data, 0x08, 128);
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

        p.PersonalityValue = Utility.GetUnsignedNumber<uint>(data, 0x00, 4);
        string shuffledOrder = GetSubstructureOrder(p.PersonalityValue, true);

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
                    p.PokemonIdentity = Lookup.GetPokemonByFormId(Lookup.GetPokemonFormIdByGameIndex(4, Utility.GetUnsignedNumber<ushort>(blockBytes, 0x00, 2)), Lookup.GetLanguageIdByIdentifier(Language)); 
                    p.HeldItemId = Lookup.GetItemIdByGameIndex(4, Utility.GetUnsignedNumber<ushort>(blockBytes, 0x02, 2));
                    p.OriginalTrainer.PublicId = Utility.GetUnsignedNumber<ushort>(blockBytes, 0x04, 2);
                    p.OriginalTrainer.SecretId = Utility.GetUnsignedNumber<ushort>(blockBytes, 0x06, 2);
                    p.ExperiencePoints = Utility.GetUnsignedNumber<uint>(blockBytes, 0x08, 4);
                    p.Friendship = Utility.GetUnsignedNumber<byte>(blockBytes, 0x0C, 1);
                    p.AbilityId = Utility.GetUnsignedNumber<byte>(blockBytes, 0x0D, 1);
                    p.Markings = new(4, Utility.GetUnsignedNumber<byte>(blockBytes, 0x0E, 1));
                    p.LanguageId = Lookup.GetLanguageIdByGameIndex(Utility.GetUnsignedNumber<byte>(blockBytes, 0x0F, 1));
                    ev.HP = Utility.GetUnsignedNumber<byte>(blockBytes, 0x10, 1);
                    ev.Attack = Utility.GetUnsignedNumber<byte>(blockBytes, 0x11, 1);
                    ev.Defense = Utility.GetUnsignedNumber<byte>(blockBytes, 0x12, 1);
                    ev.Speed = Utility.GetUnsignedNumber<byte>(blockBytes, 0x13, 1);
                    ev.SpecialAttack = Utility.GetUnsignedNumber<byte>(blockBytes, 0x14, 1);
                    ev.SpecialDefense = Utility.GetUnsignedNumber<byte>(blockBytes, 0x15, 1);
                    p.Coolness = Utility.GetUnsignedNumber<byte>(blockBytes, 0x16, 1);
                    p.Beauty = Utility.GetUnsignedNumber<byte>(blockBytes, 0x17, 1);
                    p.Cuteness = Utility.GetUnsignedNumber<byte>(blockBytes, 0x18, 1);
                    p.Smartness = Utility.GetUnsignedNumber<byte>(blockBytes, 0x19, 1);
                    p.Toughness = Utility.GetUnsignedNumber<byte>(blockBytes, 0x1A, 1);
                    p.Sheen = Utility.GetUnsignedNumber<byte>(blockBytes, 0x1B, 1);
                    p.Ribbons.HoennSet = Utility.GetUnsignedNumber<uint>(blockBytes, 0x1C, 4);
                    break;

                case 'B':
                    // Moves
                    p.Moves[0].Id = Utility.GetUnsignedNumber<ushort>(blockBytes, 0x0, 2);
                    p.Moves[1].Id = Utility.GetUnsignedNumber<ushort>(blockBytes, 0x2, 2);
                    p.Moves[2].Id = Utility.GetUnsignedNumber<ushort>(blockBytes, 0x4, 2);
                    p.Moves[3].Id = Utility.GetUnsignedNumber<ushort>(blockBytes, 0x6, 2);
                    p.Moves[0].Pp = Utility.GetUnsignedNumber<byte>(blockBytes, 0x8, 1);
                    p.Moves[1].Pp = Utility.GetUnsignedNumber<byte>(blockBytes, 0x9, 1);
                    p.Moves[2].Pp = Utility.GetUnsignedNumber<byte>(blockBytes, 0xA, 1);
                    p.Moves[3].Pp = Utility.GetUnsignedNumber<byte>(blockBytes, 0xB, 1);
                    p.Moves[0].TimesIncreased = Utility.GetUnsignedNumber<byte>(blockBytes, 0xC, 1);
                    p.Moves[1].TimesIncreased = Utility.GetUnsignedNumber<byte>(blockBytes, 0xD, 1);
                    p.Moves[2].TimesIncreased = Utility.GetUnsignedNumber<byte>(blockBytes, 0xE, 1);
                    p.Moves[3].TimesIncreased = Utility.GetUnsignedNumber<byte>(blockBytes, 0xF, 1);
                    p.Moves[0].SlotId = 0;
                    p.Moves[1].SlotId = 1;
                    p.Moves[2].SlotId = 2;
                    p.Moves[3].SlotId = 3;

                    // IVs and more
                    uint ivData = Utility.GetUnsignedNumber<uint>(blockBytes, 0x10, 4);
                    string ivBinary = Utility.ReverseString(Convert.ToString(ivData, 2).PadLeft(32, '0'));
                    iv.HP = Convert.ToByte(Utility.ReverseString(ivBinary.Substring(0, 5)), 2);
                    iv.Attack = Convert.ToByte(Utility.ReverseString(ivBinary.Substring(5, 5)), 2);
                    iv.Defense = Convert.ToByte(Utility.ReverseString(ivBinary.Substring(10, 5)), 2);
                    iv.Speed = Convert.ToByte(Utility.ReverseString(ivBinary.Substring(15, 5)), 2);
                    iv.SpecialAttack = Convert.ToByte(Utility.ReverseString(ivBinary.Substring(20, 5)), 2);
                    iv.SpecialDefense = Convert.ToByte(Utility.ReverseString(ivBinary.Substring(25, 5)), 2);
                    p.IsEgg = ivBinary[30] == '1';
                    p.HasNickname = ivBinary[31] == '1';

                    // Heonn Ribbons
                    p.Ribbons.HoennSet = Utility.GetUnsignedNumber<uint>(blockBytes, 0x14, 4);

                    // Flags
                    int flagsData = Utility.GetUnsignedNumber<byte>(blockBytes, 0x18, 1);
                    string flagsBinary = Utility.ReverseString(Convert.ToString(flagsData, 2).PadLeft(8, '0'));

                    p.Origin.FatefulEncounter = flagsBinary[0] == '1';
                    if (flagsBinary[2] == '1') p.Gender = Gender.GENDERLESS;
                    else
                    {
                        p.Gender = flagsBinary[1] == '1' ? Gender.FEMALE : Gender.MALE;
                    }
                    
                    p.AlternateFormId = (ushort)Convert.ToInt16(Utility.ReverseString(flagsBinary.Substring(3, 5)), 2);
                    p.ShinyLeaves = Utility.GetUnsignedNumber<byte>(blockBytes, 0x19, 1);

                    p.Origin.EggHatchLocationPlatinumId = Lookup.GetLocationIdByGameIndex(4, Utility.GetUnsignedNumber<ushort>(blockBytes, 0x1C, 2));
                    p.Origin.MetLocationPlatinumId = Lookup.GetLocationIdByGameIndex(4, Utility.GetUnsignedNumber<ushort>(blockBytes, 0x1E, 2));
                    break;

                case 'C':
                    byte[] nicknameBytes = Utility.GetBytes(blockBytes, 0x0, 20);
                    p.Nickname = Utility.GetDecodedString(nicknameBytes, Game, Language);
                    p.Origin.GameVersionId = Lookup.GetVersionIdByGameIndex(Utility.GetUnsignedNumber<byte>(blockBytes, 0x17, 1));
                    p.Ribbons.SinnohSet2 = Utility.GetUnsignedNumber<uint>(blockBytes, 0x18, 4);
                    break;

                case 'D':
                    byte[] otNameBytes = Utility.GetBytes(blockBytes, 0x0, 16);
                    p.OriginalTrainer.Name = Utility.GetDecodedString(otNameBytes, Game, Language);
                    
                    byte eggYear = Utility.GetUnsignedNumber<byte>(blockBytes, 0x10, 1);
                    byte eggMonth = Utility.GetUnsignedNumber<byte>(blockBytes, 0x11, 1);
                    byte eggDay = Utility.GetUnsignedNumber<byte>(blockBytes, 0x12, 1);
                    if (eggDay > 0)
                    {
                        p.Origin.EggReceiveDate = new DateTime(eggYear + 2000, eggMonth, eggDay);
                    }

                    byte metYear = Utility.GetUnsignedNumber<byte>(blockBytes, 0x13, 1);
                    byte metMonth = Utility.GetUnsignedNumber<byte>(blockBytes, 0x14, 1);
                    byte metDay = Utility.GetUnsignedNumber<byte>(blockBytes, 0x15, 1);
                    if (metDay > 0)
                    {
                        p.Origin.MetDateTime = new DateTime(metYear + 2000, metMonth, metDay);
                    }
                    
                    p.Origin.EggHatchLocationId = Lookup.GetLocationIdByGameIndex(4, Utility.GetUnsignedNumber<ushort>(blockBytes, 0x16, 2));
                    p.Origin.MetLocationId = Lookup.GetLocationIdByGameIndex(4, Utility.GetUnsignedNumber<ushort>(blockBytes, 0x18, 2));

                    byte pokerusData = Utility.GetUnsignedNumber<byte>(blockBytes, 0x1A, 1);
                    string pokerusBinary = Convert.ToString(pokerusData, 2).PadLeft(8, '0');
                    p.PokerusStrain = Convert.ToByte(pokerusBinary.Substring(0, 4), 2);
                    p.PokerusDaysRemaining = Convert.ToByte(pokerusBinary.Substring(4, 4), 2);

                    p.Origin.PokeballId = Utility.GetUnsignedNumber<byte>(blockBytes, 0x1B, 1);

                    int originData = Utility.GetUnsignedNumber<byte>(blockBytes, 0x1C, 1);
                    string originBinary = Utility.ReverseString(Convert.ToString(originData, 2).PadLeft(8, '0'));
                    p.Origin.MetLevel = Convert.ToByte(Utility.ReverseString(originBinary.Substring(0, 6)), 2);
                    p.OriginalTrainer.Gender = originBinary[7] == '1' ? Gender.FEMALE : Gender.MALE;
                    p.Origin.EncounterTypeId = Utility.GetUnsignedNumber<byte>(blockBytes, 0x1D, 1);

                    if (Game.VersionGroupId == 10)
                    {
                        if (p.Origin.PokeballId == 0)
                        {
                            p.Origin.PokeballId = Utility.GetUnsignedNumber<byte>(blockBytes, 0x1E, 1);
                        }
                        p.WalkingMood = Utility.GetUnsignedNumber<byte>(blockBytes, 0x1F, 1);
                    }
                    break;
                default:
                    break;
            }
        }

        // Calculations        
        // Program.Logger.LogInformation($"Done reading: {PokemonIdentity.SpeciesIdentifier}");
        p.Stats = new(true, iv, ev, p.PokemonIdentity.SpeciesId, p.Level, p.Nature);
        return p;
    }

    public override void PrintPokedex()
    {
        int offset = 0x0;
        switch (Game.VersionGroupId)
        {
            case 8:
                offset = 0x12DC;
                break;
            case 9:
                offset = 0x1328;
                break;
            case 10:
                offset = 0x12B8;
                break;
        }
        int capturedOffset = offset + 4;
        byte[] ownedBytes = Utility.GetBytes(GeneralBlocks[GeneralBlockIndex].Data, capturedOffset, 0x3F);
        byte[] seenBytes = Utility.GetBytes(GeneralBlocks[GeneralBlockIndex].Data, capturedOffset+0x40, 0x3F);
        byte[] femaleBytes = Utility.GetBytes(GeneralBlocks[GeneralBlockIndex].Data, capturedOffset+0x80, 0x3F);
        byte[] maleBytes = Utility.GetBytes(GeneralBlocks[GeneralBlockIndex].Data, capturedOffset+0xC0, 0x3F);
        for (int i = 0; i < 493; i++)
        {
            PokemonIdentity pokemon = Lookup.GetPokemonBySpeciesId(i+1, Lookup.GetLanguageIdByIdentifier(Language));
            int ownedBit = ownedBytes[i >> 3] >> (i & 7) & 1;
            int seenBit = seenBytes[i >> 3] >> (i & 7) & 1;
            int femaleBit = femaleBytes[i >> 3] >> (i & 7) & 1;
            int maleBit = maleBytes[i >> 3] >> (i & 7) & 1;
            Program.Logger.LogInformation($"{seenBit}/{ownedBit}({femaleBit}|{maleBit}) - {pokemon.SpeciesName}");
        }
        return;
    }

    public override void WriteToPokedex(int nationalIndex, bool seen = true, bool owned = true)
    {
        int offset = 0x0;
        switch (Game.VersionGroupId)
        {
            case 8:
                offset = 0x12DC;
                break;
            case 9:
                offset = 0x1328;
                break;
            case 10:
                offset = 0x12B8;
                break;
        }
        int capturedOffset = offset + 4;
        byte[] ownedBytes = Utility.GetBytes(GeneralBlocks[GeneralBlockIndex].Data, capturedOffset, 0x3F);
        byte[] seenBytes = Utility.GetBytes(GeneralBlocks[GeneralBlockIndex].Data, capturedOffset+0x40, 0x3F);

        int byteIndex = (nationalIndex-1) >> 3;
        int bitIndex = (nationalIndex-1) % 8;
        if (seen)
        {
            seenBytes[byteIndex] = (byte)((int)Math.Pow(2, bitIndex) | seenBytes[byteIndex]);
            Buffer.BlockCopy(seenBytes, 0, GeneralBlocks[GeneralBlockIndex].Data, capturedOffset+0x40, 0x3F);
        }
        if (owned)
        {
            ownedBytes[byteIndex] = (byte)((int)Math.Pow(2, bitIndex) | ownedBytes[byteIndex]);
            Buffer.BlockCopy(ownedBytes, 0, GeneralBlocks[GeneralBlockIndex].Data, capturedOffset, 0x3F);
        }

        return;
    }

    public override byte[] GetBoxBytesFromPartyPokemon(PartyPokemon p)
    {
        byte[] bytes = new byte[0x88];
        Array.Fill<byte>(bytes, 0);

        byte[] pvData = [.. BitConverter.GetBytes(p.PersonalityValue)];
        Buffer.BlockCopy(pvData, 0, bytes, 0x00, 4);

        // A block
        byte[] a = new byte[0x20];

        byte[] speciesData = BitConverter.GetBytes(Lookup.GetPokemonGameIndexByFormId(4, p.PokemonIdentity.FormId));
        Buffer.BlockCopy(speciesData, 0, a, 0x00, 2);
 
        byte[] itemData = BitConverter.GetBytes(Lookup.GetItemGameIndexById(4, p.HeldItemId));
        Buffer.BlockCopy(itemData, 0, a, 0x02, 2);

        byte[] otIdPublicData = [.. BitConverter.GetBytes(p.OriginalTrainer.PublicId)];
        Buffer.BlockCopy(otIdPublicData, 0, a, 0x04, 2);

        byte[] otIdSecretData = [.. BitConverter.GetBytes(p.OriginalTrainer.SecretId)];
        Buffer.BlockCopy(otIdSecretData, 0, a, 0x06, 2);

        byte[] experienceData = BitConverter.GetBytes(p.ExperiencePoints);
        Buffer.BlockCopy(experienceData, 0, a, 0x08, 4);

        a[0x0A] = p.Friendship;
        a[0x0B] = (byte)p.AbilityId;
        a[0x0C] = p.Markings.AsGen4Byte();
        a[0x0D] = Lookup.GetLanguageGameIndexById(p.LanguageId);
        a[0x0E] = (byte)p.Stats.Modern.HP.Ev;
        a[0x0F] = (byte)p.Stats.Modern.Attack.Ev;
        a[0x10] = (byte)p.Stats.Modern.Defense.Ev;
        a[0x11] = (byte)p.Stats.Modern.Speed.Ev;
        a[0x12] = (byte)p.Stats.Modern.SpecialAttack.Ev;
        a[0x13] = (byte)p.Stats.Modern.SpecialDefense.Ev;
        a[0x14] = (byte)p.Coolness;
        a[0x15] = (byte)p.Beauty;
        a[0x16] = (byte)p.Cuteness;
        a[0x17] = (byte)p.Smartness;
        a[0x18] = (byte)p.Toughness;
        a[0x19] = (byte)p.Sheen;

        byte[] sinnohRibbon1Data = BitConverter.GetBytes(p.Ribbons.SinnohSet1);
        Buffer.BlockCopy(sinnohRibbon1Data, 0, a, 0x08, 4);

        // B block
        byte[] b = new byte[0x20];

        byte[] m1 = [.. BitConverter.GetBytes(p.Moves[0].Id)];
        Buffer.BlockCopy(m1, 0, b, 0x00, 2);
        byte[] m2 = [.. BitConverter.GetBytes(p.Moves[1].Id)];
        Buffer.BlockCopy(m2, 0, b, 0x02, 2);
        byte[] m3 = [.. BitConverter.GetBytes(p.Moves[2].Id)];
        Buffer.BlockCopy(m3, 0, b, 0x04, 2);
        byte[] m4 = [.. BitConverter.GetBytes(p.Moves[3].Id)];
        Buffer.BlockCopy(m4, 0, b, 0x06, 2);
        
        b[0x08] = p.Moves[0].Pp;
        b[0x09] = p.Moves[1].Pp;
        b[0x0A] = p.Moves[2].Pp;
        b[0x0B] = p.Moves[3].Pp;

        b[0x0C] = p.Moves[0].TimesIncreased;
        b[0x0D] = p.Moves[1].TimesIncreased;
        b[0x0E] = p.Moves[2].TimesIncreased;
        b[0x0F] = p.Moves[3].TimesIncreased;

        uint iv = (uint)(
            p.Stats.Modern.HP.Iv + 
            (p.Stats.Modern.Attack.Iv << 5) + 
            (p.Stats.Modern.Defense.Iv << 10) + 
            (p.Stats.Modern.Speed.Iv << 15) + 
            (p.Stats.Modern.SpecialAttack.Iv << 20) +
            (p.Stats.Modern.SpecialDefense.Iv << 25) +
            ((p.IsEgg ? 1 : 0) << 30) + 
            ((p.HasNickname ? 1 :0) << 31)
        );

        byte[] ivEggAbilityData = [.. BitConverter.GetBytes(iv)];
        Buffer.BlockCopy(ivEggAbilityData, 0, b, 0x10, 4);

        byte[] heonnRibbonData = BitConverter.GetBytes(p.Ribbons.HoennSet);
        Buffer.BlockCopy(heonnRibbonData, 0, b, 0x14, 4);

        b[0x18] = (byte)(
            (p.Origin.FatefulEncounter ? 1 : 0) + 
            (p.Gender == Gender.FEMALE ? 2 : 0) + 
            (p.Gender == Gender.GENDERLESS ? 4 : 0) +
            (p.AlternateFormId << 3)
        );
        
        b[0x19] = p.ShinyLeaves;
        
        byte[] platEggLocationData = BitConverter.GetBytes(Lookup.GetLocationGameIndexById(4, p.Origin.EggHatchLocationPlatinumId));
        Buffer.BlockCopy(platEggLocationData, 0, b, 0x1C, 2);

        byte[] platLocation = BitConverter.GetBytes(Lookup.GetLocationGameIndexById(4, p.Origin.MetLocationPlatinumId));
        Buffer.BlockCopy(platEggLocationData, 0, b, 0x1E, 2);

        // C block
        byte[] c = new byte[0x20];

        byte[] nicknameData = Utility.GetEncodedString(p.Nickname, 10, Game, Language);
        Buffer.BlockCopy(nicknameData, 0, c, 0x00, 21);

        c[0x15] = Lookup.GetGameGameIndexById(p.Origin.GameVersionId);

        byte[] sinnohRibbon2Data = BitConverter.GetBytes(p.Ribbons.SinnohSet2);
        Buffer.BlockCopy(sinnohRibbon2Data, 0, c, 0x16, 4);

        // D block
        byte[] d = new byte[0x20];

        byte[] otNameData = Utility.GetEncodedString(p.OriginalTrainer.Name, 7, Game, Language);
        Buffer.BlockCopy(otNameData, 0, d, 0x00, 15);

        d[0x10] = (byte)(p.Origin.EggReceiveDate.HasValue ? p.Origin.EggReceiveDate.Value.Year - 2000 : 0);
        d[0x11] = (byte)(p.Origin.EggReceiveDate.HasValue ? p.Origin.EggReceiveDate.Value.Month : 0);
        d[0x12] = (byte)(p.Origin.EggReceiveDate.HasValue ? p.Origin.EggReceiveDate.Value.Day : 0);

        d[0x13] = (byte)(p.Origin.MetDateTime.HasValue ? p.Origin.MetDateTime.Value.Year - 2000 : 0);
        d[0x14] = (byte)(p.Origin.MetDateTime.HasValue ? p.Origin.MetDateTime.Value.Month : 0);
        d[0x15] = (byte)(p.Origin.MetDateTime.HasValue ? p.Origin.MetDateTime.Value.Day : 0);

        byte[] dpEggLocationData = [.. BitConverter.GetBytes(Lookup.GetLocationGameIndexById(4, p.Origin.EggHatchLocationId))];
        Buffer.BlockCopy(dpEggLocationData, 0, d, 0x16, 2);

        byte[] dpMetLocationData = [.. BitConverter.GetBytes(Lookup.GetLocationGameIndexById(4, p.Origin.MetLocationId))];
        Buffer.BlockCopy(dpMetLocationData, 0, d, 0x18, 2);

        d[0x1A] = (byte)(p.PokerusDaysRemaining + (p.PokerusStrain << 4));
        d[0x1B] = Lookup.GetBallGameIndexByItemId(p.Origin.PokeballId);
        d[0x1C] = (byte)(p.Origin.MetLevel + (p.OriginalTrainer.Gender == Gender.FEMALE ? 0x80 : 0));
        d[0x1D] = p.Origin.EncounterTypeId;
        d[0x1E] = Lookup.GetBallGameIndexByItemId(p.Origin.PokeballId);
        d[0x1F] = p.WalkingMood;

        // Arrange substructure
        byte[] substructure = new byte[48];
        Array.Fill<byte>(substructure, 0x00);

        string order = GetSubstructureOrder(p.PersonalityValue);
        for (int i = 0; i < order.Length; i++)
        {
            if (string.Equals(order[i].ToString(), "g", StringComparison.OrdinalIgnoreCase))
            {
                Buffer.BlockCopy(g, 0, substructure, i*12, 12);
            }
            if (string.Equals(order[i].ToString(), "a", StringComparison.OrdinalIgnoreCase))
            {
                Buffer.BlockCopy(a, 0, substructure, i*12, 12);
            }
            if (string.Equals(order[i].ToString(), "m", StringComparison.OrdinalIgnoreCase))
            {
                Buffer.BlockCopy(m, 0, substructure, i*12, 12);
            }
            if (string.Equals(order[i].ToString(), "e", StringComparison.OrdinalIgnoreCase))
            {
                Buffer.BlockCopy(e, 0, substructure, i*12, 12);
            }
        }
        ushort checksum = 0;
        for (int i = 0; i < 48; i += 2)
        {
            checksum += Utility.GetUnsignedNumber<ushort>(substructure, 0x1 * i, 2);
        }
        byte[] checksumData = [.. BitConverter.GetBytes(checksum)];
        Buffer.BlockCopy(checksumData, 0, bytes, 0x1C, 2);

        // Encrypt the substructure
        byte[] encrypted = XorSubstructure(Utility.GetUnsignedNumber<uint>(bytes, 0x00, 4) ^ Utility.GetUnsignedNumber<uint>(bytes, 0x04, 4), substructure);
        Buffer.BlockCopy(encrypted, 0, bytes, 0x20, 48);

        return bytes;
    }

    public override int AddPokemonToNextOpenBox(PartyPokemon pokemon)
    {
        return -1;
    }

    private string GetSubstructureOrder(uint pv, bool unencrypt)
    {
        // block order maps
        var blockOrder = new Dictionary<int, string>
        {
            {0,"ABCD"}, {1,"ABDC"}, {2,"ACBD"}, {3,"ACDB"},
            {4,"ADBC"}, {5,"ADCB"}, {6,"BACD"}, {7,"BADC"},
            {8,"BCAD"}, {9,"BCDA"}, {10,"BDAC"}, {11,"BDCA"},
            {12,"CABD"}, {13,"CADB"}, {14,"CBAD"}, {15,"CBDA"},
            {16,"CDAB"}, {17,"CDBA"}, {18,"DABC"}, {19,"DACB"},
            {20,"DBAC"}, {21,"DBCA"}, {22,"DCAB"}, {23,"DCBA"}
        };

        var inverseOrder = new Dictionary<int, string>
        {
            {0,"ABCD"}, {1,"ABDC"}, {2,"ACBD"}, {3,"ADBC"},
            {4,"ACDB"}, {5,"ADCB"}, {6,"BACD"}, {7,"BADC"},
            {8,"CABD"}, {9,"DABC"}, {10,"CADB"}, {11,"DACB"},
            {12,"BCAD"}, {13,"BDAC"}, {14,"CBAD"}, {15,"DBAC"},
            {16,"CDAB"}, {17,"DCAB"}, {18,"BCDA"}, {19,"BDCA"},
            {20,"CBDA"}, {21,"DBCA"}, {22,"CDBA"}, {23,"DCBA"}
        };

        int s = (int)(((pv & 0x3E000) >> 0xD) % 24);
        return unencrypt ? blockOrder[s] : inverseOrder[s];
    }
}