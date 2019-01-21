﻿using System;
using System.Collections.Generic;
using System.Linq;
using Micser.Common.DataAccess;
using NLog;
using Xunit;

namespace Micser.Infrastructure.Test.DataAccess
{
    public class DatabaseTest
    {
        private class TestClass
        {
            public TestClass()
            {
                Id = Guid.NewGuid();
            }

            public Guid Id { get; }
            public IDictionary<string, object> Dictionary { get; set; }
            public int Integer { get; set; }
            public string String { get; set; }
        }

        [Fact]
        public void InstantiateDb()
        {
            var db = new Database("test.db", LogManager.CreateNullLogger());
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
            var db = new Database("test.db", LogManager.CreateNullLogger());

            Guid id;
            
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
            }
        }
    }
}