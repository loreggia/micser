using System;
using System.Collections.Generic;
using System.Linq;

namespace Micser.Infrastructure
{
    public class TreeNode<T>
    {
        public TreeNode(T item)
        {
            Item = item;
        }

        public IEnumerable<TreeNode<T>> Children { get; set; }
        public T Item { get; }
        public TreeNode<T> Parent { get; set; }

        public static IEnumerable<TreeNode<T>> CreateTree(IEnumerable<T> source, Func<T, object> idSelector,
            Func<T, object> parentIdSelector, Func<T, object> orderFunc)
        {
            var treeNodeList = source.Select(x => new TreeNode<T>(x)).ToArray();

            foreach (var treeNode in treeNodeList)
            {
                treeNode.Children = treeNodeList.Where(c => Equals(parentIdSelector(c.Item), idSelector(treeNode.Item))).OrderBy(x => orderFunc(x.Item));
                foreach (var child in treeNode.Children)
                {
                    child.Parent = treeNode;
                }
            }

            return treeNodeList.Where(x => x.Parent == null);
        }
    }
}