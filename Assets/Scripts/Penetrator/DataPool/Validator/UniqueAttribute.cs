using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Penetrator.DataPool.Validator
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true, Inherited = true)]
    public class UniqueAttribute : TableConstraintAttribute
    {
        public override TableConstraintValidator GetValidator()
        {
            return new UniqueAttributeValidator();
        }
    }

    public class UniqueAttributeValidator : TableConstraintValidator
    {
        public List<ValidationResult> Validate<T>(IEnumerable<T> table, PropertyInfo property)
        {
            TypeCode typeCode = Type.GetTypeCode(property.PropertyType);
            var values = table.Select(e => property.GetValue(e)).ToList();

            return ValidateSingleColumn(table, values, property.Name, property.PropertyType);
        }
        public List<ValidationResult> Validate<T>(IEnumerable<T> table, FieldInfo field)
        {
            TypeCode typeCode = Type.GetTypeCode(field.FieldType);

            var values = table.Select(e => field.GetValue(e)).ToList();

            return ValidateSingleColumn(table, values, field.Name, field.FieldType);
        }

        private List<ValidationResult> ValidateSingleColumn<T>(IEnumerable<T> table, IList<dynamic> values, string columnName, Type columnType)
        {
            var typeName = typeof(T).Name;

            var valueDict = values.Select((e, i) => (Index: i, Value: e)).GroupBy(e => e.Value).ToDictionary(e => e.Key, e => e);
            var results = valueDict.Where(e => e.Value.Count() > 1).Select(e =>
            {
                var result = new ValidationResult();
                var value = e.Value.First().Value;
                var indexes = e.Value.Select(e => e.Index).ToArray();

                var location = new RecordLocation(){
                    LocationType = RecordLocationType.Table,
                    DataIndex = indexes[0], //first Index Only
                    TypeName = columnType.Name,
                    ColumnName = columnName,
                    TableName = typeName
                };

                result.Message = buildErrorMessage(indexes, typeName, columnName, columnType.Name, value != null ? value.ToString() : "(null)");
                result.Location = location;
                return result;
            }).ToList();

            return results;
        }

        private string buildErrorMessage(int[] dataIndexes, string tableTypeName, string columnName, string columnTypeName, string value)
        {
            return $"Error: InvalidRecord. Constraint: Unique. Index: {dataIndexes} TableName: {tableTypeName}, ColumnName: {columnName}, ColumnTypeName: {columnTypeName}, Value: {value}";
        }

    }
}
