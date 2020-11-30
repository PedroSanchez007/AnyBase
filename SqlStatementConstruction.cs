using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AnyBase
{
    /// <summary>
    /// Helper functions to build text required for SQL commands.
    /// </summary>
    /// <remarks></remarks>
    internal static class SqlStatementConstruction
    {
        /// <summary>
        /// Convert a generic list to comma-seperated text with each value proceded by the SQL placeholder symbol '@'.
        /// </summary>
        internal static string BuildCommaSeparatedPlaceholdersText<T>(List<T> items, string prefix)
        {
            // Convert the generic list to a list of placeholders, to call the general method.
            var itemsConverted = items.Select(x => prefix + x.ToString()).ToList();

            return itemsConverted.ToCommaSeparated();
        }

        /// <summary>
        /// Text used in parameterised queries. e.g. 'FullName = @WhereFullName'
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="prefix"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        internal static string BuildEqualsPlaceholderText(string fieldName, string prefix)
        {
            return $"{fieldName} = {prefix}{fieldName}";
        }

        internal static string BuildEqualsPlaceholderSetText(List<string> fieldNames, string prefix)
        {

            // Convert list of fieldNames to list of equals parameter statements.
            var placeHolders = fieldNames.Select(x => BuildEqualsPlaceholderText(x, prefix)).ToList();

            return placeHolders.ToCommaSeparated();
        }

        /// <summary>
        /// Build text for the 'WHERE' section of a SQL statement, which may contain ANDs if it has compound primary keys.
        /// </summary>
        /// <returns></returns>
        /// <remarks>The placeholder text @Where is used to differentiate from @Set parameters.</remarks>
        internal static string BuildParameterisedWhereText(List<string> fieldNames, List<List<object>> fieldValueSets)
        {
            StringBuilder result = new StringBuilder();

            // Leave the 'WHERE' text blank if there are no where values.
            if (fieldValueSets.HasItems())
            {
                var isFirstField = true;
                foreach (var whereField in fieldNames)
                {
                    string sqlText;
                    if (isFirstField)
                    {

                        // Note that the 'WHERE' must be included here in case there are no 'WHERE' parameters and the 'WHERE' should not be included in the SQL text.
                        sqlText = " WHERE " + BuildEqualsPlaceholderText(whereField, "@Where");
                        isFirstField = false;
                    }
                    else
                        sqlText = " AND " + BuildEqualsPlaceholderText(whereField, "@Where");

                    result.Append(sqlText);
                }
            }

            return result.ToString();
        }
    }
}