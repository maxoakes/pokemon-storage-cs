using System.Data;
using Microsoft.Data.Sqlite;

namespace PokemonStorageLibrary.Models;

public class Trainer
{
    public string Name { get; set; }
    public Gender Gender { get; set; }
    public ushort PublicId { get; set; }
    public ushort SecretId { get; set; }

    public Trainer(string name, int gender, ushort publicId, ushort secretId)
    {
        Name = name;
        Gender = (Gender)gender;
        PublicId = publicId;
        SecretId = secretId;
    }

    public Trainer(Int64 trainerPrimaryKey)
    {
        List<SqliteParameter> parameters = [
            new SqliteParameter("Id", SqliteType.Integer) { Value = trainerPrimaryKey }
        ];

        DataTable dataTable = DbInterface.RetrieveTable($"SELECT * FROM original_trainer WHERE id = @Id", Lookup.StorageConnectionString, parameters);
        if (dataTable.Rows.Count == 0)
        {
            throw new Exception($"No Original Trainer found with primary key {trainerPrimaryKey}");
        }

        foreach (DataRow row in dataTable.Rows)
        {
            Name = row.Field<string>("name") ?? "???";
            Gender = (Gender)row.Field<Int64>("gender");
            PublicId = (ushort)row.Field<Int64>("public_id");
            SecretId = (ushort)row.Field<Int64>("secret_id");
        }
    }

    public int InsertIntoDatabase()
    {
        List<SqliteParameterPair> parameterPairs =
        [
            new SqliteParameterPair("name", SqliteType.Text, Name),
            new SqliteParameterPair("gender", SqliteType.Integer, (int)Gender),
            new SqliteParameterPair("public_id", SqliteType.Integer, PublicId),
            new SqliteParameterPair("secret_id", SqliteType.Integer, SecretId)
        ];

        return DbInterface.InsertIntoDatabase("original_trainer", parameterPairs, Lookup.StorageConnectionString);
    }

    public int GetDatabasePrimaryKeyIfExists()
    {
        List<SqliteParameterPair> parameterPairs =
        [
            new SqliteParameterPair("name", SqliteType.Text, Name),
            new SqliteParameterPair("gender", SqliteType.Integer, (int)Gender),
            new SqliteParameterPair("public_id", SqliteType.Integer, PublicId),
            new SqliteParameterPair("secret_id", SqliteType.Integer, SecretId)
        ];

        object primaryKey = DbInterface.RetrieveScalar("SELECT id FROM original_trainer WHERE public_id = @public_id AND secret_id = @secret_id", Lookup.StorageConnectionString, parameterPairs.Select(x => x.SqliteParameter).ToList());
        if (primaryKey == null || primaryKey == DBNull.Value)
        {
            return -1;
        }
        else
        {
            return Convert.ToInt32(primaryKey);
        }
    }

    public int GetDatabasePrimaryKeyAndInsertIfNotExist()
    {
        int primaryKey = GetDatabasePrimaryKeyIfExists();
        if (primaryKey < 0)
        {
            return InsertIntoDatabase();
        }
        else
        {
            return primaryKey;
        }
    }

    public override string ToString()
    {
        return $"{Name} ({Gender}) ID:{PublicId}_{SecretId}";
    }
}
