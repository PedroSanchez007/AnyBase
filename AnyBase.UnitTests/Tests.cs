using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace AnyBase.UnitTests
{
    [TestFixture]
    public class Tests
    {
        // General variables.
        static readonly string _testDatabaseName = "test_database";
        // Do not change the test table name as it is derived from the TestTable type name.
        private static readonly string TestTableName = "testtable";
        private static readonly string PiMutexName = "746B478E-DE77-4348-9137-F8CEB0DF9AE7";

        // SQLite variables.
        private static readonly string WorkFolder = @"C:\Everyone";
        private static readonly string SqliteTestDatabaseName = $"{_testDatabaseName}.s3db";

        // MySQL test connection parameters.
        private static readonly DatabaseProvider MySqlProvider = DatabaseProvider.MySql;
        private static readonly string MySqlServerAddress = "localHost";
        private static readonly int MySqlPort = 8249;
        private static readonly string MySqlUserId = "root";
        private static readonly string MySqlPassword = "p$5a3*AmYHQ1";

        // SQL Server connection using SQL Server Authentication
        // private static readonly DatabaseProvider ExampleSqlServerProvider = DatabaseProvider.SqlServer;
        // private static readonly string ExampleSqlServerServerAddress = @"PETE-ROG\STRATFORDHERALD1";
        // private static readonly int ExampleSqlServerPort = 8249;
        // private static readonly string ExampleSqlServerUserId = @"PETE-ROG\Pete";
        // private static readonly string ExampleSqlServerPassword = "flop587)rook";
        
        // SQL Server connection using SQL Server Authentication
        private static readonly DatabaseProvider SqlServerProvider = DatabaseProvider.SqlServer;
        private static readonly string SqlServerServerAddress = @"DESKTOP-QR9HO7J";
        private static readonly int SqlServerPort = 8249;
        private static readonly string SqlServerUserId = @"PETE-ROG\Pete";
        private static readonly string SqlServerPassword = "flop587)rook";

        [Test]
        public void TestDatabaseCreate()
        {
            var testConnection = new TrustedConnectionDetail(
                SqlServerProvider,
                SqlServerServerAddress,
                "to_delete2");

            var testObjects = TestTable.SqlServerTestObjects();
            // var testObjects = new List<TestTable2> {new TestTable2()};

            var genericCrud = new GenericCrud(testConnection);
            
            Assert.IsTrue(genericCrud.Database.DropDatabase());

            Assert.IsTrue(genericCrud.Database.CreateDatabase());
            
            Assert.IsTrue(genericCrud.Database.CreateTable<TestTable>(false));

            var writeRow = genericCrud.InsertGenericRecords(testObjects);
            Assert.IsTrue(!writeRow.Errors.Any());

            // Connect again. There should be no connection error this time.
            genericCrud = new GenericCrud(testConnection);

            // Test generic crud operations.GenericCrudOperationsInner(genericCrud);
            // Assert.IsTrue(genericCrud.Database.DropDatabase());
        }
    }

    public class TestTable2
    {
        public int Primary1 { get; }

        public TestTable2()
        {
            Primary1 = 1;
        }
    }

    public class TestTable
    {
        public int Primary1 { get; }
        public string Primary2 { get; }
        public bool BooleanType { get; }
        public byte ByteType { get; }
        public byte[] ByteArray { get; }
        public DateTime DateTimeType { get; }
        public decimal DecimalType { get; }
        public float SingleType { get; }
        public double DoubleType { get; }
        public Guid GuidType { get; }
        public short ShortType { get; }
        public int IntegerType { get; }
        public long LongType { get; }
        //public object ObjectType { get; }
        //public sbyte SByteType { get; }
        // public string StringType { get; }
        // public  TimeSpan TimeSpanType { get; }
        // public ushort UShortType { get; }
        // public uint UIntegerType { get; }
        //public ulong ULongType { get; }
        // public bool? BooleanNullable { get; }
        // public byte? ByteNullable { get; }
        // public DateTime? DateTimeNullable { get; }
        // public decimal? DecimalNullable { get; }
        // public double? DoubleNullable { get; }
        // public Guid? GuidNullable { get; }
        // public short? ShortNullable { get; }
        // public int? IntegerNullable { get; }
        // public long? LongNullable { get; }
        // public sbyte? SByteNullable { get; }
        // public float? SingleNullable { get; }
        // public TimeSpan? TimeSpanNullable { get; }
        // public ushort? UShortNullable { get; }
        // public uint? UIntegerNullable { get; }
        //public ulong? ULongNullable { get; }
        
        // Test byte arrays.
        private static readonly Byte[] TestByteArray = {0, 0, 255};
        private static readonly Byte[] DefaultByteArray = new byte[0];
        
        /// <summary>
        ///  I don't know why when Decimal.MinValue is written to SQLite and then returned to a datatable it throws an overflow exception,
        ///  but if I reduce the value slightly, it works. I suspect that SQLite is rounding the number very slightly, to reduce storage.
        ///  </summary>
        private static readonly double SqliteDecimalMin = -79228162514264300000000000000d;

        private static readonly decimal SqliteDecimalMax = 79228162514264300000000000000m;
        private static readonly decimal SqliteDecimalNegativeMaxPrecision = -9.01234567890123m;
        private static readonly decimal SqliteDecimalPositiveMaxPrecision = 9.01234567890123m;
        private static readonly ulong SqliteUInt64Max = 18446744073709500000ul;

        private static readonly decimal MySqlDecimalNegativeMaxPrecision = -0.1234567890123456789012345679m;
        private static readonly decimal MySqlDecimalPositiveMaxPrecision = 0.1234567890123456789012345679m;
        private static readonly DateTime MySqlDateMax = new DateTime(9999, 12, 31, 23, 59, 59, 999);

        public TestTable()
        {
            
        }
        
        public TestTable(
            int primary1,
            string primary2,
            bool booleanType,
            byte byteType,
            byte[] byteArray,
            DateTime dateTime,
            decimal decimalType,
            float singleType,
            double doubleType,
            Guid guid,
            short shortType,
            int integerType,
            long longType
            //object objectType
            //sbyte sByteType,
            // string stringType,
            // TimeSpan spanOfTime)
            //ushort uShortType,
            //uint uIntegerType,
            //ulong uLongType,
            // bool? booleanNullable,
            // byte? byteNullable,
            // DateTime? dateTimeNullable,
            // decimal? decimalNullable,
            // double? doubleNullable,
            // Guid? guidNullable,
            // short? shortNullable,
            // int? integerNullable,
            // long? longNullable,
            //sbyte? sByteNullable,
            // float? singleNullable,
            // TimeSpan? panOfTimeNullable)
            //ushort? uShortNullable,
            //uint? uIntegerNullable)
            //ulong? uLongNullable
            )
                   {
                        Primary1 = primary1;
                        Primary2 = primary2;
                        BooleanType = booleanType;
                        ByteType = byteType;
                        ByteArray = byteArray;
                        DateTimeType = dateTime;
                        DecimalType = decimalType;
                        SingleType = singleType;
                        DoubleType = doubleType;
                        GuidType = guid;
                        ShortType = shortType;
                        IntegerType = integerType;
                        LongType = longType;
                        //ObjectType = objectType;
                        //SByteType = sByteType;
                        // StringType = stringType;
                        // TimeSpanType = spanOfTime;
                        //UShortType = uShortType;
                        //UIntegerType = uIntegerType;
                        //ULongType = uLongType;
                        // BooleanNullable = booleanNullable;
                        // ByteNullable = byteNullable;
                        // DateTimeNullable = dateTimeNullable;
                        // DecimalNullable = decimalNullable;
                        // DoubleNullable = doubleNullable;
                        // GuidNullable = guidNullable;
                        // ShortNullable = shortNullable;
                        // IntegerNullable = integerNullable;
                        // LongNullable = longNullable;
                        //SByteNullable = sByteNullable;
                        // SingleNullable = singleNullable;
                        // TimeSpanNullable = panOfTimeNullable;
                        //UShortNullable = uShortNullable;
                        //UIntegerNullable = uIntegerNullable;
                        //ULongNullable = uLongNullable;                    
                   }

        public static List<TestTable> SqlServerTestObjects()
        {
            return new List<TestTable> { new TestTable(
                1,
                "pants",
                false,
                byte.MinValue,
                TestByteArray,
                new DateTime(1753, 1, 1),
                999m,
                -999f,
                99999d,
                new Guid("9266d1ea-9106-499f-9677-23b93a36d08c"),
                short.MinValue,
                int.MinValue,
                long.MinValue
                //new object()
                //sbyte.MinValue,
                // "",
                // TimeSpan.MinValue
                //ushort.MinValue,
                //uint.MinValue,
                //ulong.MinValue,
                // false,
                // byte.MinValue,
                // new DateTime(1753, 1, 1),
                // decimal.MinValue,
                // double.MinValue,
                // new Guid("9266d1ea-9106-499f-9677-23b93a36d08c"),
                // short.MinValue,
                // int.MinValue,
                // long.MinValue,
                //sbyte.MinValue,
                // float.MinValue,
                // TimeSpan.MinValue
                //ushort.MinValue,
                //uint.MinValue
                //ulong.MinValue
                )};
        }
    }
}