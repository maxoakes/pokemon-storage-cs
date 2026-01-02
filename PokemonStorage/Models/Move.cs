using System.Data;
using Microsoft.Data.Sqlite;

namespace PokemonStorage.Models;

public class Move
{
    public ushort Id { get; set; }
    public string Identifier { get { return Lookup.GetIdentifierById("moves", Id); } }
    public byte Pp { get; set; }
    public byte TimesIncreased { get; set; }
    public byte SlotId { get; set; }

    public Move(ushort id, byte pp, byte timesIncreased, byte slotId)
    {
        Id = id;
        Pp = pp;
        TimesIncreased = timesIncreased;
        SlotId = slotId;
    }

    public int InsertIntoDatabase(int pokemonId)
    {
        List<SqliteParameterPair> parameterPairs =
        [
            new SqliteParameterPair("pokemon_id", SqliteType.Integer, pokemonId),
            new SqliteParameterPair("slot_id", SqliteType.Integer, SlotId),
            new SqliteParameterPair("move_id", SqliteType.Integer, Id),
            new SqliteParameterPair("move_pp", SqliteType.Integer, Pp),
            new SqliteParameterPair("times_increased", SqliteType.Integer, TimesIncreased),
        ];

        return DbInterface.InsertIntoDatabase("move_set", parameterPairs, "storage");
    }

    public void LoadFromDatabase(Int64 id)
    {
        List<SqliteParameter> parameters = 
        [
            new SqliteParameter("id", SqliteType.Integer) { Value = id }
        ];

        DataTable dataTable = DbInterface.RetrieveTable("SELECT * FROM move_set WHERE id = @id", "storage", parameters);
        if (dataTable.Rows.Count == 0)
        {
            throw new Exception($"No move with {id} found in database.");
        }

        foreach (DataRow row in dataTable.Rows)
        {
            Id = (ushort)row.Field<Int64>("move_id");
            SlotId = (byte)row.Field<Int64>("slot_id");
            Pp = (byte)row.Field<Int64>("move_pp");
            TimesIncreased = (byte)row.Field<Int64>("times_increased");
        }
    }

    public override string ToString()
    {
        if (Id == 0) return "";
        return $"{Id}:{Identifier} ({Pp}p{TimesIncreased})";
    }
}