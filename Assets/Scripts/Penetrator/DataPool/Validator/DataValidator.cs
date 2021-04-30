using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Penetrator.DataPool.Validator
{
    public class DataValidator
    {
        private static List<Type> SingleRecordConstraints = new List<Type>() {
            typeof(NotDefaultAttribute)
        };
        private static List<Type> TableConstraints = new List<Type>() {
            typeof(UniqueAttribute)
        };
        private static List<Type> MultiTableConstraints = new List<Type>() {
            typeof(ForeignKeyAttribute)
        };

        private static List<Type> ConstraintsInner;

        private static List<Type> Constraints {
            get {
                if (ConstraintsInner == null)
                {
                    ConstraintsInner = new List<Type>();
                    ConstraintsInner.AddRange(SingleRecordConstraints);
                    ConstraintsInner.AddRange(TableConstraints);
                    ConstraintsInner.AddRange(MultiTableConstraints);
                }
                return ConstraintsInner;
            }
        }



        /**
         * 1レコードの整合性確認
         * - SingleRecordConstraint
         *
         **/
        public static List<ValidationResult> ValidateRecord<T>(T record)
        {
            return ValidateTable(new List<T> { record });
        }

        /**
         * 1テーブル(レコード群)の整合性確認
         * - SingleRecordConstraint
         * - TableConstraint
         **/
        public static List<ValidationResult> ValidateTable<T>(IEnumerable<T> records)
        {
            var type = typeof(T);
            var properties = type.GetProperties().ToList();
            var fields = type.GetFields().ToList();

            var singleRecordConstraintProperties = SearchTargetAttributes<SingleRecordConstraintAttribute>(properties);
            var singleRecordConstraintFields = SearchTargetAttributes<SingleRecordConstraintAttribute>(fields);

            var results = new List<ValidationResult>();

            var singleRecordMode = records.Count() == 1;

            results.AddRange(
                singleRecordConstraintProperties.SelectMany((e, i) =>
                {
                    var validator = e.Attribute.GetValidator();
                    var typeCode = Type.GetTypeCode(e.Property.PropertyType);
                    RecordLocation location = new RecordLocation()
                    {
                        TableName = type.Name,
                        TypeName = e.Property.PropertyType.Name,
                        ColumnName = e.Property.Name
                    };

                    if (singleRecordMode)
                    {
                        location.LocationType = RecordLocationType.SingleRecord;
                    }
                    else
                    {
                        location.LocationType = RecordLocationType.Table;
                        location.DataIndex = i;
                    }

                    return records.SelectMany(e2 => {
                        var value = e.Property.GetValue(e2);
                        return validator.Validate(location, value, e.Property.PropertyType, typeCode);
                    });
                }));
            results.AddRange(
                singleRecordConstraintFields.SelectMany((e, i) =>
                {
                    var validator = e.Attribute.GetValidator();
                    var typeCode = Type.GetTypeCode(e.Field.FieldType);
                    RecordLocation location = new RecordLocation()
                    {
                        TableName = type.Name,
                        TypeName = e.Field.FieldType.Name,
                        ColumnName = e.Field.Name
                    };
                    if (singleRecordMode)
                    {
                        location.LocationType = RecordLocationType.SingleRecord;

                    }
                    else
                    {
                        location.LocationType = RecordLocationType.Table;
                        location.DataIndex = i;
                    }

                    return records.SelectMany(e2 => {
                        var value = e.Field.GetValue(e2);
                        return validator.Validate(location, value, e.Field.FieldType, typeCode);
                    });
                }));

            if (!singleRecordMode)
            {
                var tableConstraintProperties = SearchTargetAttributes<TableConstraintAttribute>(properties);
                var tableConstraintFields = SearchTargetAttributes<TableConstraintAttribute>(fields);

                results.AddRange(
                    tableConstraintProperties.SelectMany(e => {
                        var validator = e.Attribute.GetValidator();

                        return validator.Validate<T>(records, e.Property);
                    }));
                results.AddRange(
                    tableConstraintFields.SelectMany(e => {
                        var validator = e.Attribute.GetValidator();

                        return validator.Validate<T>(records, e.Field);
                    }));
            }

            return results;
        }

        /**
         * テーブル群の整合性確認
         * - SingleRecordConstraint
         * - TableConstraint
         * - MultiTableConstraint
         **/
        public static List<ValidationResult> ValidateTables(DataPool tables)
        {
            var validationResults = tables.AvailableTypes().Select(e =>
            {
                var properties = e.GetProperties().ToList();
                var fields = e.GetFields().ToList();

                var multiTableContstraintProperties = SearchTargetAttributes<MultiTableConstraintAttribute>(properties);
                var multiTableConstraintFields = SearchTargetAttributes<MultiTableConstraintAttribute>(fields);

                return (TableType: e, MatchedProperties: multiTableContstraintProperties, MatchedFields: multiTableConstraintFields);
            })
            .Where(e => (e.MatchedFields != null && e.MatchedFields.Count() > 0) || (e.MatchedProperties != null && e.MatchedProperties.Count() > 0))
            .SelectMany(e =>
            {
                var result = new List<ValidationResult>();
                result.AddRange(
                    e.MatchedFields.SelectMany(e2 =>
                    {
                        var validator = e2.Attribute.GetValidator();
                        return validator.Validate(tables, e.TableType, e2.Field);
                    }).ToList()
                );

                result.AddRange(
                    e.MatchedProperties.SelectMany(e2 =>
                    {
                        var validator = e2.Attribute.GetValidator();
                        return validator.Validate(tables, e.TableType, e2.Property);
                    }).ToList()
                );

                return result;
            }).ToList();

            var validateTableMethod = typeof(DataValidator).GetMethod("ValidateTable", BindingFlags.Static | BindingFlags.NonPublic);
            validationResults.AddRange(tables.AvailableTypes().SelectMany(e => {
                var bindedMethod = validateTableMethod.MakeGenericMethod(e);
                List<ValidationResult> results = (List<ValidationResult>) bindedMethod.Invoke(null, new object[] { tables });
                return results;
             }));

            return validationResults;
        }
        private static List<ValidationResult> ValidateTable<T>(DataPool tables)
        {
            return ValidateTable(tables.Get<T>());
        }


        private static IEnumerable<(T Attribute, PropertyInfo Property)> SearchTargetAttributes<T>(List<PropertyInfo> properties) where T : DataConstraintAttribute
        {
            return properties
                .SelectMany(e => 
                    e.GetCustomAttributes()
                        .Where(e => e is T)
                        .Select(e2 => (Attribute: e2 as T, Property: e)));
        }
        private static IEnumerable<(T Attribute, FieldInfo Field)> SearchTargetAttributes<T>(List<FieldInfo> fields) where T : DataConstraintAttribute
        {
            return fields
                .SelectMany(e => 
                    e.GetCustomAttributes()
                        .Where(e => e is T)
                        .Select(e2 => (Attribute: e2 as T, Field: e)));
        }
    }

    public struct ValidationResult
    {
        public string Message;
        public RecordLocation Location;
    }

    /**
     * DataValidationの対象を明示するAttribute
     **/
    public class DataConstraintAttribute : Attribute
    {

    }

    /**
     * 1レコード内で完結する制約
     **/
    public abstract class SingleRecordConstraintAttribute : DataConstraintAttribute
    {
        public abstract SingleRecordConstraintValidator GetValidator();
    }
    /**
     * 1テーブル内で完結する制約
     **/
    public abstract class TableConstraintAttribute : DataConstraintAttribute
    {
        public abstract TableConstraintValidator GetValidator();
    }
    /**
     * テーブルをまたがる制約
     **/
    public abstract class MultiTableConstraintAttribute : DataConstraintAttribute
    {
        public abstract MultiTableConstraintValidator GetValidator();
    }

    public interface MultiTableConstraintValidator
    {
        public List<ValidationResult> Validate(DataPool masterData, Type fromType, PropertyInfo property);
        public List<ValidationResult> Validate(DataPool masterData, Type fromType, FieldInfo field);

    }
    public interface TableConstraintValidator
    {
        public List<ValidationResult> Validate<T>(IEnumerable<T> table, PropertyInfo property);
        public List<ValidationResult> Validate<T>(IEnumerable<T> table, FieldInfo field);
    }

    public interface SingleRecordConstraintValidator
    {
        public List<ValidationResult> Validate(RecordLocation location, dynamic value, Type type, TypeCode dataTypeCode);
    }

    public struct RecordLocation
    {
        public RecordLocationType LocationType;
        public int DataIndex;
        public string TypeName;
        public string ColumnName;
        public string TableName;
        public int GoogleSpreadSheetGID;
    }

    public enum RecordLocationType
    {
        NotInitialized,
        SingleRecord,
        Table
    }
}
