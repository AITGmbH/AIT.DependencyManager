using System.ComponentModel;
using System.ComponentModel.Composition;
using AIT.DMF.DependencyManager.Controls.Services;

namespace AIT.DMF.DependencyManager.Controls.ViewModels
{
    public abstract class ChangeTrackingViewModelBase : ComposableViewModelBase, IChangeTracking
    {
        private bool _isChanged;

        [Import]
        public IChangeTrackingService ChangeTrackingService
        {
            get;
            set;
        }

        protected override void RaiseNotifyPropertyChanged(string propertyName)
        {
            // make sure we keep track of the changes
            if (propertyName != "IsChanged" && propertyName != "IsValid")
            {
                // the order of these two operations is important 
                // to keep the service in a consistent state
                IsChanged = true;

                if (ChangeTrackingService != null)
                {
                    ChangeTrackingService.Add(this);
                }
            }

            base.RaiseNotifyPropertyChanged(propertyName);
        }

        #region IChangeTracking implementation

        /// <summary>
        /// Resets the object's state to unchanged by accepting the modifications.
        /// </summary>
        public void AcceptChanges()
        {
            OnAcceptingChanges();

            // order of the following two operations is important
            // to keep the change tracking service in a consistent state
            if (ChangeTrackingService != null)
            {
                ChangeTrackingService.Remove(this);
            }

            // reset our own state
            IsChanged = false;

            OnAcceptChanges();
        }

        /// <summary>
        /// Called when the <see cref="AcceptChanges"/> method was invoked, but before the changes have been accepted, as an extension hook for derived classes.
        /// </summary>
        protected virtual void OnAcceptingChanges()
        {
        }

        /// <summary>
        /// Called when the <see cref="AcceptChanges"/> method was invoked, as an extension hook for derived classes.
        /// </summary>
        protected virtual void OnAcceptChanges()
        {
        }

        /// <summary>
        /// Gets the object's changed status.
        /// </summary>
        /// <returns>true if the object's content has changed since the last call to <see cref="M:System.ComponentModel.IChangeTracking.AcceptChanges"/>; otherwise, false.</returns>
        public bool IsChanged
        {
            get
            {
                return _isChanged;
            }
            private set
            {
                if (_isChanged != value)
                {
                    _isChanged = value;
                    RaiseNotifyPropertyChanged("IsChanged");
                }
            }
        }

        #endregion

        /// <summary>
        /// Sets the <see cref="IsChanged"/> flag to true manually.
        /// </summary>
        public void SetChanged()
        {
            // the order of these two operations is important 
            // to keep the service in a consistent state
            IsChanged = true;

            if (ChangeTrackingService != null)
            {
                ChangeTrackingService.Add(this);
            }

            OnSetChanged();
        }

        /// <summary>
        /// Called when the <see cref="SetChanged"/> method was invoked, as an extension hook for derived classes.
        /// </summary>
        protected virtual void OnSetChanged()
        {
        }
    }
}
