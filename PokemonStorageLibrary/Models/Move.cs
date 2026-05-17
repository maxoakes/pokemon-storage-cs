using System.Data;
using Microsoft.Data.Sqlite;

namespace PokemonStorageLibrary.Models;

public class Move
{
    public ushort Id { get; set; }
    public DatabaseIdentity Identity { get { return Lookup.GetDatabaseIdentityById(Id, DatabaseObject.Moves); } }
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

    public Move(Int64 movePrimaryKey)
    {
        List<SqliteParameter> parameters = 
        [
            new SqliteParameter("id", SqliteType.Integer) { Value = movePrimaryKey }
        ];

        DataTable dataTable = DbInterface.RetrieveTable("SELECT * FROM move_set WHERE id = @id", Lookup.DefaultStorageConnectionString, parameters);
        if (dataTable.Rows.Count == 0)
        {
            throw new Exception($"No move with {movePrimaryKey} found in database.");
        }

        foreach (DataRow row in dataTable.Rows)
        {
            Id = (ushort)row.Field<Int64>("move_id");
            SlotId = (byte)row.Field<Int64>("slot_id");
            Pp = (byte)row.Field<Int64>("move_pp");
            TimesIncreased = (byte)row.Field<Int64>("times_increased");
        }
    }

    public byte GetPpAmount(byte increasedAmount)
    {
        List<SqliteParameter> parameters = [
            new SqliteParameter("Id", SqliteType.Integer) { Value = Id }
        ];
        int baseline = (int)(Int64)DbInterface.RetrieveScalar("SELECT PP FROM moves WHERE id=@Id", Lookup.VeekunConnectionString, parameters);
        return (byte)(baseline * (1 + (increasedAmount * 0.2f)));
    }

    public byte GetGenerationId()
    {
        if (Id == 0) return 0;
        
        List<SqliteParameter> parameters = [
            new SqliteParameter("Id", SqliteType.Integer) { Value = Id }
        ];
        return (byte)(Int64)DbInterface.RetrieveScalar("SELECT generation_id FROM moves WHERE id=@Id", Lookup.VeekunConnectionString, parameters);
    }

    public int InsertIntoDatabase(int pokemonPrimaryKey)
    {
        List<SqliteParameterPair> parameterPairs =
        [
            new SqliteParameterPair("pokemon_id", SqliteType.Integer, pokemonPrimaryKey),
            new SqliteParameterPair("slot_id", SqliteType.Integer, SlotId),
            new SqliteParameterPair("move_id", SqliteType.Integer, Id),
            new SqliteParameterPair("move_pp", SqliteType.Integer, Pp),
            new SqliteParameterPair("times_increased", SqliteType.Integer, TimesIncreased),
        ];

        return DbInterface.InsertIntoDatabase("move_set", parameterPairs, Lookup.DefaultStorageConnectionString);
    }

    public override string ToString()
    {
        if (Id == 0) return "";
        return $"{Id}:{Identity} ({Pp}p{TimesIncreased})";
    }
}