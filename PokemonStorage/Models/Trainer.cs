using System;

namespace PokemonStorage.Models;

public class Trainer
{
    public string Name { get; set; }
    public Gender Gender { get; set; }
    public byte PublicId { get; set; }
    public byte SecretId { get; set; }

    public Trainer(string name, int gender, byte publicId, byte secretId)
    {
        Name = name;
        Gender = (Gender)gender;
        PublicId = publicId;
        SecretId = secretId;
    }

    public override string ToString()
    {
        return $"{Name} ({Gender}) ID:{PublicId}_{SecretId}";
    }
}
