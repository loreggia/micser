using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Micser.Engine.DataAccess.Test
{
    [TestClass]
    public class DatabaseTest
    {
        [TestMethod]
        public void InstantiateDb()
        {
            var db = new Database("test.db");
            var moduleDescriptions = db.GetCollection("test");
            Assert.IsNotNull(moduleDescriptions);
        }
    }
}