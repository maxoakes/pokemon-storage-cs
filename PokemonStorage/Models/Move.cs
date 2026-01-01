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

    public int InsertIntoDatabase(int pokemonId, int slot)
    {
        List<SqliteParameterPair> parameterPairs =
        [
            new SqliteParameterPair("pokemon_id", SqliteType.Integer, pokemonId),
            new SqliteParameterPair("slot_id", SqliteType.Integer, slot),
            new SqliteParameterPair("move_id", SqliteType.Integer, Id),
            new SqliteParameterPair("move_pp", SqliteType.Integer, Pp),
            new SqliteParameterPair("times_increased", SqliteType.Integer, TimesIncreased),
        ];

        return DbInterface.InsertIntoDatabase("move_set", parameterPairs, "storage");
    }

    public override string ToString()
    {
        if (Id == 0) return "";
        return $"{Id}:{Identifier} ({Pp}p{TimesIncreased})";
    }
}