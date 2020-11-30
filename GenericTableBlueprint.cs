using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace AnyBase
{
    /// <summary>
    ///  Information relating to the attributes of a .net object, that will facilitate the creation and manipulation of a SQL table, whose fields represent the properties and methods of that object.
    ///  Properties and methods are reflected from the type T. These will be used as fields in the SQL table that will be created, or as fields when performing CRUD operations on this SQL table.
    ///  The configuration of each type T or resulting SQL table is stored in Settings. It contains dictionaries, that determine
    ///  1. Which field is the primary key, or which fields are the compound keys.
    ///  2. Fields which exist in the type T, but should not be used in the SQL table.
    ///  3. The Visual Basic type of each property or method, so a corresponding SQL type can be used in the SQL table.
    ///  This class also exposes helper methods to build SQL parameters and strings, based on the table blueprint, for use in CRUD operations.
    ///  This class is subclassed, for specific implementations of SQL tables, for example Pan Intelligence.
    ///  </summary>
    ///  <typeparam name="T"></typeparam>
    ///  <remarks></remarks>
    public class GenericTableBlueprint<T> : TableBlueprint
    {
        internal GenericTableBlueprint(DatabaseProvider databaseProvider) 
            : base(databaseProvider, typeof(T).Name)
        {

            // Reflect table name and properties.
            var typeProperties = typeof(T).GetProperties().ToList();

            // Qualify properties by excluding...
            // 1. Non public properties.
            // 2. Collections (include strings)
            // 3. Properties in the dictionary to exclude.
            // 4. Where there are duplicate properties because of sub-typing, exclude the base type property as it is overridden.
            var qualifiedTypeProperties = GetQualifiedTypeProperties(typeProperties);

            // Convert the properties to a field blueprints and add a primary key field if none is indicated in the library.
            Fields = CreateFieldBlueprints(qualifiedTypeProperties);
        }

        /// <summary>
        /// Create field blueprints.
        /// 
        /// </summary>
        /// <param name="qualifiedTypeProperties"></param>
        /// <returns></returns>
        /// <remarks>Subclasses may add extra blueprint fields.</remarks>
        private List<DataMemberBlueprint> CreateFieldBlueprints(List<PropertyInfo> qualifiedTypeProperties)
        {
            var results = GetDefaultPrimaryKeyIfNoneIsSpecified();

            // Add qualified properties.
            foreach (var typeProperty in qualifiedTypeProperties)
            {
                var coreTypeName = DataMemberBlueprint.ConvertToCoreType(typeProperty.PropertyType).Name;
                var sqlDataTypeName = DataTypeConversionSettings.GetSqlDataType(Provider, coreTypeName, TableName, typeProperty.Name);
                var isPrimaryKey = TableTemplateSettings.FieldIsPrimaryKey(TableName, typeProperty.Name);

                var propertyBlueprint = new PropertyBlueprint(typeProperty, Provider, sqlDataTypeName, isPrimaryKey);

                results.Add(propertyBlueprint);
            }

            return results;
        }

        /// <summary>
        /// Exclude properties that should not be added to the database because they are a list or have been explicitly excluded.
        /// In addition, if a property is reflected more than once, because it appears in a base class and it's subclass, then exclude the base class entry.
        /// </summary>
        /// <param name="properties"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        internal List<PropertyInfo> GetQualifiedTypeProperties(List<PropertyInfo> properties)
        {
            var results = new List<PropertyInfo>();

            properties = GetPropertiesWithoutHardCodedExclusions(properties);
            properties = properties.Where(x => !x.PropertyType.IsCollectionExcludingStringAndByte()).ToList();

            foreach (var prop in properties)
            {

                // Sometimes a property will already exist because a subclass property is overriding a base class property.
                // Identify the item in the list that already exists.
                PropertyInfo propertyThatAlreadyExists = null;
                foreach (var result in results.Where(result => result.Name == prop.Name))
                {
                    propertyThatAlreadyExists = result;
                }

                // If there is a property that already exists then identify the one to delete.
                if (propertyThatAlreadyExists != null)
                {
                    // If the declaring type of this property matches the type we are importing then this property is the sub-class version.
                    // If this property is the sub-class version of the property then delete the base-class version and add the sub-class version in it's place.
                    // If this is not the sub-class version then do not add it to the list, as the list already contains the sub-class version.
                    if (prop.DeclaringType == typeof(T))
                    {
                        results.Remove(propertyThatAlreadyExists);
                        results.Add(prop);
                    }
                }
                else

                    // No duplicate was found; add the property.
                    results.Add(prop);
            }

            return results;
        }

        private List<PropertyInfo> GetPropertiesWithoutHardCodedExclusions(IEnumerable<PropertyInfo> properties)
        {
            var propertiesToExclude = TableTemplateSettings.FetchPropertiesToExclude(TableName);

            return properties.Where(prop => !propertiesToExclude.Contains(prop.Name)).ToList();
        }

        /// <summary>
        /// Convert a list of generic objects to a list of objects, which will be used in a SQL query to perform crud operations.
        /// </summary>
        /// <param name="records"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        internal IEnumerable<IEnumerable<object>> FieldValues(IEnumerable<T> records)
        {
            return records
                .Select(record => Fields
                    .Select(field => field.CalculateFieldValue(record)));
        }

        /// <summary>
        /// Build a list of values for 'where' parameters, for each record. These will only be the primary key fields.
        /// </summary>
        internal IEnumerable<IEnumerable<object>> WhereFieldValues(IEnumerable<T> records)
        {
            return records
                .Select(record => Fields
                    .Where(field => field.IsPrimaryKey)
                    .Select(pkf => pkf.CalculateFieldValue(record)))
                .Cast<List<object>>();
        }
    }
}