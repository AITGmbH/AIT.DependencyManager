using System.Windows;
using AIT.DMF.Contracts.Common;
using AIT.DMF.Contracts.GUI;
using AIT.DMF.Contracts.Provider;
using AIT.DMF.Contracts.Services;

namespace AIT.DMF.Plugins.Resolver.SourceControl
{
    public class SourceControlCopyResolverType : IDependencyResolverType
    {
        #region Public Properties

        public string DisplayName
        {
            get { return "Source Control Copy"; }
        }

        public string ReferenceName
        {
            get { return "Resolver_SourceControlCopy"; }
        }

        public string DependencyType
        {
            get { return "SourceControlCopy"; }
        }

        #endregion

        /// <summary>
        /// Creates an actual <see cref="IDependencyResolver"/> for this resolver type using the provided settings
        /// </summary>
        /// <param name="settings">The settings used to configure the resolver</param>
        /// <returns></returns>
        public IDependencyResolver CreateResolver(ISettings<ResolverValidSettings> settings)
        {
            return new ResolverSourceControl(settings, ReferenceName);
        }

        /// <summary>
        /// Gets an editor control that will be used for global configuration (user settings) of the resolver
        /// </summary>
        /// <returns>Returns a editor</returns>
        public FrameworkElement GetGlobalSettingsEditor()
        {
            return new SourceControlGlobalSettingsEditor();
        }

        /// <summary>
        /// Gets the definition editor.
        /// </summary>
        /// <param name="dependencyInjectionService">The dependency injection service.</param>
        /// <param name="xmlDependencyViewModel">The XML dependency view model.</param>
        /// <param name="dependencyDefinitionFileList">The list with valid the dependency definition file names.</param>
        /// <param name="tpcUrl">The team project collection url.</param>
        /// <returns>Returns the definition editor</returns>
        public FrameworkElement GetDefinitionEditor(IDependencyInjectionService dependencyInjectionService, IXmlDependencyViewModel xmlDependencyViewModel, string dependencyDefinitionFileList, string tpcUrl)
        {
            var view = new SourceControlCopyDefinitionEditor
            {
                DataContext =
                    GetDefinitionEditorViewModel(xmlDependencyViewModel, dependencyDefinitionFileList,
                                                 tpcUrl)
            };
            return view;
        }

        /// <summary>
        /// Gets the definition editor view model.
        /// </summary>
        /// <param name="xmlDependencyViewModel">The XML dependency view model.</param>
        /// <param name="dependencyDefinitionFileList">The list with valid dependency definition file names.</param>
        /// <param name="tpcUrl">The team project collection url.</param>
        /// <returns>Returns the definition editor view model</returns>
        private IValidatingViewModel GetDefinitionEditorViewModel(IXmlDependencyViewModel xmlDependencyViewModel, string dependencyDefinitionFileList, string tpcUrl)
        {
            return new SourceControlDefinitionEditorViewModel(new TfsAccessService(), this, xmlDependencyViewModel, dependencyDefinitionFileList, tpcUrl);
        }
    }
}
