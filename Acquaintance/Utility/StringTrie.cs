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
                Entries = new ConcurrentBag<T>();
                Children = new ConcurrentDictionary<string, TrieNode>();
            }
            public string Key { get; }
            public ConcurrentBag<T> Entries { get; }
            public ConcurrentDictionary<string, TrieNode> Children { get; }
        }

        private TrieNode _root;

        public StringTrie()
        {
            _root = new TrieNode("");
        }

        public void Insert(IEnumerable<string> path, T value)
        {
            TrieNode node = _root;
            foreach (string key in path)
            {
                node = node.Children.GetOrAdd(key, k => new TrieNode(k));
            }
            node.Entries.Add(value);
        }

        public IEnumerable<T> Get(string[] path)
        {
            return GetInternal(path, 0, _root);
        }

        private IEnumerable<T> GetInternal(string[] path, int i, TrieNode node)
        {
            if (i >= path.Length)
                return node.Entries;

            string key = path[i];
            if (key != "*")
            {
                bool ok = node.Children.TryGetValue(key, out node);
                if (ok)
                    return GetInternal(path, i + 1, node);
                return Enumerable.Empty<T>();
            }

            List<T> matches = new List<T>();
            foreach (var child in node.Children.Values)
            {
                var values = GetInternal(path, i + 1, child);
                matches.AddRange(values);
            }
            return matches;
        }
    }
}
