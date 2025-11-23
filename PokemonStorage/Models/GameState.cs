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

        if (Game.VersionId == 1 || Game.VersionId == 2)
        {

            string playerName = Utility.GetEncodedString(Utility.GetBytes(content, 0x2598, 11), Game.VersionId, language);
            ushort playerId = Utility.GetUnsignedNumber<ushort>(content, 0x2605, 2, true);
            Trainer = new(playerName, (int)Gender.MALE, playerId, 0);
            Program.Logger.LogInformation(Trainer.ToString());

            byte[] partyBytes = Utility.GetBytes(content, 0x2F2C, 0x194);
            Party = GetPokemonFromStorageGen1(partyBytes, Game.VersionId, language, 0x8, 0x2C, 0x110, 0x152);

            int boxSize = 0x462;
            int[] boxOffets = [0x4000, 0x4462, 0x48C4, 0x4D26, 0x5188, 0x55EA, 0x6000, 0x6462, 0x68C4, 0x6D26, 0x7188, 0x75EA];

            for (int i = 0; i < boxOffets.Length; i++)
            {
                byte[] boxBytes = Utility.GetBytes(content, boxOffets[i], boxSize);
                string boxName = $"Box{i+1}";
                BoxList[boxName] = GetPokemonFromStorageGen1(boxBytes, Game.VersionId, language, 0x16, 0x21, 0x2AA, 0x386);
            }
        }
        else if (Game.VersionId == 3 || Game.VersionId == 4)
        {
            string playerName = Utility.GetEncodedString(Utility.GetBytes(content, 0x200B, 11), Game.VersionId, language);
            ushort playerId = Utility.GetUnsignedNumber<ushort>(content, 0x2009, 2, true);
            Trainer = new(
                playerName,
                Game.VersionId == 4 && playerId % 1 == 1 ? (int)Gender.FEMALE : (int)Gender.MALE,
                playerId,
                0
            );
            Program.Logger.LogInformation(Trainer.ToString());

            int partyOffset = Game.VersionId == 3 ? 0x288A : 0x2865;
            byte[] partyBytes = Utility.GetBytes(content, partyOffset, 428);

            Party = GetPokemonFromStorageGen2(partyBytes, Game.VersionId, language, 6, 48);

            int boxSize = 0x462;
            int[] boxOffets = [0x4000, 0x4450, 0x48A0, 0x4CF0, 0x5140, 0x5590, 0x59E0, 0x6000, 0x6450, 0x68A0, 0x6CF0, 0x7140, 0x7590, 0x79E0];

            for (int i = 0; i < boxOffets.Length; i++)
            {
                byte[] boxBytes = Utility.GetBytes(content, boxOffets[i], boxSize);
                string boxName = $"Box{i+1}";
                BoxList[boxName] = GetPokemonFromStorageGen2(boxBytes, Game.VersionId, language, 20, 32);
            }
            return;
        }
        else if (Game.VersionId == 5 || Game.VersionId == 6 || Game.VersionId == 7)
        {
            byte[] save1 = Utility.GetBytes(content, 0x0000, 57344);
            byte[] save2 = Utility.GetBytes(content, 0xE000, 57344);
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
                Utility.GetEncodedString(Utility.GetBytes(sections[0], 0, 7), Game.VersionId, language),
                Utility.GetUnsignedNumber<byte>(sections[0], 0x0008, 1),
                Utility.GetUnsignedNumber<ushort>(sections[0], 0x000A, 2),
                Utility.GetUnsignedNumber<ushort>(sections[0], 0x000C, 2)
            );

            int partySizeOffset = Game.VersionId == 5 || Game.VersionId == 6 ? 0x0234 : 0x0034;
            int partyOffset = Game.VersionId == 5 || Game.VersionId == 6 ? 0x0238 : 0x0038;
            uint partySize = Utility.GetUnsignedNumber<uint>(sections[1], partySizeOffset, 4);
            byte[] partyBytes = Utility.GetBytes(sections[1], partyOffset, 600);

            for (int i = 0; i < partySize; i++)
            {
                PartyPokemon pokemon = new(3);
                byte[] pokemonBytes = Utility.GetBytes(partyBytes, i * 100, 100);
                pokemon.LoadFromGen3Bytes(pokemonBytes, Game.VersionId, language);
                Party[i] = pokemon;
            }
        }
        else if (Game.VersionId == 8 || Game.VersionId == 9 || Game.VersionId == 10)
        {
            (int start, int end) littleBlockOffsets = (0x00000, 0x00000);
            (int start, int end) bigBlockOffsets = (0x00000, 0x00000);
            if (Game.VersionId == 8)
            {
                littleBlockOffsets = (0x0000, 0x0C0FF);
                bigBlockOffsets = (0x0C100, 0x1E2DF);
            }
            else if (Game.VersionId == 9)
            {
                littleBlockOffsets = (0x00000, 0x0CF2B);
                bigBlockOffsets = (0x0CF2C, 0x1F10F);
            }
            else if (Game.VersionId == 10)
            {
                littleBlockOffsets = (0x00000, 0x0F6FF);
                bigBlockOffsets = (0x0F700, 0x21A10);
            }

            byte[] littleBlockBytes = Utility.GetBytes(content, littleBlockOffsets.start, littleBlockOffsets.end - littleBlockOffsets.start);
            byte[] bigBlockBytes = Utility.GetBytes(content, bigBlockOffsets.start, bigBlockOffsets.end - bigBlockOffsets.start);

            Trainer = new(
                Utility.GetEncodedString(Utility.GetBytes(littleBlockBytes, 0x68, 16), Game.VersionId, language),
                Utility.GetUnsignedNumber<byte>(littleBlockBytes, 0x80, 1),
                Utility.GetUnsignedNumber<ushort>(littleBlockBytes, 0x78, 2),
                Utility.GetUnsignedNumber<ushort>(littleBlockBytes, 0x7A, 2)
            );

            int partySize = Utility.GetUnsignedNumber<byte>(littleBlockBytes, 0x9C, 1);
            byte[] partyBytes = Utility.GetBytes(littleBlockBytes, 0xA0, 1416);

            for (int i = 0; i < partySize; i++)
            {
                Program.Logger.LogInformation($"Looking at Party:{i}");
                PartyPokemon pokemon = new(4);
                byte[] pokemonBytes = Utility.GetBytes(partyBytes, i * 236, 236);
                pokemon.LoadFromGen4Bytes(pokemonBytes, Game.VersionId, language);
                Party[i] = pokemon;
            }

            int boxSize = (Game.VersionId == 10) ? 0x1000 : 0xFF0;
            for (int i = 0; i < 18; i++)
            {
                int pokemonOffset = (Game.VersionId == 10) ? 0x00 : 0x04;
                int boxNameOffset = (Game.VersionId == 10) ? 0x12008 : 0x11EE4;
                
                byte[] boxNameBytes = Utility.GetBytes(bigBlockBytes, boxNameOffset + (i * 40), 40);
                string boxName = Utility.GetEncodedString(boxNameBytes, Game.VersionId, language);
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
                    pokemon.LoadFromGen4Bytes(pokemonBytes, Game.VersionId, language);
                    BoxList[boxName][j] = pokemon;
                }

            }
        }
        else
        {
            return;
        }
    }

    private Dictionary<int, PartyPokemon> GetPokemonFromStorageGen1(byte[] content, int version, string lang, int pokemonOffset, int pokemonSize, int trainerNameOffset, int nicknamesOffset)
    {
        Dictionary<int, PartyPokemon> box = [];
        byte boxCount = Utility.GetUnsignedNumber<byte>(content, 0x00, 1, true);

        for (int i = 0; i < boxCount; i++)
        {
            byte[] nicknameBytes = Utility.GetBytes(content, nicknamesOffset + (0xB * i), 0xB);
            string nickname = Utility.GetEncodedString(nicknameBytes, version, lang);

            byte[] originalTrainerNameBytes = Utility.GetBytes(content, trainerNameOffset + (0xB * i), 0xB);
            string originalTrainerName = Utility.GetEncodedString(originalTrainerNameBytes, version, lang);

            byte[] pokemonBytes = Utility.GetBytes(content, pokemonOffset + (pokemonSize * i), pokemonSize);
            PartyPokemon pokemon = new(1);
            pokemon.LoadFromGen1Bytes(pokemonBytes, version, nickname, originalTrainerName);
            box[i] = pokemon;
        }
        return box;
    }
    
    private Dictionary<int, PartyPokemon> GetPokemonFromStorageGen2(byte[] content, int version, string lang, int capacity, int pokemonSize)
    {
        Dictionary<int, PartyPokemon> box = [];
        byte boxCount = Utility.GetUnsignedNumber<byte>(content, 0x00, 1, true);
        int pokemonOffset = 2 + capacity;
        int originalTrainerNameOffset = pokemonOffset + (pokemonSize * capacity);
        int nicknameOffset = originalTrainerNameOffset + (capacity * 0xB);

        for (int i = 0; i < boxCount; i++)
        {
            byte[] nicknameBytes = Utility.GetBytes(content, nicknameOffset + (0xB * i), 0xB);
            string nickname = Utility.GetEncodedString(nicknameBytes, version, lang);

            byte[] originalTrainerNameBytes = Utility.GetBytes(content, originalTrainerNameOffset + (0xB * i), 0xB);
            string originalTrainerName = Utility.GetEncodedString(originalTrainerNameBytes, version, lang);

            byte[] pokemonBytes = Utility.GetBytes(content, pokemonOffset + (pokemonSize * i), 32);
            PartyPokemon pokemon = new(2);
            pokemon.LoadFromGen2Bytes(pokemonBytes, version, nickname, originalTrainerName);
            box[i] = pokemon;
        }
        return box;
    }

}
