using System.Data;
using MySql.Data.MySqlClient;

namespace PokemonStorage.DatabaseIO
{
    public static class DbInterface
    {
        #region MySql Database

        /// <summary>
        /// Get an MySqlCommand based on a query.
        /// </summary>
        /// <param name="query">Raw MySql statement</param>
        /// <returns></returns>
        public static MySqlCommand PrepareSqlCommand(string query)
        {
            if (string.IsNullOrWhiteSpace(Program.ConnectionString)) throw new Exception("No connection string specified");

            MySqlConnection connection = new(Program.ConnectionString);
            MySqlCommand command = new(query, connection);
            return command;
        }

        /// <summary>
        /// Given MySqlParameter, conform the value to MySql database standards. 
        /// Incudes changing null values to DBNull, trimming strings to maximum varchar lengths, 
        /// and catching out of range dates.
        /// </summary>
        /// <param name="input">Unconformed MySqlParameter</param>
        /// <returns>SqlParameter ready for MySql statement</returns>
        public static object PrepareParameterValue(MySqlParameter input)
        {
            if (input.Value == null)
            {
                return DBNull.Value;
            }
            if (input.MySqlDbType == MySqlDbType.VarChar ||
                input.MySqlDbType == MySqlDbType.LongText ||
                input.MySqlDbType == MySqlDbType.Text ||
                input.MySqlDbType == MySqlDbType.MediumText ||
                input.MySqlDbType == MySqlDbType.TinyText
                )
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
            if (input.MySqlDbType == MySqlDbType.DateTime ||
                input.MySqlDbType == MySqlDbType.Date)
            {

                if (DateTime.TryParse(input.Value.ToString(), out DateTime dateTime))
                {
                    return DBNull.Value;
                }

                try
                {
                    if (dateTime < DateTime.Parse("1/1/1000 12:00:00 AM") || (dateTime > DateTime.Parse("12/31/9999 11:59:59 PM")))
                    {
                        Console.WriteLine(string.Format("WARNING: MySqlDateTime overflow was caught and set to null: '{0}'", dateTime));
                        return DBNull.Value;
                    }
                }
                catch
                {
                    Console.WriteLine(string.Format("WARNING: Unknown MySqlDateTime was caught and set to null: '{0}'", input.Value));
                    return DBNull.Value;
                }
            }
            return input.Value;
        }

        #endregion

        #region Read Database

        /// <summary>
        /// Get a DataTable based on an MySql select statement, and optionally a list of parameters used within the MySql statement.
        /// </summary>
        /// <param name="query">SQL select statement</param>
        /// <param name="parameters">List of MySqlParameters used in the MySql select statement</param>
        /// <param name="isStoredProcedure">True if the query parameter is the name of a table-returning stored procedure, 
        /// false if it is a whole select statement string</param>
        /// <returns></returns>
        public static DataTable RetrieveTable(string query, List<MySqlParameter>? parameters = null, bool isStoredProcedure = false)
        {
            MySqlCommand command = PrepareSqlCommand(query);
            if (isStoredProcedure)
            {
                command.CommandType = CommandType.StoredProcedure;
            }

            if (parameters != null)
            {
                command.Parameters.AddRange(parameters.ToArray());
            }
            MySqlDataAdapter adapter = new(command);

            DataSet dataSet = new();
            adapter.Fill(dataSet);
            return dataSet.Tables[0];
        }

        /// <summary>
        /// Get a single value from a query.
        /// </summary>
        /// <param name="query">SQL statement</param>
        /// <param name="parameters">List of MySqlParameters used in the MySql select statement</param>
        /// <param name="isStoredProcedure">True if the query parameter is the name of a table-returning stored procedure, 
        /// false if it is a whole select statement string</param>
        /// <returns></returns>
        public static object RetrieveScalar(string query, List<MySqlParameter>? parameters = null, bool isStoredProcedure = false)
        {
            MySqlCommand command = PrepareSqlCommand(query);
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
        /// <param name="table">Name of the MySql table.</param>
        /// <param name="column">Name of the column in the MySql table.</param>
        /// <returns></returns>
        public static int GetNextHighestNumber(string table, string column)
        {
            MySqlCommand command = PrepareSqlCommand($"SELECT MAX({column}) FROM {table}");
            int result = (int)ExecuteScalar(command);
            return result + 1;
        }

        #endregion

        #region Write Database

        /// <summary>
        /// Execute a command in the MySql database that returns a scalar value.
        /// </summary>
        /// <param name="command">Fully created MySqlCommand that includes the MySql command, parameters, and connection string.</param>
        /// <returns>Result from the MySql scalar command</returns>
        public static object ExecuteScalar(MySqlCommand command)
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
        /// Execute a command in the MySql database that does not return a value.
        /// </summary>
        /// <param name="command">Fully created MySqlCommand that includes the MySql command, parameters, and connection string.</param>
        /// <returns>Number of rows affected by the MySql command</returns>
        public static int ExecuteNonQuery(MySqlCommand command)
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

        /// <summary>
        /// Execute an MySql scalar command given a statement and list of parameters
        /// </summary>
        /// <param name="statement">SQL statement</param>
        /// <param name="parameters">Raw MySql parameters</param>
        /// <returns></returns>
        public static object InsertAndGetPrimaryKey(string statement, List<MySqlParameter> parameters)
        {
            MySqlCommand command = PrepareSqlCommand(statement);
            parameters.ForEach(e => e.Value = PrepareParameterValue(e));
            command.Parameters.AddRange(parameters.ToArray());

            return ExecuteScalar(command);
        }

        /// <summary>
        /// Execute an MySql command given a statement and list of parameters.
        /// </summary>
        /// <param name="statement">SQL statement</param>
        /// <param name="parameters">Raw MySql parameters</param>
        /// <returns>Number of rows affected</returns>
        public static int UpdateAndGetResult(string statement, List<MySqlParameter> parameters)
        {
            MySqlCommand command = PrepareSqlCommand(statement);
            parameters.ForEach(e => e.Value = PrepareParameterValue(e));
            command.Parameters.AddRange(parameters.ToArray());

            return ExecuteNonQuery(command);
        }

        #endregion

        #region Utility

        public static void PrintSqlParameters(List<MySqlParameter> MySqlParameters)
        {
            MySqlParameters.ForEach(e => Console.WriteLine($"\t{e.ParameterName}:{e.MySqlDbType}({e.Size})={e.Value}"));
        }

        private static void PrintCommandStatement(MySqlCommand command)
        {
            Console.WriteLine(command.CommandText);
            PrintSqlParameters([.. command.Parameters.Cast<MySqlParameter>()]);
        }

        #endregion
    }
}