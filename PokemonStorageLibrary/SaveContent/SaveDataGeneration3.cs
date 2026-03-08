
using PokemonStorageLibrary.Models;

namespace PokemonStorageLibrary.SaveContent;

public class SaveDataGeneration3 : SaveData
{
    private int[] SaveOffsets = [0x000000, 0x00E000];
    private int SaveIndex { get; set; }
    private List<Generation3Section> Sections = [];

    public SaveDataGeneration3(byte[] content, Game game, string language) : base(content, game, language)
    {
        uint s0 = Utility.GetUnsignedNumber<uint>(content, SaveOffsets[0] + 0x0FFC, 4);
        uint s1 = Utility.GetUnsignedNumber<uint>(content, SaveOffsets[1] + 0x0FFC, 4);
        SaveIndex = s0 > s1 ? 0 : 1;
        Console.WriteLine($"Using SaveIndex {SaveIndex}");

        for (int i = 0; i < 14; i++)
        {
            Sections.Add(new Generation3Section(Utility.GetBytes(content, SaveOffsets[SaveIndex] + (0x1000 * i), 0x1000)));
            Console.WriteLine($"Loaded section {Sections[i].SaveIndex}:{Sections[i].SectionId}");
        }
        ParseOriginalTrainer();
        AreAllChecksumsValid();
        // PrintPokedex();
        ParsePartyPokemon();
        ParseBoxPokemon();
    }

    public override bool AreAllChecksumsValid()
    {
        foreach (Generation3Section section in Sections)
        {
            Checksum checksum = new()
            {
                Real = section.Checksum,
                Calculated = section.GetCalculatedChecksum()
            };
            if (!checksum.IsShortSizeValid()) return false;
        }
        return true;
    }

    protected override void ParseOriginalTrainer()
    {
        Generation3Section trainerSection = Sections.First(x => x.SectionId == 0);

        Trainer = new(
            Utility.GetDecodedString(Utility.GetBytes(trainerSection.Data, 0, 7), Game, Language),
            Utility.GetUnsignedNumber<byte>(trainerSection.Data, 0x0008, 1),
            Utility.GetUnsignedNumber<ushort>(trainerSection.Data, 0x000A, 2),
            Utility.GetUnsignedNumber<ushort>(trainerSection.Data, 0x000C, 2)
        );
    }

    /// <summary>
    /// Fills GameState.Party with the party Pokemon parsed from the save file content.
    /// </summary>
    protected override void ParsePartyPokemon()
    {
        Generation3Section teamSection = Sections.First(x => x.SectionId == 1);
        bool isRSE = Game.VersionGroupId is 5 or 6;

        byte partySize = isRSE
            ? (byte)Utility.GetUnsignedNumber<uint>(teamSection.Data, 0x0234, 4)
            : Utility.GetUnsignedNumber<byte>(teamSection.Data, 0x0034, 1);

        int partyOffset = isRSE ? 0x0238 : 0x0038;
        byte[] partyBytes = Utility.GetBytes(teamSection.Data, partyOffset, 600);

        for (int i = 0; i < partySize; i++)
        {
            byte[] pokemonBytes = Utility.GetBytes(partyBytes, i * 100, 100);
            PartyPokemon pokemon = GetPartyPokemonFromBoxBytes(pokemonBytes);
            Party[i] = pokemon;
        }
    }

    /// <summary>
    /// Fills GameState.BoxList with the box Pokemon parsed from the save file content.
    /// </summary>
    protected override void ParseBoxPokemon()
    {
        byte[] boxBytes = GetBoxDataBytes();

        // For each of the 14 boxes
        for (int i = 0; i < 14; i++)
        {
            byte[] thisBoxBytes = Utility.GetBytes(boxBytes, 0x4 + (i * 2400), 2400);
            string thisBoxName = Utility.GetDecodedString(Utility.GetBytes(boxBytes, 0x8344 + (i * 9), 9), Game, Language);
            if (!BoxList.ContainsKey(thisBoxName)) BoxList.Add(thisBoxName, []);

            // Read all 30 slots of this box
            for (int j = 0; j < 30; j++)
            {
                byte[] pokemonBytes = Utility.GetBytes(thisBoxBytes, j * 80, 80);
                uint thisPv = Utility.GetUnsignedNumber<uint>(pokemonBytes, 0x0, 4);
                ushort thisCs = Utility.GetUnsignedNumber<ushort>(pokemonBytes, 0x1C, 2);
                if (thisPv == 0 && thisCs == 0) continue;
                
                PartyPokemon pokemon = GetPartyPokemonFromBoxBytes(pokemonBytes);
                BoxList[thisBoxName][j] = pokemon;
            }
        }
    }

