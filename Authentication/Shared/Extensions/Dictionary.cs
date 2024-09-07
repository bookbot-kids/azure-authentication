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

        public static TValue GetOrDefault<TValue>(this IDictionary<string, TValue> dictionary,
         string key,
         TValue defaultValue, bool ignoreCase = false)
        {
            if (ignoreCase)
            {
                var comparer = StringComparer.OrdinalIgnoreCase;
                foreach (var pair in dictionary)
                {
                    if (comparer.Equals(pair.Key, key))
                    {
                        return pair.Value;
                    }
                }
                return defaultValue;
            }

            return dictionary.TryGetValue(key, out var value) ? value : defaultValue;
        }

        /// <summary>
        /// Export to new dictionary without toRemove keys
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="dictionary"></param>
        /// <param name="toRemove"></param>
        /// <returns></returns>
        public static Dictionary<string, TValue> Export<TValue>(this IDictionary<string, TValue> dictionary, List<string> toRemove, bool ignoreCase = false)
        {
            return dictionary.Where(pair => !toRemove.Contains(ignoreCase? pair.Key.ToLower(): pair.Key))
                                 .ToDictionary(pair => pair.Key,
                                               pair => pair.Value);
        }
    }
}
