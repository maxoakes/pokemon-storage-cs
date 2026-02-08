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
            new(content, game, false),
            new(content, game, false, true),
        ];
        StorageBlocks =
        [
            new(content, game, true),
            new(content, game, true, true),
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
        (int start, int end) littleBlockOffsets = (0x00000, 0x00000);
        int trainerNameOffset = Game.VersionGroupId == 10 ? 0x64 : 0x68;
        int trainerPublicIdOffset = Game.VersionGroupId == 10 ? 0x74 : 0x78;
        int trainerSecretIdOffset = Game.VersionGroupId == 10 ? 0x76 : 0x7A;
        int trainerGenderOffset = Game.VersionGroupId == 10 ? 0x7C : 0x80;

        if (Game.VersionGroupId == 8)
        {
            littleBlockOffsets = (0x0000, 0x0C0FF);
        }
        else if (Game.VersionGroupId == 9)
        {
            littleBlockOffsets = (0x00000, 0x0CF2B);
        }
        else if (Game.VersionGroupId == 10)
        {
            littleBlockOffsets = (0x00000, 0x0F6FF);
        }

        byte[] littleBlockBytes = Utility.GetBytes(OriginalData, littleBlockOffsets.start, littleBlockOffsets.end - littleBlockOffsets.start);

        Trainer = new(
            Utility.GetDecodedString(Utility.GetBytes(littleBlockBytes, trainerNameOffset, 16), Game, Language),
            Utility.GetUnsignedNumber<byte>(littleBlockBytes, trainerPublicIdOffset, 1),
            Utility.GetUnsignedNumber<ushort>(littleBlockBytes, trainerSecretIdOffset, 2),
            Utility.GetUnsignedNumber<ushort>(littleBlockBytes, trainerGenderOffset, 2)
        );
    }

    /// <summary>
    /// Fills GameState.Party with the party Pokemon parsed from the save file content.
    /// </summary>
    protected override void ParsePartyPokemon()
    {
        (int start, int end) littleBlockOffsets = (0x00000, 0x00000);
        int partySizeOffset = Game.VersionGroupId == 10 ? 0x94 : 0x9C;
        int partyOffset = Game.VersionGroupId == 10 ? 0x98 : 0xA0;

        if (Game.VersionGroupId == 8)
        {
            littleBlockOffsets = (0x0000, 0x0C0FF);
        }
        else if (Game.VersionGroupId == 9)
        {
            littleBlockOffsets = (0x00000, 0x0CF2B);
        }
        else if (Game.VersionGroupId == 10)
        {
            littleBlockOffsets = (0x00000, 0x0F6FF);
        }

        byte[] littleBlockBytes = Utility.GetBytes(OriginalData, littleBlockOffsets.start, littleBlockOffsets.end - littleBlockOffsets.start);

        int partySize = Utility.GetUnsignedNumber<byte>(littleBlockBytes, partySizeOffset, 1);
        byte[] partyBytes = Utility.GetBytes(littleBlockBytes, partyOffset, 1416);

        for (int i = 0; i < partySize; i++)
        {
            PartyPokemon pokemon = new(Game);
            byte[] pokemonBytes = Utility.GetBytes(partyBytes, i * 236, 236);
            pokemon.LoadFromGen4Bytes(pokemonBytes, Game, Language);
            Party[i] = pokemon;
        }
    }

        /// <summary>
    /// Fills GameState.BoxList with the box Pokemon parsed from the save file content.
    /// </summary>
    protected override void ParseBoxPokemon()
    {
        (int start, int end) bigBlockOffsets = (0x00000, 0x00000);

        if (Game.VersionGroupId == 8)
        {
            bigBlockOffsets = (0x0C100, 0x1E2DF);
        }
        else if (Game.VersionGroupId == 9)
        {
            bigBlockOffsets = (0x0CF2C, 0x1F10F);
        }
        else if (Game.VersionGroupId == 10)
        {
            bigBlockOffsets = (0x0F700, 0x21A10);
        }

        byte[] bigBlockBytes = Utility.GetBytes(OriginalData, bigBlockOffsets.start, bigBlockOffsets.end - bigBlockOffsets.start);

        int boxSize = (Game.VersionGroupId == 10) ? 0x1000 : 0xFF0;
        for (int i = 0; i < 18; i++)
        {
            int pokemonOffset = (Game.VersionGroupId == 10) ? 0x00 : 0x04;
            int boxNameOffset = (Game.VersionGroupId == 10) ? 0x12008 : 0x11EE4;
            
            byte[] boxNameBytes = Utility.GetBytes(bigBlockBytes, boxNameOffset + (i * 40), 40);
            string boxName = Utility.GetDecodedString(boxNameBytes, Game, Language);
            if (!BoxList.ContainsKey(boxName)) BoxList.Add(boxName, []);
            byte[] thisBoxBytes = Utility.GetBytes(bigBlockBytes, pokemonOffset + (boxSize * i), 136 * 30);

            for (int j = 0; j < 30; j++)
            {
                uint thisPv = Utility.GetUnsignedNumber<uint>(thisBoxBytes, (j * 136) + 0, 4);
                ushort thisCs = Utility.GetUnsignedNumber<ushort>(thisBoxBytes, (j * 136) + 6, 2);
                if (thisPv == 0 && thisCs == 0) continue;
                
                PartyPokemon pokemon = new(Game);
                byte[] pokemonBytes = Utility.GetBytes(thisBoxBytes, j * 136, 136);
                pokemon.LoadFromGen4Bytes(pokemonBytes, Game, Language);
                BoxList[boxName][j] = pokemon;
            }
        }
    }

    public override PartyPokemon GetPartyPokemonFromBoxBytes(byte[] data)
    {
        return null;
    }

    public override void PrintPokedex()
    {
        return;
    }

    public override void WriteToPokedex(int nationalIndex, bool seen = true, bool owned = true)
    {
        return;
    }

    public override byte[] GetBoxBytesFromPartyPokemon(PartyPokemon p)
    {
        return [];
    }

    public override int AddPokemonToNextOpenBox(PartyPokemon pokemon)
    {
        return -1;
    }
}