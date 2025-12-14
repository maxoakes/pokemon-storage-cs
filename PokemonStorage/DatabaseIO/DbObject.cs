using Microsoft.Data.Sqlite;
using PokemonStorage.DatabaseIO;

namespace PokemonStorage;

/// <summary>
/// An object representation of a row of a database table. Includes properties for a number primary key, created on/by, updated on/by, and
/// soft-delete. When SqlTableName, SqlPrimaryKeyName and GetSqlParameters() are mapped to the appropriate SQL tables and columns, children 
/// of this object can be easily saved to an SQL database.
/// </summary>
public abstract class DbObject
{
    /// <summary>
    /// English noun that describes this object in a sentance.
    /// </summary>
    public abstract string SingularNoun { get; }
    /// <summary>
    /// English noun that descibes multiple of this object in a sentance.
    /// </summary>
    public abstract string PluralNoun { get; }
    /// <summary>
    /// Name of the SQL database table that this class represents. One row of that table represents one of these objects.
    /// </summary>
    public abstract string SqlTableName { get; }
    /// <summary>
    /// Name of the column in the tabase table that is the primary key.
    /// </summary>
    public abstract string SqlPrimaryKeyName { get; }
    /// <summary>
    /// Primary key of the row that this object represents.
    /// </summary>
    public int PrimaryKey { get; protected set; }
    /// <summary>
    /// Represents the commonly used column in the database used to store who created the row.
    /// </summary>
    public string CreatedBy { get; set; }
    /// <summary>
    /// Represents the commonly used column in the database used to store when the row was first inserted.
    /// </summary>
    public DateTime? CreatedOn { get; set; }
    /// <summary>
    /// Represents the commonly used column in the database used to show who last updated the row.
    /// </summary>
    public string? UpdatedBy { get; set; }
    /// <summary>
    /// Represents the commonly used column in the database used to show when the row was last updated.
    /// </summary>
    public DateTime? UpdatedOn { get; set; }
    /// <summary>
    /// Represents the commonly used column in the database to indicate if the row has been soft-deleted.
    /// </summary>
    public bool Deleted { get; set; }

    // Used for objects edited directly by a DataGridView
    /// <summary>
    /// Helper property to indicate if this a new object that has not yet been inserted into the database.
    /// </summary>
    public bool IsNew { get; set; }
    /// <summary>
    /// Helper property to indicate if this object is deleted but has not yet been deleted in the database.
    /// </summary>
    public bool IsDeleted { get; set; }
    /// <summary>
    /// Helper property to indicate if this object has been edited but not yet committed to the database
    /// </summary>
    public bool IsEdited { get; set; }

    /// <summary>
    /// Constructor that can only be called when calling base(). Sets helper IsEdited, IsNew, IsDeleted to false and PK to 0
    /// </summary>
    public DbObject()
    {
        PrimaryKey = 0;
        IsEdited = false;
        IsNew = false;
        IsDeleted = false;
        CreatedBy = "Unknown";
        UpdatedBy = null;
    }

    #region Property Settings

    /// <summary>
    /// Set the primary key of this object.
    /// </summary>
    /// <param name="newKey">New primary key of this object</param>
    public int SetPrimaryKey(int newKey)
    {
        PrimaryKey = newKey;
        return PrimaryKey;
    }

    /// <summary>
    /// Atomically set the creator and the creation date of this object.
    /// </summary>
    /// <param name="username">Username of the creator.</param>
    /// <param name="dateTime">Time the object was created.</param>
    public void SetCreator(string username, DateTime dateTime)
    {
        CreatedBy = username;
        CreatedOn = dateTime;
    }

    /// <summary>
    /// Atomically set the last user who updated the object and when it was last updated.
    /// </summary>
    /// <param name="username">Username of the updater.</param>
    /// <param name="dateTime">Time the object was last updated.</param>
    public void SetUpdater(string username, DateTime dateTime)
    {
        UpdatedBy = username;
        UpdatedOn = dateTime;
    }

    #endregion

    #region SQL

    public abstract List<SqliteParameter> GetSqlParameters();

    /// <summary>
    /// Using SqlTableName, SqlPrimaryKeyName and GetSqlParameters(), build an SQL INSERT statement used to insert this object into the database
    /// </summary>
    /// <param name="isPrimaryKeyAssignedByDatabase">True if the database sets this object's primary key automatically (using an identity). False if the insert statement needs to explicitly incude the primary key value.</param>
    /// <returns></returns>
    public virtual string GetInsertStatement(bool isPrimaryKeyAssignedByDatabase = true)
    {
        string statement =
            "DECLARE @OutTable TABLE (id INT); " +
            "INSERT INTO " + SqlTableName + " ({0}) OUTPUT INSERTED." + SqlPrimaryKeyName + " INTO @OutTable VALUES ({1}); " +
            "SELECT id FROM @OutTable;";
        List<KeyValuePair<string, string>> list = [];
        foreach (var param in GetSqlParameters())
        {
            if (isPrimaryKeyAssignedByDatabase && param.ParameterName == SqlPrimaryKeyName)
            {
                continue;
            }

            string name = param.ParameterName;
            list.Add(new KeyValuePair<string, string>(name, $"@{name}"));
        }

        return string.Format(statement, String.Join(",", list.Select(e => e.Key)), String.Join(",", list.Select(e => e.Value)));
    }

