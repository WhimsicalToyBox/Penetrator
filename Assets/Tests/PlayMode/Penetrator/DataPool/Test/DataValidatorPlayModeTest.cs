using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Penetrator.DataPool.Validator;
using NUnit.Framework;
using UnityEngine;
using Assert = UnityEngine.Assertions.Assert;

namespace Penetrator.DataPool.Test
{
    public class DataValidatorPlayModeTest
    {
        [Test]
        public void SingleRecordValidationErrorClassTest()
        {
            var record = new SingleRecordTestDataClass1(0, "", SingleRecordTestDataCategory.Default);

            var results = DataValidator.ValidateRecord(record);
            // 3 = id, name, category
            Assert.AreEqual(results.Count(), 3);
            Debug.Log($"Messages: {string.Join(",", results.Select(e => e.Message))}");
        }
        [Test]
        public void SingleRecordValidationErrorClassByTableValidationMethodTest()
        {
            var records = new List<SingleRecordTestDataClass1>()
            {
                new SingleRecordTestDataClass1(0, "", SingleRecordTestDataCategory.Default)
             };
            var results = DataValidator.ValidateTable(records);
            // 3 = id, name, category
            Assert.AreEqual(results.Count(), 3);
            Debug.Log($"Messages: {string.Join(",", results.Select(e => e.Message))}");
        }
        [Test]
        public void SingleRecordValidationErrorClassByMultiTableValidationMethodTest()
        {
            var table = new MasterData();
            var records = new List<SingleRecordTestDataClass1>()
            {
                new SingleRecordTestDataClass1(0, "", SingleRecordTestDataCategory.Default)
             };
            table.Set(records);
            var results = DataValidator.ValidateTables(table);
            // 3 = id, name, category
            Assert.AreEqual(results.Count(), 3);
            Debug.Log($"Messages: {string.Join(",", results.Select(e => e.Message))}");
        }
        [Test]
        public void SingleRecordValidationPassClassTest()
        {
            var record = new SingleRecordTestDataClass1(1, "TestData2", SingleRecordTestDataCategory.Category1);

            var results = DataValidator.ValidateRecord(record);
            Assert.AreEqual(results.Count(), 0);
        }
        [Test]
        public void SingleRecordValidationDummyClassTest()
        {
            var record = new SingleRecordTestDataClass2(0, "", SingleRecordTestDataCategory.Default);

            var results = DataValidator.ValidateRecord(record);
            Assert.AreEqual(results.Count(), 0);
        }
        [Test]
        public void TableValidationErrorClassTest()
        {
            IEnumerable<TableRecordTestDataClass1> testTable1 = new List<TableRecordTestDataClass1>() { 
                new TableRecordTestDataClass1(1, "TestData1", 1),
                new TableRecordTestDataClass1(1, "TestData2", 1),
                new TableRecordTestDataClass1(3, "TestData3", 3)
            };

            var results = DataValidator.ValidateTable(testTable1);

            Assert.AreEqual(results.Count(), 2);
            Debug.Log($"Messages: {string.Join(",", results.Select(e => e.Message))}");
        }
        [Test]
        public void TableValidationPassClassTest()
        {
            IEnumerable<TableRecordTestDataClass1> testTable1 = new List<TableRecordTestDataClass1>() { 
                new TableRecordTestDataClass1(1, "TestData1", 1),
                new TableRecordTestDataClass1(2, "TestData2", 2),
                new TableRecordTestDataClass1(3, "TestData3", 3)
            };

            var results = DataValidator.ValidateTable(testTable1);

            Assert.AreEqual(results.Count(), 0);
        }
        [Test]
        public void TableValidationDummyClassTest()
        {
            IEnumerable<TableRecordTestDataClass2> testTable1 = new List<TableRecordTestDataClass2>() { 
                new TableRecordTestDataClass2(1, "TestData1", 1),
                new TableRecordTestDataClass2(1, "TestData2", 1),
                new TableRecordTestDataClass2(3, "TestData3", 3)
            };

            var results = DataValidator.ValidateTable(testTable1);

            Assert.AreEqual(results.Count(), 0);
        }
        [Test]
        public void MultiTableValidationErrorClassTest()
        {
            DataPool masterData = new MasterData();

            IEnumerable<MultiTableTestDataClass1> table1 = new List<MultiTableTestDataClass1>()
            {
                new MultiTableTestDataClass1(1, "Data1-1", 1),
                new MultiTableTestDataClass1(2, "Data1-2", 2),
                new MultiTableTestDataClass1(3, "Data1-3", 4),
            };
            IEnumerable<MultiTableTestDataClass2> table2 = new List<MultiTableTestDataClass2>()
            {
                new MultiTableTestDataClass2(1, "Data2-1"),
                new MultiTableTestDataClass2(2, "Data2-2"),
                new MultiTableTestDataClass2(3, "Data2-3"),
            };
            masterData.Set(table1);
            masterData.Set(table2);

            var results = DataValidator.ValidateTables(masterData);

            Assert.AreEqual(results.Count(), 1);
            Debug.Log($"Messages: {string.Join(",", results.Select(e => e.Message))}");
        }
        [Test]
        public void MultiTableValidationPassClassTest()
        {
            DataPool masterData = new MasterData();

            IEnumerable<MultiTableTestDataClass1> table1 = new List<MultiTableTestDataClass1>()
            {
                new MultiTableTestDataClass1(1, "Data1-1", 1),
                new MultiTableTestDataClass1(2, "Data1-2", 2),
                new MultiTableTestDataClass1(3, "Data1-3", 3),
            };
            IEnumerable<MultiTableTestDataClass2> table2 = new List<MultiTableTestDataClass2>()
            {
                new MultiTableTestDataClass2(1, "Data2-1"),
                new MultiTableTestDataClass2(2, "Data2-2"),
                new MultiTableTestDataClass2(3, "Data2-3"),
            };
            masterData.Set(table1);
            masterData.Set(table2);

            var results = DataValidator.ValidateTables(masterData);

            Debug.Log($"Messages: {string.Join(",", results.Select(e => e.Message))}");
            Assert.AreEqual(results.Count(), 0);
        }
        [Test]
        public void MultiTableValidationDummyClassTest()
        {
            DataPool masterData = new MasterData();

            var results = DataValidator.ValidateTables(masterData);

            Assert.AreEqual(results.Count(), 0);
        }
        [Test]
        public void SingleRecordValidationErrorStructTest()
        {
            var record = new SingleRecordTestDataStruct1(0, "", SingleRecordTestDataCategory.Default);

            var results = DataValidator.ValidateRecord(record);
            // 3 = id, name, category
            Assert.AreEqual(results.Count(), 3);
            Debug.Log($"Messages: {string.Join(",", results.Select(e => e.Message))}");
        }
        [Test]
        public void SingleRecordValidationPassStructTest()
        {
            var record = new SingleRecordTestDataStruct1(1, "TestData2", SingleRecordTestDataCategory.Category1);

            var results = DataValidator.ValidateRecord(record);
            Assert.AreEqual(results.Count(), 0);
        }
        [Test]
        public void SingleRecordValidationDummyStructTest()
        {
            var record = new SingleRecordTestDataStruct2(0, "", SingleRecordTestDataCategory.Default);
            var results = DataValidator.ValidateRecord(record);
            Assert.AreEqual(results.Count(), 0); 
        }
        [Test]
        public void TableValidationErrorStructTest()
        {
            IEnumerable<TableRecordTestDataStruct1> testTable1 = new List<TableRecordTestDataStruct1>() { 
                new TableRecordTestDataStruct1(1, "TestData1", 1),
                new TableRecordTestDataStruct1(1, "TestData2", 1),
                new TableRecordTestDataStruct1(3, "TestData3", 3)
            };

            var results = DataValidator.ValidateTable(testTable1);

            Assert.AreEqual(results.Count(), 2);
            Debug.Log($"Messages: {string.Join(",", results.Select(e => e.Message))}");
        }
        [Test]
        public void TableValidationPassStructTest()
        {
            IEnumerable<TableRecordTestDataStruct1> testTable1 = new List<TableRecordTestDataStruct1>() { 
                new TableRecordTestDataStruct1(1, "TestData1", 1),
                new TableRecordTestDataStruct1(2, "TestData2", 2),
                new TableRecordTestDataStruct1(3, "TestData3", 3)
            };

            var results = DataValidator.ValidateTable(testTable1);

            Assert.AreEqual(results.Count(), 0);
        }
        [Test]
        public void TableValidationDummyStructTest()
        {
            IEnumerable<TableRecordTestDataStruct2> testTable1 = new List<TableRecordTestDataStruct2>() { 
                new TableRecordTestDataStruct2(1, "TestData1", 1),
                new TableRecordTestDataStruct2(1, "TestData2", 1),
                new TableRecordTestDataStruct2(3, "TestData3", 3)
            };

            var results = DataValidator.ValidateTable(testTable1);

            Assert.AreEqual(results.Count(), 0);
        }
        [Test]
        public void MultiTableValidationErrorStructTest()
        {
            DataPool masterData = new MasterData();

            IEnumerable<MultiTableTestDataStruct1> table1 = new List<MultiTableTestDataStruct1>()
            {
                new MultiTableTestDataStruct1(1, "Data1-1", 1),
                new MultiTableTestDataStruct1(2, "Data1-2", 2),
                new MultiTableTestDataStruct1(3, "Data1-3", 4),
            };
            IEnumerable<MultiTableTestDataStruct2> table2 = new List<MultiTableTestDataStruct2>()
            {
                new MultiTableTestDataStruct2(1, "Data2-1"),
                new MultiTableTestDataStruct2(2, "Data2-2"),
                new MultiTableTestDataStruct2(3, "Data2-3"),
            };
            masterData.Set(table1);
            masterData.Set(table2);

            var results = DataValidator.ValidateTables(masterData);

            Assert.AreEqual(results.Count(), 1);
            Debug.Log($"Messages: {string.Join(",", results.Select(e => e.Message))}");
        }
        [Test]
        public void MultiTableValidationPassStructTest()
        {
            DataPool masterData = new MasterData();

            IEnumerable<MultiTableTestDataStruct1> table1 = new List<MultiTableTestDataStruct1>()
            {
                new MultiTableTestDataStruct1(1, "Data1-1", 1),
                new MultiTableTestDataStruct1(2, "Data1-2", 2),
                new MultiTableTestDataStruct1(3, "Data1-3", 3),
            };
            IEnumerable<MultiTableTestDataStruct2> table2 = new List<MultiTableTestDataStruct2>()
            {
                new MultiTableTestDataStruct2(1, "Data2-1"),
                new MultiTableTestDataStruct2(2, "Data2-2"),
                new MultiTableTestDataStruct2(3, "Data2-3"),
            };
            masterData.Set(table1);
            masterData.Set(table2);

            var results = DataValidator.ValidateTables(masterData);

            Assert.AreEqual(results.Count(), 0);
        }
        [Test]
        public void MultiTableValidationDummyStructTest()
        {
            DataPool masterData = new MasterData();

            var results = DataValidator.ValidateTables(masterData);

            Assert.AreEqual(results.Count(), 0);
        }


