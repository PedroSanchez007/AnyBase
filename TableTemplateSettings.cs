using System.Collections.Generic;

namespace AnyBase
{
    /// <summary>
    /// Initialised table templates.
    /// </summary>
    public static class TableTemplateSettings
    {
        // Initialised table templates, to go in the initialised list.
        // Note that a table must be listed with a primary key of "id" to be created with an autonumber field added.
        private static readonly TableTemplate TestTableTemplate = new TableTemplate("TestTable",  new[] { "Primary1", "Primary2" }, dataTypeOverridesByFieldsByProvider: new Dictionary<DatabaseProvider, Dictionary<string, string>>() { { DatabaseProvider.SqlServer, new Dictionary<string, string>() { { "Primary2", "nvarchar(20)" } } } });
        private static readonly TableTemplate ActContactTemplate = new TableTemplate("ActContact",  new[] { "myId" });
        private static readonly TableTemplate BankCodeTemplate = new TableTemplate("BankCode",  new[] { "CompanyId", "ACCOUNT_REF" });
        private static readonly TableTemplate CategoryTemplate = new TableTemplate("Category",  new[] { "id" });
        private static readonly TableTemplate CharityFundTemplate = new TableTemplate("CharityFund",  new[] { "CompanyId", "FUNDID" });
        private static readonly TableTemplate CostCodeTemplate = new TableTemplate("CostCode",  new[] { "CompanyId", "CostCodeID" });
        private static readonly TableTemplate CurrencyTemplate = new TableTemplate("Currency",  new[] { "CompanyId", "CODE" });
        private static readonly TableTemplate CustomerTemplate = new TableTemplate("Customer",  new[] { "CompanyId", "ACCOUNT_REF" });
        private static readonly TableTemplate DepartmentCodeTemplate = new TableTemplate("DepartmentCode",  new[] { "CompanyId", "DEPT_REF" });
        private static readonly TableTemplate FinancialBudgetTemplate = new TableTemplate("FinancialBudget",  new[] { "CompanyId", "DepartmentID", "NominalRefn", "Year", "Period" });
        private static readonly TableTemplate HeaderDataTemplate = new TableTemplate("HeaderData",  new[] { "CompanyId", "headerRecordNumber" });
        private static readonly TableTemplate InvoiceTemplate = new TableTemplate("Invoice",  new[] { "CompanyId", "INVOICE_NUMBER" }, excludeFields: new[] { "id", "memorisedName" });
        private static readonly TableTemplate LineItemTemplate = new TableTemplate("LineItem",  new[] { "CompanyId", "documentType", "ITEMID" });
        private static readonly TableTemplate NominalCodeTemplate = new TableTemplate("NominalCode",  new[] { "CompanyId", "ACCOUNT_REF" }, excludeFields: new[] { "bankRecord" });
        private static readonly TableTemplate NominalDataTemplate = new TableTemplate("NominalData",  new[] { "ACCOUNT_REF" });
        private static readonly TableTemplate PriceListTemplate = new TableTemplate("PriceList",  new[] { "CompanyId", "reference" });
        private static readonly TableTemplate PriceTemplate = new TableTemplate("Price",  new[] { "CompanyId", "reference" });
        private static readonly TableTemplate ProductCodeTemplate = new TableTemplate("ProductCode",  new[] { "CompanyId", "STOCK_CODE" });
        private static readonly TableTemplate ProjectTemplate = new TableTemplate("Project",  new[] { "CompanyId", "RecordNumber" });
        private static readonly TableTemplate PurchaseOrderTemplate = new TableTemplate("PurchaseOrder",  new[] { "CompanyId", "ORDER_NUMBER" });
        private static readonly TableTemplate SageAddressTemplate = new TableTemplate("SageAddress",  new[] { "CompanyId", "sageAddressType", "addressKey" });
        private static readonly TableTemplate SalesAddressTemplate = new TableTemplate("SalesAddress",  new[] { "CompanyId", "sageAddressType" });
        private static readonly TableTemplate SalesOrderTemplate = new TableTemplate("SalesOrder",  new[] { "CompanyId", "ORDER_NUMBER" });
        private static readonly TableTemplate SalesRecordTemplate = new TableTemplate("SalesRecord",  new[] { "CompanyId", "ACCOUNT_REF" });
        private static readonly TableTemplate SetupDataTemplate = new TableTemplate("SetupData",  new[] { "CompanyId", "toCode" });
        private static readonly TableTemplate SupplierCodeTemplate = new TableTemplate("SupplierCode",  new[] { "CompanyId", "ACCOUNT_REF" });
        private static readonly TableTemplate SplitDataTemplate = new TableTemplate("SplitData",  new[] { "CompanyId", "TRAN_NUMBER" });
        private static readonly TableTemplate StockCategoryTemplate = new TableTemplate("StockCategory",  new[] { "CompanyId", "categoryNumber" });
        private static readonly TableTemplate TaxCodeTemplate = new TableTemplate("TaxCode",  new[] { "CompanyId", "taxRateNumber" });

