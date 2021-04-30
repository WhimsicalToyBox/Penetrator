using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Linq;
using static Google.Apis.Sheets.v4.SpreadsheetsResource;
using System.Threading.Tasks;
using System.Threading;
using Google.Apis.Util.Store;
using System;
using System.Reflection;

namespace Penetrator.DataPool
{
    public class GoogleSpreadSheetLoader
    {
        private static string ApplicationName = "Penetrator - MasterBuilder";
        private string spreadSheetId;
        private string configClassName;
        private string applicationDataPath;
        private SheetsService service;
        private MasterDataFileSystemWrapper fsWrapper;

        public GoogleSpreadSheetLoader(string _spreadSheetId, string _configClassName, string _applicationDataPath)
        {
            if (_configClassName == null || _configClassName.Length < 1 || _spreadSheetId == null || _spreadSheetId.Length < 1) {
                Debug.LogError($"Error: InvalidConfig.");        
            }

            spreadSheetId = _spreadSheetId;
            configClassName = _configClassName;
            applicationDataPath = _applicationDataPath;
            fsWrapper = new MasterDataFileSystemWrapper(_configClassName, _applicationDataPath);
        }

        public IEnumerable<Dictionary<string, string>> GetAvailableSheetProperties(SheetsService service)
        {
            SpreadsheetsResource.GetRequest request = service.Spreadsheets.Get(spreadSheetId);
            var response = request.Execute();
            return response.Sheets.Select(e => {
                    return new Dictionary<string, string>()
                    {
                        { "Title", e.Properties.Title },
                        { "SheetId", e.Properties.SheetId.ToString() }
                    };
                });
        }

        public string GetSheetURL()
        {
            return $"https://docs.google.com/spreadsheets/d/{spreadSheetId}";
        }

        public async Task<(DataPool DataPool, Dictionary<string, Dictionary<string, string>> AvailableSheetDictionary)> LoadAll()
        {
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            service = await OpenSheet();

            Debug.Log($"OpenSheet() Elapsed: {sw.ElapsedMilliseconds} ms");


            var availableSheetsDict = GetAvailableSheetProperties(service).ToDictionary(e => e["Title"]);
            MasterBuilderConfig config =  fsWrapper.LoadConfigClass(configClassName);
            List<Type> availableTableTypes = config.MasterTables()
                .Where(e =>
                {
                    var contains = availableSheetsDict.ContainsKey(e.Name);
                    if (!contains) { Debug.LogError($"Error: SheetNotFound. SheetName: {e.Name} URL:{GetSheetURL()}"); }
                    return contains;
                }).ToList();
            List<string> availableSheets = availableTableTypes
                .Select(e => e.Name)
                .ToList();
            List<string> loadRanges = availableSheets
                .Select(e => $"{e}!A1:Z").ToList();

            ValuesResource.BatchGetRequest request = service.Spreadsheets.Values.BatchGet(spreadSheetId);
            request.Ranges = loadRanges;

            Debug.Log($"Send request to Google... Elapsed: {sw.ElapsedMilliseconds} ms");
            BatchGetValuesResponse response = request.Execute();

            Debug.Log($"Send request to Google... done. Elapsed: {sw.ElapsedMilliseconds} ms");
            var masterData = new MasterData();
            if (response != null && response.ValueRanges.Count > 0)
            {
                var tableCount = availableSheets.Count;
                for (var i = 0; i < tableCount; i++)
                {
                    var table = response.ValueRanges[i];
                    var tableName = availableSheets[i];
                    var dataType = availableTableTypes[i];

                    Debug.Log($"deserialize table({i + 1} / {tableCount}) tableName: {tableName} start. Elapsed: {sw.ElapsedMilliseconds} ms");

                    var recordCount = table.Values.Count - 1;
                    IList<string> columns = table.Values.FirstOrDefault().Select(e => e.ToString()).ToList();
                    if (recordCount < 1 || columns == null || columns.Count < 1)
                    {
                        continue;
                    }
                    var propertyDict = BuildPropertyDictionary(dataType);
                    var fieldDict = BuildFieldAttributesDictionary(dataType);


                    var tasks = table.Values.Skip(1).Select((e, i) => Task.Run(() => {
                        var recordIndex = i - 1;
                        return BuildRecord(columns, e.Select(e2 => e2.ToString()).ToList(), propertyDict, fieldDict, dataType, recordIndex);
                    }));


                    var rawRecords = await Task.WhenAll(tasks.ToArray());

                    masterData.Set(dataType, rawRecords.AsEnumerable());
                    Debug.Log($"deserialize table({i + 1} / {tableCount}) tableName: {tableName} done. Elapsed: {sw.ElapsedMilliseconds} ms");
                }
            }

            return (DataPool: masterData, AvailableSheetDictionary: availableSheetsDict);
        }
        private IDictionary<string, FieldInfo> BuildFieldAttributesDictionary(Type type)
        {
            FieldInfo[] fields = type.GetFields();
            return fields.AsEnumerable().ToDictionary(e => e.Name);
        }
        private IDictionary<string, PropertyInfo> BuildPropertyDictionary(Type type)
        {
            PropertyInfo[] properties = type.GetProperties();
            return properties.AsEnumerable().ToDictionary(e => e.Name);
        }

