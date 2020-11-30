using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Text;
using System.Data;
using System.Data.Common;

namespace AnyBase
{
    /// <summary>
    /// .DLL Hell
    /// =========
    /// It is not necessary to add a reference to System.Data.SQLite in this solution or install the SQLite package however,
    /// It IS necessary to add the reference in the calling solution, which in this case is the Unit Test database. I assume that
    /// a reference will have to be added to PS and SC, and the following amendment to the app.config may be necessary....
    ///  <system.data>
    ///    <DbProviderFactories>
    ///  <remove invariant="System.Data.SQLite"/>
    ///  <add name="SQLite Data Provider" invariant="System.Data.SQLite"
    ///       description=".Net Framework Data Provider for SQLite"
    ///       type="System.Data.SQLite.SQLiteFactory, System.Data.SQLite" />
    ///    </DbProviderFactories>
    ///  </system.data>
    /// </summary>
    /// <remarks></remarks>
    internal class DatabaseCreator
    {
        internal static DatabaseAccess Factory(ConnectionDetail connectionDetail)
        {
            switch (connectionDetail.Provider)
            {
                case DatabaseProvider.MySql:
                    {
                        return new MySqlDatabase(connectionDetail);
                    }

                case DatabaseProvider.Sqlite:
                    {
                        return new SqliteDatabase(connectionDetail);
                    }

                case DatabaseProvider.SqlServer:
                    {
                        return new SqlServerDatabase(connectionDetail);
                    }

                default:
                    {
                        throw new NotImplementedException($"{connectionDetail.Provider} not catered for.");
                        break;
                    }
            }
        }
    }

    /// <summary>
    /// Connect to a database and perform generic operations on a database or its tables.
    /// </summary>
    /// <remarks></remarks>
    public abstract class DatabaseAccess
    {
        protected bool IsServerConnectionOnly;

        /// <summary>
        /// Constructor for specified database.
        /// </summary>
        protected DatabaseAccess(ConnectionDetail connectionDetail)
        {
            MyConnectionDetail = connectionDetail;

            // Create the database provider factory which will use to create connections, commands and exceptions, specific to the supported database type.
            DatabaseFactory = DbProviderFactories.GetFactory(Settings.AssembliesByProvider[connectionDetail.Provider]);
        }

        public ConnectionDetail MyConnectionDetail { get; }

        /// <summary>
        /// The database factory.
        /// </summary>
        internal DbProviderFactory DatabaseFactory { get; }

        /// <summary>
        /// Attempt to connect to the server and then the database.
        /// </summary>
        /// <remarks>Exception thrown if either the server or database do not exist or cannot be connected to.</remarks>
        public virtual void TestConnection()
        {
            using (var connection = OpenDatabaseConnection())
            {
            }
        }

        /// <summary>
        /// Check if a database exists.
        /// </summary>
        public virtual bool DatabaseExists()
        {
            var result = false;

            using (var connection = OpenServerConnection())
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = BuildDatabaseExistsSql();

                    var parameter = DatabaseFactory.CreateParameter();
                    parameter.ParameterName = "@schemaName";
                    parameter.Value = MyConnectionDetail.DatabaseName;
                    command.Parameters.Add(parameter);

                    // Execute the query to read to the datatable.
                    var sqlResult = command.ExecuteScalar();
                    result = System.Convert.ToInt32(sqlResult) > 0;
                }
            }

