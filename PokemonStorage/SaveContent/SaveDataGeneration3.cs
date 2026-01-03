using Microsoft.Extensions.Logging;
using PokemonStorage.Models;

namespace PokemonStorage.SaveContent;

public class SaveDataGeneration3 : SaveData
{
    public SaveDataGeneration3(byte[] content, Game game, string language) : base(content, game, language)
    {
    }

    public override bool AreAllChecksumsValid()
    {
        return true;
    }

    public override Trainer ParseOriginalTrainer()
    {
        byte[] save1 = Utility.GetBytes(OriginalData, 0x0000, 57344);
        byte[] save2 = Utility.GetBytes(OriginalData, 0xE000, 57344);
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
        return Trainer;
    }

    /// <summary>
    /// Fills GameState.Party with the party Pokemon parsed from the save file content.
    /// </summary>
    public override void ParsePartyPokemon()
    {
        byte[] save1 = Utility.GetBytes(OriginalData, 0x0000, 57344);
        byte[] save2 = Utility.GetBytes(OriginalData, 0xE000, 57344);
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

        /// <summary>
    /// Fills GameState.BoxList with the box Pokemon parsed from the save file content.
    /// </summary>
    public override void ParseBoxPokemon()
    {
        byte[] save1 = Utility.GetBytes(OriginalData, 0x0000, 57344);
        byte[] save2 = Utility.GetBytes(OriginalData, 0xE000, 57344);
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
                byte[] pokemonBytes = Utility.GetBytes(thisBoxBytes, j * 80, 80);
                uint thisPv = Utility.GetUnsignedNumber<uint>(pokemonBytes, 0x0, 4);
                ushort thisCs = Utility.GetUnsignedNumber<ushort>(pokemonBytes, 0x1C, 2);
                if (thisPv == 0 && thisCs == 0) continue;
                
                PartyPokemon pokemon = new(3);
                pokemon.LoadFromGen3Bytes(pokemonBytes, Game, Language);
                BoxList[thisBoxName][j] = pokemon;
            }
        }
    }
}