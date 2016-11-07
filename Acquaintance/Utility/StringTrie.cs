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
            TrieNode node = _root;
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

        public IEnumerable<T> Get(string root, string[] path)
        {
            TrieNode node = _root;
            if (!string.IsNullOrEmpty(root))
            {
                bool ok = node.Children.TryGetValue(root, out node);
                if (!ok)
                    return Enumerable.Empty<T>();
            }
            return GetInternal(path, 0, node, true).Select(n => n.Value);
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

        private IEnumerable<TrieNode> GetInternal(string[] path, int i, TrieNode node, bool allowWildcards)
        {
            if (i >= path.Length)
                return new[] { node };

            string key = path[i];
            if (key != "*")
            {
                bool ok = node.Children.TryGetValue(key, out node);
                if (ok)
                    return GetInternal(path, i + 1, node, allowWildcards);
                return Enumerable.Empty<TrieNode>();
            }

            if (!allowWildcards)
                throw new Exception("Cannot use a wildcard here");

            List<TrieNode> matches = new List<TrieNode>();
            foreach (var child in node.Children.Values)
            {
                var values = GetInternal(path, i + 1, child, true);
                matches.AddRange(values);
            }
            return matches;
        }

        // TODO: Logic to remove entries from the Trie
    }
}