        private object BuildRecord(IList<string> columns, IList<string> record, IDictionary<string, PropertyInfo> propertyDict, IDictionary<string, FieldInfo> fieldDict, Type type, int recordIndex)
        {
            var instance = Activator.CreateInstance(type);

            if ( record.Count <= 1)
            {
                return instance;
            }

            var columnCount = columns.Count;
            var recordCount = record.Count;
            for(var i=0; i < columnCount; i++)
            {
                if (i >= recordCount) continue;

                var column = columns[i];
                var value = record[i];
                PropertyInfo prop;
                propertyDict.TryGetValue(column, out prop);

                if(prop != null)
                {
                    var result =  PropertySetter.Set(instance, prop, value);
                    if (!result.Successed)
                    {
                        Debug.LogError($"Parse Failured. column: {column}, index: {recordIndex + 1}, rawValue: {result.RawValue}, propertyType: {result.PropertyType}");
                    }
                    continue;
                }

                FieldInfo field;
                fieldDict.TryGetValue(column, out field);
                if (field != null)
                {
                    var result = PropertySetter.Set(instance, field, value);
                    if (!result.Successed)
                    {
                        Debug.LogError($"Parse Failured. column: {column}, index: {recordIndex + 1}, rawValue: {result.RawValue}, fieldType: {result.PropertyType}");

                    }
                }
            }

            return instance;
        }
        private Task<UserCredential> GetUserCredential()
        {
            string[] scopes = { SheetsService.Scope.Spreadsheets };

            using (var stream = new FileStream(Path.Combine(applicationDataPath, "Editor", "Config", "MasterBuilder", "MasterBuilderGASCredential.json"), FileMode.Open, FileAccess.Read))
            {
                string credPath = Path.Combine(applicationDataPath, "Editor", "Config", "MasterBuilder");
                return GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    scopes,
                    "user", CancellationToken.None, new FileDataStore(credPath, true));
            }
        }

