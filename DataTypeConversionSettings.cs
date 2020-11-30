using System;
using System.Collections.Generic;

namespace AnyBase
{
    static class DataTypeConversionSettings
    {
        /// <summary>
        /// InMySQL, Byte[] should be converted to binary(n) but I can't do it yet. Blob works well.
        /// In SQL Server the time datatype only records up to 24 hours so convert ticks to bigint and use for TimeSpan.
        /// In SQLite, TimeSpan must be stored as INTEGER as number of ticks.
        /// This dictionary should be shared.
        /// </summary>
        /// <remarks></remarks>
        private static readonly Dictionary<DatabaseProvider, Dictionary<string, string>> SqlDataTypesByDotNetTypeByProvider = 
            new Dictionary<DatabaseProvider, Dictionary<string, string>>()
            {
                { DatabaseProvider.MySql, new Dictionary<string, string>()
                {
                    { "Boolean", "boolean" }, 
                    { "Byte", "tinyint unsigned" }, 
                    { "Byte[]", "blob" }, 
                    { "DateTime", "datetime(3)" }, 
                    { "Decimal", "decimal(58,29)" }, 
                    { "Double", "double" }, 
                    { "Guid", "char(36)" }, 
                    { "Int16", "smallint" }, 
                    { "Int32", "int" }, 
                    { "Int64", "bigint" }, 
                    { "Object", "blob" }, 
                    { "SByte", "tinyint" }, 
                    { "Single", "double" }, 
                    { "String", "varchar(60)" }, 
                    { "TimeSpan", "bigint" }, 
                    { "UInt16", "smallint unsigned" }, 
                    { "UInt32", "int unsigned" }, 
                    { "UInt64", "bigint unsigned" }
                } }, { DatabaseProvider.Sqlite, new Dictionary<string, string>()
                {
                    { "Boolean", "TINYINT" }, 
                    { "Byte", "TINYINT UNSIGNED" }, 
                    { "Byte[]", "BLOB" }, 
                    { "Char", "CHARACTER(1)" },
                    { "DateTime", "DATETIME" }, 
                    { "Decimal", "DECIMAL" }, 
                    { "Double", "REAL" }, 
                    { "Guid", "CHARACTER(36)" }, 
                    { "Int16", "SMALLINT SIGNED" }, 
                    { "Int32", "MEDIUMINT SIGNED" }, 
                    { "Int64", "BIGINT SIGNED" }, 
                    { "Object", "NONE" }, 
                    { "SByte", "SMALLINT SIGNED" }, 
                    { "Single", "REAL" }, 
                    { "String", "TEXT" }, 
                    { "TimeSpan", "BIGINT SIGNED" }, 
                    { "UInt16", "MEDIUMINT UNSIGNED" }, 
                    { "UInt32", "BIGINT UNSIGNED" }, 
                    { "UInt64", "DECIMAL(20)" }
                } }, { DatabaseProvider.SqlServer, new Dictionary<string, string>()
                {
                    { "Boolean", "bit" }, 
                    { "Byte", "tinyint" }, 
                    { "Byte[]", "sql_variant" }, 
                    { "DateTime", "datetime" }, 
                    { "Decimal", "decimal(29,19)" }, 
                    { "Double", "float" }, 
                    { "Guid", "uniqueidentifier" }, 
                    { "Int16", "smallint" }, 
                    { "Int32", "int" }, 
                    { "Int64", "bigint" }, 
                    { "Object", "sql_variant" }, 
                    { "SByte", "smallint" }, 
                    { "Single", "real" }, 
                    { "String", "text" }, 
                    { "TimeSpan", "time" }, 
                    { "UInt16", "int" }, 
                    { "UInt32", "bigint" }, 
                    { "UInt64", "decimal(20)" }
                } }
            };
        
