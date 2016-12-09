using System.Windows;
using AIT.DMF.Contracts.Common;
using AIT.DMF.Contracts.GUI;
using AIT.DMF.Contracts.Services;

namespace AIT.DMF.Contracts.Provider
{
    /// <summary>
    /// Describes an actual <see cref="IDependencyResolver"/> and is used as factory for the dependency resolver related objects like the editors, resolver itself and so on
    /// </summary>
    public interface IDependencyResolverType
    {
        /// <summary>
        /// Gets the display name of the resolver
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// A unique reference name for the <see cref="IDependencyResolver"/>
        /// </summary>
        string ReferenceName { get; }

        /// <summary>
        /// A unique name for the dependency type.
        /// </summary>
        string DependencyType { get; }

        /// <summary>
        /// Creates an actual <see cref="IDependencyResolver"/> for this resolver type using the provided settings
        /// </summary>
        /// <param name="settings">The settings file used to configure the actual resolver</param>
        /// <returns></returns>
        IDependencyResolver CreateResolver(ISettings<ResolverValidSettings> settings);

        /// <summary>
        /// Gets an editor control that will be used for global configuration (user settings) of the resolver
        /// </summary>
        /// <returns>Returns a editor <see cref="FrameworkElement"/></returns>
        FrameworkElement GetGlobalSettingsEditor();

        /// <summary>
        /// Gets an editor control that will be used by the dependency definition editor
        /// </summary>
        /// <param name="dependencyInjectionService">The service to use to resolve dependencies.</param>
        /// <param name="xmlDependencyViewModel">The view model of the dependency object.</param>
        /// <param name="validDependencyDefinitionFilenameList">Dependency definition filename list (Semicolon separated values).</param>
        /// <param name="teamProjectCollectionUrl">Team Project Collection url</param>
        /// <returns></returns>
        FrameworkElement GetDefinitionEditor(IDependencyInjectionService dependencyInjectionService, IXmlDependencyViewModel xmlDependencyViewModel, string validDependencyDefinitionFilenameList, string teamProjectCollectionUrl);
    }
}