    /// <summary>
    /// Using SqlTableName, SqlPrimaryKeyName, PrimaryKey and GetSqlParameters(), build an SQL UPDATE statement to update/overwrite this object to an existing object in the database.
    /// </summary>
    /// <returns></returns>
    public string GetUpdateStatement()
    {
        string statement = "UPDATE " + SqlTableName + " SET {0} WHERE " + SqlPrimaryKeyName + "=@" + SqlPrimaryKeyName + ";";
        List<string> sets = GetSqlParameters().Where(e => e.ParameterName != SqlPrimaryKeyName).Select(e => string.Format("{0} = @{0}", e.ParameterName)).ToList();

        return string.Format(statement, String.Join(", ", sets));
    }

    /// <summary>
    /// Using SqlTableName, SqlPrimaryKeyName, PrimaryKey and GetSqlParameters(), build an SQL DELETE statement to delete this object from the database.
    /// </summary>
    /// <returns></returns>
    public string GetDeleteStatement()
    {
        string statement = "DELETE FROM " + SqlTableName + " WHERE " + SqlPrimaryKeyName + "=@" + SqlPrimaryKeyName + ";";
        List<string> sets = GetSqlParameters().Where(e => e.ParameterName == SqlPrimaryKeyName).Select(e => string.Format("{0} = @{0}", e.ParameterName)).ToList();

        return statement;
    }

    /// <summary>
    /// Insert this object into the database.
    /// </summary>
    /// <param name="isPrimaryKeyAssignedByDatabase">True if the database assigns the primary key automatically, false if the insert 
    /// statement needs to explicitly include the primary key value.</param>
    /// <returns>Primary key of the newly inserted row</returns>
    public object InsertIntoDatabase(bool isPrimaryKeyAssignedByDatabase = true)
    {
        return DbInterface.InsertAndGetPrimaryKey(GetInsertStatement(isPrimaryKeyAssignedByDatabase), GetSqlParameters(), "storage");
    }

    /// <summary>
    /// Insert this object into the database.
    /// </summary>
    /// <param name="username">Username of the user who first created the row.</param>
    /// <param name="dateTime">Time that the row was first created.</param>
    /// <param name="isPrimaryKeyAssignedByDatabase">True if the database assigns the primary key automatically, false if the insert 
    /// statement needs to explicitly include the primary key value.</param>
    /// <returns></returns>
    public object InsertIntoDatabase(string username, DateTime dateTime, bool isPrimaryKeyAssignedByDatabase = true)
    {
        SetCreator(username, dateTime);
        return InsertIntoDatabase(isPrimaryKeyAssignedByDatabase);
    }

    /// <summary>
    /// Update this object into the database.
    /// </summary>
    /// <returns>Number of rows affected</returns>
    public int UpdateToDatabase()
    {
        return DbInterface.UpdateAndGetResult(GetUpdateStatement(), GetSqlParameters(), "storage");
    }

    /// <summary>
    /// Update this object into the database.
    /// </summary>
    /// <param name="username">Username of the user that last updated this object.</param>
    /// <param name="dateTime">Time that this object was last updated.</param>
    /// <returns>Number of rows affected</returns>
    public int UpdateToDatabase(string username, DateTime dateTime)
    {
        SetUpdater(username, dateTime);
        return UpdateToDatabase();
    }

    /// <summary>
    /// Delete this object from the database.
    /// </summary>
    /// <returns>Number of rows affected</returns>
    public int DeleteFromDatabase()
    {
        return DbInterface.UpdateAndGetResult(GetDeleteStatement(), GetSqlParameters(), "storage");
    }

    #endregion

    /// <summary>
    /// Gets the primary key of a DbObject if it is not null.
    /// </summary>
    /// <param name="obj">DbObject</param>
    /// <returns>Primary key of parameter DbObject. Null if object is null</returns>
    public static int? GetNullableForeignKey(object obj)
    {
        if (obj == null)
        {
            return null;
        }
        else
        {
            DbObject? dbObject = obj as DbObject;
            return dbObject?.PrimaryKey;
        }
    }

    public override string ToString()
    {
        string value = "";
        foreach (var item in GetType().GetProperties())
        {
            value += string.Format("{0}='{1}';", item.Name, item.GetValue(this));
        }
        return value;
    }
}