        [SetUp]
        public void SetUp()
        {
            MasterData.ClearStaticCache();
        }
    }
    
    [Serializable]
    class SingleRecordTestDataClass1
    {
        [NotDefault]
        public int Id;
        [NotDefault]
        public SingleRecordTestDataCategory Category;

        [NotDefault]
        public string Name { get; set; }

        public SingleRecordTestDataClass1(int id, string name, SingleRecordTestDataCategory category)
        {
            Id = id;
            Name = name;
            Category = category;
        }
    }

    [Serializable]
    class SingleRecordTestDataClass2
    {
        public int Id;
        public SingleRecordTestDataCategory Category;
        public string Name { get; set; }

        public SingleRecordTestDataClass2(int id, string name, SingleRecordTestDataCategory category)
        {
            Id = id;
            Name = name;
            Category = category;
        }
    }
    enum SingleRecordTestDataCategory
    {
        Default,
        Category1,
        Category2
    }

    [Serializable]
    class TableRecordTestDataClass1
    {
        [Unique]
        public int Id;
        public string Name;

        [Unique]
        public int Id2 { get; set; }

        public TableRecordTestDataClass1(int id, string name, int id2)
        {
            Id = id;
            Name = name;
            Id2 = id2;
        }
    }

    [Serializable]
    class TableRecordTestDataClass2
    {
        public int Id;
        public string Name;

