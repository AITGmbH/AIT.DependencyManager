// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BinaryRepositoryResolverType.cs" company="AIT GmbH & Co. KG">
//   All rights reserved by AIT GmbH & Co. KG
// </copyright>
// <summary>
//   Defines the BinaryRepositoryResolverType type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace AIT.DMF.Plugins.Resolver.BinaryRepository
{
    using System.Windows;
    using Contracts.Common;
    using Contracts.GUI;
    using Contracts.Provider;
    using Contracts.Services;
    using DependencyManager.Controls.Services;

    /// <summary>
    /// Defines the resolver type for the BinaryRepository dependency type.
    /// </summary>
    public class BinaryRepositoryResolverType : IDependencyResolverType
    {
        /// <summary>
        /// Gets the display name.
        /// </summary>
        public string DisplayName
        {
            get { return "Binary Repository"; }
        }

        /// <summary>
        /// Gets the reference name.
        /// </summary>
        public string ReferenceName
        {
            get { return "Resolver_BinaryRepository"; }
        }

        /// <summary>
        /// Gets the dependency type.
        /// </summary>
        public string DependencyType
        {
            get { return "BinaryRepository"; }
        }

        /// <summary>
        /// Creates a new BinaryRepository resolver.
        /// </summary>
        /// <param name="settings">The settings.</param>
        /// <returns>The <see cref="IDependencyResolver"/>.</returns>
        public IDependencyResolver CreateResolver(ISettings<ResolverValidSettings> settings)
        {
            return new ResolverBinaryRepository(settings);
        }

        /// <summary>
        /// Gets an editor control that will be used for global configuration (user settings) of the resolver.
        /// </summary>
        /// <returns>Returns the global settings editor.</returns>
        public FrameworkElement GetGlobalSettingsEditor()
        {
            return new BinaryRepositoryGlobalSettingsEditor();
        }

        /// <summary>
        /// Gets the definition editor for BinaryRepository dependencies.
        /// </summary>
        /// <param name="dependencyInjectionService">The dependency injection service.</param>
        /// <param name="xmlDependencyViewModel">The XML dependency view model.</param>
        /// <param name="dependencyDefinitionFileList">The list with valid dependency definition filenames.</param>
        /// <param name="tpcUrl">The binary team project collection url.</param>
        /// <returns>Returns the BinaryRepository definition editor.</returns>
        public FrameworkElement GetDefinitionEditor(IDependencyInjectionService dependencyInjectionService, IXmlDependencyViewModel xmlDependencyViewModel, string dependencyDefinitionFileList, string tpcUrl)
        {
            var view = new BinaryRepositoryDefinitionEditor
                {
                    DataContext =
                        GetDefinitionEditorViewModel(
                            dependencyInjectionService, xmlDependencyViewModel, dependencyDefinitionFileList, tpcUrl)
                };
            return view;
        }

        /// <summary>
        /// Gets the definition editor view model.
        /// </summary>
        /// <param name="dependencyInjectionService">The dependency injection service.</param>
        /// <param name="xmlDependencyViewModel">The XML dependency view model.</param>
        /// <param name="dependencyDefinitionFileList">The list with valid dependency definition filenames.</param>
        /// <param name="tpcUrl">The binary team project collection url.</param>
        /// <returns>The BinaryRepository dependency editor view model.</returns>
        private IValidatingViewModel GetDefinitionEditorViewModel(IDependencyInjectionService dependencyInjectionService, IXmlDependencyViewModel xmlDependencyViewModel, string dependencyDefinitionFileList, string tpcUrl)
        {
            var referencedComponentsTrackingService = dependencyInjectionService.GetDependency<IReferencedComponentsTrackingService>();
            var filter = new UsedComponentsFilter(referencedComponentsTrackingService);

            return new BinaryRepositoryDefinitionEditorViewModel(new TfsAccessService(), this, xmlDependencyViewModel, dependencyDefinitionFileList, filter, tpcUrl);
        }
    }
}
