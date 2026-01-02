using Microsoft.Extensions.Logging;

namespace PokemonStorage.Models;

public class GameState
{
    public int TargetBox { get; set; }
    public int TargetSlot { get; set; }
    public byte[] Content { get; set; } = [];
    public Game Game { get; set; }
    public string Language { get; set; }
    public Trainer Trainer { get; set; }
    public Dictionary<int, PartyPokemon> Party { get; set; } = [];
    public Dictionary<string, Dictionary<int, PartyPokemon>> BoxList { get; set; } = [];

    public GameState(byte[] content, Game game, string language)
    {
        Content = content;
        Game = game;
        Language = language;
    }

    /// <summary>
    /// Fills GameState.Trainer with the original trainer parsed from the save file content.
    /// </summary>
    /// <returns></returns>
    public Trainer ParseOriginalTrainer()
    {
        if (Game.VersionId == 1 || Game.VersionId == 2)
        {
            string playerName = Utility.GetEncodedString(Utility.GetBytes(Content, 0x2598, 11), Game, Language);
            ushort playerId = Utility.GetUnsignedNumber<ushort>(Content, 0x2605, 2, true);
            Trainer = new(playerName, (int)Gender.MALE, playerId, 0);
        }
        else if (Game.VersionId == 3 || Game.VersionId == 4)
        {
            string playerName = Utility.GetEncodedString(Utility.GetBytes(Content, 0x200B, 11), Game, Language);
            ushort playerId = Utility.GetUnsignedNumber<ushort>(Content, 0x2009, 2, true);
            Trainer = new(
                playerName,
                Game.VersionId == 4 && playerId % 1 == 1 ? (int)Gender.FEMALE : (int)Gender.MALE,
                playerId,
                0
            );
        }
        else if (Game.VersionId >= 5 && Game.VersionId <= 7)
        {
            byte[] save1 = Utility.GetBytes(Content, 0x0000, 57344);
            byte[] save2 = Utility.GetBytes(Content, 0xE000, 57344);
            uint save1Index = Utility.GetUnsignedNumber<uint>(save1, 0x0FFC, 4);
            uint save2Index = Utility.GetUnsignedNumber<uint>(save2, 0x0FFC, 4);
            byte[] saveData;
            if (save1Index >= save2Index && save1Index != uint.MaxValue)
            {
                Console.WriteLine($"Using Save 1 (Index: {save1Index})");
                saveData = save1;
            }
            else
            {
                Console.WriteLine($"Using Save 2 (Index: {save2Index})");
                saveData = save2;
            }
            Dictionary<int, byte[]> sections = [];
            for (int i = 0; i < 14; i++)
            {
                int sectionOffset = 0x1000 * i;
                ushort sectionId = Utility.GetUnsignedNumber<ushort>(saveData, sectionOffset + 0x0FF4, 2);
                byte[] sectionData = Utility.GetBytes(saveData, sectionOffset, 0x1000);
                sections[sectionId] = sectionData;
            }

            Trainer = new(
                Utility.GetEncodedString(Utility.GetBytes(sections[0], 0, 7), Game, Language),
                Utility.GetUnsignedNumber<byte>(sections[0], 0x0008, 1),
                Utility.GetUnsignedNumber<ushort>(sections[0], 0x000A, 2),
                Utility.GetUnsignedNumber<ushort>(sections[0], 0x000C, 2)
            );
        }
        else if (Game.VersionId >= 8 && Game.VersionId <= 10)
        {
            (int start, int end) littleBlockOffsets = (0x00000, 0x00000);
            int trainerNameOffset = Game.VersionId == 10 ? 0x64 : 0x68;
            int trainerPublicIdOffset = Game.VersionId == 10 ? 0x74 : 0x78;
            int trainerSecretIdOffset = Game.VersionId == 10 ? 0x76 : 0x7A;
            int trainerGenderOffset = Game.VersionId == 10 ? 0x7C : 0x80;

            if (Game.VersionId == 8)
            {
                littleBlockOffsets = (0x0000, 0x0C0FF);
            }
            else if (Game.VersionId == 9)
            {
                littleBlockOffsets = (0x00000, 0x0CF2B);
            }
            else if (Game.VersionId == 10)
            {
                littleBlockOffsets = (0x00000, 0x0F6FF);
            }

            byte[] littleBlockBytes = Utility.GetBytes(Content, littleBlockOffsets.start, littleBlockOffsets.end - littleBlockOffsets.start);

            Trainer = new(
                Utility.GetEncodedString(Utility.GetBytes(littleBlockBytes, trainerNameOffset, 16), Game, Language),
                Utility.GetUnsignedNumber<byte>(littleBlockBytes, trainerPublicIdOffset, 1),
                Utility.GetUnsignedNumber<ushort>(littleBlockBytes, trainerSecretIdOffset, 2),
                Utility.GetUnsignedNumber<ushort>(littleBlockBytes, trainerGenderOffset, 2)
            );
        }
        return Trainer;
    }

