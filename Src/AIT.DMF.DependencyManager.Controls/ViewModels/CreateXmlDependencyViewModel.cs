using System;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows.Threading;
using AIT.DMF.Common;
using AIT.DMF.Contracts.GUI;
using AIT.DMF.Contracts.Parser;
using AIT.DMF.Contracts.Provider;
using AIT.DMF.Contracts.Services;

namespace AIT.DMF.DependencyManager.Controls.ViewModels
{
    public class CreateXmlDependencyViewModel : ComposableViewModelBase, IXmlDependencyViewModel
    {
        private ObservableCollection<IDependencyResolverType> _availableResolverTypes;
        private IDependencyResolverType _selectedResolverType;

        public event EventHandler<XmlDependencyCreationEventArgs> XmlDependencyCreationRequest;

        public ObservableCollection<IDependencyResolverType> AvailableResolverTypes
        {
            get
            {
                return _availableResolverTypes;
            }
            set
            {
                if (_availableResolverTypes != value)
                {
                    _availableResolverTypes = value;
                    RaiseNotifyPropertyChanged("AvailableResolverTypes");
                }
            }
        }

        public IDependencyResolverType SelectedResolverType
        {
            get
            {
                return _selectedResolverType;
            }
            set
            {
                if (_selectedResolverType != value)
                {
                    _selectedResolverType = value;
                    RaiseNotifyPropertyChanged("SelectedResolverType");

                    // raise the create event
                    if (_selectedResolverType != null)
                    {
                        RaiseXmlDependencyCreationRequest();
                    }

                    // once the event was raised, we recursively reset ourselves to null
                    // (workaround: must be executed using the dispatcher or it won't be picked up by the binding engine)
                    Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() => SelectedResolverType = null));
                }
            }
        }

        [Import]
        public IDependencyService DependencyService
        {
            get;
            set;
        }

        private void RaiseXmlDependencyCreationRequest()
        {
            var handlers = XmlDependencyCreationRequest;
            if (handlers != null)
            {
                handlers(this, new XmlDependencyCreationEventArgs(SelectedResolverType));
            }
        }

        public override void OnImportsSatisfied()
        {
            var availableResolvers = DependencyService.GetDependencyResolvers();
            var disabledResolvers = DependencyManagerSettings.Instance.DisabledResolvers;
            var enabledResolvers = availableResolvers.Where(x => disabledResolvers.Contains(x.ReferenceName) == false);
            AvailableResolverTypes = new ObservableCollection<IDependencyResolverType>(enabledResolvers);
        }

        #region Implementation of IXmlDependencyViewModel

        public IXmlDependency XmlDependency
        {
            get
            {
                return null;
            }
        }

        public string ReferencedComponentName
        {
            get { return null; }
        }

        public string ReferencedComponentVersion
        {
            get { return null; }
        }

        public string Type
        {
            get { return null; }
        }

        public void SetChanged()
        {
            // this view model does not support changes
        }

        public void SetValid(bool isValid)
        {
            // this view model does not support validation
        }

        #endregion
    }
}
