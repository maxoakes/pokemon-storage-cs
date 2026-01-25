using Microsoft.Extensions.Logging;
using PokemonStorage.Models;

namespace PokemonStorage.SaveContent;

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

        for (int i = 0; i < 14; i++)
        {
            Sections.Add(new Generation3Section(Utility.GetBytes(content, SaveOffsets[SaveIndex] + (0x1000 * i), 0x1000)));
            Program.Logger.LogWarning($"Loaded section {Sections[i].SaveIndex}:{Sections[i].SectionId}");
        }
        ParseOriginalTrainer();
        AreAllChecksumsValid();
        PrintPokedex();
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
        byte[] fullEncryptedData = Utility.GetBytes(data, 0x20, 48);
        int order_index = (int)(p.PersonalityValue % 24);

        Dictionary<int, string> order = new()
        {
            {0, "GAEM"}, {1,"GAME"}, {2,"GEAM"}, {3,"GEMA"},{4, "GMAE"}, {5,"GMEA"}, {6,"AGEM"}, {7,"AGME"},
            {8, "AEGM"}, {9,"AEMG"},{10,"AMGE"},{11,"AMEG"},{12,"AGAM"},{13,"EGMA"},{14,"EAGM"},{15,"EAMG"},
            {16,"EMGA"},{17,"EMAG"},{18,"MGAE"},{19,"MGEA"},{20,"MAGE"},{21,"MAEG"},{22,"MEGA"},{23,"MEAG"}
        };

        string orderString = order[order_index];
        // Program.Logger.LogInformation($"Decryption Order: {order_index}:{orderString}");
        uint decryptionKey = p.PersonalityValue ^ otId;
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
                    // Program.Logger.LogInformation($"Pokerus Data: {pokerusBinary}");
                    p.PokerusStrain = Convert.ToByte(pokerusBinary.Substring(0, 4), 2);
                    p.PokerusDaysRemaining = Convert.ToByte(pokerusBinary.Substring(4, 4), 2);

                    // Origin
                    p.Origin.MetLocationId = Lookup.GetLocationIdByGameIndex(3, Utility.GetUnsignedNumber<ushort>(substructureBytes, 0x01, 1));
                    ushort originData = Utility.GetUnsignedNumber<ushort>(substructureBytes, 0x02, 2);
                    string originBinary = Convert.ToString(originData, 2).PadLeft(16, '0');

                    // G Poke Game Level
                    // 0 0000 0000 0000000
                    // Program.Logger.LogInformation($"Origin Data: {originBinary}");

                    p.OriginalTrainer.Gender = originBinary[0] == '1' ? Gender.FEMALE : Gender.MALE;
                    p.Origin.PokeballId = Convert.ToByte(originBinary.Substring(1, 4), 2);
                    p.Origin.GameVersionId = Lookup.GetVersionIdByGameIndex(Convert.ToUInt16(originBinary.Substring(5, 4), 2));
                    p.Origin.MetLevel = Convert.ToByte(originBinary.Substring(9, 7), 2);

                    // IVs, Egg, Ability
                    uint miscData = Utility.GetUnsignedNumber<uint>(substructureBytes, 0x04, 4);
                    string miscBinary = Convert.ToString(miscData, 2).PadLeft(32, '0');

                    // A E SpD   SpA   Spe   Def   Atk   HP
                    // 0 0 00000 00000 00000 00000 00000 00000
                    // Program.Logger.LogInformation($"Misc Data: {miscBinary}");

                    abilitySlotId = Int32.Parse(miscBinary[0].ToString());
                    p.IsEgg = miscBinary[1] == '1';
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
                    p.Obedience = ribbonBinary[0] == '1';
                    p.Ribbons.World = ribbonBinary[5] == '1';
                    p.Ribbons.Earth = ribbonBinary[6] == '1';
                    p.Ribbons.National = ribbonBinary[7] == '1';
                    p.Ribbons.Country = ribbonBinary[8] == '1';
                    p.Ribbons.Sky = ribbonBinary[9] == '1';
                    p.Ribbons.Land = ribbonBinary[10] == '1';
                    p.Ribbons.Marine = ribbonBinary[11] == '1';
                    p.Ribbons.Effort = ribbonBinary[12] == '1';
                    p.Ribbons.Artist = ribbonBinary[13] == '1';
                    p.Ribbons.Victory = ribbonBinary[14] == '1';
                    p.Ribbons.Winning = ribbonBinary[15] == '1';
                    p.Ribbons.Champion = ribbonBinary[16] == '1';
                    p.Ribbons.HeonnTough = Convert.ToByte(ribbonBinary.Substring(17, 3), 2);
                    p.Ribbons.HeonnSmart = Convert.ToByte(ribbonBinary.Substring(20, 3), 2);
                    p.Ribbons.HeonnCute = Convert.ToByte(ribbonBinary.Substring(23, 3), 2);
                    p.Ribbons.HeonnBeauty = Convert.ToByte(ribbonBinary.Substring(26, 3), 2);
                    p.Ribbons.HeonnCool = Convert.ToByte(ribbonBinary.Substring(29, 3), 2);
                    break;

                default:
                    break;
            }
        }

        // Calculations
        // Program.Logger.LogInformation($"Done reading: {PokemonIdentity.SpeciesIdentifier}");
        p.Stats = new(true, iv, ev, p.PokemonIdentity.SpeciesId, p.Level, p.Nature);
        p.Gender = p.GetGenderByPersonalityValue();
        p.AbilityId = p.GetAbilityFromSlotId(abilitySlotId);
        p.HasNickname = p.DoesNicknameExist();
        return p;
    }

    public override void PrintPokedex()
    {
        Generation3Section readPokedex = Sections.First(x => x.SectionId == 0);
        byte[] owned = Utility.GetBytes(readPokedex.Data, 0x0028, 49);
        byte[] seen = Utility.GetBytes(readPokedex.Data, 0x005C, 49);
        for (int i = 0; i < 386; i++)
        {
            PokemonIdentity pokemon = Lookup.GetPokemonBySpeciesId(i+1, Lookup.GetLanguageIdByIdentifier(Language));
            int ownedBit = owned[i >> 3] >> (i & 7) & 1;
            int seenBit = seen[i >> 3] >> (i & 7) & 1;
            Program.Logger.LogInformation($"{seenBit}/{ownedBit} - {pokemon.SpeciesName}");
        }
    }

    public override void WriteToPokedex(int nationalIndex, bool seen = true, bool owned = true)
    {
        
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

        bytes[0x1B] = p.Markings.Bits;

        // Subsections
        // Growth
        byte[] g = new byte[12];
        byte[] speciesData = BitConverter.GetBytes(Lookup.GetPokemonGameIndexByFormId(3, p.PokemonIdentity.FormId));
        Buffer.BlockCopy(speciesData, 0, g, 0x00, 2);
 
        byte[] itemData = BitConverter.GetBytes(Lookup.GetItemGameIndexById(3, p.HeldItemId));
        Buffer.BlockCopy(itemData, 0, g, 0x02, 2);

        byte[] experienceData = BitConverter.GetBytes(p.ExperiencePoints);
        Buffer.BlockCopy(itemData, 0, g, 0x04, 4);

        g[0x08] = (byte)(
            p.Moves[0].TimesIncreased + 
            p.Moves[1].TimesIncreased << 2 + 
            p.Moves[2].TimesIncreased << 4 + 
            p.Moves[3].TimesIncreased << 6
        );

        g[0x09] = p.Friendship;

        // Attacks
        byte[] a = new byte[12];

        bytes[0x02] = (byte)p.Moves[0].Id;
        bytes[0x03] = (byte)p.Moves[1].Id;
        bytes[0x04] = (byte)p.Moves[2].Id;
        bytes[0x05] = (byte)p.Moves[3].Id;
        
        bytes[0x17] = (byte)((p.Moves[0].TimesIncreased << 6) + Math.Min((byte)63, p.Moves[0].Pp));
        bytes[0x18] = (byte)((p.Moves[1].TimesIncreased << 6) + Math.Min((byte)63, p.Moves[1].Pp));
        bytes[0x19] = (byte)((p.Moves[2].TimesIncreased << 6) + Math.Min((byte)63, p.Moves[2].Pp));
        bytes[0x1A] = (byte)((p.Moves[3].TimesIncreased << 6) + Math.Min((byte)63, p.Moves[3].Pp));

        // EVs and Condition
        byte[] e = new byte[12];

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

        // Misc
        byte[] m = new byte[12];
        
        bytes[0x15] = (byte)((p.Stats.Old.Attack.Iv << 4) + p.Stats.Old.Defense.Iv);
        bytes[0x16] = (byte)((p.Stats.Old.Speed.Iv << 4) + p.Stats.Old.SpecialAttack.Iv);
        bytes[0x1C] = (byte)((int)(p.PokerusStrain << 4) + p.PokerusDaysRemaining);
        byte timeOfDay = 2;
        if (p.Origin.MetDateTime.HasValue)
        {
            if (p.Origin.MetDateTime.Value.TimeOfDay.Hours < 4 || p.Origin.MetDateTime.Value.TimeOfDay.Hours > 5) timeOfDay = 3;
            else if (p.Origin.MetDateTime.Value.TimeOfDay.Hours > 3 || p.Origin.MetDateTime.Value.TimeOfDay.Hours < 10) timeOfDay = 1;
        }
        bytes[0x1D] = (byte)((timeOfDay << 6) + p.Level);
        bytes[0x1E] = (byte)(((int)p.OriginalTrainer.Gender << 7) + Lookup.GetLocationGameIndexById(2, p.Origin.MetLocationId));        
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
            if (section.SectionId >= 5 && section.SectionId <= 12)
            {
                section.Data = Utility.GetBytes(boxData, (5-0)*section.SectionId, 3968);
            }
            else if (section.SectionId == 13)
            {
                section.Data = Utility.GetBytes(boxData, 3968*12, 2000);
            }
        }
    }

    public void SaveBoxDataSectionsToBytes()
    {
        
    }

    #endregion
}