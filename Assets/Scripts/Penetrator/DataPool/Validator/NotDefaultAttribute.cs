using System;
using System.Collections.Generic;

namespace Penetrator.DataPool.Validator
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true, Inherited = true)]
    public class NotDefaultAttribute : SingleRecordConstraintAttribute
    {
        public override SingleRecordConstraintValidator GetValidator()
        {
            return new NotDefaultAttributeValidator();
        }
    }

    public class NotDefaultAttributeValidator : SingleRecordConstraintValidator
    {
        public List<ValidationResult> Validate(RecordLocation location, dynamic value, Type type, TypeCode dataTypeCode)
        {
            switch(dataTypeCode)
            {
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    return ValidateUintValue(location, (uint) value);
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                    return type.IsEnum ? ValidateEnumValue(location,Convert.ToInt32(value)) : ValidateIntValue(location, (int) value);
                case TypeCode.Double:
                    return ValidateDoubleValue(location, (double) value);
                case TypeCode.String:
                    return ValidateStringValue(location, (string) value);
                default:
                    return new List<ValidationResult>();
            }
        }

        protected List<ValidationResult> ValidateUintValue(RecordLocation location, uint value)
        {
            var results = new List<ValidationResult>();

            if (value < 1)
            {
                var result = new ValidationResult();
                result.Message = $"{buildErrorMessagePrefix(location)} Value: {value}";
                result.Location = location;
                results.Add(result);
            }
            return results;
        }
        protected List<ValidationResult> ValidateIntValue(RecordLocation location, int value)
        {
            var results = new List<ValidationResult>();

            if (value == 0)
            {
                var result = new ValidationResult();
                result.Message = $"{buildErrorMessagePrefix(location)} Value: {value}";
                result.Location = location;
                results.Add(result);
            }

            return results;
        }
        protected List<ValidationResult> ValidateEnumValue(RecordLocation location, int value)
        {
            var results = new List<ValidationResult>();

            // 0 is default
            if (value == 0)
            {
                var result = new ValidationResult();
                result.Message = $"{buildErrorMessagePrefix(location)} Value: {value}";
                result.Location = location;
                results.Add(result);
            }

            return results;
        }
        protected List<ValidationResult> ValidateDoubleValue(RecordLocation location, double value)
        {
            var results = new List<ValidationResult>();

            if (value == 0.0d)
            {
                var result = new ValidationResult();
                result.Message = $"{buildErrorMessagePrefix(location)} Value: {value}";
                result.Location = location;
                results.Add(result);
            }

            return results;
        }
        protected List<ValidationResult> ValidateStringValue(RecordLocation location, string value)
        {
            var results = new List<ValidationResult>();

            if (value == null || value.Length < 1)
            {
                var result = new ValidationResult();
                var parsedValue = value == null ? "Null" : value;
                result.Message = $"{buildErrorMessagePrefix(location)} Value: {parsedValue}";
                result.Location = location;
                results.Add(result);
            }

            return results;
        }

        private string buildErrorMessagePrefix(RecordLocation location)
        {
            switch (location.LocationType)
            {
                case RecordLocationType.Table:
                    return $"Error: InvalidRecord. Constraint: NotBlank Index: {location.DataIndex} TableName: {location.TableName}, ColumnName: {location.ColumnName}";
                case RecordLocationType.SingleRecord:
                default:
                    return $"Error: InvalidRecord. Constraint: NotBlank. TableName: {location.TableName}, ColumnName: {location.ColumnName}";
            }
        }
    }
}
