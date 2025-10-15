using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace percentagToAmp
{
    public class ObservableDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {
        private readonly Dictionary<TKey, TValue> _dict = new();

        // 變更通知：action = Add / Update / Remove / Clear
        public event Action<string, TKey, TValue> OnChanged;

        public TValue this[TKey key]
        {
            get => _dict[key];
            set => AddOrUpdate(key, value);
        }

        public ICollection<TKey> Keys => _dict.Keys;
        public ICollection<TValue> Values => _dict.Values;
        public int Count => _dict.Count;
        public bool IsReadOnly => false;

        /// <summary>
        /// 新增或更新 (如果 value 沒變，不會觸發通知)
        /// </summary>
        public void AddOrUpdate(TKey key, TValue value)
        {
            if (_dict.TryGetValue(key, out var oldValue))
            {
                // 已存在 → 檢查是否真的改變
                if (!EqualityComparer<TValue>.Default.Equals(oldValue, value))
                {
                    _dict[key] = value;
                    OnChanged?.Invoke("Update", key, value);
                }
            }
            else
            {
                _dict[key] = value;
                OnChanged?.Invoke("Add", key, value);
            }
        }

        public void Add(TKey key, TValue value) => AddOrUpdate(key, value);

        public bool Remove(TKey key)
        {
            if (_dict.TryGetValue(key, out var oldValue) && _dict.Remove(key))
            {
                OnChanged?.Invoke("Remove", key, oldValue);
                return true;
            }
            return false;
        }

        public bool ContainsKey(TKey key) => _dict.ContainsKey(key);
        public bool TryGetValue(TKey key, out TValue value) => _dict.TryGetValue(key, out value);

        public void Clear()
        {
            _dict.Clear();
            OnChanged?.Invoke("Clear", default, default);
        }

        public void Add(KeyValuePair<TKey, TValue> item) => AddOrUpdate(item.Key, item.Value);
        public bool Contains(KeyValuePair<TKey, TValue> item) => _dict.ContainsKey(item.Key);
        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) =>
            ((IDictionary<TKey, TValue>)_dict).CopyTo(array, arrayIndex);
        public bool Remove(KeyValuePair<TKey, TValue> item) => Remove(item.Key);
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _dict.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _dict.GetEnumerator();
    }
}
