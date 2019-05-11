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
        /// <summary>
        /// Creates an instance of the <see cref="TreeNode{T}"/> class wrapping an object.
        /// </summary>
        /// <param name="item">The object that corresponds to this node in the tree.</param>
        public TreeNode(T item)
        {
            Item = item;
        }

        /// <summary>
        /// Gets the children of this node.
        /// </summary>
        public IEnumerable<TreeNode<T>> Children { get; private set; }

        /// <summary>
        /// Gets the wrapped object.
        /// </summary>
        public T Item { get; }

        /// <summary>
        /// Gets the parent node.
        /// </summary>
        public TreeNode<T> Parent { get; private set; }

        /// <summary>
        /// Creates a tree consisting of <see cref="TreeNode{T}"/> nodes.
        /// </summary>
        /// <param name="source">The source objects.</param>
        /// <param name="idSelector">A selector function that returns an object's ID that is used to match parent/child relationships.</param>
        /// <param name="parentIdSelector">A selector function that returns an object's parent ID that is used to match parent/child relationships.</param>
        /// <param name="orderFunc">A function returning a value by which siblings are ordered.</param>
        /// <typeparam name="TId">The type of the ID and parent ID values.</typeparam>
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