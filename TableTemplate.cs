using System.Collections.Generic;
using System.Linq;

namespace AnyBase
{
    public class TableTemplate
    {
        private readonly Dictionary<DatabaseProvider, Dictionary<string, string>> _dataTypeOverridesByFieldsByProvider;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="primaryKeys">If no primary key is passed, then "id" will be assumed to be the primary key.</param>
        /// <param name="excludeFields"></param>
        /// <param name="dataTypeOverridesByFieldsByProvider">Override of a fields data type.</param>
        /// <remarks></remarks>
        public TableTemplate(string tableName, string[] primaryKeys = null, string[] excludeFields = null, Dictionary<DatabaseProvider, Dictionary<string, string>> dataTypeOverridesByFieldsByProvider = null)
        {
            this.TableName = tableName;
            this.PrimaryKeys = primaryKeys != null ? primaryKeys.ToList() : new List<string>() {"id"};
            ExcludedFields = excludeFields != null ? excludeFields.ToList() : new List<string>();
            _dataTypeOverridesByFieldsByProvider = dataTypeOverridesByFieldsByProvider.HasItems() ? dataTypeOverridesByFieldsByProvider : new Dictionary<DatabaseProvider, Dictionary<string, string>>();
        }

        public string TableName { get; }

        public List<string> PrimaryKeys { get; }

        /// <summary>
        /// Fields that should be skipped in the ORM between a .NET object and it's agnostic destination database.
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        public List<string> ExcludedFields { get; }

        public bool HasDefaultPrimaryKey => PrimaryKeys != null && PrimaryKeys.Count == 1 && PrimaryKeys.First() == "id";

        /// <summary>
        /// Fetch the override data type name, if any.
        /// </summary>
        /// <returns>Empty string if this field isn't overridden.</returns>
        public string OverrideDataTypeName(DatabaseProvider provider, string field)
        {
            var result = string.Empty;

            if (_dataTypeOverridesByFieldsByProvider.TryGetValue(provider, out var lookedUpDataTypeNamesByField))
            {
                if (lookedUpDataTypeNamesByField.TryGetValue(field, out var lookedUpDataTypeName))
                    result = lookedUpDataTypeName;
            }

            return result;
        }
    }
}