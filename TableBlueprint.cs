using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace AnyBase
{
    /// <summary>

    /// Metadata required to create and manipulate a database table.

    /// </summary>

    /// <remarks></remarks>
    public class TableBlueprint
    {
        /// <summary>
        /// Constructor used by generic consumer.
        /// </summary>
        internal TableBlueprint(DatabaseProvider databaseProvider, string tableName)
        {
            // Reflect table name and properties.
            Provider = databaseProvider;
            TableName = tableName;
        }

        /// <summary>
        /// Consumer used by non-generic consumer.
        /// </summary>
        internal TableBlueprint(DatabaseProvider databaseProvider, string tableName, List<string> fieldNames, List<Type> fieldTypes)
        {

            // Reflect table name and properties.
            Provider = databaseProvider;
            TableName = tableName;
            Fields = CreateFieldBlueprints(fieldNames, fieldTypes);
        }

        protected internal string TableName { get; }
        protected internal List<DataMemberBlueprint> Fields { get; protected set; }
        protected internal DatabaseProvider Provider { get; }
        
        /// <summary>
        /// Create a data table who's column names and data types match that of the generic object, so a SQL adapter knows can extract data in the correct format.
        /// </summary>
        /// <returns></returns>
        /// <remarks></remarks>
        internal DataTable Container
        {
            get
            {
                var result = new DataTable();
                foreach (var field in Fields)
                    result.Columns.Add(field.FieldName, field.CoreType);
                return result;
            }
        }

        protected List<string> ExistingPrimaryKeyFields => TableTemplateSettings.FetchPrimaryFields(TableName);

        /// <summary>
        /// We set all fields when receiving an object.
        /// </summary>
        /// <returns></returns>
        /// <remarks></remarks>
        protected internal List<string> SetFieldNames => FieldNames();
        
        protected internal List<Type> FieldTypes => GetFieldTypes();

        /// <summary>
        /// Field names that are primary keys and therefore necessary for a where clause.
        /// </summary>
        /// <returns></returns>
        /// <remarks></remarks>
        protected internal List<string> WhereFieldNames => FieldNames(true);

        private string PrimaryKeysText
        {
            get
            {
                var result = string.Empty;

                // For SQLite, don't build the primary key sql text if a default primary key is being used; it will already have been declared PRIMARY KEY AUTOINCREMENT.
                if (Provider != DatabaseProvider.Sqlite || !TableTemplateSettings.HasDefaultPrimaryKey(TableName))
                {
                    var keys = (from field in Fields where field.IsPrimaryKey select field.FieldName).ToList();

                    if (keys.Count > 0)
                        result = $", PRIMARY KEY ({keys.ToCommaSeparated()})";
                }

                return result;
            }
        }
        
        private List<DataMemberBlueprint> CreateFieldBlueprints(List<string> fieldNames, List<Type> fieldTypes)
        {
            var results = GetDefaultPrimaryKeyIfNoneIsSpecified();

            // Add properties.
            for (var index = 0; index <= fieldNames.Count - 1; index++)
            {
                var fieldName = fieldNames[index];
                var fieldType = fieldTypes[index];

                var coreTypeName = DataMemberBlueprint.ConvertToCoreType(fieldType).Name;
                var sqlDataTypeName = DataTypeConversionSettings.GetSqlDataType(Provider, coreTypeName, TableName, fieldType.Name);
                var isPrimaryKey = TableTemplateSettings.FieldIsPrimaryKey(TableName, fieldName);
                var fieldBlueprint = new FieldBlueprint(fieldName, fieldType, provider: Provider, sqlDataTypeName: sqlDataTypeName, isPrimaryKey: isPrimaryKey);

                results.Add(fieldBlueprint);
            }

            return results;
        }

        internal List<DataMemberBlueprint> GetDefaultPrimaryKeyIfNoneIsSpecified()
        {
            var results = new List<DataMemberBlueprint>();

            // Add a default primary key field if none is specified.
            if (TableTemplateSettings.HasDefaultPrimaryKey(TableName))
            {
                var sqlDataTypeName = DataTypeConversionSettings.GetSqlDataType(Provider, "Int32", TableName, "id");
                var incrementBlueprint = new IncrementBlueprint(fieldName: "id", provider: Provider, sqlDataTypeName: sqlDataTypeName, isPrimaryKey: true);

                results.Add(incrementBlueprint);
            }

            return results;
        }

        /// <summary>
        /// Get all field names. Only include fields that are primary keys if specified.
        /// </summary>
        protected List<string> FieldNames(bool primaryKeysOnly = false)
        {
            return Fields.Where(f => !primaryKeysOnly || f.IsPrimaryKey).Select(n => n.FieldName).ToList();
        }
        
        protected List<Type> GetFieldTypes(bool primaryKeysOnly = false)
        {
            return Fields.Select(n => n.FieldType).ToList();
        }

        /// <summary>
        /// Create text of fields to be created in SQL. For example '(ACCOUNT_REF TEXT, CustomerId INTEGER)'
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// Visual Basic types are converted to SQL types using a dictionary.
        /// Excludes collections but includes strings.
        /// </remarks>
        protected internal string ToTableCreationSqlText()
        {
            // Convert the list of fields to a list of SQL field creation statements such as 'id INTEGER'.
            var fieldCreationSqlTexts = Fields.Select(tableField => tableField.GetSqlFieldCreationText()).ToList();

            // Return the list as text with the fields separated by commas and the whole thing encase
            return $"({fieldCreationSqlTexts.ToCommaSeparated()}{ PrimaryKeysText })";
        }
    }
}