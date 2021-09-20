using System;
using System.Collections.Generic;
using System.Linq;

namespace Extensions
{
    /// <summary>
    /// Dictionary extensions
    /// </summary>
    public static class Dictionary
    {
        /// <summary>
        /// Add value if it's not empty
        /// </summary>
        /// <typeparam name="TKey">key type</typeparam>
        /// <param name="dictionary">input dictionary</param>
        /// <param name="key">key name</param>
        /// <param name="value">key value</param>
        public static void AddIfNotEmpty<TKey>(this Dictionary<TKey, object> dictionary, TKey key, object value)
        {
            if (value == null || (value is string && string.IsNullOrWhiteSpace((string)value)))
            {
                return;
            }

            dictionary.Add(key, value);
        }

        public static TValue GetOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary,
         TKey key,
         TValue defaultValue)
        {
            TValue value;
            return dictionary.TryGetValue(key, out value) ? value : defaultValue;
        }

        /// <summary>
        /// Export to new dictionary without toRemove keys
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="dictionary"></param>
        /// <param name="toRemove"></param>
        /// <returns></returns>
        public static Dictionary<TKey, TValue> Export<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, List<TKey> toRemove)
        {
            return dictionary.Where(pair => !toRemove.Contains(pair.Key))
                                 .ToDictionary(pair => pair.Key,
                                               pair => pair.Value);
        }

    }
}
