using System;
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

        switch (Game.VersionId)
        {
            case 1:
            case 2:
                string playerName = Utility.GetEncodedString(Utility.GetBytes(content, 0x2598, 11), Game.VersionId, language);
                ushort playerId = Utility.GetUnsignedShort(content, 0x2605, true);
                Trainer = new(playerName, (int)Gender.MALE, playerId, 0);
                Program.Logger.LogInformation(Trainer.ToString());

                byte[] partyBytes = Utility.GetBytes(content, 0x2F2C, 0x194);
                Party = GetPokemonFromStorageGen1(partyBytes, Game.VersionId, language, 0x8, 0x2C, 0x110, 0x152);
                return;
            default:
                return;
        }
    }
    
    private Dictionary<int, PartyPokemon> GetPokemonFromStorageGen1(byte[] content, int version, string lang, int pokemonOffset, int pokemonSize, int trainerNameOffset, int nicknamesOffset)
    {
        Dictionary<int, PartyPokemon> box = [];
        byte boxCount = Utility.GetByte(content, 0x00);

        for (int i = 0; i < boxCount; i++)
        {
            byte[] nicknameBytes = Utility.GetBytes(content, nicknamesOffset + (0xB * i), 0xB);
            string nickname = Utility.GetEncodedString(nicknameBytes, version, lang);

            byte[] originalTrainerNameBytes = Utility.GetBytes(content, trainerNameOffset + (0xB * i), 0xB);
            string originalTrainerName = Utility.GetEncodedString(originalTrainerNameBytes, version, lang);

            byte[] pokemonBytes = Utility.GetBytes(content, pokemonOffset + (pokemonSize * i), 0x21);
            PartyPokemon pokemon = new PartyPokemon(1);
            pokemon.LoadFromGen1Bytes(pokemonBytes, version, nickname, originalTrainerName);
            box[i] = pokemon;
        }
        return box;
        
    }

}
