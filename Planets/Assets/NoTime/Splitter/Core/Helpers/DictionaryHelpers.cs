using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace NoTime.Splitter.Helpers
{
    [Serializable]
    public class DictionaryIntInt
    {
        public List<int> keys = new List<int>();
        public List<int> values = new List<int>();
        public int this[int key]
        {
            get => GetValue(key);
            set => SetValue(key, value);
        }

        private int GetValue(int key)
        {
            return values[keys.IndexOf(key)];
        }

        public bool ContainsKey(int key)
        {
            return keys.Any(x => x == key);
        }
        private void SetValue(int key, int value)
        {
            if (!ContainsKey(key))
            {
                keys.Add(key);
                values.Add(0);
            }

            values[keys.IndexOf(key)] = value;
        }

        public void Add(int key, int value)
        {
            SetValue(key, value);
        }

        public void Remove(int key)
        {
            values.RemoveAt(keys.IndexOf(key));
            keys.RemoveAt(keys.IndexOf(key));
        }
        public List<int> Keys()
        {
            return keys;
        }
        public int Count()
        {
            return keys.Count();
        }
    }

    [Serializable]
    public class DictionaryIntGameObject
    {
        public List<int> keys = new List<int>();
        public List<GameObject> values = new List<GameObject>();
        public GameObject this[int key]
        {
            get => GetValue(key);
            set => SetValue(key, value);
        }

        private GameObject GetValue(int key)
        {
            return values[keys.IndexOf(key)];
        }

        public bool ContainsKey(int key)
        {
            return keys.Any(x => x == key);
        }
        private void SetValue(int key, GameObject value)
        {
            if (!ContainsKey(key))
            {
                keys.Add(key);
                values.Add(null);
            }

            values[keys.IndexOf(key)] = value;
        }

        public void Add(int key, GameObject value)
        {
            SetValue(key, value);
        }

        public void Remove(int key)
        {
            values.RemoveAt(keys.IndexOf(key));
            keys.RemoveAt(keys.IndexOf(key));
        }

        public int Count()
        {
            return keys.Count();
        }
    }

    public static class DictionaryIntGameObjectExtensions
    {
        public static IEnumerable<GameObject> Select(this DictionaryIntGameObject dict, Func<GameObject, GameObject> action)
        {
            return dict.values.Where(x => action(x));
        }
    }
}
