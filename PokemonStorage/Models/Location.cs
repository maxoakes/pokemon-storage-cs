using System;

namespace PokemonStorage.Models;

public class Location
{
    public int Id { get; set; }
    public string Name { get; set; }
    public Dictionary<int, int> IdMapping = [];

    public Location(int id, string identifer)
    {
        Id = id;
        Name = identifer;
    }
}
