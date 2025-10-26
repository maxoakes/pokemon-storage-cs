using System;

namespace PokemonStorage.Models;

public class Game
{
    public int GenerationId { get; set; }
    public int VersionId { get; set; }
    public int GameId { get; set; }
    public string GameName { get; set; }
    public Dictionary<int, int> IdMapping = [];

    public Game(int gameId, string gameName, int versionId, int generationId)
    {
        GameId = gameId;
        GameName = gameName;
        VersionId = versionId;
        GenerationId = generationId;
    }

    public override string ToString()
    {
        return $"{GameName} (Generation {GenerationId})";
    }
}
