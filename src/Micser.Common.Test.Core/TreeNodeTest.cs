using Micser.App.Infrastructure;
using System.Linq;
using Xunit;

namespace Micser.Common.Test
{
    public class TreeNodeTest
    {
        [Fact]
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

            Assert.Equal(2, treeArray.Length);
            Assert.Null(treeArray[0].Parent);
            Assert.Null(treeArray[1].Parent);
            Assert.Equal("Root 1", treeArray[0].Item.Text);
            Assert.Equal("Root 2", treeArray[1].Item.Text);
            Assert.Single(treeArray[0].Children);
            Assert.Empty(treeArray[1].Children);
            Assert.Equal("Item", treeArray[0].Children.First().Item.Text);
            Assert.Equal("Child", treeArray[0].Children.First().Children.First().Item.Text);
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