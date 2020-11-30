using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Data.Common;
using System.Threading;

namespace AnyBase
{
    /// <summary>
    ///  Execute CRUD parameterised queries in a transaction.
    ///  </summary>
    ///  <remarks></remarks>
    internal abstract class QueryExecution
    {
        protected readonly DatabaseAccess Database;
        protected readonly string SqlText;
        protected readonly List<CrudError> Errors = new List<CrudError>();

        internal QueryExecution(DatabaseAccess database, string sqlText)
        {
            Database = database;
            SqlText = sqlText;
        }

        protected string MutexName => Database.MyConnectionDetail.MutexName;
    }

    internal class ScalarExecution : QueryExecution
    {
        private readonly List<DbParameter> _scalarParameters;

        internal ScalarExecution(DatabaseAccess database, string sqlText, List<DbParameter> scalarParameters) 
            : base(database, sqlText)
        {
            _scalarParameters = scalarParameters;

            object resultValue = null;
            try
            {
                resultValue = ExecuteScalar();
            }
            catch (Exception ex)
            {
                var err = new CrudError(ex);
                Errors.Add(err);
            }

            ScalarResult = new ScalarResult(resultValue, Errors.FirstOrDefault());
        }

        internal ScalarResult ScalarResult { get; }

        /// <summary>
        /// Execute scalar query with optional mutex lock.
        /// </summary>
        /// <returns></returns>
        /// <remarks></remarks>
        private object ExecuteScalar()
        {
            var result = new object();

            // Use a mutex if the name was passed.
            if (MutexName != null)
            {
                using (var myMutex = new Mutex(false, MutexName))
                {
                    result = ExecuteScalarInner();
                }
            }
            else
                result = ExecuteScalarInner();

            return result;
        }

        /// <summary>
        /// Execute a scaler query to return one result.
        /// </summary>
        /// <returns></returns>
        /// <remarks></remarks>
        private object ExecuteScalarInner()
        {
            var result = new object();

            using (var connection = Database.OpenDatabaseConnection())
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = SqlText;

                    // Queries with no parameters are very general such as dropping a table.
                    if (_scalarParameters.HasItems())

                        // Clear and add in parameters.
                        command.Parameters.AddRange(_scalarParameters.ToArray());

                    // Execute the query to read to the datatable.
                    result = command.ExecuteScalar();
                }
            }

