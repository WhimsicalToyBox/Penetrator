using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Penetrator.DataPool
{
    public class MasterDataFileSystemWrapper
    {
        private string masterDataRootPathInner;
        private string configClassName;
        private string applicationDataPath;
        private MasterBuilderConfig configInner;

        private string MasterDataRootPath 
        {
            get
            {
                if (masterDataRootPathInner == null)
                {
                    masterDataRootPathInner = Path.Combine(applicationDataPath, "Config", "DataPool");
                }

                return masterDataRootPathInner;
            }
        }

        private MasterBuilderConfig Config
        {
            get
            {
                if(configInner == null)
                {
                    configInner = LoadConfigClass(configClassName);
                }

                return configInner;
            }
        }

        public MasterDataFileSystemWrapper(string _configClassName, string _applicationDataPath)
        {
            configClassName = _configClassName;
            applicationDataPath = _applicationDataPath;
        }

        public string CreateSerializedFileName(string tableName)
        {
            return $"{tableName}.json";
        }

        public DataPool ImportAll()
        {
            var masterData = new MasterData();
            var unWrapEnveloveMethodBase = typeof(MasterDataFileSystemWrapper).GetMethod("UnWrapEnvelopve", BindingFlags.NonPublic | BindingFlags.Static);

            foreach(var tableType in Config.MasterTables())
            {
                var enveloveType = typeof(JsonEnvelope<>).MakeGenericType(tableType);

                var filePath = Path.Combine(MasterDataRootPath, CreateSerializedFileName(tableType.Name));
                var fileInfo = new FileInfo(filePath);

                if(fileInfo.Exists)
                {
                    using (var reader = new StreamReader(fileInfo.OpenRead()))
                    {
                        try
                        {
                            string content = reader.ReadToEnd();
                            var recordsWithEnvelove = JsonUtility.FromJson(content, enveloveType);
                            if(recordsWithEnvelove != null)
                            {
                                var unWrapEnveloveMethod = unWrapEnveloveMethodBase.MakeGenericMethod(tableType);
                                unWrapEnveloveMethod.Invoke(null, new[] { recordsWithEnvelove, masterData });
                            }
                            reader.Close();
                        } catch (Exception ex)
                        {
                            reader.Close();
                            Debug.LogError(ex.StackTrace);
                        }
                    }
                }
            }

            return masterData;
        } 

        private static void UnWrapEnvelove<T>(object obj, MasterData masterData)
        {
            IEnumerable<T> records = (IEnumerable<T>)obj;

            masterData.Set(records);
        }

        public void ExportAll(DataPool masterData)
        {
            SetUpMasterDataRoot();

            var serializeMethod = this.GetType().GetMethod("Serialize", BindingFlags.NonPublic | BindingFlags.Static);
            foreach(var tableType in Config.MasterTables())
            {
                var bindedMethod = serializeMethod.MakeGenericMethod(tableType);
                var serializedContent = bindedMethod.Invoke(null, new object[] { masterData.Get(tableType) }) as string;

                var filePath = Path.Combine(MasterDataRootPath, CreateSerializedFileName(tableType.Name));
                var fileInfo = new FileInfo(filePath);
                if (fileInfo.Exists)
                {
                    fileInfo.Delete();
                }

                using (var writer = new StreamWriter(fileInfo.OpenWrite()))
                {
                    try
                    {
                        writer.WriteLine(serializedContent);
                        writer.Close();
                    } catch (Exception ex)
                    {
                        writer.Close();
                        Debug.LogError(ex.StackTrace);
                    }
                }
            }
        }

        private static string Serialize<T>(IEnumerable<object> records)
        {
            List<T> convertedRecords = records.Select(e => (T)e).ToList();
            var envelove = new JsonEnvelope<T>(convertedRecords);
            var converted = JsonUtility.ToJson(envelove);

            return converted;
        }

        public MasterBuilderConfig LoadConfigClass(string configClassName)
        {
            Debug.Log($"ConfigClssName: {configClassName}");
            Type configType = LoadConfigClassType(configClassName);
            ConstructorInfo constructor = configType.GetConstructor(new Type[] { });
            MasterBuilderConfig config = (MasterBuilderConfig) constructor.Invoke(null);

            return config;
        }
        public Type LoadConfigClassType(string configClassName)
        {
            Type result = null;
            try
            {
                result = Type.GetType(configClassName);
            } catch (Exception) { }

            if (result != null)
            {
                return result;
            }

            var lastDotIndex = configClassName.LastIndexOf(".");
            var assemblyName = configClassName.Substring(0, lastDotIndex);
            var configClassNameClassNameOnly = configClassName.Substring(lastDotIndex + 1, configClassName.Length - (lastDotIndex +1));

            result = loadTypeWithAssembly(assemblyName, configClassNameClassNameOnly, configClassName);

            return result;
        }
        private Type loadTypeWithAssembly (string assemblyName, string configClassNameNameOnly, string configClassNameFull)
        {
            Type innerResult = null;

            try
            {
                var assembly = Assembly.Load(assemblyName);
                innerResult = assembly.GetType(configClassNameFull);
            } catch (Exception){}

            if(innerResult != null)
            {
                Debug.Log($"LoadAssembly Name: {assemblyName}");
                return innerResult;
            }

            var lastDotIndex = assemblyName.LastIndexOf(".");
            if (lastDotIndex > 1)
            {
                var assemblyNameInner = assemblyName.Substring(0, lastDotIndex);
                return loadTypeWithAssembly(assemblyNameInner, configClassNameNameOnly, configClassNameFull);
            }

            return innerResult;
        }


        private void SetUpMasterDataRoot()
        {
            if (!Directory.Exists(MasterDataRootPath))
            {
                Directory.CreateDirectory(MasterDataRootPath);
            }
        }
    }

    [Serializable]
    public class JsonEnvelope<T>
    {
        [SerializeField]
        public List<T> Records;
        public JsonEnvelope(List<T> records)
        {
            Records = records; 
        }
    }


}