        // Initialised table templates list.
        private static readonly Dictionary<string, TableTemplate> TableTemplatesByName = new Dictionary<string, TableTemplate>()
        {
            { "TestTable", TestTableTemplate }, 
            { "ActContact", ActContactTemplate }, 
            { "BankCode", BankCodeTemplate }, 
            { "Category", CategoryTemplate }, 
            { "CharityFund", CharityFundTemplate }, 
            { "CostCode", CostCodeTemplate }, 
            { "Customer", CustomerTemplate }, 
            { "DepartmentCode", DepartmentCodeTemplate }, 
            { "FinancialBudget", FinancialBudgetTemplate }, 
            { "HeaderData", HeaderDataTemplate }, 
            { "Invoice", InvoiceTemplate }, 
            { "LineItem", LineItemTemplate }, 
            { "NominalCode", NominalCodeTemplate }, 
            { "NominalData", NominalDataTemplate }, 
            { "PriceList", PriceListTemplate }, 
            { "Price", PriceTemplate }, 
            { "ProductCode", ProductCodeTemplate }, 
            { "Project", ProjectTemplate }, 
            { "PurchaseOrder", PurchaseOrderTemplate }, 
            { "SageAddress", SageAddressTemplate }, 
            { "SalesAddress", SalesAddressTemplate }, 
            { "SalesOrder", SalesOrderTemplate }, 
            { "SalesRecord", SalesRecordTemplate }, 
            { "SetupData", SetupDataTemplate }, 
            { "SupplierCode", SupplierCodeTemplate }, 
            { "SplitData", SplitDataTemplate }, 
            { "StockCategory", StockCategoryTemplate }, 
            { "TaxCode", TaxCodeTemplate }
        };

        /// <summary>
        /// Fetch id field for this table.
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        public static List<string> FetchPrimaryFields(string tableName)
        {
            var result = new List<string>();
            
            // TODO Change to default(_) if this is not a reference type
            if (TableTemplatesByName.TryGetValue(tableName, out var tableTemplate))
                result = tableTemplate.PrimaryKeys;

            return result;
        }

        /// <summary>
        /// Fetch the override data type name, if any.
        /// </summary>
        /// <returns>Empty string if this field isn't overridden.</returns>
        internal static string FetchOverrideDataTypeName(DatabaseProvider provider, string tableName, string field)
        {
            var result = string.Empty;
            
            // TODO Change to default(_) if this is not a reference type
            if (TableTemplatesByName.TryGetValue(tableName, out var tableTemplate))
                result = tableTemplate.OverrideDataTypeName(provider, field);

            return result;
        }

        /// <summary>
        /// Fetch fields that should be skipped in the ORM between a .NET object and it's agnostic destination database.
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns>An empty list, if the dictionary does not contain properties to exclude for this table.</returns>
        /// <remarks></remarks>
        internal static List<string> FetchPropertiesToExclude(string tableName)
        {
            var result = new List<string>();

            // TODO Change to default(_) if this is not a reference type
            if (TableTemplatesByName.TryGetValue(tableName, out var tableTemplate))
                result = tableTemplate.ExcludedFields;

            return result;
        }

        /// <summary>
        /// Test if this field is a primary or compound key in this table.
        /// </summary>
        internal static bool FieldIsPrimaryKey(string tableName, string fieldName)
        {
            var result = false;

            // TODO Change to default(_) if this is not a reference type
            if (TableTemplatesByName.TryGetValue(tableName, out var tableTemplate))
                result = tableTemplate.PrimaryKeys.Contains(fieldName);

            return result;
        }

        /// <summary>
        /// Test if this table has the default primary key of "id".
        /// </summary>
        internal static bool HasDefaultPrimaryKey(string tableName)
        {
            var result = false;

            // TODO Change to default(_) if this is not a reference type
            if (TableTemplatesByName.TryGetValue(tableName, out var tableTemplate))
                result = tableTemplate.HasDefaultPrimaryKey;

            return result;
        }
    }
}