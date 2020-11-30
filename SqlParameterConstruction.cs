using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Common;

namespace AnyBase
{
    /// <summary>
    /// Construct parameters, converting any types that database providers cannot automatically convert.
    /// </summary>
    /// <remarks></remarks>
    internal static class SqlParameterConstruction
    {
        private static readonly Dictionary<DatabaseProvider, Dictionary<Type, Type>> DestinationTypesByDotNetTypeByProvider 
            = new Dictionary<DatabaseProvider, Dictionary<Type, Type>>()
            {
                { DatabaseProvider.SqlServer, new Dictionary<Type, Type>() { 
                    { typeof(sbyte), typeof(int) },
                    { typeof(ushort), typeof(int) }, 
                    { typeof(uint), typeof(long) }, 
                    { typeof(ulong), typeof(decimal) } } }
            };

        /// <summary>
        /// Convert a co-ordinated list of field names and field values to a list of SQL parameters, ready to use in a query.
        /// </summary>
        /// <remarks>
        /// A bug in SQL Server does not cast SByte to int automatically. It is fudged here.
        /// 
        /// </remarks>
        internal static List<DbParameter> BuiltParameters(DatabaseAccess database, List<string> fieldNames, List<object> fieldValues, string prefix, List<Type> typesToCastTo = null)
        {
            var results = new List<DbParameter>();

            for (var index = 0; index <= fieldNames.Count - 1; index++)
            {
                var fieldValue = fieldValues[index];
                var typeToCastTo = typesToCastTo?[index];

                var parameterName = prefix + fieldNames[index];
                var parameter = BuiltParameter(database, parameterName, fieldValue, typeToCastTo);

                results.Add(parameter);
            }

            return results;
        }

        /// <summary>
        /// Build the parameter.
        /// </summary>
        /// <remarks>
        /// Is set to DBNull if null.
        /// Is cast if the destination type is passed.
        /// </remarks>
        private static DbParameter BuiltParameter(DatabaseAccess database, string parameterName, object fieldValue, Type typeToCastTo = null)
        {
            var result = database.DatabaseFactory.CreateParameter();

            // Set name.
            result.ParameterName = parameterName;

            // Set value.
            // Set to dbnull if null.
            if (fieldValue == null)
                result.Value = DBNull.Value;
            else if (typeToCastTo != null)
                result.Value = Convert.ChangeType(fieldValue, typeToCastTo);
            else
                result.Value = fieldValue;

            return result;
        }

        /// <summary>
        /// Create sets of SQL parameters, for db records.
        /// </summary>
        /// <param name="database"></param>
        /// <param name="fieldNames"></param>
        /// <param name="fieldTypes"></param>
        /// <param name="parameterValueSets">
        /// The contained list of objects represent values of different types that we will make SQL parameters from. The containing list is of db records that we will perform CRUD operations on.
        /// </param>
        /// <param name="prefix"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        internal static List<List<DbParameter>> BuiltSqlParameterSets(
            DatabaseAccess database, 
            List<string> fieldNames,
            List<List<object>> parameterValueSets, 
            string prefix,
            IEnumerable<Type> fieldTypes = null)
        {
            var results = new List<List<DbParameter>>();

            if (parameterValueSets.HasItems())
            {
                // We need to check if there are any data types that the database provider cannot cast. This should be done once for a record to avoid expensive reflection on each value of each record.
                var typesToCastTo = fieldTypes?.Select(f => TypeToCastTo(database.MyConnectionDetail.Provider, f)).ToList();

                // Create an list of SQL parameter sets.
                results.AddRange(parameterValueSets.Select(parameterValues => BuiltParameters(database, fieldNames, parameterValues, prefix, typesToCastTo)));
            }

            return results;
        }

        /// <summary>
        /// Lookup destination type to cast to in the dictionary of .NET types that providers cannot cast.
        /// </summary>
        /// <returns>Null values in the array if this field does not need to be converted.</returns>
        private static Type TypeToCastTo(DatabaseProvider provider, Type typeToSearch)
        {
            var result = typeToSearch;

            if (DestinationTypesByDotNetTypeByProvider.TryGetValue(provider, out var sqlDataTypesByDotNetType))
                sqlDataTypesByDotNetType.TryGetValue(typeToSearch, out result);

            return result;
        }
    }
}