            return result;
        }

        /// <summary>
        /// Drop the database.
        /// </summary>
        /// <returns>True if dropped successfully.</returns>
        public virtual bool DropDatabase()
        {
            if (DatabaseExists())
            {
                // Drop database.
                using (var connection = OpenServerConnection())
                {
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = BuildDropDatabaseSql();
                        command.ExecuteNonQuery();
                    }
                }
            }

            // Verify it does not exist now.
            return !DatabaseExists();
        }

        /// <summary>
        /// Create the database.
        /// </summary>
        /// <returns>True if created successfully.</returns>
        /// <remarks></remarks>
        public virtual bool CreateDatabase()
        {
            using (var connection = OpenServerConnection())
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = BuildCreateDatabaseSql();
                    command.ExecuteNonQuery();
                }
            }

            // Verify it now exists.
            return DatabaseExists();
        }

        /// <summary>
        /// Check if a table exists.
        /// This generic query syntax works in both SQL Server and MySQL.
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        public virtual bool TableExists(string tableName)
        {
            var crud = new ReadQuery(
                this,
                "information_schema.tables",
                new DataTable(),
                new List<string>(),
                new List<string> {"table_catalog", "table_name"},
                new List<List<object>> {{new List<object> {MyConnectionDetail.DatabaseName, tableName}}});

            return crud.ReadResult.Data.Rows.Count > 0;
        }

        public virtual bool DropTable(string tableName)
        {
            if (TableExists(tableName))
            {
                using (var connection = OpenDatabaseConnection())
                {
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = BuildDropTableSql(tableName);
                        command.ExecuteNonQuery();
                    }
                }
            }

            // Verify it does not exist now.
            return !DatabaseExists();
        }

        /// <summary>
        /// Create a table using a blueprint.
        /// </summary>
        /// <param name="tableBlueprint"></param>
        /// <param name="recreateIfItAlreadyExists"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        public bool CreateTable(TableBlueprint tableBlueprint, bool recreateIfItAlreadyExists)
        {
            var tableDoesExist = TableExists(tableBlueprint.TableName);

            // If overwriting the table is specified the table already exists then drop it.
            if (recreateIfItAlreadyExists && tableDoesExist)
                DropTable(tableBlueprint.TableName);

            // If we are recreating or the table doesn't exist then create it.
            if (recreateIfItAlreadyExists || !tableDoesExist)
            {
                using (var connection = OpenDatabaseConnection())
                {
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = BuildCreateTableSql(tableBlueprint);
                        command.ExecuteNonQuery();
                    }
                }
            }

            var tableExists = TableExists(tableBlueprint.TableName);
            return tableExists;
        }

        /// <summary>
        /// Create a table, defined by the passed parameters.
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="fieldNames"></param>
        /// <param name="fieldTypes"></param>
        /// <param name="recreateIfItAlreadyExists"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        public bool CreateTable(string tableName, List<string> fieldNames, List<Type> fieldTypes, bool recreateIfItAlreadyExists)
        {
            var tableBlueprint = new TableBlueprint(MyConnectionDetail.Provider, tableName, fieldNames, fieldTypes);

            return CreateTable(tableBlueprint, recreateIfItAlreadyExists);
        }

        /// <summary>
        /// Create a table, whose columns are the reflected properties of the passed object.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="recreateIfItAlreadyExists"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        public bool CreateTable<T>(bool recreateIfItAlreadyExists)
        {
            var tableBlueprint = new GenericTableBlueprint<T>(MyConnectionDetail.Provider);

            var result = CreateTable(tableBlueprint, recreateIfItAlreadyExists);

            return result;
        }

        /// <summary>
        /// getOpenServerConnection
        /// </summary>
        /// <returns></returns>
        /// <remarks></remarks>
        internal DbConnection OpenServerConnection()
        {
            IsServerConnectionOnly = true;
            var result = DatabaseFactory.CreateConnection();
            result.ConnectionString = MyConnectionDetail.ServerConnectionString;
            result.Open();

            return result;
        }

        /// <summary>
        /// getOpenDatabaseConnection
        /// </summary>
        /// <returns></returns>
        /// <remarks></remarks>
        internal virtual DbConnection OpenDatabaseConnection()
        {
            IsServerConnectionOnly = false;
            var result = DatabaseFactory.CreateConnection();
            result.ConnectionString = MyConnectionDetail.DatabaseConnectionString;
            result.Open();

            return result;
        }

        protected abstract string BuildDatabaseExistsSql();

        protected abstract string BuildCreateDatabaseSql();

        protected abstract string BuildDropDatabaseSql();

        protected virtual string BuildDropTableSql(string tableName)
        {
            return "DROP TABLE " + tableName;
        }

        protected virtual string BuildCreateTableSql(TableBlueprint tableBlueprint)
        {
            return $"{UseClause()} CREATE TABLE {tableBlueprint.TableName} {tableBlueprint.ToTableCreationSqlText()}";
        }

        /// <summary>
        /// look Up Config Connection String
        /// </summary>
        /// <param name="connectionStringName"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        private string LookUpConfigConnectionString(string connectionStringName)
        {
            try
            {
                return ConfigurationManager.ConnectionStrings[connectionStringName].ToString();
            }
            catch (Exception ex)
            {
                throw new ArgumentOutOfRangeException($"Could not find connections string {connectionStringName}");
            }
        }

        /// <summary>
        /// Convert the exception to a better description of what went wrong with the parameters involved.
        /// </summary>
        /// <param name="exception"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        private string ConvertError(DbException exception)
        {
            var result = exception.Message;

            var generalMessagePrefix = MyConnectionDetail.DetailsForErrorMessage(IsServerConnectionOnly);

            if (exception.Message.StartsWith("Access denied for user"))
                result = generalMessagePrefix + "because the password was incorrect.";
            else if (exception.Message == "Unable to connect to any of the specified MySQL hosts.")
                result = generalMessagePrefix + "because the port is invalid.";

            return result;
        }

        /// <summary>
        /// A 'USE database' clause must be prefixed to SQL Server statements.
        /// </summary>
        /// <returns>Empty string if the provider is not SQL Server.</returns>
        /// <remarks></remarks>
        private string UseClause()
        {
            return MyConnectionDetail.Provider == DatabaseProvider.SqlServer 
                ? $"USE {MyConnectionDetail.DatabaseName};" 
                : string.Empty;
        }
    }

    internal class MySqlDatabase : DatabaseAccess
    {
        public MySqlDatabase(ConnectionDetail connectionDetail) : base(connectionDetail)
        {
        }

        protected override string BuildDatabaseExistsSql()
        {
            return "SELECT COUNT(*) FROM INFORMATION_SCHEMA.SCHEMATA WHERE SCHEMA_NAME = @schemaName";
        }

        protected override string BuildCreateDatabaseSql()
        {
            return "CREATE SCHEMA " + MyConnectionDetail.DatabaseName;
        }

        protected override string BuildDropDatabaseSql()
        {
            return "DROP SCHEMA " + MyConnectionDetail.DatabaseName;
        }
    }

    internal class SqliteDatabase : DatabaseAccess
    {
        public SqliteDatabase(ConnectionDetail connectionDetail) : base(connectionDetail)
        {
            IsServerConnectionOnly = false;
        }

        private SqliteConnectionDetail SqlLiteConnectionDetail => (SqliteConnectionDetail)MyConnectionDetail;

        /// <summary>
        /// Check if a database file exists.
        /// </summary>
        /// <returns></returns>
        /// <remarks></remarks>
        public override bool DatabaseExists()
        {
            return File.Exists(SqlLiteConnectionDetail.FullFilePath);
        }

        public override bool DropDatabase()
        {
            File.Delete(SqlLiteConnectionDetail.FullFilePath);

            return !DatabaseExists();
        }

        internal override DbConnection OpenDatabaseConnection()
        {
            var result = DatabaseFactory.CreateConnection();
            result.ConnectionString = MyConnectionDetail.DatabaseConnectionString;
            result.Open();

            return result;
        }

        /// <summary>
        /// Create a SQLite database. Note that simply attempting to connect to a SQLite database that does not exist, creates it.
        /// </summary>
        /// <returns></returns>
        /// <remarks></remarks>
        public override bool CreateDatabase()
        {
            using (var connection = OpenDatabaseConnection())
            {
            }

            return DatabaseExists();
        }

        /// <summary>
        /// Check if a table exists.
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        public override bool TableExists(string tableName)
        {
            var query = new ReadQuery(
                this,
                "sqlite_master",
                new DataTable(),
                new List<string>(),
                new List<string> {"type", "name"},
                new List<List<object>>{ {new List<object> {"table", tableName}}});

            return query.ReadResult.Data.Rows.Count > 0;
        }

        protected override string BuildDatabaseExistsSql()
        {
            throw new NotImplementedException("Checking if a SQLite database exists is not implemented in SQLite. Instead, we check if the file that is the database, exists.");
        }

        protected override string BuildCreateDatabaseSql()
        {
            throw new NotImplementedException("Creating a SQLite database is not implemented in SQLite. Simply connecting to a non-existent database creates it.");
        }

        protected override string BuildDropDatabaseSql()
        {
            throw new NotImplementedException("SQLite does not have a server, so database instances cannot be deleted. Instead the file that is the database is deleted.");
        }
    }

    internal class SqlServerDatabase : DatabaseAccess
    {
        public SqlServerDatabase(ConnectionDetail connectionDetail) : base(connectionDetail)
        {
        }

        protected override string BuildDatabaseExistsSql()
        {
            return $"SELECT COUNT(db_id('{MyConnectionDetail.DatabaseName}'))";
        }

        protected override string BuildCreateDatabaseSql()
        {
            const string folderPath = @"C:\Program Files\Microsoft SQL Server\MSSQL15.MSSQLSERVER\MSSQL\DATA\";
            var builder = new StringBuilder();
            {
                var withBlock = builder;
                withBlock.Append("USE [master]; ");
                withBlock.Append("CREATE DATABASE [");
                withBlock.Append(MyConnectionDetail.DatabaseName);
                withBlock.Append("] ");
                withBlock.Append("CONTAINMENT = NONE ");
                withBlock.Append("ON PRIMARY ");
                withBlock.Append("( NAME = N'");
                withBlock.Append(MyConnectionDetail.DatabaseName);
                withBlock.Append(@"', FILENAME = N'");
                withBlock.Append(folderPath);
                withBlock.Append(MyConnectionDetail.DatabaseName);
                withBlock.Append(".mdf' , SIZE = 8192KB , MAXSIZE = UNLIMITED, FILEGROWTH = 65536KB ) ");
                withBlock.Append("LOG ON ");
                withBlock.Append("( NAME = N'");
                withBlock.Append(MyConnectionDetail.DatabaseName);
                withBlock.Append(@"_log', FILENAME = N'");
                withBlock.Append(folderPath);
                withBlock.Append(MyConnectionDetail.DatabaseName);
                withBlock.Append("_log.ldf' , SIZE = 8192KB , MAXSIZE = 2048GB , FILEGROWTH = 65536KB );");
            }

            return builder.ToString();
        }

        protected override string BuildDropDatabaseSql()
        {
            var builder = new StringBuilder();
            {
                var withBlock = builder;
                withBlock.Append("USE [master]; ");
                withBlock.Append("DROP DATABASE [");
                withBlock.Append(MyConnectionDetail.DatabaseName);
                withBlock.Append("] ");
            }

            return builder.ToString();
        }
    }
}