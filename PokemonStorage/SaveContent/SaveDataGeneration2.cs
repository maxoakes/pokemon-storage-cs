using Microsoft.Extensions.Logging;
using PokemonStorage.Models;

namespace PokemonStorage.SaveContent;

public class SaveDataGeneration2 : SaveData
{
    public SaveDataGeneration2(byte[] content, Game game, string language) : base(content, game, language)
    {
    }

    public override bool AreAllChecksumsValid()
    {
        return true;
    }

    public override Trainer ParseOriginalTrainer()
    {
        string playerName = Utility.GetEncodedString(Utility.GetBytes(OriginalData, 0x200B, 11), Game, Language);
        ushort playerId = Utility.GetUnsignedNumber<ushort>(OriginalData, 0x2009, 2, true);
        Trainer = new(
            playerName,
            Game.VersionId == 4 && playerId % 1 == 1 ? (int)Gender.FEMALE : (int)Gender.MALE,
            playerId,
            0
        );
        return Trainer;
    }
    

    /// <summary>
    /// Fills GameState.Party with the party Pokemon parsed from the save file content.
    /// </summary>
    public override void ParsePartyPokemon()
    {
        int partyOffset = Game.VersionId == 3 ? 0x288A : 0x2865;
        byte[] partyBytes = Utility.GetBytes(OriginalData, partyOffset, 428);
        Party = GetPokemonFromStorageGen2(partyBytes, Language, 6, 48);
    }

        /// <summary>
    /// Fills GameState.BoxList with the box Pokemon parsed from the save file content.
    /// </summary>
    public override void ParseBoxPokemon()
    {
        int boxSize = 0x462;
        int[] boxOffets = [0x4000, 0x4450, 0x48A0, 0x4CF0, 0x5140, 0x5590, 0x59E0, 0x6000, 0x6450, 0x68A0, 0x6CF0, 0x7140, 0x7590, 0x79E0];

        for (int i = 0; i < boxOffets.Length; i++)
        {
            byte[] boxBytes = Utility.GetBytes(OriginalData, boxOffets[i], boxSize);
            string boxName = $"Box{i+1}";
            BoxList[boxName] = GetPokemonFromStorageGen2(boxBytes, Language, 20, 32);
        }
        return;
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
            PartyPokemon pokemon = new(Game);
            pokemon.LoadFromGen2Bytes(pokemonBytes, Game, nickname, originalTrainerName, lang);
            box[i] = pokemon;
        }
        return box;
    }

    public override PartyPokemon GetPartyPokemonFromBoxBytes(byte[] data)
    {
        throw new NotImplementedException();
    }
}