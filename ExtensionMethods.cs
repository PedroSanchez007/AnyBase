using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Data.Common;

namespace AnyBase
{
    public static class ExtensionMethods
    {
        /// <summary>
        /// Add second tier range of one list to another.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="domain"></param>
        /// <param name="toAdd"></param>
        /// <remarks></remarks>
        internal static void AddSecondTierRange<T>(this List<List<T>> domain, List<List<T>> toAdd)
        {
            for (var i = 0; i <= domain.Count - 1; i++)

                domain[i].AddRange(toAdd[i]);
        }

        /// <summary>
        /// Return a string even if null.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        internal static string EmptyIfNull(this object source)
        {
            return source == null ? string.Empty : source.ToString();
        }

        public static object GetPropertyValue<T>(this T anObject, string propertyName)
        {
            var objectType = anObject.GetType();
            var objectProperty = objectType.GetProperty(propertyName);
            var result = objectProperty.GetValue(anObject);

            return result;
        }

        /// <summary>
        /// Determine whether a list is not null and has items.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="l"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        internal static bool HasItems<T>(this IEnumerable<T> l)
        {
            return l != null && l.Any();
        }

        /// <summary>
        /// Check if the generic dictionary is not null and has at least one item.
        /// </summary>
        /// <typeparam name="TK"></typeparam>
        /// <typeparam name="TV"></typeparam>
        /// <param name="dict"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        internal static bool HasItems<TK, TV>(this Dictionary<TK, TV> dict)
        {
            return dict != null && dict.Count > 0;
        }

        /// <summary>
        /// Determine whether the object is a collection, excluding strings which are technically a collection.
        /// </summary>
        /// <param name="typeToTest"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        internal static bool IsCollectionExcludingStringAndByte(this Type typeToTest)
        {
            var isCollection = typeof(IEnumerable).IsAssignableFrom(typeToTest);
            var isString = typeToTest.Name == "String";
            var isByteArray = typeToTest.Name == "Byte[]";

            return isCollection && !isString && !isByteArray;
        }

        internal static bool IsNullable<T>(this T value)
        {
            var type = typeof(T);

            return type.IsNullable();
        }

        internal static bool IsNullable(this Type type)
        {
            return  Nullable.GetUnderlyingType(type) != null;
        }

        internal static bool IsNullableOrString(this Type type)
        {
            if (type == typeof(string))
                return true;

            return type.IsNullable();
        }

        /// <summary>
        /// Batch the passed list of lists.
        /// </summary>
        /// <param name="items"></param>
        /// <param name="batchSize"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        internal static List<List<List<T>>> ToBatches<T>(this List<List<T>> items, int batchSize)
        {
            var results = new List<List<List<T>>>();

            if (items.HasItems() && batchSize > 0)
            {
                foreach (var item in items)
                {

                    // If this is the first batch or the batch is full then start a new one.
                    if (results.Count == 0 || results.Last().Count == batchSize)
                        results.Add(new List<List<T>>());

                    // Add the item to the batch.
                    results.Last().Add(item);
                }
            }

            return results;
        }

        /// <summary>
        /// Convert a generic list to comma-separated text.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        internal static string ToCommaSeparated<T>(this List<T> items)
        {
            var result = new StringBuilder();

            if (items != null)
            {
                // To facilitate adding commas before all except the first.
                const string commaSeparatedText = ", ";
                var isFirstValue = true;

                foreach (var value in items)
                {

                    // Use comma prefix if not the first entry.
                    if (isFirstValue)
                        isFirstValue = false;
                    else
                        result.Append(commaSeparatedText);

                    result.Append(value.ToString());
                }
            }

            return result.ToString();
        }

        /// <summary>
        /// Return the list if populated or an empty list if null.
        /// </summary>
        internal static List<T> ToEmptyListIfNull<T>(this List<T> items)
        {
            return items ?? new List<T>();
        }

        /// <summary>
        /// Convert generic records to a list of their field names.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="records"></param>
        /// <param name="recordProperties"></param>
        /// <returns></returns>
        /// <remarks>Preserving the order is imperative.</remarks>
        internal static IEnumerable ToFieldNames<T>(this List<T> records, List<PropertyInfo> recordProperties)
        {
            return recordProperties.Select(r => r.Name);
        }
        
        /// <summary>
        /// Convert a single tier list to a list of lists, where each item is the only item in its own list.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="singleTierList"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        internal static List<List<T>> ToListOfList<T>(this IEnumerable<T> singleTierList)
        {
            return singleTierList.Select(item => new List<T>() {item}).ToList();
        }

        /// <summary>
        /// Convert MySQL parameter set to text.
        /// </summary>
        /// <param name="parameterSet"></param>
        /// <returns>Never returns exception, but "NULL".</returns>
        /// <remarks></remarks>
        internal static string ToText(this DbParameter parameterSet)
        {
            var result = "NULL";

            if (parameterSet != null)
            {
                var name = "NULL";
                if (parameterSet.ParameterName != null)
                    name = parameterSet.ParameterName.ToString();

                var value = "NULL";
                if (parameterSet.Value != null)
                    value = parameterSet.Value.ToString();

                result = $"({name}, {value})";
            }

            return result;
        }

        /// <summary>
        /// Convert a list of parameter sets to text.
        /// </summary>
        /// <param name="parameterSets"></param>
        /// <returns>Never returns exception, but "NULL list".</returns>
        /// <remarks></remarks>
        internal static string ToText(this List<DbParameter> parameterSets)
        {
            var result = "NULL list";

            if (parameterSets != null)
            {
                var parameterTexts = parameterSets.Select(parameterSet => parameterSet.ToText()).ToList();

                result = parameterTexts.ToCommaSeparated();
            }

            return result;
        }
    }
}