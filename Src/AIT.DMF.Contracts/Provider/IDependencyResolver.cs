using System.Collections.Generic;
using System.Xml.Linq;
using AIT.DMF.Contracts.Common;
using AIT.DMF.Contracts.Services;

namespace AIT.DMF.Contracts.Provider
{
    /// <summary>
    /// The basic interface that every specific provider has to implemented so that it can be used to resolve dependencies
    /// </summary>
    public interface IDependencyResolver
    {
        /// <summary>
        /// Determines the resolver type.
        /// </summary>
        string ResolverType { get; }

        /// <summary>
        /// Determines the component targets name.
        /// </summary>
        string ComponentTargetsName { get; }

        /// <summary>
        /// Contains all the information needed like the team project collection url, the workspace information etc.
        /// This url can be a url to a webservice address (TFS) or to a file share or something else.
        /// </summary>
        ISettings<ResolverValidSettings> ResolverSettings { get; }

        /// <summary>
        /// This method is used to discover all available component names in a specific repository
        /// </summary>
        /// <returns>Returns all component names which are availabel for the specific provider</returns>
        IEnumerable<IComponentName> GetAvailableComponentNames();

        /// <summary>
        /// Gets all available versions for a specific component
        /// </summary>
        /// <param name="componentName">The name of the component</param>
        /// <returns>A list with all versions</returns>
        IEnumerable<IComponentVersion> GetAvailableVersions(IComponentName componentName);

        /// <summary>
        /// Loads a specific component targets file.
        /// </summary>
        /// <param name="componentName">The name of the component</param>
        /// <param name="version">The exact version of the component</param>
        /// <returns>The loaded xml file (component.targets)</returns>
        XDocument LoadComponentTarget(IComponentName componentName, IComponentVersion version);

        /// <summary>
        /// Determins wether a component exists
        /// </summary>
        /// <param name="name">The name of the componet</param>
        /// <returns>true if the component exists; false if not</returns>
        bool ComponentExists(IComponentName name);

        /// <summary>
        /// Determines wether a component exists having a specific version
        /// </summary>
        /// <param name="name">The name of the component</param>
        /// <param name="version">The specific version of the component</param>
        /// <returns>true if the component exists at the version; false otherwise</returns>
        bool ComponentExists(IComponentName name, IComponentVersion version);
    }
}