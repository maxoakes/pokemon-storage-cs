using System.Data;
using Microsoft.Data.Sqlite;

namespace PokemonStorage.DatabaseIO
{
    public static class DbInterface
    {
        #region Private Methods

        /// <summary>
        /// Get an SqliteCommand based on a query.
        /// </summary>
        /// <param name="query">Raw Sqlite statement</param>
        /// <returns></returns>
        private static SqliteCommand PrepareSqlCommand(string query, string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString)) throw new Exception("No connection string specified");

            SqliteConnection connection = new(connectionString);
            SqliteCommand command = new(query, connection);
            return command;
        }

        /// <summary>
        /// Given SqliteParameter, conform the value to Sqlite database standards. 
        /// Incudes changing null values to DBNull, trimming strings to maximum varchar lengths, 
        /// and catching out of range dates.
        /// </summary>
        /// <param name="input">Unconformed SqliteParameter</param>
        /// <returns>SqlParameter ready for Sqlite statement</returns>
        private static object PrepareParameterValue(SqliteParameter input)
        {
            if (input.Value == null)
            {
                return DBNull.Value;
            }
            if (input.SqliteType == SqliteType.Text)
            {
                string str = (string)input.Value;
                if (string.IsNullOrWhiteSpace(str))
                {
                    return DBNull.Value;
                }
                else
                {
                    return Utility.TruncateString(str, input.Size);
                }
            }
            return input.Value;
        }

        /// <summary>
        /// Execute a command in the Sqlite database that returns a scalar value.
        /// </summary>
        /// <param name="command">Fully created SqliteCommand that includes the Sqlite command, parameters, and connection string.</param>
        /// <returns>Result from the Sqlite scalar command</returns>
        private static object ExecuteScalar(SqliteCommand command)
        {
            object? result = null;
            try
            {
                using (command.Connection)
                {
                    command.Connection.Open();
                    result = command.ExecuteScalar();
                }
            }
            catch { throw; }

            return result;
        }

        /// <summary>
        /// Execute a command in the Sqlite database that does not return a value.
        /// </summary>
        /// <param name="command">Fully created SqliteCommand that includes the Sqlite command, parameters, and connection string.</param>
        /// <returns>Number of rows affected by the Sqlite command</returns>
        private static int ExecuteNonQuery(SqliteCommand command)
        {
            int result = -1;
            try
            {
                using (command.Connection)
                {
                    command.Connection.Open();
                    result = command.ExecuteNonQuery();
                }
            }
            catch { throw; }

            return result;
        }

        #endregion

        #region Read Database

        /// <summary>
        /// Get a DataTable based on an Sqlite select statement, and optionally a list of parameters used within the Sqlite statement.
        /// </summary>
        /// <param name="query">SQL select statement</param>
        /// <param name="parameters">List of SqliteParameters used in the Sqlite select statement</param>
        /// <param name="isStoredProcedure">True if the query parameter is the name of a table-returning stored procedure, 
        /// false if it is a whole select statement string</param>
        /// <returns></returns>
        public static DataTable RetrieveTable(string query, string connectionString, List<SqliteParameter>? parameters = null, bool isStoredProcedure = false)
        {
            SqliteCommand command = PrepareSqlCommand(query, connectionString);
            if (isStoredProcedure)
            {
                command.CommandType = CommandType.StoredProcedure;
            }

            if (parameters != null)
            {
                command.Parameters.AddRange(parameters.ToArray());
            }
            
            DataTable dataTable;
            using (SqliteDataReader dr = command.ExecuteReader())
            {
                do
                {
                    dataTable = new DataTable();
                    dataTable.BeginLoadData();
                    dataTable.Load(dr);
                    dataTable.EndLoadData();

                } while (!dr.IsClosed && dr.NextResult());
            }

            return dataTable;
        }

        /// <summary>
        /// Get a single value from a query.
        /// </summary>
        /// <param name="query">SQL statement</param>
        /// <param name="parameters">List of SqliteParameters used in the Sqlite select statement</param>
        /// <param name="isStoredProcedure">True if the query parameter is the name of a table-returning stored procedure, 
        /// false if it is a whole select statement string</param>
        /// <returns></returns>
        public static object RetrieveScalar(string query, string connectionString, List<SqliteParameter>? parameters = null, bool isStoredProcedure = false)
        {
            SqliteCommand command = PrepareSqlCommand(query, connectionString);
            if (isStoredProcedure)
            {
                command.CommandType = CommandType.StoredProcedure;
            }

            if (parameters != null)
            {
                command.Parameters.AddRange(parameters.ToArray());
            }

            return ExecuteScalar(command);

        }

        /// <summary>
        /// Query a table and get the next highest integer for a given column. Used to get the next available primary key for tables that to not use identities.
        /// </summary>
        /// <param name="table">Name of the Sqlite table.</param>
        /// <param name="column">Name of the column in the Sqlite table.</param>
        /// <returns></returns>
        public static int GetNextHighestNumber(string table, string column, string connectionString)
        {
            SqliteCommand command = PrepareSqlCommand($"SELECT MAX({column}) FROM {table}", connectionString);
            int result = (int)ExecuteScalar(command);
            return result + 1;
        }

        #endregion

        #region Write Database

        /// <summary>
        /// Execute an Sqlite scalar command given a statement and list of parameters
        /// </summary>
        /// <param name="statement">SQL statement</param>
        /// <param name="parameters">Raw Sqlite parameters</param>
        /// <returns></returns>
        public static object InsertAndGetPrimaryKey(string statement, List<SqliteParameter> parameters, string connectionString)
        {
            SqliteCommand command = PrepareSqlCommand(statement, connectionString);
            parameters.ForEach(e => e.Value = PrepareParameterValue(e));
            command.Parameters.AddRange(parameters.ToArray());

            return ExecuteScalar(command);
        }

        /// <summary>
        /// Execute an Sqlite command given a statement and list of parameters.
        /// </summary>
        /// <param name="statement">SQL statement</param>
        /// <param name="parameters">Raw Sqlite parameters</param>
        /// <returns>Number of rows affected</returns>
        public static int UpdateAndGetResult(string statement, List<SqliteParameter> parameters, string connectionString)
        {
            SqliteCommand command = PrepareSqlCommand(statement, connectionString);
            parameters.ForEach(e => e.Value = PrepareParameterValue(e));
            command.Parameters.AddRange(parameters.ToArray());

            return ExecuteNonQuery(command);
        }

        #endregion

        #region Utility

        public static void PrintSqlParameters(List<SqliteParameter> SqliteParameters)
        {
            SqliteParameters.ForEach(e => Console.WriteLine($"\t{e.ParameterName}:{e.SqliteType}({e.Size})={e.Value}"));
        }

        private static void PrintCommandStatement(SqliteCommand command)
        {
            Console.WriteLine(command.CommandText);
            PrintSqlParameters([.. command.Parameters.Cast<SqliteParameter>()]);
        }

        #endregion
    }
}