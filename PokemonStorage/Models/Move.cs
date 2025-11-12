using System;

namespace PokemonStorage.Models;

    public class Move
    {
        public int Id { get; set; }
        public string Identifier { get { return Lookup.Moves.TryGetValue(Id, out var name) ? name : string.Empty; } }
        public int Pp { get; set; }
        public int TimesIncreased { get; set; }

        public Move(int id, int pp, int timesIncreased)
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