    /// <summary>
    /// Fills GameState.Party with the party Pokemon parsed from the save file content.
    /// </summary>
    public void ParsePartyPokemon()
    {
        if (Game.VersionId == 1 || Game.VersionId == 2)
        {
            byte[] partyBytes = Utility.GetBytes(Content, 0x2F2C, 0x194);
            Party = GetPokemonFromStorageGen1(partyBytes, Language, 0x8, 0x2C, 0x110, 0x152);
        }
        else if (Game.VersionId == 3 || Game.VersionId == 4)
        {
            int partyOffset = Game.VersionId == 3 ? 0x288A : 0x2865;
            byte[] partyBytes = Utility.GetBytes(Content, partyOffset, 428);
            Party = GetPokemonFromStorageGen2(partyBytes, Language, 6, 48);
        }
        else if (Game.VersionId == 5 || Game.VersionId == 6 || Game.VersionId == 7)
        {
            byte[] save1 = Utility.GetBytes(Content, 0x0000, 57344);
            byte[] save2 = Utility.GetBytes(Content, 0xE000, 57344);
            uint save1Index = Utility.GetUnsignedNumber<uint>(save1, 0x0FFC, 4);
            uint save2Index = Utility.GetUnsignedNumber<uint>(save2, 0x0FFC, 4);
            byte[] saveData;
            if (save1Index >= save2Index && save1Index != uint.MaxValue)
            {
                Console.WriteLine($"Using Save 1 (Index: {save1Index})");
                saveData = save1;
            }
            else
            {
                Console.WriteLine($"Using Save 2 (Index: {save2Index})");
                saveData = save2;
            }
            Dictionary<int, byte[]> sections = [];
            for (int i = 0; i < 14; i++)
            {
                int sectionOffset = 0x1000 * i;
                ushort sectionId = Utility.GetUnsignedNumber<ushort>(saveData, sectionOffset + 0x0FF4, 2);
                byte[] sectionData = Utility.GetBytes(saveData, sectionOffset, 0x1000);
                sections[sectionId] = sectionData;
            }

            int partySizeOffset = Game.VersionId == 5 || Game.VersionId == 6 ? 0x0234 : 0x0034;
            int partyOffset = Game.VersionId == 5 || Game.VersionId == 6 ? 0x0238 : 0x0038;
            uint partySize = Utility.GetUnsignedNumber<uint>(sections[1], partySizeOffset, 4);
            byte[] partyBytes = Utility.GetBytes(sections[1], partyOffset, 600);

            for (int i = 0; i < partySize; i++)
            {
                PartyPokemon pokemon = new(3);
                byte[] pokemonBytes = Utility.GetBytes(partyBytes, i * 100, 100);
                pokemon.LoadFromGen3Bytes(pokemonBytes, Game, Language);
                Party[i] = pokemon;
            }
        }
        else if (Game.VersionId == 8 || Game.VersionId == 9 || Game.VersionId == 10)
        {
            (int start, int end) littleBlockOffsets = (0x00000, 0x00000);
            int partySizeOffset = Game.VersionId == 10 ? 0x94 : 0x9C;
            int partyOffset = Game.VersionId == 10 ? 0x98 : 0xA0;

            if (Game.VersionId == 8)
            {
                littleBlockOffsets = (0x0000, 0x0C0FF);
            }
            else if (Game.VersionId == 9)
            {
                littleBlockOffsets = (0x00000, 0x0CF2B);
            }
            else if (Game.VersionId == 10)
            {
                littleBlockOffsets = (0x00000, 0x0F6FF);
            }

            byte[] littleBlockBytes = Utility.GetBytes(Content, littleBlockOffsets.start, littleBlockOffsets.end - littleBlockOffsets.start);

            int partySize = Utility.GetUnsignedNumber<byte>(littleBlockBytes, partySizeOffset, 1);
            byte[] partyBytes = Utility.GetBytes(littleBlockBytes, partyOffset, 1416);

            for (int i = 0; i < partySize; i++)
            {
                Program.Logger.LogInformation($"Looking at Party:{i}");
                PartyPokemon pokemon = new(4);
                byte[] pokemonBytes = Utility.GetBytes(partyBytes, i * 236, 236);
                pokemon.LoadFromGen4Bytes(pokemonBytes, Game, Language);
                Party[i] = pokemon;
            }
        }
    }

