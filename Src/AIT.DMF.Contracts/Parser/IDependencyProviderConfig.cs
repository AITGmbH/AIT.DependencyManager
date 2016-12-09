namespace AIT.DMF.Contracts.Parser
{
    public interface IDependencyProviderConfig
    {
        /// <summary>
        /// Gets the provider settings type.
        /// </summary>
        string Type { get; set; }

        /// <summary>
        /// Returns the provider settings object.
        /// </summary>
        IDependencyProviderSettings Settings { get; set; }
    }
}