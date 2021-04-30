using System.Collections.Generic;
using System;
using Penetrator.DataPool.Validator;

namespace Penetrator.DataPool.Test
{
    public class MasterBuilderConfigImplForTest : MasterBuilderConfig
    {
        List<Type> MasterBuilderConfig.MasterTables()
        {
            return new List<Type>{
                typeof(TestData1),
                typeof(TestData2),
                typeof(TestData3)
                };
        }
    }
  
    [Serializable]
    public struct TestData1
    {
        [Unique]
        public int Id;
        public string Name;

        public string Name2 { get { return $"[{Name}]"; } }

        public override string ToString() {
            return $"Id: {Id}, Name: {Name}, Name2: {Name2}";
        }
    } 
    [Serializable]
    public struct TestData2
    {
        [NotDefault] 
        [Unique]
        public int Id;
        [Unique] 
        public string Key1;
        [ForeignKey(ReferenceTableType = typeof(TestData3), ColumnName = "CategoryName")]
        public string Category;
    } 
    [Serializable]
    public struct TestData3
    {
        [NotDefault]
        [Unique]
        public string CategoryName;
        public string Note;
        public TempCategory SubCategory;
    }

    public enum TempCategory
    {
        Category1,
        Category2,
        Category3,
    };
}