    // https://bulbapedia.bulbagarden.net/wiki/Pok%C3%A9mon_data_structure_(Generation_III)
    public override PartyPokemon GetPartyPokemonFromBoxBytes(byte[] data)
    {
        PartyPokemon p = new(Game);
        p.Origin = new Origin(Game.GameId);
        p.PersonalityValue = Utility.GetUnsignedNumber<uint>(data, 0x00, 4);
        uint otId = Utility.GetUnsignedNumber<uint>(data, 0x04, 4);
        p.OriginalTrainer = new Trainer(
            Utility.GetDecodedString(Utility.GetBytes(data, 0x14, 7), Game, Language),
            0,
            Utility.GetUnsignedNumber<ushort>(data, 0x04, 2),
            Utility.GetUnsignedNumber<ushort>(data, 0x06, 2)
        );

        p.Nickname = Utility.GetDecodedString(Utility.GetBytes(data, 0x08, 10), Game, Language);
        p.LanguageId = Lookup.GetLanguageIdByIdentifier(Language);
        p.Gen3Misc = Utility.GetUnsignedNumber<byte>(data, 0x13, 1);
        p.Markings = new Markings(3, Utility.GetUnsignedNumber<byte>(data, 0x1B, 1));

        // Data Section Decryption
        ushort checksum = Utility.GetUnsignedNumber<ushort>(data, 0x1C, 2);
        string orderString = GetSubstructureOrder(p.PersonalityValue);
        byte[] decryptedData = XorSubstructure(p.PersonalityValue ^ otId, Utility.GetBytes(data, 0x20, 48));

        uint calculated = 0;
        for (int i = 0; i < 48; i += 2)
        {
            calculated += Utility.GetUnsignedNumber<ushort>(decryptedData, 0x1 * i, 2);
        }

        Console.WriteLine($"{checksum & 0xffff} ?== {calculated & 0xffff}");
        Console.WriteLine($"CHSM:{Convert.ToString(checksum, 2).PadLeft(17, '0')}");
        Console.WriteLine($"CALC:{Convert.ToString(calculated, 2).PadLeft(17, '0')}");
        bool checksumResult = (checksum & 0xffff) == (calculated & 0xffff);
        if (!checksumResult)
        {
            throw new Exception($"Bad checksum result. Expected {checksum & 0xffff} and got {calculated & 0xffff}");
        }

        StatHextuple ev = new StatHextuple();
        StatHextuple iv = new StatHextuple();
        foreach ((char c, int i) in orderString.Select((c, i) => (c, i)))
        {
            int offset = i * 12;
            byte[] substructureBytes = Utility.GetBytes(decryptedData, offset, 12);
            // Console.WriteLine($"{c} ==> {BitConverter.ToString(substructureBytes)}");
            switch (c)
            {
                case 'G':
                    p.PokemonIdentity = Lookup.GetPokemonByFormId(Lookup.GetPokemonFormIdByGameIndex(3, Utility.GetUnsignedNumber<ushort>(substructureBytes, 0x00, 2)), p.LanguageId); 
                    p.HeldItemId = Lookup.GetItemIdByGameIndex(3, Utility.GetUnsignedNumber<ushort>(substructureBytes, 0x02, 2));
                    p.ExperiencePoints = Utility.GetUnsignedNumber<uint>(substructureBytes, 0x04, 4);

                    byte ppBonuses = Utility.GetUnsignedNumber<byte>(substructureBytes, 0x08, 1);
                    string ppBinary = Convert.ToString(ppBonuses, 2).PadLeft(8, '0');
                    for (int j = 0; j < 4; j++)
                    {
                        byte bonuses = Convert.ToByte(ppBinary.Substring(j * 2, 2), 2);
                        p.Moves[j].TimesIncreased = bonuses;
                    }
                    p.Friendship = Utility.GetUnsignedNumber<byte>(substructureBytes, 0x09, 1);
                    break;

                case 'A':
                    for (int j = 0; j < 4; j++)
                    {
                        p.Moves[j].Id = Utility.GetUnsignedNumber<ushort>(substructureBytes, j * 2, 2);
                        p.Moves[j].SlotId = (byte)j;
                    }

                    for (int j = 0; j < 4; j++)
                    {
                        p.Moves[j].Pp = Utility.GetUnsignedNumber<byte>(substructureBytes, 0x08 + j, 1);
                    }
                    break;

                case 'E':
                    ev.HP = Utility.GetUnsignedNumber<byte>(substructureBytes, 0x00, 1);
                    ev.Attack = Utility.GetUnsignedNumber<byte>(substructureBytes, 0x01, 1);
                    ev.Defense = Utility.GetUnsignedNumber<byte>(substructureBytes, 0x02, 1);
                    ev.Speed = Utility.GetUnsignedNumber<byte>(substructureBytes, 0x03, 1);
                    ev.SpecialAttack = Utility.GetUnsignedNumber<byte>(substructureBytes, 0x04, 1);
                    ev.SpecialDefense = Utility.GetUnsignedNumber<byte>(substructureBytes, 0x05, 1);

                    p.Coolness = Utility.GetUnsignedNumber<byte>(substructureBytes, 0x06, 1);
                    p.Beauty = Utility.GetUnsignedNumber<byte>(substructureBytes, 0x07, 1);
                    p.Cuteness = Utility.GetUnsignedNumber<byte>(substructureBytes, 0x08, 1);
                    p.Smartness = Utility.GetUnsignedNumber<byte>(substructureBytes, 0x09, 1);
                    p.Toughness = Utility.GetUnsignedNumber<byte>(substructureBytes, 0x0A, 1);
                    p.Sheen = Utility.GetUnsignedNumber<byte>(substructureBytes, 0x0B, 1);
                    break;

                case 'M':
                    // Pokerus
                    byte pokerusData = Utility.GetUnsignedNumber<byte>(substructureBytes, 0x00, 1);
                    string pokerusBinary = Convert.ToString(pokerusData, 2).PadLeft(8, '0');

                    // Stra Days
                    // 0000 0000
                    // Console.WriteLine($"Pokerus Data: {pokerusBinary}");
                    p.PokerusStrain = Convert.ToByte(pokerusBinary.Substring(0, 4), 2);
                    p.PokerusDaysRemaining = Convert.ToByte(pokerusBinary.Substring(4, 4), 2);

                    // Origin
                    p.Origin.MetLocationId = Lookup.GetLocationIdByGameIndex(3, Utility.GetUnsignedNumber<ushort>(substructureBytes, 0x01, 1));
                    ushort originData = Utility.GetUnsignedNumber<ushort>(substructureBytes, 0x02, 2);
                    string originBinary = Convert.ToString(originData, 2).PadLeft(16, '0');

                    // G Poke Game Level
                    // 0 0000 0000 0000000
                    // Console.WriteLine($"Origin Data: {originBinary}");

                    p.OriginalTrainer.Gender = originBinary[0] == '1' ? Gender.FEMALE : Gender.MALE;
                    p.Origin.PokeballId = Convert.ToByte(originBinary.Substring(1, 4), 2);
                    p.Origin.GameVersionId = Lookup.GetVersionIdByGameIndex(Convert.ToUInt16(originBinary.Substring(5, 4), 2));
                    p.Origin.MetLevel = Convert.ToByte(originBinary.Substring(9, 7), 2);

                    // IVs, Egg, Ability
                    uint miscData = Utility.GetUnsignedNumber<uint>(substructureBytes, 0x04, 4);
                    string miscBinary = Convert.ToString(miscData, 2).PadLeft(32, '0');

                    // A E SpD   SpA   Spe   Def   Atk   HP
                    // 0 0 00000 00000 00000 00000 00000 00000
                    // Console.WriteLine($"Misc Data: {miscBinary}");

                    p.AbilityNumber = byte.Parse(miscBinary[0].ToString());
                    p.IsEgg = miscBinary[1] == '1';
                    iv.SpecialDefense = Convert.ToByte(miscBinary.Substring(2, 5), 2);
                    iv.SpecialAttack = Convert.ToByte(miscBinary.Substring(7, 5), 2);
                    iv.Speed = Convert.ToByte(miscBinary.Substring(12, 5), 2);
                    iv.Defense = Convert.ToByte(miscBinary.Substring(17, 5), 2);
                    iv.Attack = Convert.ToByte(miscBinary.Substring(22, 5), 2);
                    iv.HP = Convert.ToByte(miscBinary.Substring(27, 5), 2);

                    // Ribbons, Obedience
                    // O ---- W E N C n r b E A V W C Tou Sma Cut Bea Coo
                    // 0 0000 0 0 0 0 0 0 0 0 0 0 0 0 000 000 000 000 000
                    p.Obedience = (Utility.GetUnsignedNumber<uint>(substructureBytes, 0x08, 4) & 0x80000000) > 0;
                    p.Ribbons.HoennSet = Utility.GetUnsignedNumber<uint>(substructureBytes, 0x08, 4) & 0x7FFFFFFF;
                    break;

                default:
                    break;
            }
        }

        // Calculations
        // Console.WriteLine($"Done reading: {PokemonIdentity.SpeciesIdentifier}");
        p.Stats = new(true, iv, ev, p.PokemonIdentity.SpeciesId, p.Level, p.Nature);
        p.Gender = p.GetGenderByPersonalityValue();
        p.AbilityId = p.GetAbilityIdFromAbilityNumber();
        p.HasNickname = p.DoesNicknameExist();
        return p;
    }

