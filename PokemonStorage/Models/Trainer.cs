using System;
using Microsoft.Data.Sqlite;
using PokemonStorage.DatabaseIO;

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

    public List<SqliteParameter> GetSqliteParameters()
    {
        return new List<SqliteParameter>
        {
            new SqliteParameter("Name", SqliteType.Text) { Value = Name, Size = 12 },
            new SqliteParameter("Gender", SqliteType.Integer) { Value = (int)Gender },
            new SqliteParameter("PublicId", SqliteType.Integer) { Value = PublicId },
            new SqliteParameter("SecretId", SqliteType.Integer) { Value = SecretId }
        };
    }

    public int GetDatabasePrimaryKey()
    {
        object primaryKey = DbInterface.RetrieveScalar("SELECT id FROM original_trainer WHERE PublicId = @PublicId AND SecretId = @SecretId", "storage",GetSqliteParameters());
        if (primaryKey == null || primaryKey == DBNull.Value)
        {
            return (int)DbInterface.RetrieveScalar("INSERT INTO original_trainer (Name, Gender, PublicId, SecretId) VALUES (@Name, @Gender, @PublicId, @SecretId); SELECT last_insert_rowid();", "storage", GetSqliteParameters());
        }
        else
        {
            return Convert.ToInt32(primaryKey);
        }
    }

    public override string ToString()
    {
        return $"{Name} ({Gender}) ID:{PublicId}_{SecretId}";
    }
}
