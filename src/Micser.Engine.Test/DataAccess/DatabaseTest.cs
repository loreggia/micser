using Microsoft.VisualStudio.TestTools.UnitTesting;
using Micser.Engine.DataAccess;

namespace Micser.Engine.Test.DataAccess
{
    [TestClass]
    public class DatabaseTest
    {
        [TestMethod]
        public void InstantiateDb()
        {
            var db = new DbContext(new ConnectionString("test.db"));
            var moduleDescriptions = db.GetCollection("test");
            Assert.IsNotNull(moduleDescriptions);
        }
    }
}