using System;
using System.Reflection;
using System.Data.Common;

namespace AnyBase
{
    /// <summary>
    /// A blueprint or store of information relating to a field, in a .net object, that will facilitate the creation and manipulation of a MySQL table.
    /// Overriden by more specific method, property, fixed value or incremental index blueprints.
    /// </summary>
    /// <remarks></remarks>
    public abstract class DataMemberBlueprint
    {
        protected readonly DatabaseProvider _provider;
        protected readonly string _sqlDataTypeName;

        internal DataMemberBlueprint(string fieldName, Type fieldType, DatabaseProvider provider, string sqlDataTypeName, bool isPrimaryKey = false)
        {
            FieldName = fieldName;
            FieldType = fieldType;
            CoreType = ConvertToCoreType(FieldType);
            _provider = provider;
            _sqlDataTypeName = sqlDataTypeName;
            IsPrimaryKey = isPrimaryKey;
        }

        /// <summary>
         /// This fields type, stripped of its nullable wrapper, if applicable.
         /// </summary>
         /// <value></value>
         /// <returns></returns>
         /// <remarks></remarks>
        internal Type CoreType { get; }

        internal string FieldName { get; }

        internal Type FieldType { get; }

        internal bool IsPrimaryKey { get; }

        /// <summary>
         /// Get the value of either the property or the method, depending on which was populated.
         /// </summary>
         /// <typeparam name="T"></typeparam>
         /// <param name="record"></param>
         /// <returns></returns>
         /// <remarks></remarks>
        protected internal abstract object CalculateFieldValue<T>(T record);

        /// <summary>
         /// Create a DbParameter object.
         /// </summary>
         /// <typeparam name="T"></typeparam>
         /// <param name="record"></param>
         /// <param name="onlyPrimaryKey"></param>
         /// <param name="prefix"></param>
         /// <returns></returns>
         /// <remarks></remarks>
        internal DbParameter GetDbParameter<T>(DatabaseAccess database, T record, bool onlyPrimaryKey, string prefix)
        {
            DbParameter result = null;
            if (!onlyPrimaryKey || IsPrimaryKey)
            {
                result = database.DatabaseFactory.CreateParameter();
                result.ParameterName = prefix + FieldName;
                result.Value = CalculateFieldValue(record);
            }

            return result;
        }

        /// <summary>
         /// Get the nullable text required for creating a field in a table.
         /// </summary>
         /// <returns>Either "NULL" or "NOT NULL"</returns>
         /// <remarks>Some properties are nullable (integer?) and this should be specified when creating a table, so the constraint is applied.
         /// Note that primary keys cannot be nullable.</remarks>
        internal string GetNullableText()
        {
            var result = string.Empty;

            if (!IsPrimaryKey && FieldType.IsNullableOrString())
                result = "NULL";
            else
                result = "NOT NULL";

            return result;
        }

        protected internal virtual string GetSqlFieldCreationText()
        {
            return string.Format("{0} {1} {2}", FieldName, _sqlDataTypeName, GetNullableText());
        }

        /// <summary>
         /// Get the core type, which is the type stripped of its nullable wrapper if it has one, Int32 in the case of Enums or the given type if non of the above apply.
         /// </summary>
         /// <returns></returns>
         /// <remarks>
         /// Some properties are Enums. Always convert these to Integer type as they can only be integer, byte, sbyte, short, ushort, uint, long, or ulong.
         /// All these convert to Integer. It is very unlikely that an enum would have a long value that is greater than the upper limit of an integer.
         /// </remarks>
        internal static Type ConvertToCoreType(Type standardType)
        {

            // Return integer for Enums.
            if (standardType.BaseType != null && standardType.BaseType.Name == "Enum")
                return typeof(Int32);

            // Nullable types store their type name a little differently.
            if (standardType.IsNullable())
                return Nullable.GetUnderlyingType(standardType);

            // Otherwise the core type is the same as the type.
            return standardType;
        }

        public override string ToString()
        {
            return FieldName;
        }
    }