    /// <summary>
    /// Fills GameState.BoxList with the box Pokemon parsed from the save file content.
    /// </summary>
    public void ParseBoxPokemon()
    {
        if (Game.VersionId == 1 || Game.VersionId == 2)
        {
            int boxSize = 0x462;
            int[] boxOffets = [0x4000, 0x4462, 0x48C4, 0x4D26, 0x5188, 0x55EA, 0x6000, 0x6462, 0x68C4, 0x6D26, 0x7188, 0x75EA];

            for (int i = 0; i < boxOffets.Length; i++)
            {
                byte[] boxBytes = Utility.GetBytes(Content, boxOffets[i], boxSize);
                string boxName = $"Box{i+1}";
                Program.Logger.LogInformation($"Looking at Box {boxName}");
                BoxList[boxName] = GetPokemonFromStorageGen1(boxBytes, Language, 0x16, 0x21, 0x2AA, 0x386);
            }
        }
        else if (Game.VersionId == 3 || Game.VersionId == 4)
        {
            int boxSize = 0x462;
            int[] boxOffets = [0x4000, 0x4450, 0x48A0, 0x4CF0, 0x5140, 0x5590, 0x59E0, 0x6000, 0x6450, 0x68A0, 0x6CF0, 0x7140, 0x7590, 0x79E0];

            for (int i = 0; i < boxOffets.Length; i++)
            {
                byte[] boxBytes = Utility.GetBytes(Content, boxOffets[i], boxSize);
                string boxName = $"Box{i+1}";
                Program.Logger.LogInformation($"Looking at Box {boxName}");
                BoxList[boxName] = GetPokemonFromStorageGen2(boxBytes, Language, 20, 32);
            }
            return;
        }
        else if (Game.VersionId == 5 || Game.VersionId == 6 || Game.VersionId == 7)
        {
            byte[] save1 = Utility.GetBytes(Content, 0x0000, 57344);
            byte[] save2 = Utility.GetBytes(Content, 0xE000, 57344);
            uint save1Index = Utility.GetUnsignedNumber<uint>(save1, 0x0FFC, 4);
            uint save2Index = Utility.GetUnsignedNumber<uint>(save2, 0x0FFC, 4);
            byte[] saveData;
            if (save1Index >= save2Index && save1Index != uint.MaxValue)
            {
                Console.WriteLine($"Using Save 1 (Index: {save1Index})");
                saveData = save1;
            }
            else
            {
                Console.WriteLine($"Using Save 2 (Index: {save2Index})");
                saveData = save2;
            }
            Dictionary<int, byte[]> sections = [];
            for (int i = 0; i < 14; i++)
            {
                int sectionOffset = 0x1000 * i;
                ushort sectionId = Utility.GetUnsignedNumber<ushort>(saveData, sectionOffset + 0x0FF4, 2);
                byte[] sectionData = Utility.GetBytes(saveData, sectionOffset, 0x1000);
                sections[sectionId] = sectionData;
            }

            List<byte> boxByteList = [];
            for (int i = 0; i < 14; i++)
            {
                if (i >= 5 && i <= 13) boxByteList.AddRange(sections[i].Take(3968));
            }

            byte[] boxBytes = [.. boxByteList];
            for (int i = 0; i < 14; i++)
            {
                byte[] thisBoxBytes = Utility.GetBytes(boxBytes, 0x4 + (i * 2400), 2400);
                string thisBoxName = Utility.GetEncodedString(Utility.GetBytes(boxBytes, 0x8344 + (i * 9), 9), Game, Language);
                if (!BoxList.ContainsKey(thisBoxName)) BoxList.Add(thisBoxName, []);

                for (int j = 0; j < 30; j++)
                {
                    Program.Logger.LogInformation($"Looking at {thisBoxName}:{j}");
                    byte[] pokemonBytes = Utility.GetBytes(thisBoxBytes, j * 80, 80);
                    uint thisPv = Utility.GetUnsignedNumber<uint>(pokemonBytes, 0x0, 4);
                    ushort thisCs = Utility.GetUnsignedNumber<ushort>(pokemonBytes, 0x1C, 2);
                    if (thisPv == 0 && thisCs == 0)
                    {
                        Program.Logger.LogInformation("No Pokemon");
                        continue;
                    }
                    
                    PartyPokemon pokemon = new(3);
                    pokemon.LoadFromGen3Bytes(pokemonBytes, Game, Language);
                    BoxList[thisBoxName][j] = pokemon;
                }
            }
        }
        else if (Game.VersionId == 8 || Game.VersionId == 9 || Game.VersionId == 10)
        {
            (int start, int end) bigBlockOffsets = (0x00000, 0x00000);

            if (Game.VersionId == 8)
            {
                bigBlockOffsets = (0x0C100, 0x1E2DF);
            }
            else if (Game.VersionId == 9)
            {
                bigBlockOffsets = (0x0CF2C, 0x1F10F);
            }
            else if (Game.VersionId == 10)
            {
                bigBlockOffsets = (0x0F700, 0x21A10);
            }

            byte[] bigBlockBytes = Utility.GetBytes(Content, bigBlockOffsets.start, bigBlockOffsets.end - bigBlockOffsets.start);

            int boxSize = (Game.VersionId == 10) ? 0x1000 : 0xFF0;
            for (int i = 0; i < 18; i++)
            {
                int pokemonOffset = (Game.VersionId == 10) ? 0x00 : 0x04;
                int boxNameOffset = (Game.VersionId == 10) ? 0x12008 : 0x11EE4;
                
                byte[] boxNameBytes = Utility.GetBytes(bigBlockBytes, boxNameOffset + (i * 40), 40);
                string boxName = Utility.GetEncodedString(boxNameBytes, Game, Language);
                if (!BoxList.ContainsKey(boxName)) BoxList.Add(boxName, []);
                byte[] thisBoxBytes = Utility.GetBytes(bigBlockBytes, pokemonOffset + (boxSize * i), 136 * 30);

                for (int j = 0; j < 30; j++)
                {
                    Program.Logger.LogInformation($"Looking at {boxName}:{j}");
                    uint thisPv = Utility.GetUnsignedNumber<uint>(thisBoxBytes, (j * 136) + 0, 4);
                    ushort thisCs = Utility.GetUnsignedNumber<ushort>(thisBoxBytes, (j * 136) + 6, 2);
                    if (thisPv == 0 && thisCs == 0)
                    {
                        Program.Logger.LogInformation("No Pokemon");
                        continue;
                    }
                    
                    PartyPokemon pokemon = new(4);
                    byte[] pokemonBytes = Utility.GetBytes(thisBoxBytes, j * 136, 136);
                    pokemon.LoadFromGen4Bytes(pokemonBytes, Game, Language);
                    BoxList[boxName][j] = pokemon;
                }
            }
        }
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
    
    private Dictionary<int, PartyPokemon> GetPokemonFromStorageGen2(byte[] storageBytes, string lang, int capacity, int pokemonSize)
    {
        Dictionary<int, PartyPokemon> box = [];
        byte boxCount = Utility.GetUnsignedNumber<byte>(storageBytes, 0x00, 1, true);
        int pokemonOffset = 2 + capacity;
        int originalTrainerNameOffset = pokemonOffset + (pokemonSize * capacity);
        int nicknameOffset = originalTrainerNameOffset + (capacity * 0xB);

        for (int i = 0; i < boxCount; i++)
        {
            byte[] nicknameBytes = Utility.GetBytes(storageBytes, nicknameOffset + (0xB * i), 0xB);
            string nickname = Utility.GetEncodedString(nicknameBytes, Game, lang);

            byte[] originalTrainerNameBytes = Utility.GetBytes(storageBytes, originalTrainerNameOffset + (0xB * i), 0xB);
            string originalTrainerName = Utility.GetEncodedString(originalTrainerNameBytes, Game, lang);

            byte[] pokemonBytes = Utility.GetBytes(storageBytes, pokemonOffset + (pokemonSize * i), 32);
            PartyPokemon pokemon = new(2);
            pokemon.LoadFromGen2Bytes(pokemonBytes, Game, nickname, originalTrainerName, lang);
            box[i] = pokemon;
        }
        return box;
    }

    public object GetEntireStorageObject()
    {
        var pokemonStorageDictionary = new Dictionary<string, Dictionary<string, PartyPokemon>>();
        foreach ((int index, PartyPokemon pokemon) in Party)
        {
            if (!pokemonStorageDictionary.ContainsKey("Party"))
                pokemonStorageDictionary["Party"] = [];

            pokemonStorageDictionary["Party"].Add(index.ToString(), pokemon);
        }

        foreach ((string box, Dictionary<int, PartyPokemon> boxDictionary) in BoxList)
        {
            if (!pokemonStorageDictionary.ContainsKey(box))
                pokemonStorageDictionary[box] = [];

            foreach ((int slot, PartyPokemon pokemon) in boxDictionary)
            {
                string slotId = slot.ToString();
                if (!pokemonStorageDictionary[box].ContainsKey(slotId.ToString()))
                    pokemonStorageDictionary[box].Add(slotId.ToString(), pokemon);
            }
        }
        return pokemonStorageDictionary;
    }
}
