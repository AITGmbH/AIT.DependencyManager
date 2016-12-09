using AIT.DMF.Contracts.Enums;

namespace AIT.DMF.Contracts.Parser
{
    public interface IXmlDependency
    {
        /// <summary>
        /// Gets the configuration for the intended provider to use.
        /// </summary>
        IDependencyProviderConfig ProviderConfiguration { get; set; }

        /// <summary>
        /// Gets the type of dependency specified in the XML document.
        /// </summary>
        DependencyType Type { get; set; }
    }
}