    internal class FieldBlueprint : DataMemberBlueprint
    {
        internal FieldBlueprint(string fieldName, Type fieldType, DatabaseProvider provider, string sqlDataTypeName, bool isPrimaryKey = false) : base(fieldName, fieldType, provider, sqlDataTypeName, isPrimaryKey)
        {
        }

        /// <summary>
         /// This object never has a field value.
         /// </summary>
         /// <typeparam name="T"></typeparam>
         /// <param name="record"></param>
         /// <returns></returns>
         /// <remarks></remarks>
        protected internal override object CalculateFieldValue<T>(T record)
        {
            return new object();
        }
    }

    internal class IncrementBlueprint : DataMemberBlueprint
    {
        internal IncrementBlueprint(string fieldName, DatabaseProvider provider, string sqlDataTypeName, bool isPrimaryKey = false) : base(fieldName, typeof(int), provider, sqlDataTypeName, isPrimaryKey)
        {
        }

        /// <summary>
         /// This object never has a field value.
         /// </summary>
         /// <typeparam name="T"></typeparam>
         /// <param name="record"></param>
         /// <returns></returns>
         /// <remarks></remarks>
        protected internal override object CalculateFieldValue<T>(T record)
        {
            return new object();
        }

        protected internal override string GetSqlFieldCreationText()
        {
            switch (_provider)
            {
                case DatabaseProvider.MySql:
                    {
                        return $"{FieldName} {_sqlDataTypeName} NOT NULL AUTO_INCREMENT";
                    }

                case DatabaseProvider.Sqlite:
                    {
                        return $"{FieldName} INTEGER PRIMARY KEY AUTOINCREMENT";
                    }

                case DatabaseProvider.SqlServer:
                    {
                        throw new NotImplementedException($"Database provider {_provider} not catered for.");
                        return $"{FieldName} {_sqlDataTypeName} NOT NULL AUTO_INCREMENT";
                    }

                default:
                    {
                        throw new NotImplementedException(
                            $"Database provider {_provider} not catered for.");
                        break;
                    }
            }
        }
    }

    internal class MethodBlueprint : DataMemberBlueprint
    {
        private readonly MethodInfo _fieldMethod;

        internal MethodBlueprint(string fieldName, Type fieldType, MethodInfo fieldMethod, DatabaseProvider provider, string sqlDataTypeName, bool isPrimaryKey = false) : base(fieldName, fieldType, provider, sqlDataTypeName, isPrimaryKey)
        {
            _fieldMethod = fieldMethod;
        }

        /// <summary>
         /// Reflect the value of the method.
         /// </summary>
         /// <typeparam name="T"></typeparam>
         /// <param name="record"></param>
         /// <returns></returns>
         /// <remarks></remarks>
        protected internal override object CalculateFieldValue<T>(T record)
        {
            return _fieldMethod.Invoke(record, null);
        }
    }

    internal class PropertyBlueprint : DataMemberBlueprint
    {
        private readonly PropertyInfo _fieldProperty;

        protected internal PropertyBlueprint(PropertyInfo fieldProperty, DatabaseProvider provider, string sqlDataTypeName, bool isPrimaryKey = false) : base(fieldProperty.Name, fieldProperty.PropertyType, provider, sqlDataTypeName, isPrimaryKey)
        {
            _fieldProperty = fieldProperty;
        }

        /// <summary>
         /// Reflect the value of the property.
         /// </summary>
         /// <typeparam name="T"></typeparam>
         /// <param name="record"></param>
         /// <returns></returns>
         /// <remarks></remarks>
        protected internal override object CalculateFieldValue<T>(T record)
        {
            return _fieldProperty.GetValue(record, null);
        }
    }

    internal class ValueBlueprint : DataMemberBlueprint
    {
        private readonly object fieldValue;

        protected internal ValueBlueprint(string fieldName, Type fieldType, object fieldValue, DatabaseProvider provider, string sqlDataTypeName, bool isPrimaryKey = false) 
            : base(fieldName, fieldType, provider, sqlDataTypeName, isPrimaryKey)
        {
            this.fieldValue = fieldValue;
        }

        /// <summary>
         /// Reflect the value of the property.
         /// </summary>
        protected internal override object CalculateFieldValue<T>(T record)
        {
            return fieldValue;
        }
    }
}