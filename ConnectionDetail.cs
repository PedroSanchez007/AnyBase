using System.IO;

namespace AnyBase
{
    /// <summary>
    ///  Base class that all methods of connecting to a database derive from.
    ///  </summary>
    ///  <remarks></remarks>
    public abstract class ConnectionDetail
    {
        protected ConnectionDetail(string databaseName = null, string mutexName = null)
        {
            DatabaseName = databaseName;
            MutexName = mutexName;
        }

        public abstract DatabaseProvider Provider { get; }
        public string DatabaseName { get; }

        /// <summary>
        /// The name of the global mutex, that locks a database machine wide.
        /// </summary>
        /// <value>Null when no mutex is being used.</value>
        /// <returns></returns>
        /// <remarks>Some databases, such as SQLite, can experience problems if the same database has several commands run on it at the same time.
        /// Therefore, the option exists to use a named Mutex, so all applications and processes on this machine can only access this database one at a time.</remarks>
        public string MutexName { get; }

        public abstract string ServerConnectionString { get; }
        public abstract string DatabaseConnectionString { get; }
        public abstract string DetailsForErrorMessage(bool isServerConnectionOnly);
    }

    internal class ConfigConnectionDetail : ConnectionDetail
    {
        private readonly DatabaseProvider _provider;
        private readonly string _hardcodedServerConnectionString;
        private readonly string _hardcodedDatabaseConnectionString;

        public ConfigConnectionDetail(DatabaseProvider provider, string hardcodedServerConnectionString, string hardcodedDatabaseConnectionString)
        {
            _provider = provider;
            _hardcodedServerConnectionString = hardcodedServerConnectionString;
            _hardcodedDatabaseConnectionString = hardcodedDatabaseConnectionString;
        }

        public override DatabaseProvider Provider => _provider;

        /// <summary>
        /// Connection string stored in app.config to connect to the server only.
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        public override string ServerConnectionString => _hardcodedServerConnectionString;

        /// <summary>
        /// Connection string stored in app.config to connect to the database.
        /// </summary>
        /// <value>Null when the connection is only to the server and not a database.</value>
        /// <returns></returns>
        /// <remarks></remarks>
        public override string DatabaseConnectionString => _hardcodedDatabaseConnectionString;

        public override string DetailsForErrorMessage(bool isServerConnectionOnly)
        {
            if (isServerConnectionOnly)
            {
                return _hardcodedServerConnectionString == null 
                    ? $"Could not connect to the {Provider} server using the app.config connection string because it was not set." 
                    : $"Could not connect to the {Provider} server using app.config connection string: {_hardcodedServerConnectionString}";
            }
            else 
            {
                return _hardcodedDatabaseConnectionString == null ?
                $"Could not connect to the {Provider} database using the app.config connection string because it was not set." :
                $"Could not connect to the {Provider} database using app.config connection string: {_hardcodedDatabaseConnectionString}";
            }
        }
    }

    public class TrustedConnectionDetail : ConnectionDetail
    {
        protected readonly string ServerAddress;

        public TrustedConnectionDetail(DatabaseProvider provider, string serverAddress, string databaseName = null) : base(databaseName: databaseName)
        {
            Provider = provider;
            ServerAddress = serverAddress;
        }

        public override DatabaseProvider Provider { get; }

        public override string ServerConnectionString => $"Server={ServerAddress};Trusted_Connection=Yes;";

        public override string DatabaseConnectionString =>
            $"Server={ServerAddress};Database={DatabaseName};Trusted_Connection=Yes;";

        public override string DetailsForErrorMessage(bool isServerConnectionOnly)
        {
            return isServerConnectionOnly 
                ? $"Could not connect to {Provider} server with the Trusted Connection: {ServerConnectionString}" 
                : $"Could not connect to {Provider} database with the Trusted Connection: {DatabaseConnectionString}";
        }
    }

    internal abstract class NonConfigConnectionDetail : ConnectionDetail
    {
        internal NonConfigConnectionDetail(string databaseName = null, string password = null, string mutexName = null) : base(databaseName, mutexName)
        {
            Password = password;
        }

