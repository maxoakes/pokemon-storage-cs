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
    public Dictionary<int, Dictionary<int, PartyPokemon>> BoxList { get; set; } = [];

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
                BoxList[i] = GetPokemonFromStorageGen1(boxBytes, Game.VersionId, language, 0x16, 0x21, 0x2AA, 0x386);
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
                BoxList[i] = GetPokemonFromStorageGen2(boxBytes, Game.VersionId, language, 20, 32);
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
