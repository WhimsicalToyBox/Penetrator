using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using System;
using System.IO;
using Penetrator.DataPool.Validator;
using System.Collections.Generic;

namespace Penetrator.DataPool
{
    public class MasterBuilderEditorWindow: EditorWindow
    {
        private TextField ClassNameTextField;
        private TextField SpreadSheetIDTextField;
        private Button OpenSheetButton;
        private Toggle ForceOverWriteOnValidationErrorToggle;
        private Button SaveConfigButton;
        private Button SyncButton;
        private ScrollView ErrorLogScrollView;

        // [SerializeField]
        // private MasterBuilderConfig config;

        [MenuItem("Window/Penetrator/MasterBuilder")]
        public static void ShowWindow()
        {
            GetWindow<MasterBuilderEditorWindow>("Penetrator-MasterBuilder");
        }

        public MasterBuilderEditorWindowConfig GetCurrentConfig()
        {
            return new MasterBuilderEditorWindowConfig()
            {
                configClassName = ClassNameTextField.value,
                spreadsheetID = SpreadSheetIDTextField.value,
                forceOverwriteOnValidationError = ForceOverWriteOnValidationErrorToggle.value
            };
        }

        private void OnEnable()
        {
            var configRepo = new MasterBuilderEditorWindowConfigRepository(Application.dataPath);
            var config = configRepo.LoadOrInitializeMasterBuilderConfig();

            rootVisualElement.Add(new Label("設定"));
            ClassNameTextField = new TextField("設定クラス(T extends MasterBuilderConfig)");
            ClassNameTextField.value = config.configClassName;
            rootVisualElement.Add(ClassNameTextField);

            SpreadSheetIDTextField = new TextField("SpreadSheetID(GoogleApps)");
            SpreadSheetIDTextField.value = config.spreadsheetID;
            rootVisualElement.Add(SpreadSheetIDTextField);
            OpenSheetButton = new Button();
            OpenSheetButton.Add(new Label("シートをブラウザで開く"));
            OpenSheetButton.clicked += () =>
            {
                Application.OpenURL(GoogleSpreadSheetHelper.GetSheetUrl(SpreadSheetIDTextField.value));
            };
            rootVisualElement.Add(OpenSheetButton);


            rootVisualElement.Add(new Label("整合性チェックエラー時もローカルのマスタを上書きする"));
            ForceOverWriteOnValidationErrorToggle = new Toggle()
            {
                value = config.forceOverwriteOnValidationError
            };
            rootVisualElement.Add(ForceOverWriteOnValidationErrorToggle);

            SaveConfigButton = new Button();
            SaveConfigButton.clicked += () => {
                configRepo.SaveConfig(GetCurrentConfig());
            };
            SaveConfigButton.Add(new Label("設定保存"));
            rootVisualElement.Add(SaveConfigButton);

            rootVisualElement.Add(new Label("通信処理"));
            SyncButton = new Button();
            SyncButton.clicked += async () => 
            {
                System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

                var currentConfig = GetCurrentConfig();
                sw.Start();
                var loader = new GoogleSpreadSheetLoader(currentConfig.spreadsheetID, currentConfig.configClassName, Application.dataPath);
                var result = await loader.LoadAll();

                ErrorLogScrollView.Clear();
                var validationResults = DataValidator.ValidateTables(result.DataPool);
                if (validationResults.Count > 0)
                {
                    Debug.LogWarning($"ValidatonError Count: {validationResults.Count}");
                    if (!currentConfig.forceOverwriteOnValidationError)
                    {
                        foreach (var validationResult in validationResults)
                        {
                            Debug.LogError(validationResult.Message);
                            AddErrorLogScrollViewContent(validationResult, currentConfig, result.AvailableSheetDictionary);
                        }
                        sw.Stop();
                        Debug.LogError($"Synchronization Failured. Elapsed: {sw.ElapsedMilliseconds} ms");


                        return;
                    }

                    foreach(var validationResult in validationResults)
                    {
                        Debug.LogWarning(validationResult.Message);
                        AddErrorLogScrollViewContent(validationResult, currentConfig, result.AvailableSheetDictionary);
                    }
                }
                var fsWrapper = new MasterDataFileSystemWrapper(config.configClassName, Application.dataPath);
                fsWrapper.ExportAll(result.DataPool);

                AddAllClearScrollViewContent();

                sw.Stop();
                Debug.Log($"Syncronization done. Elapsed: {sw.ElapsedMilliseconds} ms");
                
            };
            SyncButton.Add(new Label("マスタ同期"));
            rootVisualElement.Add(SyncButton);

            rootVisualElement.Add(new Label("整合性チェックエラー行へのリンク"));

            ErrorLogScrollView = new ScrollView();

            rootVisualElement.Add(ErrorLogScrollView);
        }