    public override void PrintPokedex()
    {
        Generation3Section readPokedex = Sections.First(x => x.SectionId == 0);
        byte[] ownedBytes = Utility.GetBytes(readPokedex.Data, 0x0028, 49);
        byte[] seenBytes = Utility.GetBytes(readPokedex.Data, 0x005C, 49);
        for (int i = 0; i < 386; i++)
        {
            PokemonIdentity pokemon = Lookup.GetPokemonBySpeciesId(i+1, Lookup.GetLanguageIdByIdentifier(Language));
            int ownedBit = ownedBytes[i >> 3] >> (i & 7) & 1;
            int seenBit = seenBytes[i >> 3] >> (i & 7) & 1;
            Console.WriteLine($"{seenBit}/{ownedBit} - {pokemon.SpeciesName}");
        }
    }

    public override void WriteToPokedex(int nationalIndex, bool seen = true, bool owned = true)
    {
        Generation3Section readPokedex = Sections.First(x => x.SectionId == 0);
        byte[] ownedBytes = Utility.GetBytes(readPokedex.Data, 0x0028, 49);
        byte[] seenBytes = Utility.GetBytes(readPokedex.Data, 0x005C, 49);

        int byteIndex = (nationalIndex-1) >> 3;
        int bitIndex = (nationalIndex-1) % 8;
        if (seen)
        {
            seenBytes[byteIndex] = (byte)((int)Math.Pow(2, bitIndex) | seenBytes[byteIndex]);
        }
        if (owned)
        {
            ownedBytes[byteIndex] = (byte)((int)Math.Pow(2, bitIndex) | ownedBytes[byteIndex]);
        }
        
        if (owned)
        {
            Buffer.BlockCopy(ownedBytes, 0, readPokedex.Data, 0x0028, 49);
        }

        if (seen)
        {
            Buffer.BlockCopy(seenBytes, 0, readPokedex.Data, 0x005C, 49);
            Generation3Section seenB = Sections.First(x => x.SectionId == 1);
            Generation3Section seenC = Sections.First(x => x.SectionId == 4);
            int bOffset = 0;
            int cOffset = 0;

            switch (Game.VersionGroupId)
            {
                case 5:
                    bOffset = 0x0938;
                    cOffset = 0x0C0C;
                    break;
                case 6:
                    bOffset = 0x0988;
                    cOffset = 0x0CA4;
                    break;
                case 7:
                    bOffset = 0x05F8;
                    cOffset = 0x0B98;
                    break;
            }

            Buffer.BlockCopy(seenBytes, 0, seenB.Data, bOffset, 49);
            Buffer.BlockCopy(seenBytes, 0, seenC.Data, cOffset, 49);
            seenB.Checksum = seenB.GetCalculatedChecksum();
            seenC.Checksum = seenC.GetCalculatedChecksum();
            readPokedex.Checksum = readPokedex.GetCalculatedChecksum();
        }
    }

