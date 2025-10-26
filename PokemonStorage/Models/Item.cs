using System;

namespace PokemonStorage.Models;

public class Item
{
    public int Id { get; set; }
    public string Name { get; set; }
    public Dictionary<int, int> IdMapping = [];

    public Item(int id, string identifer)
    {
        Id = id;
        Name = identifer;
    }
}
