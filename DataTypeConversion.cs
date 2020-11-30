using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace AnyBase
{
    /// <summary>
    /// ''' Database providers do not always have appropriate data types, that the corresponding .NET data type can be mapped to.
    /// ''' Dictionaries in DataTypeConversionSettings store which conversion is necessary and in which direction.
    /// ''' Note that when performing generic CRUD operations the type is known, and a TableBlueprint is created, which contains the properties' data types but,
    /// ''' if non-generic CRUD operations are being performed then we 
    /// ''' </summary>
    /// ''' <remarks></remarks>
    internal class DataTypeConversion<T>
    {
        private readonly GenericTableBlueprint<T> _tableBlueprint;

        internal DataTypeConversion(GenericTableBlueprint<T> tableBlueprint)
        {
            _tableBlueprint = tableBlueprint;
        }

        /// <summary>
        /// Convert the values in the passed sets to a different data type, as specified by the conversion delegate.
        /// </summary>
        /// <param name="originalFieldValueSets">Sets of records, containing the values that may need to be converted.</param>
        /// <param name="direction"></param>
        /// <returns></returns>
        /// <remarks>Some conversions cannot be automatically deduced and performed by the CLR.</remarks>
        internal List<List<object>> ConvertValueSets(IEnumerable<IEnumerable<object>> originalFieldValueSets, ConversionDirection direction)
        {
            var requiredConversions = LookedUpRequiredConversions(_tableBlueprint.Provider, direction);

            return originalFieldValueSets.Select(originalFieldValues => ConvertValues(originalFieldValues.ToList(), requiredConversions)).ToList();
        }

        /// <summary>
        /// Convert the values in the passed sets to a different data type, as specified by the conversion delegate.
        /// </summary>
        /// <param name="sourceData">A data table containing the values that may need to be converted.</param>
        /// <returns></returns>
        /// <remarks>Some conversions cannot be automatically deduced and performed by the CLR.</remarks>
        internal List<T> ConvertValueSets(DataTable sourceData)
        {
            var results = new List<T>();

            var requiredConversions = LookedUpRequiredConversions(_tableBlueprint.Provider, ConversionDirection.FromSqlToDotNet);

            foreach (DataRow row in sourceData.Rows)
            {
                var convertedValues = ConvertValues(row.ItemArray.ToList(), requiredConversions);

                var convertedObject = CreateObject(convertedValues);

                results.Add(convertedObject);
            }

            return results;
        }

        /// <summary>
        /// Generically create a new object, and populate it's properties.
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        private T CreateObject(List<object> values)
        {
            var result = default(T);
            //var result = Activator.CreateInstance(typeof(T));

            for (var index = 0; index <= _tableBlueprint.Fields.Count - 1; index++)
            {
                var field = _tableBlueprint.Fields[index];

                var prop = typeof(T).GetProperty(field.FieldName);
                prop.SetValue(result, values[index]);
            }

            return result;
        }

        /// <summary>
        /// Look up any required convertions.
        /// </summary>
        /// <param name="provider"></param>
        /// <param name="direction">Whether we are converting to .NET to the provider data type or vice versa.</param>
        /// <returns>Conversions, as delegates, where null indicates that no conversion is required.</returns>
        /// <remarks></remarks>
        private List<Func<object, object>> LookedUpRequiredConversions(DatabaseProvider provider, ConversionDirection direction)
        {
            var results = new List<Func<object, object>>();

            foreach (var field in _tableBlueprint.Fields)
            {
                var conversionRequired = DataTypeConversionSettings.LookUpDataTypeConversion(provider, direction, field.CoreType.Name);

                results.Add(conversionRequired);
            }

            return results;
        }

        /// <summary>
        /// Convert the value to the new data type of using the passed conversion delegate, if required.
        /// </summary>
        /// <param name="conversion">The delegate pointing to the conversion. Is null if no conversion is required.</param>
        /// <param name="originalValue"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        private static object ConvertValue(Func<object, object> conversion, object originalValue)
        {
            if (Convert.IsDBNull(originalValue) | originalValue == null)
                return null;
            return conversion == null ? originalValue : conversion(originalValue);
        }

        /// <summary>
        /// Convert each value to it's new data type using the passed conversion delegate, if required.
        /// </summary>
        /// <param name="originalValues"></param>
        /// <param name="conversions">Delegate to the method that converts the value. A null delegate indicates that no conversion is required.</param>
        /// <returns></returns>
        /// <remarks></remarks>
        private static List<object> ConvertValues(IReadOnlyList<object> originalValues, IReadOnlyList<Func<object, object>> conversions)
        {
            var results = new List<object>();

            for (var index = 0; index <= originalValues.Count - 1; index++)
            {
                var convertedValue = ConvertValue(conversions[index], originalValues[index]);

                results.Add(convertedValue);
            }

            return results;
        }
    }

    internal enum ConversionDirection
    {
        None,
        FromDotNetToSql,
        FromSqlToDotNet
    }
}