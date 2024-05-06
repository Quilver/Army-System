using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Campaign
{
    [System.Serializable]
    public class StatWrapper
    {
        [SerializeField, HideInInspector]
        int XP = 0;
        public StatSystem.UnitStats statBase;
        SerializableDictionary<string, StatSystem.Stat> statsGained;
    }
    [System.Serializable]
    class SerializableDictionary<TKey, TValue>: Dictionary<TKey, TValue>, ISerializationCallbackReceiver
    {
        [SerializeField] List<TKey> keys;
        [SerializeField] List<TValue> values;

        public void OnAfterDeserialize()
        {
            this.Clear();
            if(keys.Count != values.Count)
            {
                Debug.Log("Tried to deserialize dictionary but keys and value did not match");
                for (int i = 0; i < keys.Count; i++)
                {
                    this.Add(keys[i], values[i]);
                }
            }
        }

        public void OnBeforeSerialize()
        {
            keys.Clear(); values.Clear();
            foreach (var pair in this)
            {
                keys.Add(pair.Key);
                values.Add(pair.Value);
            }
        }
    }
}