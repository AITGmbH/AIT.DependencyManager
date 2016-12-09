using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using AIT.DMF.DependencyManager.Controls.Messaging;
using AIT.DMF.DependencyManager.Controls.Messaging.Events;

namespace AIT.DMF.DependencyManager.Controls.Services
{
    [Export(typeof(IChangeTrackingService))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class ChangeTrackingService : IChangeTrackingService
    {
        private readonly List<IChangeTracking> _changedObjects = new List<IChangeTracking>();

        #region Implementation of INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Imported Properties

        [Import]
        public IEventPublisher EventPublisher
        {
            get;
            set;
        }

        #endregion

        #region Implementation of IChangeTrackingService

        public event EventHandler<EventArgs> HasChangesChanged;

        public bool HasChanges
        {
            get
            {
#if DEBUG
                if (_changedObjects.Any(o => !o.IsChanged))
                {
                    throw new InvalidOperationException("An unchanged object is registered for the change tracking service.");
                }
#endif

                return _changedObjects.Any(o => o.IsChanged);
            }
        }

        public ChangeTrackingService()
        {
            DependencyInjectionService.Instance.CompositionService.SatisfyImportsOnce(this);
        }

        private void RaiseHasChangesPropertyChanged()
        {
            var handlers = PropertyChanged;
            if (handlers != null)
            {
                handlers(this, new PropertyChangedEventArgs("HasChanges"));
            }

            var hasChangesChangedHandlers = HasChangesChanged;
            if (hasChangesChangedHandlers != null)
            {
                hasChangesChangedHandlers(this, EventArgs.Empty);
            }
        }

        public void Add(IChangeTracking changedObject)
        {
            if (!changedObject.IsChanged)
            {
                throw new ArgumentOutOfRangeException("changedObject", "An unchanged object shall be registered for the change tracking service.");
            }

            if (!_changedObjects.Contains(changedObject))
            {
                _changedObjects.Add(changedObject);
                RaiseHasChangesPropertyChanged();
            }
        }

        public void Remove(IChangeTracking trackedObject)
        {
            if (_changedObjects.Contains(trackedObject))
            {
                _changedObjects.Remove(trackedObject);
                RaiseHasChangesPropertyChanged();
            }
        }

        public void Save()
        {
            if (EventPublisher != null)
            {
                EventPublisher.Publish(new SaveAllChangesEvent(null));
            }
        }

        public void Save(string fileName)
        {
            if (EventPublisher != null)
            {
                EventPublisher.Publish(new SaveAllChangesEvent(fileName));
            }
        }

        #endregion
    }
}