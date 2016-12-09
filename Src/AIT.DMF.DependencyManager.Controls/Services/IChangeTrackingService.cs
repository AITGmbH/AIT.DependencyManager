using System;
using System.ComponentModel;

namespace AIT.DMF.DependencyManager.Controls.Services
{
    public interface IChangeTrackingService : INotifyPropertyChanged
    {
        bool HasChanges
        {
            get;
        }

        event EventHandler<EventArgs> HasChangesChanged;

        void Add(IChangeTracking changedObject);
        void Remove(IChangeTracking unchangedObject);
        void Save();
        void Save(string fileName);
    }
}
