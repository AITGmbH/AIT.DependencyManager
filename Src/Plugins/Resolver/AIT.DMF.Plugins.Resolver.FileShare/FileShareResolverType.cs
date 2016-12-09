// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FileShareResolverType.cs" company="AIT GmbH & Co. KG">
//   All rights reserved by AIT GmbH & Co. KG
// </copyright>
// <summary>
//   Defines the FileShareResolverType type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace AIT.DMF.Plugins.Resolver.FileShare
{
    using System.Windows;
    using Contracts.Common;
    using Contracts.GUI;
    using Contracts.Provider;
    using Contracts.Services;

    /// <summary>
    /// The file share resolver type.
    /// </summary>
    public class FileShareResolverType : IDependencyResolverType
    {
        /// <summary>
        /// Gets the display name of the resolver.
        /// </summary>
        public string DisplayName
        {
            get { return "File Share"; }
        }

        /// <summary>
        /// Gets the unique reference name for the <see cref="IDependencyResolver" />.
        /// </summary>
        public string ReferenceName
        {
            get { return "Resolver_FileShare"; }
        }

        /// <summary>
        /// Gets the unique name for the dependency type.
        /// </summary>
        public string DependencyType
        {
            get { return "FileShare"; }
        }

        /// <summary>
        /// Creates an <see cref="IDependencyResolver" /> for this resolver type using the provided settings.
        /// </summary>
        /// <param name="settings">The settings used to configure the resolver.</param>
        /// <returns>The created ResolverFileShare resolver.</returns>
        public IDependencyResolver CreateResolver(ISettings<ResolverValidSettings> settings)
        {
            return new ResolverFileShare(settings);
        }

        /// <summary>
        /// Gets the editor control that will be used for global configuration (user settings) of the resolver.
        /// </summary>
        /// <returns>The global settings editor <see cref="FrameworkElement" /></returns>
        public FrameworkElement GetGlobalSettingsEditor()
        {
            return new FileShareGlobalSettingsEditor();
        }

        /// <summary>
        /// Gets the file share definition editor.
        /// </summary>
        /// <param name="dependencyInjectionService">The dependency injection service.</param>
        /// <param name="xmlDependencyViewModel">The XML dependency view model.</param>
        /// <param name="dependencyDefinitionFileList">The dependency definition file list.</param>
        /// <param name="tpcUrl">The team project collection URL.</param>
        /// <returns>The initialized file share definition editor</returns>
        public FrameworkElement GetDefinitionEditor(IDependencyInjectionService dependencyInjectionService, IXmlDependencyViewModel xmlDependencyViewModel, string dependencyDefinitionFileList, string tpcUrl)
        {
            var view = new FileShareDefinitionEditor
                {
                    DataContext = GetDefinitionEditorViewModel(xmlDependencyViewModel, dependencyDefinitionFileList)
                };
            return view;
        }

        /// <summary>
        /// Gets the file share definition editor view model.
        /// </summary>
        /// <param name="xmlDependencyViewModel">The XML dependency view model.</param>
        /// <param name="dependencyDefinitionFileList">The dependency definition file list.</param>
        /// <returns>The file share definition editor view model</returns>
        private IValidatingViewModel GetDefinitionEditorViewModel(IXmlDependencyViewModel xmlDependencyViewModel, string dependencyDefinitionFileList)
        {
            return new FileShareDefinitionEditorViewModel(this, xmlDependencyViewModel, dependencyDefinitionFileList);
        }
    }
}
