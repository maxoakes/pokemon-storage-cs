using System;

namespace PokemonStorage.Models;

    public class Move
    {
        public int Id { get; set; }
        public string Identifier { get; set; }
        public int Pp { get; set; }
        public int TimesIncreased { get; set; }

        public Move(int id, int pp, int timesIncreased)
        {
            Id = id;
            Identifier = Lookup.Moves.TryGetValue(id, out var name) ? name : string.Empty;
            Pp = pp;
            TimesIncreased = timesIncreased;
        }

        public override string ToString()
        {
            if (Id == 0) return "";
            return $"{Id}:{Identifier} ({Pp}p{TimesIncreased})";
        }
    }