        public async Task<SheetsService> OpenSheet()
        {
            UserCredential credential;
            credential = await GetUserCredential();
            var service = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName
            });
            return service;
        }

    }

    class PropertySetterResult
    {
        public bool Successed;
        public string PropertyName;
        public string RawValue;
        public string PropertyType;
    }
    class PropertySetter
    {
        public static PropertySetterResult Set(object target, FieldInfo field, string rawValue)
        {
            var result = new PropertySetterResult();
            result.PropertyName = field.Name;
            result.RawValue = rawValue;

            TypeCode propertyType = Type.GetTypeCode(field.FieldType);
            switch (propertyType)
            {
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    result.PropertyType = "uint";
                    if (!SetUInt(target, field, rawValue))
                    {
                        result.Successed = false;
                        return result;
                    }

                    break;
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                    if (field.FieldType.IsEnum)
                    {
                        result.PropertyType = "Enum";
                        if (!SetEnum(target, field, rawValue))
                        {
                            result.Successed = false;
                            return result;
                        }
                    }
                    else
                    {
                        result.PropertyType = "int";
                        if (!SetInt(target, field, rawValue))
                        {
                            result.Successed = false;
                            return result;
                        }
                    }
                    break;
                case TypeCode.Double:
                    result.PropertyType = "double";
                    if (!SetDouble(target, field, rawValue))
                    {
                        result.Successed = false;
                        return result;
                    }
                    break;

                case TypeCode.String:
                    result.PropertyType = "string";
                    field.SetValue(target, rawValue != null ? rawValue : "");
                    break;
                default:
                    result.PropertyType = "unknown";
                    field.SetValue(target, rawValue);
                    break;
            }

            result.Successed = true;
            return result;

        }
        public static PropertySetterResult Set(object target, PropertyInfo property, string rawValue)
        {
            var result = new PropertySetterResult();
            result.PropertyName = property.Name;
            result.RawValue = rawValue;

            TypeCode propertyType = Type.GetTypeCode(property.PropertyType);
            switch (propertyType)
            {
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    result.PropertyType = "uint";
                    if (!SetUInt(target, property, rawValue))
                    {
                        result.Successed = false;
                        return result;
                    }
                    break;
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                    if (property.PropertyType.IsEnum)
                    {
                        result.PropertyType = "Enum";
                        if (!SetEnum(target, property, rawValue))
                        {
                            result.Successed = false;
                            return result;
                        }
                    }
                    else
                    {
                        result.PropertyType = "int";
                        if (!SetInt(target, property, rawValue))
                        {
                            result.Successed = false;
                            return result;
                        }
                    }
                    break;
                case TypeCode.Double:
                    result.PropertyType = "double";
                    if (!SetDouble(target, property, rawValue))
                    {
                        result.Successed = false;
                        return result;
                    }
                    break;

                case TypeCode.String:
                    result.PropertyType = "string";
                    property.SetValue(target, rawValue);
                    break;
                default:
                    result.PropertyType = "unknown";
                    property.SetValue(target, rawValue);
                    break;
            }

            result.Successed = true;
            return result;
        }

        public static bool SetInt(object target, PropertyInfo property, string rawValue)
        {
            var value = 0;
            var parsed = int.TryParse(rawValue, out value);

            property.SetValue(target, value);

            return parsed;
        }
        public static bool SetUInt(object target, PropertyInfo property, string rawValue)
        {
            var value = 0u;
            var parsed = uint.TryParse(rawValue, out value);

            property.SetValue(target, value);

            return parsed;
        }

        public static bool SetDouble(object target, PropertyInfo property, string rawValue)
        {
            var value = 0.0;
            var parsed = double.TryParse(rawValue, out value);

            property.SetValue(target, value);

            return parsed;
        }
        public static bool SetEnum(object target, PropertyInfo property, string rawValue)
        {
            var parsedValue = Enum.Parse(property.PropertyType, rawValue);
            var parsed = false;

            try
            {
                property.SetValue(target, parsedValue);
                parsed = true;
            }
            catch (Exception)
            {
            }

            return parsed;
        }
        public static bool SetInt(object target, FieldInfo field, string rawValue)
        {
            var value = 0;
            var parsed = int.TryParse(rawValue, out value);

            field.SetValue(target, value);

            return parsed;
        }
        public static bool SetUInt(object target, FieldInfo field, string rawValue)
        {
            var value = 0u;
            var parsed = uint.TryParse(rawValue, out value);

            field.SetValue(target, value);

            return parsed;
        }

        public static bool SetDouble(object target, FieldInfo field, string rawValue)
        {
            var value = 0.0;
            var parsed = double.TryParse(rawValue, out value);

            field.SetValue(target, value);

            return parsed;
        }
        public static bool SetEnum(object target, FieldInfo field, string rawValue)
        {
            var parsedValue = Enum.Parse(field.FieldType, rawValue);
            var parsed = false;

            try
            {
                field.SetValue(target, parsedValue);
                parsed = true;
            }
            catch (Exception)
            {
            }

            return parsed;
        }
    }
}
