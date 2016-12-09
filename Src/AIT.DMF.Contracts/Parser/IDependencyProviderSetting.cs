namespace AIT.DMF.Contracts.Parser
{
    public interface IDependencyProviderSetting
    {
        /// <summary>
        /// Returns the settings name
        /// </summary>
        DependencyProviderValidSettingName Name { get; set; }

        /// <summary>
        /// Returns value saved in the settings object.
        /// </summary>
        string Value { get; set; }
    }
}
