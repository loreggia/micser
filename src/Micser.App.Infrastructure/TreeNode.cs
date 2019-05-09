using System;
using System.Collections.Generic;
using System.Linq;

namespace Micser.App.Infrastructure
{
    /// <summary>
    /// A basic hierarchical tree node.
    /// </summary>
    /// <typeparam name="T">Type of the attached data.</typeparam>
    public class TreeNode<T>
    {
        public TreeNode(T item)
        {
            Item = item;
        }

        public IEnumerable<TreeNode<T>> Children { get; private set; }
        public T Item { get; }
        public TreeNode<T> Parent { get; private set; }

        public static IEnumerable<TreeNode<T>> CreateTree<TId>(IEnumerable<T> source, Func<T, TId> idSelector,
                                                               Func<T, TId> parentIdSelector, Func<T, object> orderFunc)
        {
            var treeNodeList = source.Select(x => new TreeNode<T>(x)).ToArray();

            foreach (var treeNode in treeNodeList)
            {
                treeNode.Children = treeNodeList.Where(c => Equals(parentIdSelector(c.Item), idSelector(treeNode.Item)))
                                                .OrderBy(x => orderFunc(x.Item));
                foreach (var child in treeNode.Children)
                {
                    child.Parent = treeNode;
                }
            }

            return treeNodeList.Where(x => x.Parent == null).OrderBy(x => orderFunc(x.Item));
        }
    }
}