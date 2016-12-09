using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows;
using AIT.DMF.Contracts.Services;
using AIT.DMF.DependencyManager.Controls.Messaging;
using AIT.DMF.DependencyManager.Controls.Messaging.Events;
using AIT.DMF.DependencyManager.Controls.Model;
using AIT.DMF.DependencyManager.Controls.Services;

namespace AIT.DMF.DependencyManager.Controls.ViewModels
{
    public class ProviderSettingsEditorViewModel : ComposableViewModelBase
    {
        #region Private members

        private XmlDependencyViewModel _xmlDependency;
        private FrameworkElement _providerSettingsEditor;

        #endregion

        #region Properties

        public XmlDependencyViewModel XmlDependency
        {
            get
            {
                return _xmlDependency;
            }
            private set
            {
                if (_xmlDependency != value)
                {
                    _xmlDependency = value;
                    RaiseNotifyPropertyChanged("XmlDependency");
                }
            }
        }

        public FrameworkElement ProviderSettingsEditor
        {
            get
            {
                return _providerSettingsEditor;
            }
            private set
            {
                if (_providerSettingsEditor != value)
                {
                    _providerSettingsEditor = value;
                    RaiseNotifyPropertyChanged("ProviderSettingsEditor");
                }
            }
        }

        #endregion

        #region Imported Properties

        [Import]
        public IEventPublisher EventPublisher
        {
            get;
            set;
        }

        /// <summary>
        /// DependencyService property introduced for MEF
        /// </summary>
        [Import]
        public IDependencyService DependencyService
        {
            get;
            set;
        }

        /// <summary>
        /// TargetsFileData property introduced for MEF
        /// </summary>
        [Import]
        public TargetsFileData TargetsFileData
        {
            get;
            set;
        }

        /// <summary>
        /// TeamProjectCollectionData property introduced for MEF
        /// </summary>
        [Import]
        public TeamProjectCollectionData TeamProjectCollectionData
        {
            get;
            set;
        }

        #endregion

        #region Overrides

        public override void OnImportsSatisfied()
        {
            // subscribe to events
            var observable = EventPublisher.GetEvent<SelectedXmlDependencyChangedEvent>();
            observable.Subscribe(SelectedXmlDependencyChanged);
        }

        #endregion

        #region Events

        private void SelectedXmlDependencyChanged(SelectedXmlDependencyChangedEvent ev)
        {
            XmlDependency = ev.NewValue;

            if (XmlDependency != null)
            {
                // Get resolver type
                // Todo Introduce query method GetDependencyResolver(string type)
                var resolverType = DependencyService.GetDependencyResolvers().Where(x => x.ReferenceName.Equals("Resolver_" + XmlDependency.Type)).FirstOrDefault();
                // HACK MRI: SourceControlCopy resolver should be used for SourceControl dependencies
                if (resolverType == null && XmlDependency.Type.Equals("SourceControl"))
                {
                    resolverType = DependencyService.GetDependencyResolvers().Where(x => x.ReferenceName.Equals("Resolver_SourceControlMapping")).FirstOrDefault();
                }

                // Get the editor based on type
                if (resolverType != null)
                {
                    // Clean old data context to remove binding between UI and view model
                    if(ProviderSettingsEditor != null && ProviderSettingsEditor.DataContext != null)
                    {
                        ProviderSettingsEditor.DataContext = null;
                        ProviderSettingsEditor = null;
                    }

                    var validDepDefFileList = DependencyService.ServiceSettings.GetSetting(ServiceValidSettings.DefaultDependencyDefinitionFilename);
                    ProviderSettingsEditor = resolverType.GetDefinitionEditor(DependencyInjectionService.Instance, XmlDependency, validDepDefFileList, TeamProjectCollectionData.TPCUri);
                }
            }
            else
            {
                // reset editor 
                ProviderSettingsEditor = null;
            }
        }

        #endregion
    }
}
