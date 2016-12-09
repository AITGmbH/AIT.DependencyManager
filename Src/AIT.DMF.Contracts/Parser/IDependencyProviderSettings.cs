using System.Collections.Generic;

namespace AIT.DMF.Contracts.Parser
{
    public interface IDependencyProviderSettings
    {
        /// <summary>
        /// Returns the type of settings list.
        /// </summary>
        DependencyProviderSettingsType Type { get; set; }

        /// <summary>
        /// Returns a list of settings.
        /// </summary>
        List<IDependencyProviderSetting> SettingsList { get; set; }

        /// <summary>
        /// Gets the component name which is described by this dependency.
        /// </summary>
        string GetComponentName();

        /// <summary>
        /// Gets the component version which is decribed by this dependency.
        /// </summary>
        string GetComponentVersion();

        /// <summary>
        /// Gets an existing setting value. For non existing items null will be returned.
        /// </summary>
        /// <param name="name">Settings type</param>
        /// <returns>Value string or null</returns>
        string GetSettingValue(DependencyProviderValidSettingName name);

        /// <summary>
        /// Sets a value for a setting. If not existing creates it or deletes it if needed.
        /// </summary>
        /// <param name="name">Settings type</param>
        /// <param name="value">Settings value</param>
        void SetSettingValue(DependencyProviderValidSettingName name, string value);
    }
}