        private void AddErrorLogScrollViewContent(ValidationResult result, MasterBuilderEditorWindowConfig config, Dictionary<string, Dictionary<string, string>> sheetNameDictionary)
        {
            var button = new Button();
            button.Add(new Label(result.Message));
            button.clicked += () =>
            {
                var tableProperties = sheetNameDictionary[result.Location.TableName];
                var tableGID = tableProperties["SheetId"];

                var url = (result.Location.LocationType == RecordLocationType.SingleRecord)
                    ? GoogleSpreadSheetHelper.GetSheetUrl(config.spreadsheetID, tableGID)
                    : GoogleSpreadSheetHelper.GetSheetUrl(config.spreadsheetID, tableGID, $"{result.Location.DataIndex + 1 + 1}:{result.Location.DataIndex + 1 + 1}"); // +1 is adjust origin(0 or 1), + 1 is Columns Offset
                Application.OpenURL(url);
            };

            ErrorLogScrollView.Add(button);
        }

        private void AddAllClearScrollViewContent()
        {
            ErrorLogScrollView.Add(new Label("All Clear."));
        }

    }

    [Serializable]
    public class MasterBuilderEditorWindowConfig
    {
        public string configClassName;
        public string spreadsheetID;
        public bool forceOverwriteOnValidationError;
    }

    public class MasterBuilderEditorWindowConfigRepository
    {

        private const string configFileName = "MasterBuilderEditorWindowConfig.json";
        private static string configDirectoryPathInner;
        private static string configFilePathInner;
        private static string applicationDataPathInner;

        private static string ConfigDirectoryPath
        {
            get
            {
                if (configDirectoryPathInner == null)
                {
                    configDirectoryPathInner = Path.Combine(applicationDataPathInner, "Editor", "Config", "MasterBuilder");
                }
                return configDirectoryPathInner;
            }
        }

        private static string ConfigFilePath
        {
            get {
                    if (configFilePathInner == null)
                    {
                        configFilePathInner = Path.Combine(ConfigDirectoryPath, configFileName);
                    }
                    return configFilePathInner;
                }
        }
        public MasterBuilderEditorWindowConfigRepository(string applictionDataPath)
        {
            applicationDataPathInner = applictionDataPath;
        }
        public MasterBuilderEditorWindowConfig LoadOrInitializeMasterBuilderConfig()
        {
            var config = new MasterBuilderEditorWindowConfig();
            if (!Directory.Exists(ConfigDirectoryPath))
            {
                Directory.CreateDirectory(ConfigDirectoryPath);
            }

            var fileInfo = new FileInfo(ConfigFilePath);
            if (!fileInfo.Exists) {
                using (var writer = new StreamWriter(fileInfo.Create()))
                {
                    var json = EditorJsonUtility.ToJson(config);
                    try
                    {
                        writer.WriteLine(json);
                        writer.Close();
                    } catch (Exception e)
                    {
                        writer.Close();
                        Debug.LogError(e.StackTrace);
                    }
                }
            } else
            {
                using (var reader = new StreamReader(fileInfo.OpenRead()))
                {
                    try
                    {
                        var text = reader.ReadToEnd();
                        EditorJsonUtility.FromJsonOverwrite(text, config);
                    } catch(Exception e)
                    {
                        reader.Close();
                        Debug.LogError(e.StackTrace);
                    }
                }

            }

            return config;
        }
        public void SaveConfig(MasterBuilderEditorWindowConfig config)
        {
            if (!Directory.Exists(ConfigDirectoryPath))
            {
                Directory.CreateDirectory(ConfigDirectoryPath);
            }
            var fileInfo = new FileInfo(ConfigFilePath);
            using (var writer = fileInfo.Exists ? new StreamWriter(fileInfo.OpenWrite()) : new StreamWriter(fileInfo.Create()))
            {
                var json = EditorJsonUtility.ToJson(config);
                try
                {
                    writer.WriteLine(json);
                    writer.Close();
                } catch (Exception e)
                {
                    writer.Close();
                    Debug.LogError(e.StackTrace);
                }
            }

        }

    }
}

