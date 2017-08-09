using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Acquaintance.Utility
{
    public class StringTrie<T>
    {
        public class TrieNode
        {
            public TrieNode(string key)
            {
                Key = key;
                Children = new ConcurrentDictionary<string, TrieNode>();
            }
            public string Key { get; }
            public T Value { get; private set; }
            public bool HasValue { get; private set; }
            public void SetValueIfMissing(T value)
            {
                if (HasValue)
                    return;
                Value = value;
                HasValue = true;
            }
            public ConcurrentDictionary<string, TrieNode> Children { get; }
        }

        private readonly TrieNode _root;

        public StringTrie()
        {
            _root = new TrieNode("");
        }

        public T GetOrInsert(string root, IEnumerable<string> path, Func<T> getValue)
        {
            var node = _root;
            if (!string.IsNullOrEmpty(root))
            {
                if (root == "*")
                    throw new Exception("Cannot use wildcards for GetOrInsert");
                node = node.Children.GetOrAdd(root, k => new TrieNode(root));
            }

            foreach (string key in path)
            {
                if (key == "*")
                    throw new Exception("Cannot use wildcards for GetOrInsert");
                node = node.Children.GetOrAdd(key, k => new TrieNode(k));
            }
            node.SetValueIfMissing(getValue());
            return node.Value;
        }

        public T GetOrInsert(string root1, string root2, IEnumerable<string> path, Func<T> getValue)
        {
            var node = _root;
            if (!string.IsNullOrEmpty(root1))
            {
                if (root1 == "*")
                    throw new Exception("Cannot use wildcards for GetOrInsert");
                node = node.Children.GetOrAdd(root1, k => new TrieNode(root1));
            }

            if (!string.IsNullOrEmpty(root2))
            {
                if (root2 == "*")
                    throw new Exception("Cannot use wildcards for GetOrInsert");
                node = node.Children.GetOrAdd(root2, k => new TrieNode(root2));
            }

            foreach (string key in path)
            {
                if (key == "*")
                    throw new Exception("Cannot use wildcards for GetOrInsert");
                node = node.Children.GetOrAdd(key, k => new TrieNode(k));
            }
            node.SetValueIfMissing(getValue());
            return node.Value;
        }


        public IEnumerable<T> Get(string root, string[] path)
        {
            var node = _root;
            if (!string.IsNullOrEmpty(root))
            {
                bool ok = node.Children.TryGetValue(root, out node);
                if (!ok)
                    return Enumerable.Empty<T>();
            }
            var foundNodes = new List<TrieNode>();
            GetInternal(path, 0, node, foundNodes, true);
            return foundNodes.Select(n => n.Value);
        }

        public IEnumerable<T> Get(string root1, string root2, string[] path)
        {
            var node = _root;
            if (!string.IsNullOrEmpty(root1))
            {
                bool ok = node.Children.TryGetValue(root1, out node);
                if (!ok)
                    return Enumerable.Empty<T>();
            }
            if (!string.IsNullOrEmpty(root2))
            {
                bool ok = node.Children.TryGetValue(root2, out node);
                if (!ok)
                    return Enumerable.Empty<T>();
            }
            var foundNodes = new List<TrieNode>();
            GetInternal(path, 0, node, foundNodes, true);
            return foundNodes.Select(n => n.Value);
        }

        public void OnEach(Action<T> act)
        {
            OnEach(_root, act);
        }

        private void OnEach(TrieNode node, Action<T> act)
        {
            act(node.Value);
            foreach (var child in node.Children.Values)
                OnEach(child, act);
        }

        private void GetInternal(string[] path, int i, TrieNode node, List<TrieNode> resultNodes, bool allowWildcards)
        {
            if (i >= path.Length)
            {
                resultNodes.Add(node);
                return;
            }

            string key = path[i];
            if (key != "*")
            {
                bool ok = node.Children.TryGetValue(key, out node);
                if (ok)
                    GetInternal(path, i + 1, node, resultNodes, allowWildcards);
                return;
            }

            if (!allowWildcards)
                throw new Exception("Cannot use a wildcard here");

            foreach (var child in node.Children.Values)
                GetInternal(path, i + 1, child, resultNodes, true);
        }

        // TODO: Logic to remove entries from the Trie
    }
}
