using System.Collections.Generic;

namespace AnyBase
{
    /// <summary>
    /// Perform CRUD operations on a database from source .NET objects.
    /// Convert data types.
    /// </summary>
    /// <remarks>
    /// A database can experience problems if the same database has several commands run on it at the same time.
    /// Therefore, the option exists to use a named Mutex, so all applications and processes on this machine can only access this database one at a time.
    /// The named Mutex is passed in to CRUD commands.
    /// </remarks>
    public class GenericCrud : Crud
    {
        public GenericCrud(ConnectionDetail connectionDetail) : base(connectionDetail)
        {
        }

        // Methods.

        public virtual GenericReadResult<T> ReadGenericRecords<T>(List<T> records, List<string> selectFieldNames = null)
        {
            selectFieldNames = selectFieldNames.ToEmptyListIfNull();

            var tableBlueprint = new GenericTableBlueprint<T>(Database.MyConnectionDetail.Provider);
            var conversion = new DataTypeConversion<T>(tableBlueprint);
            var convertedWhereValueSets = conversion.ConvertValueSets(tableBlueprint.WhereFieldValues(records), ConversionDirection.FromDotNetToSql);

            var query = new ReadQuery(Database, tableBlueprint.TableName, tableBlueprint.Container, selectFieldNames, tableBlueprint.WhereFieldNames, convertedWhereValueSets);

            var convertedResults = conversion.ConvertValueSets(query.ReadResult.Data);
            var results = new GenericReadResult<T>(convertedResults, query.ReadResult.Errors);

            return results;
        }

        public virtual CudResult DeleteGenericRecords<T>(List<T> records)
        {

            // Convert the object to a table blueprint, which contains all the necessary information and provides the necessary functionality.
            var tableBlueprint = new GenericTableBlueprint<T>(Database.MyConnectionDetail.Provider);
            var conversion = new DataTypeConversion<T>(tableBlueprint);
            var convertedWhereValueSets = conversion.ConvertValueSets(tableBlueprint.WhereFieldValues(records), ConversionDirection.FromDotNetToSql);

            var query = new DeleteQuery(Database, tableBlueprint.TableName, tableBlueprint.WhereFieldNames, convertedWhereValueSets);

            return query.CudResult;
        }

        public CudResult InsertGenericRecords<T>(List<T> records)
        {
            var tableBlueprint = new GenericTableBlueprint<T>(Database.MyConnectionDetail.Provider);
            var conversion = new DataTypeConversion<T>(tableBlueprint);
            var convertedSetValueSets = conversion.ConvertValueSets(tableBlueprint.FieldValues(records), ConversionDirection.FromDotNetToSql);

            var query = new InsertQuery(Database, tableBlueprint.TableName, tableBlueprint.SetFieldNames, tableBlueprint.FieldTypes, convertedSetValueSets);

            return query.CudResult;
        }

        public CudResult UpdateGenericRecords<T>(List<T> records)
        {
            var tableBlueprint = new GenericTableBlueprint<T>(Database.MyConnectionDetail.Provider);
            return UpdateGenericRecords<T>(records, tableBlueprint);
        }

        public CudResult UpdateGenericRecords<T>(List<T> records, GenericTableBlueprint<T> tableBlueprint)
        {
            var conversion = new DataTypeConversion<T>(tableBlueprint);
            var convertedSetValueSets = conversion.ConvertValueSets(tableBlueprint.FieldValues(records), ConversionDirection.FromDotNetToSql);
            var convertedWhereValueSets = conversion.ConvertValueSets(tableBlueprint.WhereFieldValues(records), ConversionDirection.FromDotNetToSql);

            var query = new UpdateQuery(
                Database, 
                tableBlueprint.TableName, 
                tableBlueprint.SetFieldNames,
                tableBlueprint.FieldTypes,
                convertedSetValueSets, 
                tableBlueprint.WhereFieldNames, 
                convertedWhereValueSets);

            return query.CudResult;
        }
    }
}