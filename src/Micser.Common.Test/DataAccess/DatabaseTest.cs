using Micser.Common.DataAccess;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Micser.Common.Test.DataAccess
{
    public class DatabaseTest : IDisposable
    {
        private readonly TestFileManager _testFileManager;

        public DatabaseTest(ITestOutputHelper testOutputHelper)
        {
            _testFileManager = new TestFileManager(testOutputHelper);
        }

        public void Dispose()
        {
            _testFileManager.DeleteFiles();
        }

        [Fact]
        public void InstantiateDb()
        {
            var fileName = _testFileManager.GetFileName();
            var db = new MicserDbContext(fileName, LogManager.CreateNullLogger());
            Assert.NotNull(db);

            using (var dbContext = db.GetContext())
            {
                Assert.NotNull(dbContext);

                var collection = dbContext.GetCollection<TestClass>();

                Assert.NotNull(collection);
            }
        }

        [Fact]
        public void SaveAndLoadObject()
        {
            var fileName = _testFileManager.GetFileName();
            var db = new MicserDbContext(fileName, LogManager.CreateNullLogger());

            var id = Guid.NewGuid();
            var now = DateTime.Now;

            using (var saveContext = db.GetContext())
            {
                var testObject = new TestClass
                {
                    Id = id,
                    Dictionary = new Dictionary<string, object>
                    {
                        {"key", "value"}
                    },
                    Integer = 42,
                    String = "Test",
                    DateTime = now
                };
                id = testObject.Id;

                saveContext.GetCollection<TestClass>().Insert(testObject);
                saveContext.Save();
            }

            using (var loadContext = db.GetContext())
            {
                var result = loadContext.GetCollection<TestClass>().FirstOrDefault(x => x.Id == id);

                Assert.NotNull(result);
                Assert.Equal("Test", result.String);
                Assert.Equal(42, result.Integer);
                Assert.NotNull(result.Dictionary);
                Assert.Equal(1, result.Dictionary.Count);
                Assert.True(result.Dictionary.ContainsKey("key"));
                Assert.Equal("value", result.Dictionary["key"]);
                Assert.Equal(now, result.DateTime);
            }
        }

        private class TestClass
        {
            public DateTime DateTime { get; set; }
            public IDictionary<string, object> Dictionary { get; set; }
            public Guid Id { get; set; }
            public int Integer { get; set; }
            public string String { get; set; }
        }
    }
}