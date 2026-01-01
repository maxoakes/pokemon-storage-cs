using Microsoft.Data.Sqlite;

namespace PokemonStorage.Models;

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

    public int InsertIntoDatabase()
    {
        List<SqliteParameterPair> parameterPairs =
        [
            new SqliteParameterPair("name", SqliteType.Text, Name),
            new SqliteParameterPair("gender", SqliteType.Integer, (int)Gender),
            new SqliteParameterPair("public_id", SqliteType.Integer, PublicId),
            new SqliteParameterPair("secret_id", SqliteType.Integer, SecretId)
        ];

        return DbInterface.InsertIntoDatabase("original_trainer", parameterPairs, "storage");
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

        object primaryKey = DbInterface.RetrieveScalar("SELECT id FROM original_trainer WHERE public_id = @public_id AND secret_id = @secret_id", "storage", parameterPairs.Select(x => x.SqliteParameter).ToList());
        if (primaryKey == null || primaryKey == DBNull.Value)
        {
            return -1;
        }
        else
        {
            return Convert.ToInt32(primaryKey);
        }
    }

    public int GetDatabasePrimaryKey()
    {
        int primaryKey = GetDatabasePrimaryKeyIfExists();
        if (primaryKey < 0)
        {
            return (int)InsertIntoDatabase();
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