        public int Id2 { get; set; }

        public TableRecordTestDataClass2(int id, string name, int id2)
        {
            Id = id;
            Name = name;
            Id2 = id2;
        }
    }
    [Serializable]
    class MultiTableTestDataClass1
    {
        public int Id;
        public string Name;

        [ForeignKey(ReferenceTableType = typeof(MultiTableTestDataClass2), ColumnName = "Id")]
        public int Id2;

        public MultiTableTestDataClass1(int id, string name, int id2)
        {
            Id = id;
            Name = name;
            Id2 = id2;
        }
    }
    [Serializable] 
    class MultiTableTestDataClass2
    {
        public int Id;
        public string Name;

        public MultiTableTestDataClass2(int id, string name)
        {
            Id = id;
            Name = name;
        }
    }
    [Serializable]
    struct SingleRecordTestDataStruct1
    {
        [NotDefault]
        public int Id;
        [NotDefault]
        public SingleRecordTestDataCategory Category;

        [NotDefault]
        public string Name { get; set; }

        public SingleRecordTestDataStruct1(int id, string name, SingleRecordTestDataCategory category)
        {
            Id = id;
            Name = name;
            Category = category;
        }
    }

    [Serializable]
    struct SingleRecordTestDataStruct2
    {
        public int Id;
        public SingleRecordTestDataCategory Category;
        public string Name { get; set; }