    public override byte[] GetBoxBytesFromPartyPokemon(PartyPokemon p)
    {
        byte[] bytes = new byte[0x50];
        Array.Fill<byte>(bytes, 0);

        byte[] pvData = [.. BitConverter.GetBytes(p.PersonalityValue)];
        Buffer.BlockCopy(pvData, 0, bytes, 0x00, 4);

        byte[] otIdPublicData = [.. BitConverter.GetBytes(p.OriginalTrainer.PublicId)];
        byte[] otIdSecretData = [.. BitConverter.GetBytes(p.OriginalTrainer.SecretId)];
        Buffer.BlockCopy(otIdPublicData, 0, bytes, 0x04, 2);
        Buffer.BlockCopy(otIdSecretData, 0, bytes, 0x06, 2);

        byte[] nicknameData = Utility.GetEncodedString(p.Nickname, 10, Game, Language);
        Buffer.BlockCopy(nicknameData, 0, bytes, 0x08, 10);

        bytes[0x12] = Lookup.GetLanguageGameIndexById(p.LanguageId);
        bytes[0x13] = 2;

        byte[] otNameData = Utility.GetEncodedString(p.OriginalTrainer.Name, 7, Game, Language);
        Buffer.BlockCopy(otNameData, 0, bytes, 0x14, 7);

        bytes[0x1B] = p.Markings.AsGen3Byte();

        // Subsections
        // Growth
        byte[] g = new byte[12];
        byte[] speciesData = BitConverter.GetBytes(Lookup.GetPokemonGameIndexByFormId(3, p.PokemonIdentity.FormId));
        Buffer.BlockCopy(speciesData, 0, g, 0x00, 2);
 
        byte[] itemData = BitConverter.GetBytes(Lookup.GetItemGameIndexById(3, p.HeldItemId));
        Buffer.BlockCopy(itemData, 0, g, 0x02, 2);

        byte[] experienceData = BitConverter.GetBytes(p.ExperiencePoints);
        Buffer.BlockCopy(experienceData, 0, g, 0x04, 4);

        g[0x08] = (byte)(
            p.Moves[0].TimesIncreased + 
            (p.Moves[1].TimesIncreased << 2) + 
            (p.Moves[2].TimesIncreased << 4) + 
            (p.Moves[3].TimesIncreased << 6)
        );

        g[0x09] = p.Friendship;

        // Attacks
        byte[] a = new byte[12];

        byte[] m1 = [.. BitConverter.GetBytes(p.Moves[0].Id)];
        Buffer.BlockCopy(m1, 0, a, 0x00, 2);
        byte[] m2 = [.. BitConverter.GetBytes(p.Moves[1].Id)];
        Buffer.BlockCopy(m2, 0, a, 0x02, 2);
        byte[] m3 = [.. BitConverter.GetBytes(p.Moves[2].Id)];
        Buffer.BlockCopy(m3, 0, a, 0x04, 2);
        byte[] m4 = [.. BitConverter.GetBytes(p.Moves[3].Id)];
        Buffer.BlockCopy(m4, 0, a, 0x06, 2);
        
        a[0x08] = p.Moves[0].Pp;
        a[0x09] = p.Moves[1].Pp;
        a[0x0A] = p.Moves[2].Pp;
        a[0x0B] = p.Moves[3].Pp;

        // EVs and Condition
        byte[] e = new byte[12];
        e[0x00] = (byte)p.Stats.Modern.HP.Ev;
        e[0x01] = (byte)p.Stats.Modern.Attack.Ev;
        e[0x02] = (byte)p.Stats.Modern.Defense.Ev;
        e[0x03] = (byte)p.Stats.Modern.Speed.Ev;
        e[0x04] = (byte)p.Stats.Modern.SpecialAttack.Ev;
        e[0x05] = (byte)p.Stats.Modern.SpecialDefense.Ev;
        e[0x06] = (byte)p.Coolness;
        e[0x07] = (byte)p.Beauty;
        e[0x08] = (byte)p.Cuteness;
        e[0x09] = (byte)p.Smartness;
        e[0x0A] = (byte)p.Toughness;
        e[0x0B] = (byte)p.Sheen;

        // Misc
        byte[] m = new byte[12];
        m[0x00] = (byte)(p.PokerusDaysRemaining + (p.PokerusStrain << 4));
        m[0x01] = (byte)Lookup.GetLocationGameIndexById(3, p.Origin.MetLocationId);
        ushort origin = (ushort)(
            p.Origin.MetLevel + 
            (Lookup.GetGameGameIndexById(p.Origin.GameVersionId) << 7) + 
            (Lookup.GetBallGameIndexByItemId(p.Origin.PokeballId) << 11) +
            (((byte)p.OriginalTrainer.Gender) << 15)
        );
        byte[] originData = [.. BitConverter.GetBytes(origin)];
        Buffer.BlockCopy(originData, 0, m, 0x02, 2);

        uint ivEggAbility = (uint)(
            p.Stats.Modern.HP.Iv + 
            (p.Stats.Modern.Attack.Iv << 5) + 
            (p.Stats.Modern.Defense.Iv << 10) + 
            (p.Stats.Modern.Speed.Iv << 15) + 
            (p.Stats.Modern.SpecialAttack.Iv << 20) +
            (p.Stats.Modern.SpecialDefense.Iv << 25) +
            ((p.IsEgg ? 1 : 0) << 30) + 
            (p.AbilityNumber << 31)
        );

        byte[] ivEggAbilityData = [.. BitConverter.GetBytes(ivEggAbility)];
        Buffer.BlockCopy(ivEggAbilityData, 0, m, 0x04, 4);

        uint ribbon = (uint)(p.Ribbons.HoennSet + ((p.Obedience ? 1 : 0) << 31));
        byte[] ribbonData = [.. BitConverter.GetBytes(ribbon)];
        Buffer.BlockCopy(ribbonData, 0, m, 0x08, 4);

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
        int slot = 0;
        byte[] boxData = GetBoxDataBytes();
        for (int i = 0; i < 420; i += 80)
        {
            slot++;
            if (boxData[i + 0x4] > 0) continue;
            else
            {
                Buffer.BlockCopy(GetBoxBytesFromPartyPokemon(pokemon), 0, boxData, i + 0x4, 80);
                break;
            }
        }
        SaveBoxDataBytesToSections(boxData);
        return slot;
    }

