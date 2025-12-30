using System;
using Microsoft.Data.Sqlite;

namespace PokemonStorage.Models;

    public class Move
    {
        public ushort Id { get; set; }
        public string Identifier { get { return Lookup.GetIdentifierById("moves", Id); } }
        public byte Pp { get; set; }
        public byte TimesIncreased { get; set; }

        public Move(ushort id, byte pp, byte timesIncreased)
        {
            Id = id;
            Pp = pp;
            TimesIncreased = timesIncreased;
        }

        public List<SqliteParameter> GetSqliteParameters()
        {
            return new List<SqliteParameter>
            {
                new SqliteParameter("MoveId", SqliteType.Integer) { Value = Id },
                new SqliteParameter("Pp", SqliteType.Integer) { Value = Pp },
                new SqliteParameter("TimesIncreased", SqliteType.Integer) { Value = TimesIncreased }
            };
        }

        public override string ToString()
        {
            if (Id == 0) return "";
            return $"{Id}:{Identifier} ({Pp}p{TimesIncreased})";
        }
    }