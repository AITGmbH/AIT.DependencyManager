using System.Windows;
using AIT.DMF.Contracts.Common;
using AIT.DMF.Contracts.GUI;
using AIT.DMF.Contracts.Provider;
using AIT.DMF.Contracts.Services;

namespace AIT.DMF.Plugins.Resolver.BuildResult
{
    using System;

    public class BuildResultResolverType : IDependencyResolverType
    {
        public string DisplayName
        {
            get { return "Build Result"; }
        }

        public string ReferenceName
        {
            get { return "Resolver_BuildResult"; }
        }

        public string DependencyType
        {
            get { return "BuildResult"; }
        }

        public IDependencyResolver CreateResolver(ISettings<ResolverValidSettings> settings)
        {
            return new ResolverBuildResult(settings);
        }

        public FrameworkElement GetGlobalSettingsEditor()
        {
            return new BuildResultGlobalSettingsEditor();
        }

        public FrameworkElement GetDefinitionEditor(IDependencyInjectionService dependencyInjectionService, IXmlDependencyViewModel xmlDependencyViewModel, string dependencyDefinitionFileList, string tpcUrl)
        {
            var view = new BuildResultDefinitionEditor();
            view.DataContext = GetDefinitionEditorViewModel(xmlDependencyViewModel, dependencyDefinitionFileList, tpcUrl);
            return view;
        }

        private IValidatingViewModel GetDefinitionEditorViewModel(IXmlDependencyViewModel xmlDependencyViewModel, string dependencyDefinitionFileList, string tpcUrl)
        {
            return new BuildResultDefinitionEditorViewModel(new TfsBuildHelper(new Uri(tpcUrl)), this, xmlDependencyViewModel, dependencyDefinitionFileList);
        }
    }
}
