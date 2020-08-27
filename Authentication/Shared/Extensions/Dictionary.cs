using System.Collections.Generic;

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
    }
}
