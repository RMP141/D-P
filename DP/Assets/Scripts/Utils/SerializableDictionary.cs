using System.Collections.Generic;
using UnityEngine;

namespace ConvoyManager.Utils
{
    /// <summary>
    /// —ловарь, который можно сериализовать в Unity (отображаетс€ в инспекторе) и использовать с JSON.
    /// </summary>
    [System.Serializable]
    public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
    {
        [SerializeField] private List<TKey> _keys = new List<TKey>();
        [SerializeField] private List<TValue> _values = new List<TValue>();

        // ѕустой конструктор необходим дл€ сериализации
        public SerializableDictionary() { }

        //  онструктор, принимающий обычный словарь дл€ удобства создани€
        public SerializableDictionary(IDictionary<TKey, TValue> dictionary) : base(dictionary) { }

        public void OnBeforeSerialize()
        {
            _keys.Clear();
            _values.Clear();
            foreach (var kvp in this)
            {
                _keys.Add(kvp.Key);
                _values.Add(kvp.Value);
            }
        }

        public void OnAfterDeserialize()
        {
            Clear();
            for (int i = 0; i < _keys.Count; i++)
                Add(_keys[i], _values[i]);
        }
    }
}