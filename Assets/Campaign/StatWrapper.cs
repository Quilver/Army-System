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
        [Range(5, 50)]
        public int CostToField = 10;
        public StatSystem.UnitStats statBase;
        [SerializeField]
        SerializableDictionary<string, StatSystem.Stat> statsGained;
        public void Update()
        {

        }
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
            keys=new(); values=new();
            foreach (var pair in this)
            {
                keys.Add(pair.Key);
                values.Add(pair.Value);
            }
        }
    }
}