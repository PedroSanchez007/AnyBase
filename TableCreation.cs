using System;
using System.Collections.Generic;

namespace AnyBase
{
    /// <summary>
    /// ''' This class is not used going forward, but I am keeping it in case the add column functionality will be needed by the data cache.
    /// ''' </summary>
    /// ''' <remarks></remarks>
    internal class TableCreation
    {
        private readonly DatabaseAccess Database;
        private readonly TableBlueprint TableBlueprint;

        public TableCreation(DatabaseAccess database, bool recreateIfItAlreadyExists, TableBlueprint tableBlueprint)
        {
            Database = database;
            TableBlueprint = tableBlueprint;
        }

        public TableCreation(DatabaseAccess database, string tableName, List<string> fieldNames, List<Type> fieldTypes, bool recreateIfItAlreadyExists, TableBlueprint tableBlueprint, CudExecution queryExecution)
        {
            Database = database;
            TableBlueprint = tableBlueprint;
            Database.CreateTable(tableName, fieldNames, fieldTypes, recreateIfItAlreadyExists);
        }

        /// <summary>
        ///     ''' Add a column to a MySQL table.
        ///     ''' </summary>
        ///     ''' <param name="columnName"></param>
        ///     ''' <param name="typeName"></param>
        ///     ''' <remarks>Throws dbException if the column already exists.</remarks>
        private CrudError AddColumn(string columnName, string typeName)
        {
            CrudError result = null/* TODO Change to default(_) if this is not a reference type */;

            // Add the column to the SQL table with the same properties as the Visual Basic type.
            var addColumnSql = $"ALTER TABLE {TableBlueprint.TableName} ADD COLUMN {columnName} {typeName}";

            var addColumnQuery = new ParameterlessExecution(Database, addColumnSql);

            return addColumnQuery.crudError;
        }

        /// <summary>
        ///     ''' Add a column of the corresponding SQLite data type.
        ///     ''' </summary>
        ///     ''' <param name="columnName"></param>
        ///     ''' <param name="propertyTypeName"></param>
        ///     ''' <remarks></remarks>
        private void AddTypedColumn(string columnName, string propertyTypeName)
        {

            // Convert Visual Basic type to SQLite type.
            var typeName = DataTypeConversionSettings.GetSqlDataType(Database.MyConnectionDetail.Provider, propertyTypeName, TableBlueprint.TableName, columnName);

            AddColumn(columnName, typeName);
        }
    }
}