        /// <summary>
        /// The catalogue of conversion delegates, used to convert a .NET data type to a database provider's data type.
        /// </summary>
        /// <remarks></remarks>
        internal static readonly Dictionary<DatabaseProvider, Dictionary<string, Func<object, object>>> ConversionsFromDotNetTypesByDotNetDataTypesByProvider = 
            new Dictionary<DatabaseProvider, Dictionary<string, Func<object, object>>>()
            {
                { DatabaseProvider.MySql, new Dictionary<string, Func<object, object>>
                {
                    { "TimeSpan", DataTypeConverters.ToTicks }
                }}, 
                { DatabaseProvider.Sqlite, new Dictionary<string, Func<object, object>>
                {
                    { "Guid", DataTypeConverters.ToStringFromNullableGuid }, 
                    { "TimeSpan", DataTypeConverters.ToTicks }, 
                    { "UInt32", DataTypeConverters.ToInt64FromUInt32 }, 
                    { "UInt64", DataTypeConverters.ToDecimalFromUInt64 }
                } }, { DatabaseProvider.SqlServer, new Dictionary<string, Func<object, object>>
                {
                    { "TimeSpan", DataTypeConverters.ToTicks }
                } }
            };

        /// <summary>
        /// The catalogue of conversion delegates, used to convert a database provider data type to a .NET data type.
        /// </summary>
        /// <remarks></remarks>
        private static readonly Dictionary<DatabaseProvider, Dictionary<string, Func<object, object>>> ConversionsToDotNetTypesByDotNetTypesByProvider = 
            new Dictionary<DatabaseProvider, Dictionary<string, Func<object, object>>>()
            {
                { DatabaseProvider.Sqlite, new Dictionary<string, Func<object, object>>()
                {
                    { "Pants", DataTypeConverters.ToBoolean }
                } }, 
                { DatabaseProvider.SqlServer, new Dictionary<string, Func<object, object>>()
                {
                    { "TimeSpan", DataTypeConverters.ToTimespan }
                } }
            };

        /// <summary>
        /// Look up the conversion required, if any.
        /// </summary>
        /// <param name="provider"></param>
        /// <param name="direction"></param>
        /// <param name="dotNetDataTypeName"></param>
        /// <returns>The conversion delegate if found, or null if no extrapolation is required.</returns>
        /// <remarks></remarks>
        internal static Func<object, object> LookUpDataTypeConversion(DatabaseProvider provider, ConversionDirection direction, string dotNetDataTypeName)
        {
            Func<object, object> result = null;

            var conversionsByDataTypeByProvider = direction == ConversionDirection.FromDotNetToSql 
                    ? ConversionsFromDotNetTypesByDotNetDataTypesByProvider 
                    : ConversionsToDotNetTypesByDotNetTypesByProvider;

            if (conversionsByDataTypeByProvider.TryGetValue(provider, out var conversionsByType))
                conversionsByType.TryGetValue(dotNetDataTypeName, out result);

            return result;
        }

        /// <summary>
        /// Look up the provider-specific data type from the .NET data type. Note that these can be overridden in the table template.
        /// </summary>
        /// <param name="provider"></param>
        /// <param name="dotNetDataTypeName"></param>
        /// <param name="tableName"></param>
        /// <param name="field"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        internal static string GetSqlDataType(DatabaseProvider provider, string dotNetDataTypeName, string tableName, string field)
        {
            var overrideType = TableTemplateSettings.FetchOverrideDataTypeName(provider, tableName, field);

            return 
                overrideType == string.Empty 
                ? LookUpSqlDataType(provider, dotNetDataTypeName) 
                : overrideType;
        }

        private static string LookUpSqlDataType(DatabaseProvider provider, string dotNetDataTypeName)
        {
            var result = string.Empty;

            if (!SqlDataTypesByDotNetTypeByProvider.TryGetValue(provider, out var sqlDataTypesByDotNetType))

                // Visual Basic type not found.
                throw new NotImplementedException("Database provider '" + provider + "' is not in the dictionary of type conversions.");
            else if (!sqlDataTypesByDotNetType.TryGetValue(dotNetDataTypeName, out result))

                // Visual Basic type not found.
                throw new NotImplementedException(".NET data type '" + dotNetDataTypeName + "' is not in the dictionary of type conversions.");

            return result;
        }
    }
}