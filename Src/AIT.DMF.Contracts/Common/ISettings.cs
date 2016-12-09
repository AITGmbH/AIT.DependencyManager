using System.Collections.Generic;

namespace AIT.DMF.Contracts.Common
{
    public interface ISettings<T>
    {
        /// <summary>
        /// Stores settings with key and value.
        /// </summary>
        Dictionary<T, string> SettingsDictionary { get; }

        /// <summary>
        /// Adds a new setting to the dictionary.
        /// </summary>
        /// <param name="newSetting">New setting</param>
        void AddSetting(KeyValuePair<T, string> newSetting);

        /// <summary>
        /// Gets a setting based on the settings name from the dictionary.
        /// </summary>
        /// <param name="settName">Settings name</param>
        string GetSetting(T settName);
    }
}
