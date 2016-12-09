using System;
using System.ComponentModel.Composition;
using AIT.DMF.DependencyManager.Controls.Messaging;
using AIT.DMF.DependencyManager.Controls.Messaging.Events;

namespace AIT.DMF.DependencyManager.Controls.ViewModels
{
    public class DependencyDetailsEditorViewModel : ComposableViewModelBase
    {
        #region Private members

        private XmlDependencyViewModel _xmlDependency;

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

        #endregion

        #region Imported Properties

        [Import]
        public IEventPublisher EventPublisher
        {
            get;
            set;
        }

        #endregion

        #region Overrides

        public override void OnImportsSatisfied()
        {
            // subscribe to event
            var observable = EventPublisher.GetEvent<SelectedXmlDependencyChangedEvent>();
            observable.Subscribe(SelectedXmlDependencyChanged);
        }

        #endregion

        #region Events

        private void SelectedXmlDependencyChanged(SelectedXmlDependencyChangedEvent ev)
        {
            XmlDependency = ev.NewValue;
        }

        #endregion
    }
}