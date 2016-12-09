using System;
using System.Collections.Generic;
using System.ComponentModel;
using AIT.DMF.Contracts.GUI;
using AIT.DMF.Contracts.Parser;

namespace AIT.DMF.DependencyManager.Controls.ViewModels
{
    /// <summary>
    /// The view model which encapsulates a single IXMLDependency object.
    /// </summary>
    public class XmlDependencyViewModel : ChangeTrackingViewModelBase, IXmlDependencyViewModel, IEditableObject
    {
        #region Private Members

        private readonly IXmlDependency _xmlDependency;
        private bool _isValid;
        private bool _isNew;

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor checks argument and saves xml dependency.
        /// </summary>
        /// <param name="xmlDependency"></param>
        public XmlDependencyViewModel(IXmlDependency xmlDependency, bool isNew)
        {
            if (xmlDependency == null)
            {
                throw new ArgumentNullException("xmlDependency");
            }

            _xmlDependency = xmlDependency;
            _isNew = isNew;
            _isValid = true;

            // create an initial reference point
            ((IEditableObject)this).BeginEdit();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets a value indicating whether the XML dependency is newly created (not saved once).
        /// </summary>
        public bool IsNew
        {
            get
            {
                return _isNew;
            }
            private set
            {
                if (_isNew != value)
                {
                    _isNew = value;
                    RaiseNotifyPropertyChanged("IsNew");
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the XML dependency is valid.
        /// </summary>
        public bool IsValid
        {
            get
            {
                return _isValid;
            }
            set
            {
                if (_isValid != value)
                {
                    _isValid = value;
                    RaiseNotifyPropertyChanged("IsValid");
                }
            }
        }

        /// <summary>
        /// Gets the type embedded in the xml dependency.
        /// </summary>
        public string Type
        {
            get
            {
                if (_xmlDependency == null || _xmlDependency.ProviderConfiguration == null)
                {
                    return null;
                }

                return _xmlDependency.ProviderConfiguration.Type;
            }
        }

        /// <summary>
        /// Gets the settings list embedded in the xml dependency.
        /// </summary>
        public IDependencyProviderSettings Settings
        {
            get
            {
                if (_xmlDependency == null || _xmlDependency.ProviderConfiguration == null)
                {
                    return null;
                }

                return _xmlDependency.ProviderConfiguration.Settings;
            }
        }

        /// <summary>
        /// Gets the referenced component name embedded in the xml dependency.
        /// </summary>
        public string ReferencedComponentName
        {
            get
            {
                if (_xmlDependency == null || _xmlDependency.ProviderConfiguration == null || _xmlDependency.ProviderConfiguration.Settings == null)
                {
                    return null;
                }

                return _xmlDependency.ProviderConfiguration.Settings.GetComponentName();
            }
        }

        /// <summary>
        /// Gets the referenced component version embedded in the xml dependency.
        /// </summary>
        public string ReferencedComponentVersion
        {
            get
            {
                if (_xmlDependency == null || _xmlDependency.ProviderConfiguration == null || _xmlDependency.ProviderConfiguration.Settings == null)
                {
                    return null;
                }

                return _xmlDependency.ProviderConfiguration.Settings.GetComponentVersion();
            }
        }

        #endregion

        #region Overrides

        protected override void OnSetChanged()
        {
            // if someone has manually set the changed flag for us,
            // we need to notify the UI manually about all potentially
            // changed properties that are not covered by INotifyPropertyChanged
            RaiseAllNotifyProperyChangedEvents();
        }

        protected override void OnAcceptingChanges()
        {
            // now we're not new anymore
            IsNew = false;
        }

        protected override void OnAcceptChanges()
        {
            // resets our backed up values to the current ones
            ((IEditableObject)this).EndEdit();
        }

        #endregion

        private void RaiseAllNotifyProperyChangedEvents()
        {
            RaiseNotifyPropertyChanged("Type");
            RaiseNotifyPropertyChanged("Settings");
            RaiseNotifyPropertyChanged("ReferencedComponentName");
            RaiseNotifyPropertyChanged("ReferencedComponentVersion");
        }

        #region Implementation of IEditableObject

        /// <summary>
        /// Begins an edit on an object.
        /// </summary>
        void IEditableObject.BeginEdit()
        {
            // this is equivalent to starting a new "transaction"
            CloneValues();
        }

        /// <summary>
        /// Pushes changes since the last <see cref="M:System.ComponentModel.IEditableObject.BeginEdit"/> or <see cref="M:System.ComponentModel.IBindingList.AddNew"/> call into the underlying object.
        /// </summary>
        void IEditableObject.EndEdit()
        {
            // this is the "commit"
            // => simply create a new reference point by cloning the current values again
            CloneValues();
        }

        /// <summary>
        /// Discards changes since the last <see cref="M:System.ComponentModel.IEditableObject.BeginEdit"/> call.
        /// </summary>
        void IEditableObject.CancelEdit()
        {
            // this is the "rollback"
            // => restore all settings from the backup
            foreach (var setting in _backup)
            {
                if (Settings != null)
                {
                    Settings.SetSettingValue(setting.Key, setting.Value);
                }
            }

            // Now remove settings that are set and not set
            if (Settings != null)
            {
                foreach (var setting in Settings.SettingsList)
                {
                    if (!_backup.ContainsKey(setting.Name))
                    {
                        setting.Value = null;
                    }
                }
            }

            // notify the UI
            RaiseAllNotifyProperyChangedEvents();

            // call accept changes to remove us from change tracking
            AcceptChanges();
        }

        private readonly Dictionary<DependencyProviderValidSettingName, string> _backup = new Dictionary<DependencyProviderValidSettingName, string>();

        private void CloneValues()
        {
            _backup.Clear();

            if (Settings != null)
            {
                foreach (var setting in Settings.SettingsList)
                {
                    _backup.Add(setting.Name, setting.Value);
                }
            }
        }

        #endregion

        #region Implementation of IXmlDependencyViewModel

        /// <summary>
        /// Gets the xml dependency.
        /// </summary>
        public IXmlDependency XmlDependency
        {
            get
            {
                return _xmlDependency;
            }
        }

        /// <summary>
        /// Sets the valid flag for the dependency to signal whether the dependency is valid.
        /// </summary>
        /// <param name="isValid"></param>
        public void SetValid(bool isValid)
        {
            IsValid = isValid;
        }

        #endregion
    }
}
