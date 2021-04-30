using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Penetrator.DataPool.Validator
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true, Inherited = true)]
    public class ForeignKeyAttribute : MultiTableConstraintAttribute
    {
        /**
         * 参照先のテーブル型名
         **/
        public Type ReferenceTableType;
        /**
         * 参照先のカラム名(Field or Property名)
         **/
        public string ColumnName;
        public override MultiTableConstraintValidator GetValidator()
        {
            return new ForeignKeyAttributeValidator(this);
        }
    }

    public class ForeignKeyAttributeValidator : MultiTableConstraintValidator
    {
        private ForeignKeyAttribute Attribute;

        public ForeignKeyAttributeValidator(ForeignKeyAttribute atribute)
        {
            Attribute = atribute;
        }
        public List<ValidationResult> Validate(DataPool masterData, Type fromType, PropertyInfo property)
        {
            var fromColumnName = property.Name;
            var fromColumnType = property.PropertyType;
            var toTableType = Attribute.ReferenceTableType;
            var toColumnName = Attribute.ColumnName;

            var fromValues = GetTableValues(masterData, fromType, fromColumnName);
            var toValues = GetTableValues(masterData, toTableType, toColumnName);

            return Validate(fromValues, toValues, fromType, fromColumnName, fromColumnType, toTableType, toColumnName);
        }
        public List<ValidationResult> Validate(DataPool masterData, Type fromType, FieldInfo field)
        {
            var fromColumnName = field.Name;
            var fromColumnType = field.FieldType;
            var toTableType = Attribute.ReferenceTableType;
            var toColumnName = Attribute.ColumnName;

            var fromValues = GetTableValues(masterData, fromType, fromColumnName);
            var toValues = GetTableValues(masterData, toTableType, toColumnName);
            return Validate(fromValues, toValues, fromType, fromColumnName, fromColumnType, toTableType, toColumnName);
        }

        private List<ValidationResult> Validate(IEnumerable<dynamic> fromValues, IEnumerable<dynamic> toValues, Type fromType, string fromColumnName, Type fromColumnType, Type toTableType, string toColumnName)
        {
            var result = new List<ValidationResult>();

            return fromValues.Where(e =>
            {
                return toValues.Where(e2 => e2 == e).Count() < 1;
            })
            .Select((e, i) =>
            {
                var result = new ValidationResult();
                result.Message = $"Error: InvalidRecord. Constraint: ForeignKey. (Table: {fromType.Name} Column: {fromColumnName} Value: {e}) Not in (Table: {toTableType} Value: {toColumnName})";
                result.Location = new RecordLocation()
                {
                    LocationType = RecordLocationType.Table,
                    ColumnName = fromColumnName,
                    TypeName = fromColumnType.Name,
                    TableName = fromType.Name,
                    DataIndex = i
                };

                return result;
            }).ToList();

        }

        public IEnumerable<dynamic> GetTableValues(DataPool masterData, Type tableType, string columnName)
        {
            MethodInfo method = this.GetType().GetMethod("GetTableValues", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo bindedMethod = method.MakeGenericMethod(tableType);

            return bindedMethod.Invoke(this, new object[] { masterData, columnName }) as IEnumerable<dynamic>;
        }

        private IEnumerable<dynamic> GetTableValues<T>(DataPool masterData, string columnName)
        {
            IEnumerable<T> table = masterData.Get<T>();
            var targetTable = typeof(T);
            var property = SearchReferenceProperty(targetTable, columnName);
            if (property != null)
            {
                return table.Select(e => property.GetValue(e)).ToList();
            }

            var field = SearchRefernceField(targetTable, columnName);

            return field != null
                ? table.Select(e => field.GetValue(e)).ToList()
                : new List<dynamic>();
        }

        public static FieldInfo SearchRefernceField(Type targetType, string columnName)
        {
            return targetType.GetField(columnName);
        }

        public static PropertyInfo SearchReferenceProperty(Type targetType, string columnName)
        {
            return targetType.GetProperty(columnName);
        }
    }
}
