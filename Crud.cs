using System;
using System.Collections.Generic;
using System.Data;

namespace AnyBase
{
    // 1. See remarks. The solution to this is to have a NonGenericCrud class, which inherits Crud. It can keep a running dictionary to convert the data types.
    // Both NonGenericCrud and GenericCrud will perform conversions through a different methodology but deliver the converted values to Crud, which will not perform any conversions.
    // 2. There is another problem which needs resolving. I want to be able to store a Crud instance, to keep a connection alive (though closed) for speed. However, that leads to this superfluous class
    // that forces the instantiation of the Crud query class, which enforce thread safety.
    // 3. Currently, the CrudQuery ReadQuery does not batch read operations but it should.
    // 4. Test that all the fieldBlueprints return the correct underlying type, not just the type in the case of it being nullable.

    /// <summary>
    /// Non-generic CRUD operations.
    /// </summary>
    /// <remarks>
    /// When generic CRUD operations are performed, the object is known and therefore the data types of the properties can be reflected once.
    /// With non-generic CRUD operation however, we will only be sure to have an exhaustive list of the property types after the last field of the last record has been analysed.
    /// Rather than reflect the type of every field which is expensive, we will keep a running dictionary of known property types by field name,
    /// so they only need to be reflected once and can be accessed quickly.
    /// _destinationTypesByDotNetTypeByProvider has to go as part of this.
    /// </remarks>
    public class Crud
    {
        protected Crud(ConnectionDetail connectionDetail)
        {
            Database = DatabaseCreator.Factory(connectionDetail);
        }

        /// <summary>
        /// Database information, including the log on information.
        /// </summary>
        public DatabaseAccess Database { get; }

        public virtual CudResult DeleteRecords(string tableName, List<string> whereFieldNames, List<List<object>> whereValueSets)
        {
            var query = new DeleteQuery(Database, tableName, whereFieldNames, whereValueSets);

            return query.CudResult;
        }

        public virtual CudResult InsertRecords(string tableName, List<string> insertFieldNames, List<Type> insertFieldTypes, List<List<object>> insertValueSets)
        {
            var query = new InsertQuery(Database, tableName, insertFieldNames, insertFieldTypes, insertValueSets);

            return query.CudResult;
        }

        public virtual NonGenericReadResult ReadRecords(string tableName, List<string> whereFieldNames, List<List<object>> whereValueSets, List<string> selectFields = null)
        {
            selectFields = selectFields.ToEmptyListIfNull();

            var query = new ReadQuery(Database, tableName, new DataTable(), selectFields, whereFieldNames, whereValueSets);

            return query.ReadResult;
        }

        public virtual CudResult UpdateRecords(string tableName, List<string> setFieldNames, List<Type> updateFieldTypes, List<List<object>> setValueSets, List<string> whereFieldNames, List<List<object>> whereValueSets)
        {
            var query = new UpdateQuery(Database, tableName, setFieldNames, updateFieldTypes, setValueSets, whereFieldNames, whereValueSets);

            return query.CudResult;
        }

        public virtual ScalarResult Scalar(string sqlPrefix, List<string> whereFieldNames, List<Type> fieldTypes, List<object> whereValueSets)
        {
            var query = new ScalarQuery(Database, sqlPrefix, whereFieldNames, fieldTypes, whereValueSets);

            return query.ScalarResult;
        }

        // Bespoke SQLite queries for where an object is stored in one field.

        /// <summary>
        /// Fetch a datatable of a field in a table and it's id.
        /// </summary>
        /// <returns>Empty dictionary if none are found.</returns>
        /// <remarks></remarks>
        public NonGenericReadResult RetrieveField(string tableName, string idFieldName, string targetFieldName)
        {
            var crudResult = new ReadQuery(
                Database, 
                tableName, 
                new DataTable(), 
                new List<string> { idFieldName, targetFieldName },
                new List<string>() {idFieldName},
                new List<List<object>>{ new List<object>( )});

            return crudResult.ReadResult;
        }

        /// <summary>
        /// Select a field in database table.
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="idFieldName"></param>
        /// <param name="recordId"></param>
        /// <param name="targetFieldName"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        public NonGenericReadResult RetrieveFieldById(string tableName, string idFieldName, int recordId, string targetFieldName)
        {
            var query = new ReadQuery(
                Database, 
                tableName, 
                new DataTable(), 
                new List<string>() {idFieldName},
                new List<string>() {idFieldName},
                new List<List<object>> {new List<object>() {recordId}});

            return query.ReadResult;
        }

        /// <summary>
        /// Update record in a field in a database table.
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="idFieldName"></param>
        /// <param name="recordId"></param>
        /// <param name="fieldToUpdate"></param>
        /// <param name="correctedText"></param>
        /// <remarks></remarks>
        public CudResult UpdateField(string tableName, string idFieldName, Type fieldType, int recordId, string fieldToUpdate, string correctedText)
        {
            var crud = new UpdateQuery(
                Database,
                tableName,
                new List<string>() {fieldToUpdate},
                new List<Type> { fieldType},
                new List<List<object>> {new List<object>(){correctedText}},
                new List<string>() {fieldToUpdate},
                new List<List<object>> {new List<object>() {recordId}});

            return crud.CudResult;
        }
    }
}