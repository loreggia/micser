using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Micser.Common.DataAccess;
using NLog;

namespace Micser.Engine.Test.DataAccess
{
    [TestClass]
    public class DatabaseTest
    {
        [TestMethod]
        public void InstantiateDb()
        {
            var db = new Database("test.db", LogManager.CreateNullLogger());
            var dbContext = db.GetContext();
            Assert.IsNotNull(dbContext);
        }

        [TestMethod]
        public void SaveAndLoadObject()
        {
            var db = new Database("test.db", LogManager.CreateNullLogger());
            using (var saveContext = db.GetContext())
            {
                var testObject = new TestClass
                {
                    Dictionary = new Dictionary<string, object>
                    {
                        {"key", "value"}
                    },
                    Integer = 42,
                    String = "Test"
                };

                saveContext.GetCollection<TestClass>().Insert(testObject);
            }

//            saveContext.SetObject(testObject);
//            saveContext.Save();
//
//            var loadContext = db.GetContext();
//            var result = loadContext.GetObject<TestClass>();
//
//            Assert.IsNotNull(result);
//            Assert.AreEqual("Test", result.String);
//            Assert.AreEqual(42, result.Integer);
//            Assert.IsNotNull(result.Dictionary);
//            Assert.AreEqual(1, result.Dictionary.Count);
//            Assert.IsTrue(result.Dictionary.ContainsKey("key"));
//            Assert.AreEqual("value", result.Dictionary["key"]);
        }

        private class TestClass
        {
            public IDictionary<string, object> Dictionary { get; set; }
            public int Integer { get; set; }
            public string String { get; set; }
        }
    }
}