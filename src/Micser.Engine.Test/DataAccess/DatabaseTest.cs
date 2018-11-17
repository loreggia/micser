using Microsoft.VisualStudio.TestTools.UnitTesting;
using Micser.Infrastructure.DataAccess;
using NLog;

namespace Micser.Engine.Test.DataAccess
{
    [TestClass]
    public class DatabaseTest
    {
        [TestMethod]
        public void InstantiateDb()
        {
            var db = new Database("test.db", LogManager.GetCurrentClassLogger());
            var dbContext = db.GetContext();
            Assert.IsNotNull(dbContext);
        }
    }
}