        /// <summary>
        /// Password required to log on to or access a database.
        /// </summary>
        internal string Password { get; }
    }

    internal abstract class ServerBasedConnectionDetail : NonConfigConnectionDetail
    {

        protected ServerBasedConnectionDetail(string serverAddress, int portNumber, string userName, string databaseName = null, string password = null, string mutexName = null) 
            : base(databaseName, password, mutexName)
        {
            ServerAddress = serverAddress;
            PortNumber = portNumber;
            UserName = userName;
        }

        protected string ServerAddress { get; }
        protected int PortNumber { get; }
        protected string UserName { get; }
    }

    internal class MySqlConnectionDetail : ServerBasedConnectionDetail
    {
        public MySqlConnectionDetail(string serverAddress, int portNumber, string userName, string databaseName = null, string password = null, string mutexName = null) : base(serverAddress, portNumber, userName, databaseName, password, mutexName)
        {
        }

        public override DatabaseProvider Provider => DatabaseProvider.MySql;

        /// <summary>
        /// Build a MySql connection string
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        public override string ServerConnectionString =>
            $"server={ServerAddress};port={PortNumber.ToString()};user id={UserName};password={Password}";

        /// <summary>
        /// Build a MySql database connection string
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        public override string DatabaseConnectionString =>
            $"server={ServerAddress};port={PortNumber.ToString()};database={DatabaseName};user id={UserName};password={Password}";

        public override string DetailsForErrorMessage(bool isServerConnectionOnly)
        {
            return isServerConnectionOnly 
                ? $"Could not connect to {Provider} server. Server address = {ServerAddress}, port number = {PortNumber.ToString()}, user name = {UserName}" 
                : $"Could not connect to {Provider} database. Server address = {DatabaseName}, database name = {ServerAddress}, port number = {PortNumber.ToString()}, user name = {UserName}";
        }
    }

    internal class SqliteConnectionDetail : NonConfigConnectionDetail
    {

        public SqliteConnectionDetail(string folderPath, string libraryDatabaseName, string password = null, string mutexName = null) 
            : base(libraryDatabaseName, password, mutexName)
        {
            FolderPath = folderPath;
        }

        public override DatabaseProvider Provider => DatabaseProvider.Sqlite;

        private string FolderPath { get; }

        public string FullFilePath => Path.Combine(FolderPath, DatabaseName);

        public override string ServerConnectionString =>
            // SQLite does not have a server.
            null;

        public override string DatabaseConnectionString =>
            $"Data Source={FullFilePath};Version=3;Pooling=true;Max Pool Size=100;Password={Password}";

        public override string DetailsForErrorMessage(bool isServerConnectionOnly)
        {
            return isServerConnectionOnly 
                ? "Cannot connect to a SQLite server. SQLite does not use servers" 
                : $@"Could not connect to {Provider} database. Filepath = {FolderPath}\{DatabaseName}";
        }
    }

    internal class SqlServerConnectionDetail : ServerBasedConnectionDetail
    {
        public SqlServerConnectionDetail(string serverAddress, int portNumber, string userName, string databaseName = null, string password = null, string mutexName = null) : base(serverAddress, portNumber, userName, databaseName, password, mutexName)
        {
        }

        public override DatabaseProvider Provider => DatabaseProvider.SqlServer;

        public override string ServerConnectionString =>
            // SQLite does not have a server.
            $"Server={ServerAddress};User Id={UserName};Password={Password};";

        public override string DatabaseConnectionString =>
            // SQLite does not have a server.
            $"Server={ServerAddress};Database={DatabaseName};User Id={UserName};Password={Password};";

        public override string DetailsForErrorMessage(bool isServerConnectionOnly)
        {
            return isServerConnectionOnly 
                ? $"Could not connect to {Provider} server. Server address = {ServerAddress}, port number = {PortNumber.ToString()}, user name = {UserName}" 
                : $"Could not connect to {Provider} database. Server address = {DatabaseName}, database name = {ServerAddress}, port number = {PortNumber.ToString()}, user name = {UserName}";
        }
    }
}