        public SingleRecordTestDataStruct2(int id, string name, SingleRecordTestDataCategory category)
        {
            Id = id;
            Name = name;
            Category = category;
        }
    }

    [Serializable]
    struct TableRecordTestDataStruct1
    {
        [Unique]
        public int Id;
        public string Name;

        [Unique]
        public int Id2 { get; set; }

        public TableRecordTestDataStruct1(int id, string name, int id2)
        {
            Id = id;
            Name = name;
            Id2 = id2;
        }
    }

    [Serializable]
    struct TableRecordTestDataStruct2
    {
        public int Id;
        public string Name;

        public int Id2 { get; set; }

        public TableRecordTestDataStruct2(int id, string name, int id2)
        {
            Id = id;
            Name = name;
            Id2 = id2;
        }
    }
    [Serializable]
    struct MultiTableTestDataStruct1
    {
        public int Id;
        public string Name;

        [ForeignKey(ReferenceTableType = typeof(MultiTableTestDataStruct2), ColumnName = "Id")]
        public int Id2;

        public MultiTableTestDataStruct1(int id, string name, int id2)
        {
            Id = id;
            Name = name;
            Id2 = id2;
        }
    }
    [Serializable] 
    struct MultiTableTestDataStruct2
    {
        public int Id;
        public string Name;

        public MultiTableTestDataStruct2(int id, string name)
        {
            Id = id;
            Name = name;
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