            return result;
        }
    }

    internal class ReadExecution : QueryExecution
    {
        /// <summary>
        /// A list of records, each a list of parameters, to be read from the database. If nothing is passed, then a parameterless query will be executed.
        /// </summary>
        /// <remarks></remarks>
        protected readonly List<List<DbParameter>> ParameterSets;
        
        /// <summary>
        /// A strongly-typed data table can be passed, which the SQL database adapter will use to select, and convert to the correct data types when extracting data.
        /// Alternatively, an empty data table can be passed, in which case the adapter will guess.
        /// </summary>
        /// <remarks></remarks>
        protected readonly DataTable Container;
        
        internal ReadExecution(DatabaseAccess database, DataTable container, string sqlText, List<List<DbParameter>> parameterSets) : base(database, sqlText)
        {
            Container = container;
            ParameterSets = parameterSets;
            var readData = ExecuteReadQuery();

            ReadResult = new NonGenericReadResult(readData, Errors);
        }

        public NonGenericReadResult ReadResult { get; }
        
        /// <summary>
        /// Execute a SQL read query, using Mutex locking if specified.
        /// </summary>
        private DataTable ExecuteReadQuery()
        {
            DataTable results = null/* TODO Change to default(_) if this is not a reference type */;

            // Use a mutex if the name was passed.
            if (MutexName != null)
            {
                using (var myMutex = new Mutex(false, MutexName))
                {
                    results = ExecuteReadQueryInner();
                }
            }
            else
                results = ExecuteReadQueryInner();

            return results;
        }

        /// <summary>
        /// Execute a non-query SQL statement.
        /// </summary>
        /// <returns>The data table of results.</returns>
        /// <remarks></remarks>
        private DataTable ExecuteReadQueryInner()
        {
            var results = Container;

            using (var connection = Database.OpenDatabaseConnection())
            {
                using (var transaction = connection.BeginTransaction())
                {
                    using (var command = connection.CreateCommand())
                    {
                        command.Transaction = transaction;
                        command.CommandText = SqlText;

                        // Do not stop the entire transaction when an error occurs; just record the error and allow the others to be actioned.
                        try
                        {
                            // Queries with no parameters are very general such as dropping a table.
                            if (ParameterSets.HasItems())
                            {

                                // Iterate through the sets of parameters within the transaction for speed.
                                foreach (var parameterSet in ParameterSets)
                                {

                                    // Clear and add in parameters.
                                    command.Parameters.Clear();
                                    command.Parameters.AddRange(parameterSet.ToArray());

                                    // Execute the query to read to the datatable.
                                    using (var reader = command.ExecuteReader())
                                    {

                                        // Keep loading more reader results to the same datatable.
                                        results.Load(reader);
                                    }
                                }
                            }
                            else

                                // Execute the query to read to the datatable.
                                using (var reader = command.ExecuteReader())
                                {

                                    // Keep loading more reader results to the same datatable.
                                    results.Load(reader);
                                }
                        }
                        catch (DbException ex)
                        {
                            var categorisedError = new CrudError(ex);
                            Errors.Add(categorisedError);
                        }

                        // Remove the general after testing as it should stop execution and be handled elsewhere.
                        catch (Exception ex)
                        {
                            var categorisedError = new CrudError(ex);
                            Errors.Add(categorisedError);
                        }
                    }

                    transaction.Commit();
                }
            }

            return results;
        }
    }

    internal class ParameterlessExecution : CudExecution
    {
        private readonly CrudError _crudError;

        internal ParameterlessExecution(DatabaseAccess database, string sqlText) : base(database, sqlText)
        {
            if (CudResult.Errors.ToList().HasItems())
                _crudError = CudResult.Errors.First();
        }

        internal CrudError crudError => _crudError;
    }

    internal class CudExecution : QueryExecution
    {
        /// <summary>
        /// A list of records, each a list of parameters, to be created, updated or deleted in the database. If nothing is passed, then a parameterless query will be executed.
        /// </summary>
        /// <remarks></remarks>
        private readonly List<List<DbParameter>> _parameterSets;

        internal CudExecution(DatabaseAccess database, string sqlText, List<List<DbParameter>> parameterSets = null) : base(database, sqlText)
        {
            if (parameterSets != null)
                _parameterSets = parameterSets;

            var rowsAffectedCount = ExecuteCudQueries();
            CudResult = new CudResult(rowsAffectedCount, Errors);
        }

        internal CudResult CudResult { get; }

        /// <summary>
        /// Execute a create, update or delete query, using Mutex locking if specified.
        /// </summary>
        private int ExecuteCudQueries()
        {
            var result = 0;

            // Use a mutex if the name was passed.
            if (MutexName != null)
            {
                using (var myMutex = new Mutex(false, MutexName))
                {
                    result = ExecuteCudQueriesInner();
                }
            }
            else
                result = ExecuteCudQueriesInner();

            return result;
        }

        /// <summary>
        /// Execute a SQL create, update or delete query.
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// Records are passed as a list of parameters, so that multiple records can be updated within one transaction for speed.
        /// </remarks>
        private int ExecuteCudQueriesInner()
        {
            var result = 0;

            using (var connection = Database.OpenDatabaseConnection())
            {
                using (var transaction = connection.BeginTransaction())
                {
                    using (var command = connection.CreateCommand())
                    {
                        command.Transaction = transaction;
                        command.CommandText = SqlText;

                        // Do not stop the entire transaction when an error occurs; just record the error and allow the others to be actioned.
                        try
                        {

                            // Queries with no parameters are very general such as dropping a table.
                            if (_parameterSets.HasItems())
                            {

                                // Iterate through the sets of parameters within the transaction for speed.
                                foreach (var parameterSet in _parameterSets)
                                {

                                    // Clear and add in parameters.
                                    command.Parameters.Clear();
                                    command.Parameters.AddRange(parameterSet.ToArray());

                                    // Execute the query and store the number of rows affected.
                                    result += command.ExecuteNonQuery();
                                }
                            }
                            else

                                // Execute the query and store the number of rows affected.
                                result += command.ExecuteNonQuery();
                        }
                        catch (Exception ex)
                        {
                            var categorisedError = new CrudError(ex);
                            Errors.Add(categorisedError);
                        }
                    }

                    transaction.Commit();
                }
            }

            return result;
        }
    }
}