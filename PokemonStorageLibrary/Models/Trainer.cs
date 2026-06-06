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

        DataTable dataTable = DbInterface.RetrieveTable($"SELECT * FROM original_trainer WHERE id = @Id", Lookup.DefaultStorageConnectionString, parameters);
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

    public int InsertIntoDatabase(string connectionString)
    {
        List<SqliteParameterPair> parameterPairs =
        [
            new SqliteParameterPair("name", SqliteType.Text, Name),
            new SqliteParameterPair("gender", SqliteType.Integer, (int)Gender),
            new SqliteParameterPair("public_id", SqliteType.Integer, PublicId),
            new SqliteParameterPair("secret_id", SqliteType.Integer, SecretId)
        ];

        return DbInterface.InsertIntoDatabase("original_trainer", parameterPairs, connectionString);
    }

    public Int64? GetDatabasePrimaryKey(string connectionString)
    {
        List<SqliteParameter> parameters = [
            new SqliteParameter("Name", SqliteType.Text) { Value = Name },
            new SqliteParameter("PublicId", SqliteType.Integer) { Value = PublicId },
            new SqliteParameter("SecretId", SqliteType.Integer) { Value = SecretId },
        ];

        return (long?)DbInterface.RetrieveScalar("SELECT id FROM original_trainer ot WHERE ot.name=@Name AND ot.public_id=@PublicId AND ot.secret_id=@SecretId", connectionString, parameters);
    }

    public bool IsTrainerUsedByPokemon(string connectionString)
    {
        Int64? thisPrimaryKey = GetDatabasePrimaryKey(connectionString);
        if (thisPrimaryKey.HasValue)
        {
            List<SqliteParameter> parameters = [
                new SqliteParameter("PrimaryKey", SqliteType.Integer) { Value = thisPrimaryKey.Value },
            ];
            long result = (long?)DbInterface.RetrieveScalar("SELECT count(*) FROM pokemon p LEFT JOIN original_trainer ot ON p.fk_original_trainer = ot.id WHERE p.fk_original_trainer=@PrimaryKey GROUP BY p.fk_original_trainer", connectionString, parameters) ?? 0;
            return result > 0;
        }
        else
        {
            return false;
        }
    }

    public int DeleteFromDatabase(string connectionString)
    {
        Int64? thisPrimaryKey = GetDatabasePrimaryKey(connectionString);
        if (thisPrimaryKey.HasValue)
        {
            List<SqliteParameter> parameters = [
                new SqliteParameter("PrimaryKey", SqliteType.Integer) { Value = thisPrimaryKey.Value },
            ];

            return DbInterface.ExecuteStatement("DELETE FROM original_trainer WHERE id=@PrimaryKey", connectionString, parameters);
        }
        else
        {
            return -1;
        }
    }

    public override string ToString()
    {
        return $"{Name} ({Gender}) ID:{PublicId}_{SecretId}";
    }
}
