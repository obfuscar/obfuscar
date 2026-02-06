using System;
using System.Collections;
using System.Collections.Generic;

namespace GenericDictionaryWeakReference
{
    public class DictionaryAdapter<TKey, TValue> : IDictionary<TKey, WeakReference<TValue>>
        where TValue : class
    {
        private readonly Dictionary<TKey, WeakReference<TValue>> _inner = new Dictionary<TKey, WeakReference<TValue>>();

        public void Add(TKey key, TValue value)
        {
            _inner.Add(key, new WeakReference<TValue>(value));
        }

        public void Add(TKey key, WeakReference<TValue> value)
        {
            _inner.Add(key, value);
        }

        public bool ContainsKey(TKey key)
        {
            return _inner.ContainsKey(key);
        }

        public ICollection<TKey> Keys => _inner.Keys;

        public bool Remove(TKey key)
        {
            return _inner.Remove(key);
        }

        public bool TryGetValue(TKey key, out WeakReference<TValue> value)
        {
            return _inner.TryGetValue(key, out value);
        }

        public ICollection<WeakReference<TValue>> Values => _inner.Values;

        public WeakReference<TValue> this[TKey key]
        {
            get => _inner[key];
            set => _inner[key] = value;
        }

        public void Add(KeyValuePair<TKey, WeakReference<TValue>> item)
        {
            ((ICollection<KeyValuePair<TKey, WeakReference<TValue>>>)_inner).Add(item);
        }

        public void Clear()
        {
            _inner.Clear();
        }

        public bool Contains(KeyValuePair<TKey, WeakReference<TValue>> item)
        {
            return ((ICollection<KeyValuePair<TKey, WeakReference<TValue>>>)_inner).Contains(item);
        }

        public void CopyTo(KeyValuePair<TKey, WeakReference<TValue>>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<TKey, WeakReference<TValue>>>)_inner).CopyTo(array, arrayIndex);
        }

        public int Count => _inner.Count;

        public bool IsReadOnly => false;

        public bool Remove(KeyValuePair<TKey, WeakReference<TValue>> item)
        {
            return ((ICollection<KeyValuePair<TKey, WeakReference<TValue>>>)_inner).Remove(item);
        }

        public IEnumerator<KeyValuePair<TKey, WeakReference<TValue>>> GetEnumerator()
        {
            return _inner.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public static class EntryPoint
    {
        public static int Execute()
        {
            IDictionary<string, WeakReference<string>> value = new DictionaryAdapter<string, string>();
            value.Add("k", new WeakReference<string>("v"));
            return value.Count;
        }
    }
}
