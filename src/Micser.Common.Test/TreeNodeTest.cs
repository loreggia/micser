using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Micser.App.Infrastructure;

namespace Micser.Infrastructure.Test
{
    [TestClass]
    public class TreeNodeTest
    {
        [TestMethod]
        public void CreateTree()
        {
            var input = new[]
            {
                new TestClass(2, null, "Root 2"),
                new TestClass(1, null, "Root 1"),
                new TestClass(3, 1, "Item"),
                new TestClass(4, 3, "Child")
            };
            var tree = TreeNode<TestClass>.CreateTree(input, x => x.Id, x => x.ParentId, x => x.Text);

            var treeArray = tree.ToArray();

            Assert.AreEqual(2, treeArray.Length);
            Assert.IsNull(treeArray[0].Parent);
            Assert.IsNull(treeArray[1].Parent);
            Assert.AreEqual("Root 1", treeArray[0].Item.Text);
            Assert.AreEqual("Root 2", treeArray[1].Item.Text);
            Assert.AreEqual(1, treeArray[0].Children.Count());
            Assert.AreEqual(0, treeArray[1].Children.Count());
            Assert.AreEqual("Item", treeArray[0].Children.First().Item.Text);
            Assert.AreEqual("Child", treeArray[0].Children.First().Children.First().Item.Text);
        }

        private class TestClass
        {
            public TestClass(int id, int? parentId, string text)
            {
                Id = id;
                ParentId = parentId;
                Text = text;
            }

            public int Id { get; }
            public int? ParentId { get; }
            public string Text { get; }
        }
    }
}