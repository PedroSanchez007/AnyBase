using System;
using System.Collections.Generic;

namespace AnyBase
{
    /// <summary>
    /// Store module data, dictionaries and settings.
    /// </summary>
    /// <remarks>
    /// app.config has been changed to include SQLite in the DbProviderFactories.
    /// </remarks>
    internal static class Settings
    {
        /// <summary>
        /// Store of the target field names in the various tables in the various databases.
        /// </summary>
        /// <remarks></remarks>
        private static readonly Dictionary<string, Dictionary<string, string>> TargetFieldNamesByTableByDatabase = 
            new Dictionary<string, Dictionary<string, string>>()
            {
                { "assc", new Dictionary<string, string>(){
                    { "backgroundServices", "v" }} }, 
                { "cache", new Dictionary<string, string>(){
                    { "BankData", "v" }, 
                    { "CompanyDeliveryRecord", "v" }, 
                    { "ControlData", "v" }, 
                    { "CostCode", "v" }, 
                    { "CurrencyData", "v" }, 
                    { "DepartmentData", "v" }, 
                    { "FinancialBudget", "v" }, 
                    { "HeaderData", "v" }, 
                    { "InvoiceItem", "v" }, 
                    { "InvoiceRecord", "v" }, 
                    { "NominalRecord", "v" }, 
                    { "PriceListRecord", "v" }, 
                    { "PriceRecord", "v" }, 
                    { "Project", "v" }, 
                    { "PurchaseDeliveryRecord", "v" }, 
                    { "PurchaseRecord", "v" }, 
                    { "SalesDeliveryRecord", "v" }, 
                    { "SalesRecord", "v" }, 
                    { "SetupData", "v" }, 
                    { "SOPItem", "v" }, 
                    { "SOPRecord", "v" }, 
                    { "SplitData", "v" }, 
                    { "StockCategory", "v" }, 
                    { "StockRecord", "v" }} }, 
                { "panIntelligence", new Dictionary<string, string>() }, 
                { "poa", new Dictionary<string, string>(){
                    { "groupPolicies", "restrictions" }, 
                    { "memorisedPurchaseOrders", "po" }, 
                    { "purchaseOrderAuthorisationGroup", "groupInfo" }, 
                    { "purchaseOrders", "po" }, 
                    { "userRights", "restrictions" }}}
            };

        // Properties.
        
        /// <summary>
        /// Dictionary of assembly names.
        /// </summary>
        internal static Dictionary<DatabaseProvider, string> AssembliesByProvider { get; } = new Dictionary<DatabaseProvider, string>()
        {
            { DatabaseProvider.MySql, "MySql.Data.MySqlClient"},
            { DatabaseProvider.Odbc, "System.Data.Odbc"}, 
            { DatabaseProvider.OleDb, "System.Data.OleDb"},
            { DatabaseProvider.Oracle, "System.Data.OracleClient"}, 
            { DatabaseProvider.PostGres, "Npgsql"},
            { DatabaseProvider.Sqlite, "System.Data.SQLite"}, 
            { DatabaseProvider.SqlServer, "System.Data.SqlClient"},
            { DatabaseProvider.SqlServerCompactEdition, "System.Data.SqlServerCe.4.0"}
        };

        /// <summary>
        /// CRUD operations are batched to avoid OutOfMemory exceptions.
        /// </summary>
        internal static int CrudBatchSize { get; } = 10000;

        internal static string DefaultPrimaryKeyName { get; } = "id";

        /// <summary>
        /// Default user name.
        /// </summary>
        internal static string DefaultUserName { get; } = "root";

        // Methods.

        /// <summary>
        /// Build a guid mutex name that is the company id plus the data cache guid suffix.
        /// </summary>
        /// <param name="companyId"></param>
        /// <returns></returns>
        /// <remarks>
        /// Company numbers over 
        /// </remarks>
        internal static string BuildDataCacheMutex(int companyId)
        {
            if (companyId > 99 || companyId < 0)
                throw new ArgumentOutOfRangeException("The company id " + companyId + " is out of range. 0 - 99 is allowed.");

            var result = companyId.ToString("D2");

            return result;
        }

        /// <summary>
        /// Retrieve the target field name from the hard-coded dictionary.
        /// </summary>
        /// <param name="databaseName"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        internal static string TargetFieldName(string databaseName, string tableName)
        {
            var result = string.Empty;

            // Get target field name from sub-dictionary.
            if (!TargetFieldNamesByTableByDatabase.TryGetValue(tableName, out var targetFieldNamesByTable))
                throw new NotImplementedException("Database '" + databaseName + "' is not included in the dictionary of target field names.");

            // Get target field name from sub-dictionary.
            if (!targetFieldNamesByTable.TryGetValue(tableName, out result))
                throw new NotImplementedException("Table '" + tableName + "' is not included in the dictionary of target field names.");

            return result;
        }
    }

    /// <summary>
    /// Supported databases.
    /// </summary>
    /// <remarks></remarks>
    public enum DatabaseProvider
    {
        None,
        MySql,
        Odbc,
        OleDb,
        Oracle,
        PostGres,
        Sqlite,
        SqlServer,
        SqlServerCompactEdition
    }
}