using System.Collections.Generic;
using AIT.DMF.Contracts.Common;

namespace AIT.DMF.Common.Trash
{
    public class Settings<T> : ISettings<T>
    {
        private Dictionary<T, string> _dictionary;

        /// <summary>
        /// Returns the setting dictionary.
        /// </summary>
        public Dictionary<T, string> SettingsDictionary
        {
            get { return _dictionary; }
        }

        /// <summary>
        /// Initialize ComponentSettings with a empty list.
        /// </summary>
        public Settings()
        {
            _dictionary = new Dictionary<T, string>();
        }

        /// <summary>
        /// Adds a new key value pair to the dictionary.
        /// </summary>
        /// <param name="sett">New key value pair</param>
        public void AddSetting(KeyValuePair<T, string> newSetting)
        {
            _dictionary.Add(newSetting.Key, newSetting.Value);
        }

        /// <summary>
        /// Returns the value based on the key submitted.
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>Value string if found. Otherwise null.</returns>
        public string GetSetting(T key)
        {
            string value;
            _dictionary.TryGetValue(key, out value);
            return value;
        }
    }
}
