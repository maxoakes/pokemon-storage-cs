using System;

namespace PokemonStorage.Models;

    public class Move
    {
        public ushort Id { get; set; }
        public string Identifier { get { return Lookup.Moves.TryGetValue(Id, out var name) ? name : string.Empty; } }
        public byte Pp { get; set; }
        public byte TimesIncreased { get; set; }

        public Move(ushort id, byte pp, byte timesIncreased)
        {
            Id = id;
            Pp = pp;
            TimesIncreased = timesIncreased;
        }

        public override string ToString()
        {
            if (Id == 0) return "";
            return $"{Id}:{Identifier} ({Pp}p{TimesIncreased})";
        }
    }