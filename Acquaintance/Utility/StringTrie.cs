using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Acquaintance.Utility
{
    public class StringTrie<T>
    {
        private readonly TrieNode _root;

        public StringTrie()
        {
            _root = new TrieNode("", null);
        }

        public T GetOrInsert(string root, IEnumerable<string> path, Func<T> getValue)
        {
            var node = _root;
            foreach (string key in new [] { root }.Concat(path))
            {
                ValidateKeyIsNotWildcardForInsert(key);
                node = node.Children.GetOrAdd(key, k => new TrieNode(k, node));
            }
            node.SetValueIfMissing(getValue());
            return node.Value;
        }

        public T GetOrInsert(string root1, string root2, IEnumerable<string> path, Func<T> getValue)
        {
            var node = _root;
            foreach (string key in new [] { root1, root2 }.Concat(path))
            {
                ValidateKeyIsNotWildcardForInsert(key);
                node = node.Children.GetOrAdd(key, k => new TrieNode(k, node));
            }
            node.SetValueIfMissing(getValue());
            return node.Value;
        }

        public IEnumerable<T> Get(string root, string[] path)
        {
            var node = GetStartingNode(root);
            if (node == null)
                return Enumerable.Empty<T>();
            
            var foundNodes = new List<TrieNode>();
            GetInternal(path, 0, node, foundNodes);
            return foundNodes.Select(n => n.Value);
        }

        public IEnumerable<T> Get(string root1, string root2, string[] path)
        {
            var node = GetStartingNode(root1, root2);
            if (node == null)
                return Enumerable.Empty<T>();
            var foundNodes = new List<TrieNode>();
            GetInternal(path, 0, node, foundNodes);
            return foundNodes.Select(n => n.Value);
        }

        public void OnEach(Action<T> act)
        {
            OnEach(_root, act);
        }

        public void RemoveValue(string root, string[] path, Action<T> onRemoved)
        {
            if (path == null || path.Length == 0)
                return;
            var parent = _root;
            var child = GetStartingNode(root);
            if (child == null || child == parent)
                return;
            foreach (var p in path)
            {
                if (string.IsNullOrEmpty(p) || p == "*")
                    throw new Exception("Cannot remove on a wildcard");
                parent = child;
                if (!parent.Children.TryGetValue(p, out child))
                    return;
            }
            if (child == null)
                return;
            var nodeToRemove = child;
            while (nodeToRemove.Parent != _root && nodeToRemove.Children.Count <= 1)
                nodeToRemove = nodeToRemove.Parent;
            nodeToRemove.Parent.Children.TryRemove(nodeToRemove.Key, out nodeToRemove);
            if (onRemoved != null)
                OnEach(nodeToRemove, onRemoved);
        }

        public class TrieNode
        {
            public TrieNode(string key, TrieNode parent)
            {
                Key = key;
                Parent = parent;
                Children = new ConcurrentDictionary<string, TrieNode>();
            }

            public string Key { get; }
            public T Value { get; private set; }
            public bool HasValue { get; private set; }
            public ConcurrentDictionary<string, TrieNode> Children { get; }
            public TrieNode Parent { get; }

            public void SetValueIfMissing(T value)
            {
                if (HasValue)
                    return;
                Value = value;
                HasValue = true;
            }
        }

        private TrieNode GetStartingNode(string root)
        {
            if (string.IsNullOrEmpty(root))
                return _root;
            return _root.Children.TryGetValue(root, out TrieNode node) ? node : null;
        }

        private TrieNode GetStartingNode(string root1, string root2)
        {
            if (string.IsNullOrEmpty(root1))
                return _root;
            if (!_root.Children.TryGetValue(root1, out TrieNode node))
                return null;
            if (string.IsNullOrEmpty(root2))
                return node;
            return node.Children.TryGetValue(root2, out node) ? node : null;
        }

        private void OnEach(TrieNode node, Action<T> act)
        {
            act(node.Value);
            foreach (var child in node.Children.Values)
                OnEach(child, act);
        }

        private void GetInternal(string[] path, int i, TrieNode node, List<TrieNode> resultNodes)
        {
            if (i >= path.Length)
            {
                resultNodes.Add(node);
                return;
            }

            string key = path[i];
            if (key != "*")
            {
                if (node.Children.TryGetValue(key, out node))
                    GetInternal(path, i + 1, node, resultNodes);
                return;
            }

            foreach (var child in node.Children.Values)
                GetInternal(path, i + 1, child, resultNodes);
        }

        private static void ValidateKeyIsNotWildcardForInsert(string key)
        {
            if (key == "*")
                throw new Exception("Cannot use wildcards for GetOrInsert");
        }
        // TODO: Logic to remove entries from the Trie
    }
}
