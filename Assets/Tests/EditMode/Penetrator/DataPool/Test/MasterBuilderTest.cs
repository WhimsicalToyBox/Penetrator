using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Threading.Tasks;
using System.Reflection;

namespace Penetrator.DataPool.Test
{
    public class MasterBuilderTest
    {
    
        [Test]
        public void SyncMasterData()
        {
            var applicationDataPath = Application.dataPath;
            var configRepo = new MasterBuilderEditorWindowConfigRepository(applicationDataPath);
            var config = configRepo.LoadOrInitializeMasterBuilderConfig();
        
            var task = Task.Run(() =>  SyncMasterTask(config.spreadsheetID, config.configClassName, applicationDataPath));
            task.Wait();
            //yield return task.AsIEnumerator();
        }
        
        private async Task SyncMasterTask(string spreadsheetID, string configClassName,string applicationDataPath)
        {
        
        
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        
            sw.Start();
            var loader = new GoogleSpreadSheetLoader(spreadsheetID, configClassName, applicationDataPath);
            var result = await loader.LoadAll();
            Debug.Log($"Load done. Elapsed: {sw.ElapsedMilliseconds} ms ");
        
            Debug.Log($"export start. Elapsed: {sw.ElapsedMilliseconds} ms");
            new MasterDataFileSystemWrapper(configClassName, applicationDataPath).ExportAll(result.DataPool);
            Debug.Log($"export done. Elapsed: {sw.ElapsedMilliseconds} ms");
            //new MasterDataFileSystemExporter().Export(masterData);
            sw.Stop();
            Debug.Log($"Synchronization done. Elapsed: {sw.ElapsedMilliseconds} ms");
        }
    }
    public static class TestExtensions
    {
        public static IEnumerator AsIEnumerator(this Task task)
        {
            while (!task.IsCompleted)
            {
                yield return null;
            }
         
            if (task.IsFaulted)
            {
                throw task.Exception;
            }
        }
    
    }
}
