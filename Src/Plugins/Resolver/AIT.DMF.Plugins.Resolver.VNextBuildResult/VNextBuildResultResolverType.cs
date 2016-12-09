using AIT.DMF.Contracts.Common;
using AIT.DMF.Contracts.GUI;
using AIT.DMF.Contracts.Provider;
using AIT.DMF.Contracts.Services;
using System;
using System.Windows;

namespace AIT.DMF.Plugins.Resolver.VNextBuildResult
{
    public class VNextBuildResultResolverType : IDependencyResolverType
    {
        public string DisplayName
        {
            get { return "Build Result JSON"; }
        }

        public string ReferenceName
        {
            get { return "Resolver_BuildResultJSON"; }
        }

        public string DependencyType
        {
            get { return "BuildResultJSON"; }
        }

        public IDependencyResolver CreateResolver(ISettings<ResolverValidSettings> settings)
        {
            return new ResolverVNextBuildResult(settings);
        }

        public FrameworkElement GetGlobalSettingsEditor()
        {
            return new VNextBuildResultDefinitionEditor();
        }

        public FrameworkElement GetDefinitionEditor(IDependencyInjectionService dependencyInjectionService, IXmlDependencyViewModel xmlDependencyViewModel, string dependencyDefinitionFileList, string tpcUrl)
        {
            var view = new VNextBuildResultDefinitionEditor();
            view.DataContext = GetDefinitionEditorViewModel(xmlDependencyViewModel, dependencyDefinitionFileList, tpcUrl);
            return view;
        }

        private IValidatingViewModel GetDefinitionEditorViewModel(IXmlDependencyViewModel xmlDependencyViewModel, string dependencyDefinitionFileList, string tpcUrl)
        {
            return new VNextBuildResultDefinitionEditorViewModel(new TfsBuild2Helper(new Uri(tpcUrl)), this, xmlDependencyViewModel, dependencyDefinitionFileList);
        }
    }
}