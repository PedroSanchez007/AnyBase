using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace AnyBase
{
    internal class SchemaAnalysis
    {
        private readonly DatabaseAccess database;

        internal SchemaAnalysis(ConnectionDetail connectionDetail)
        {
            database = DatabaseCreator.Factory(connectionDetail);
        }

        /// <summary>
        ///     ''' Check if a table exists.
        ///     ''' </summary>
        ///     ''' <returns></returns>
        ///     ''' <remarks>Always open the connection first and handle errors there.</remarks>
        internal static bool Test()
        {
            var result = false;

            // Dim sqlText = "select column_name from INFORMATION_SCHEMA.SCHEMATA WHERE SCHEMA_NAME = @schemaName"

            // Using command As New SqlCommand()

            // command.Connection = serverConnection
            // command.CommandText = sqlText
            // command.Parameters.AddWithValue("@schemaName", databaseName)

            // ' Execute the query to read to the datatable.
            // Dim sqlResult = command.ExecuteScalar()
            // result = CInt(sqlResult) > 0

            // End Using

            return result;
        }
    }

    internal class SqlTable
    {
        internal SqlTable(string databaseName, string tableName, DatabaseAccess database)
        {

            // Query to retrieve schema information.
            var generalSelectFieldNames = new List<string>
            {
                "column_name",
                "data_type",
                "column_type",
                "column_key"
            }.ToList();
            var generalWhereFieldNames = new List<string>
            {
                "table_schema",
                "table_name"
            };
            var generalWhereValues = new List<List<object>> { new List<object> {databaseName, tableName}};

            // Query to retrieve schema information.
            var constraintSelectFieldNames = new List<string>
            {
                "column_name",
                "constraint_name",
                "referenced_column_name",
                "referenced_table_name"
            };
            var constraintWhereFieldNames = new List<string>
            {
                "table_schema",
                "table_name"
            };
            var constraintWhereValues = new List<List<object>>{new List<object>{databaseName, tableName}};

            // Dim generalQuery = String.Format("select column_name, data_type, column_type, column_key from information_schema.columns where table_schema='{0}' and table_name='{1}';", databaseName, tableName)
            // Dim constraintQuery = String.Format("select column_name, constraint_name, referenced_column_name, referenced_table_name from information_schema.key_column_usage where table_schema='{0}' and table_name='{1}'", databaseName, tableName)

            var generalQuery = new ReadQuery(
                database, 
                "information_schema.columns", 
                new DataTable(), 
                generalSelectFieldNames, 
                generalWhereFieldNames, 
                generalWhereValues);

            var constraintQuery = new ReadQuery(
                database, 
                "information_schema.key_column_usage", 
                new DataTable(), 
                constraintSelectFieldNames, 
                constraintWhereFieldNames, 
                constraintWhereValues);

            // Convert results to objects that store the metadata of the columns.
            for (var i = 1; i <= generalQuery.ReadResult.Data.Rows.Count; i++)
            {
                var column = new SqlColumn(generalQuery.ReadResult.Data, constraintQuery.ReadResult.Data, i);
                Columns.Add(column);
            }
        }

        internal List<SqlColumn> Columns { get; } = new List<SqlColumn>();
    }

    internal class SqlColumn
    {
        internal SqlColumn(DataTable generalDataTable, DataTable constraintDataTable, int recordNo)
        {
            var generalRow = generalDataTable.Rows[recordNo];
            var constraintRow = constraintDataTable.Rows[recordNo];
            
            ColumnName = generalRow["column_name"].ToString();
            DataType = generalRow["dataType"].ToString();
            ColumnType = generalRow["column_type"].ToString();
            ColumnKey = generalRow["column_key"].ToString();
            ConstraintName = constraintRow["constraintName"].ToString();
            ReferencedColumnName = constraintRow["referencedColumnName"].ToString();
            ReferencedTableName = constraintRow["referencedTableName"].ToString();
        }

        internal string ColumnName { get; }
        internal string DataType { get; }
        internal string ColumnType { get; }
        internal string ColumnKey { get; }
        internal string ConstraintName { get; }
        internal string ReferencedColumnName { get; }
        internal string ReferencedTableName { get; }
    }
}