    #region Helper

    private byte[] XorSubstructure(uint key, byte[] bytes)
    {
        byte[] result = [];
        for (int i = 0; i < 48; i += 4)
        {
            byte[] y = Utility.GetBytes(bytes, 0x1 * i, 4);
            byte[] unencryptedBytes = y.Zip(BitConverter.GetBytes(key)).Select(x => Convert.ToByte(x.First ^ x.Second)).ToArray();
            result = result.Concat(unencryptedBytes).ToArray();
        }
        return result;
    }

    private string GetSubstructureOrder(uint pv)
    {
        int order_index = (int)(pv % 24);
        Dictionary<int, string> order = new()
        {
            {0, "GAEM"}, {1,"GAME"}, {2,"GEAM"}, {3,"GEMA"},{4, "GMAE"}, {5,"GMEA"}, {6,"AGEM"}, {7,"AGME"},
            {8, "AEGM"}, {9,"AEMG"},{10,"AMGE"},{11,"AMEG"},{12,"AGAM"},{13,"EGMA"},{14,"EAGM"},{15,"EAMG"},
            {16,"EMGA"},{17,"EMAG"},{18,"MGAE"},{19,"MGEA"},{20,"MAGE"},{21,"MAEG"},{22,"MEGA"},{23,"MEAG"}
        };

        return order[order_index];
    }

    public byte[] GetBoxDataBytes()
    {
        List<byte> bytes = [];
        foreach (Generation3Section section in Sections.OrderBy(x => x.SectionId))
        {
            if (section.SectionId >= 5) bytes.AddRange(section.Data);
        }
        return bytes.ToArray();
    }

    public void SaveBoxDataBytesToSections(byte[] boxData)
    {
        foreach (Generation3Section section in Sections)
        {
            if (section.SectionId < 5) continue;
            byte[] newData = new byte[3968];
            Array.Fill<byte>(newData, 0x00);

            int dataLength = section.SectionId == 13 ? 2000 : 3968;
            Buffer.BlockCopy(boxData, (section.SectionId-5)*3968, newData, 0, dataLength);
            section.Data = newData;
            section.Checksum = section.GetCalculatedChecksum();
        }
    }

    public void SaveBoxDataSectionsToBytes()
    {
        int physicalSectionOffset = 0;
        foreach (Generation3Section section in Sections)
        {
            Buffer.BlockCopy(section.GetBytes(), 0, ModifiedData, SaveOffsets[SaveIndex] + (physicalSectionOffset * 0x1000), 0x1000);
            physicalSectionOffset++;
        }
    }

    #endregion
}