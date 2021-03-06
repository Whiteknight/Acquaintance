﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Acquaintance.Utility
{
    public static class DictionaryExtensions
    {
        public static void AddOrUpdate<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, TValue value)
        {
            if (dict == null)
                return;
            if (dict.ContainsKey(key))
                dict[key] = value;
            else
                dict.Add(key, value);
        }

        public static void AddOrUpdate<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, TValue addValue, Func<TValue, TValue> updateValue)
        {
            if (dict == null)
                return;
            TValue defaultSetValue(TValue a) => a;
            updateValue = updateValue ?? defaultSetValue;
            if (dict.ContainsKey(key))
                dict[key] = updateValue(dict[key]);
            else
                dict.Add(key, addValue);
        }

        public static bool TryRemove<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dict, TKey key, Action<TValue> onRemoved = null)
        {
            bool ok = dict.TryRemove(key, out var ignored);
            if (ok)
                onRemoved?.Invoke(ignored);
            return ok;
        }
    }
}
