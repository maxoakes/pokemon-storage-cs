using Microsoft.Extensions.Logging;
using PokemonStorage.Models;

namespace PokemonStorage.SaveContent;

public class SaveDataGeneration4 : SaveData
{
    public SaveDataGeneration4(byte[] content, Game game, string language) : base(content, game, language)
    {
    }

    public override bool AreAllChecksumsValid()
    {
        return true;
    }

    public override Trainer ParseOriginalTrainer()
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

        byte[] littleBlockBytes = Utility.GetBytes(OriginalData, littleBlockOffsets.start, littleBlockOffsets.end - littleBlockOffsets.start);

        Trainer = new(
            Utility.GetEncodedString(Utility.GetBytes(littleBlockBytes, trainerNameOffset, 16), Game, Language),
            Utility.GetUnsignedNumber<byte>(littleBlockBytes, trainerPublicIdOffset, 1),
            Utility.GetUnsignedNumber<ushort>(littleBlockBytes, trainerSecretIdOffset, 2),
            Utility.GetUnsignedNumber<ushort>(littleBlockBytes, trainerGenderOffset, 2)
        );

        return Trainer;
    }

    /// <summary>
    /// Fills GameState.Party with the party Pokemon parsed from the save file content.
    /// </summary>
    public override void ParsePartyPokemon()
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

        byte[] littleBlockBytes = Utility.GetBytes(OriginalData, littleBlockOffsets.start, littleBlockOffsets.end - littleBlockOffsets.start);

        int partySize = Utility.GetUnsignedNumber<byte>(littleBlockBytes, partySizeOffset, 1);
        byte[] partyBytes = Utility.GetBytes(littleBlockBytes, partyOffset, 1416);

        for (int i = 0; i < partySize; i++)
        {
            PartyPokemon pokemon = new(4);
            byte[] pokemonBytes = Utility.GetBytes(partyBytes, i * 236, 236);
            pokemon.LoadFromGen4Bytes(pokemonBytes, Game, Language);
            Party[i] = pokemon;
        }
    }

        /// <summary>
    /// Fills GameState.BoxList with the box Pokemon parsed from the save file content.
    /// </summary>
    public override void ParseBoxPokemon()
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

        byte[] bigBlockBytes = Utility.GetBytes(OriginalData, bigBlockOffsets.start, bigBlockOffsets.end - bigBlockOffsets.start);

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
                uint thisPv = Utility.GetUnsignedNumber<uint>(thisBoxBytes, (j * 136) + 0, 4);
                ushort thisCs = Utility.GetUnsignedNumber<ushort>(thisBoxBytes, (j * 136) + 6, 2);
                if (thisPv == 0 && thisCs == 0) continue;
                
                PartyPokemon pokemon = new(4);
                byte[] pokemonBytes = Utility.GetBytes(thisBoxBytes, j * 136, 136);
                pokemon.LoadFromGen4Bytes(pokemonBytes, Game, Language);
                BoxList[boxName][j] = pokemon;
            }
        }
    }
}