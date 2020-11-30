using System;
using System.Data.Common;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace AnyBase
{
    /// <summary>
    /// Create Crud queries, made up of the constructed SQL commands and the parameter lists that they use.
    /// Call their execution in batches and aggregate the results. 
    /// </summary>
    /// <remarks></remarks>
    internal abstract class CrudQuery
    {
        protected readonly DatabaseAccess Database;
        protected readonly string TableName;

        protected string SqlQuery;
        protected List<List<DbParameter>> ParameterSets;
        
        internal CrudQuery(DatabaseAccess database, string tableName)
        {
            Database = database;
            TableName = tableName;
        }
        
        protected abstract string BuiltSqlQueryText();

        /// <summary>
        /// Execute the CUD query.
        /// </summary>
        /// <returns></returns>
        /// <remarks></remarks>
        protected CudResult ExecuteCudQuery(string sqlQueryText, List<List<DbParameter>> parameterSets)
        {
            // CudResult result = null;
            //
            // var batchedParameterSets = parameterSets.ToBatches(Settings.CrudBatchSize);
            // foreach (var batch in batchedParameterSets)
            // {
            //     var cudQuery = new CudExecution(Database, sqlQueryText, batch);
            //     result = new CudResult(result, cudQuery.CudResult);
            // }
            //
            // return result;

            var batchedParameterSets = parameterSets.ToBatches(Settings.CrudBatchSize);

            var affectedRowCount = 0;
            var crudErrors = new List<CrudError>();
            
            foreach (var cudQuery in batchedParameterSets.Select(batch => new CudExecution(Database, sqlQueryText, batch)))
            {
                affectedRowCount += cudQuery.CudResult.AffectedRowsCount;
                crudErrors.AddRange(cudQuery.CudResult.Errors);
            }
            return new CudResult(affectedRowCount, crudErrors);
        }

        /// <summary>
        /// A 'USE database' clause must be prefixed to some provider's statements.
        /// </summary>
        /// <returns>Empty string if the use clause isn't necessary for this provider.</returns>
        /// <remarks></remarks>
        protected string UseClause =>
            Database.MyConnectionDetail.Provider == DatabaseProvider.SqlServer 
                ? $"USE {Database.MyConnectionDetail.DatabaseName};" 
                : string.Empty;
    }

    internal abstract class WhereQuery : CrudQuery
    {
        private readonly List<Type> _fieldTypes;
        private readonly List<string> _whereFieldNames;
        private readonly List<List<object>> _whereValueSets;

        internal WhereQuery(DatabaseAccess database, string tableName, List<string> whereFieldNames, List<List<object>> whereValueSets, List<Type> fieldTypes = null) 
            : base(database, tableName)
        {
            _fieldTypes = fieldTypes;
            _whereFieldNames = whereFieldNames;
            _whereValueSets = whereValueSets;
        }

        protected string ParameterisedWhereText => SqlStatementConstruction.BuildParameterisedWhereText(_whereFieldNames, _whereValueSets);

        protected List<List<DbParameter>> WhereParameterSets => SqlParameterConstruction.BuiltSqlParameterSets(Database, _whereFieldNames, _whereValueSets, "@Where", _fieldTypes);
    }
    internal class InsertQuery : CrudQuery
    {
        private readonly List<string> _insertFieldNames;
        private readonly List<Type> _insertFieldTypes;
        private readonly List<List<object>> _insertValueSets;

        internal InsertQuery(DatabaseAccess database, string tableName, List<string> insertFieldNames, List<Type> insertFieldTypes, List<List<object>> insertValueSets) : 
            base(database, tableName)
        {
            _insertFieldNames = insertFieldNames;
            _insertValueSets = insertValueSets;
            _insertFieldTypes = insertFieldTypes;

            var sqlQueryText = BuiltSqlQueryText();
            var parameterSets = BuiltParameterSets();

            CudResult = ExecuteCudQuery(sqlQueryText, parameterSets);
        }

        internal CudResult CudResult { get; }

        protected sealed override string BuiltSqlQueryText()
        {
            var commaSeparatedInsertField = _insertFieldNames.ToCommaSeparated();

            var commaSeparatedInsertPlaceholdersText = SqlStatementConstruction.BuildCommaSeparatedPlaceholdersText(_insertFieldNames, "@");

            // Build the sql statement used to insert records. This will contain the field names and the placeholders for the corresponding values.
            return
                $"{UseClause} INSERT INTO {TableName} ({commaSeparatedInsertField}) VALUES ({commaSeparatedInsertPlaceholdersText})";
        }

        private List<List<DbParameter>> BuiltParameterSets()
        {
            return SqlParameterConstruction.BuiltSqlParameterSets(Database, _insertFieldNames, _insertValueSets, "@", _insertFieldTypes);
        }
    }
    internal class UpdateQuery : WhereQuery
    {
        private readonly List<string> _setFieldNames;
        private readonly List<Type> _updateFieldTypes;
        private readonly List<List<object>> _setValueSets;

        internal UpdateQuery(
            DatabaseAccess database, 
            string tableName, 
            List<string> setFieldNames,
            List<Type> updateFieldTypes,
            List<List<object>> setValueSets, 
            List<string> whereFieldNames, 
            List<List<object>> whereValueSets) 
            : base(database, tableName, whereFieldNames, whereValueSets, updateFieldTypes)
        {
            _setFieldNames = setFieldNames;
            _updateFieldTypes = updateFieldTypes;
            _setValueSets = setValueSets;

            var sqlQueryText = BuiltSqlQueryText();
            var parameterSets = BuiltParameterSets();

            CudResult = ExecuteCudQuery(sqlQueryText, parameterSets);
        }

        internal CudResult CudResult { get; }

        protected sealed override string BuiltSqlQueryText()
        {
            var parameterisedSetText = SqlStatementConstruction.BuildEqualsPlaceholderSetText(_setFieldNames, "@Set");

            // Build the sql statement used to update records. This will contain the field names and the placeholders for the corresponding values.
            return $"{UseClause} UPDATE {TableName} SET {parameterisedSetText}{ParameterisedWhereText}";
        }

        private List<List<DbParameter>> BuiltParameterSets()
        {
            var setParameterSets = SqlParameterConstruction.BuiltSqlParameterSets(Database, _setFieldNames, _setValueSets, "@Set", _updateFieldTypes);

            // Put the set and where parameters together.
            var setAndWhereParameters = setParameterSets;
            setAndWhereParameters.AddSecondTierRange(WhereParameterSets);

            return setAndWhereParameters;
        }
    }
internal class DeleteQuery : WhereQuery
{
    internal DeleteQuery(DatabaseAccess database, string tableName, List<string> whereFieldNames, List<List<object>> whereValueSets) 
        : base(database, tableName, whereFieldNames, whereValueSets)
    {
        var sqlQueryText = BuiltSqlQueryText();

        CudResult = ExecuteCudQuery(sqlQueryText, WhereParameterSets);
    }

    internal CudResult CudResult { get; }

    protected sealed override string BuiltSqlQueryText()
    {

        // Build the sql statement used to update records. This will contain the field names and the placeholders for the corresponding values.
        return $"{UseClause} DELETE FROM {TableName}{ParameterisedWhereText}";
    }
}

internal class ScalarQuery : WhereQuery
{
    private readonly string _sqlPrefix;

    internal ScalarQuery(DatabaseAccess database, string sqlPrefix, List<string> whereFieldNames, List<Type> fieldNames, List<object> whereValueSets) : 
        base(database, string.Empty, whereFieldNames, new List<List<object>>() {new List<object>() { whereValueSets }})
    {
        _sqlPrefix = sqlPrefix;
        SqlQuery = BuiltSqlQueryText();
        ParameterSets = WhereParameterSets;

        ScalarResult = ExecuteScalarQuery();
    }

    internal ScalarResult ScalarResult { get; }

    protected sealed override string BuiltSqlQueryText()
    {

        // Build the sql statement used to update records. This will contain the field names and the placeholders for the corresponding values.
        return $"{UseClause} {_sqlPrefix} {ParameterisedWhereText}";
    }

    private ScalarResult ExecuteScalarQuery()
    {
        var query = new ScalarExecution(Database, SqlQuery, ParameterSets.FirstOrDefault());

        return query.ScalarResult;
    }
}

internal class ReadQuery : WhereQuery
{
    private readonly List<string> _selectFieldNames;
    private readonly DataTable _container;

    internal ReadQuery(DatabaseAccess database, string tableName, DataTable container, List<string> selectFieldNames, List<string> whereFieldNames, List<List<object>> whereValueSets) 
        : base(database, tableName, whereFieldNames, whereValueSets)
    {
        _container = container;
        _selectFieldNames = selectFieldNames;
        SqlQuery = BuiltSqlQueryText();
        ParameterSets = WhereParameterSets;

        ReadResult = ExecuteReadQuery();
    }

    internal NonGenericReadResult ReadResult { get; }

    protected sealed override string BuiltSqlQueryText()
    {

        // Create the select text in the form 'id, name, address1'
        // If the list is blank then use 'SELECT *'
        var selectFieldsText = "*";
        if (_selectFieldNames.HasItems())
            selectFieldsText = _selectFieldNames.ToCommaSeparated();

        var result = $"{UseClause} SELECT {selectFieldsText} FROM {TableName}{ParameterisedWhereText}";

        return result;
    }

    private NonGenericReadResult ExecuteReadQuery()
    {
        var query = new ReadExecution(Database, _container, SqlQuery, ParameterSets);

        return query.ReadResult;
    }
}
}