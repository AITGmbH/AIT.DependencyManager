// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FileShareDefinitionEditorViewModel.cs" company="AIT GmbH & Co. KG">
//   All rights reserved by AIT GmbH & Co. KG
// </copyright>
// <summary>
//   Defines the FileShareDefinitionEditorViewModel type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace AIT.DMF.Plugins.Resolver.FileShare
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Common;
    using Common.Trash;
    using Contracts.GUI;
    using Contracts.Parser;
    using Contracts.Provider;
    using Contracts.Services;

    /// <summary>
    /// The view model for the FileShare definition editor.
    /// </summary>
    internal class FileShareDefinitionEditorViewModel : IValidatingViewModel
    {
        #region Private members

        /// <summary>
        /// The xml dependency view model.
        /// </summary>
        private readonly IXmlDependencyViewModel _xmlDependencyViewModel;

        /// <summary>
        /// The xml dependency.
        /// </summary>
        private readonly IXmlDependency _xmlDependency;

        /// <summary>
        /// The resolver type.
        /// </summary>
        private readonly FileShareResolverType _resolverType;

        /// <summary>
        /// The valid dependency definition filename list.
        /// </summary>
        private readonly string _validDependencyDefinitonFilenameList;

        /// <summary>
        /// The dictionary to save validation errors.
        /// </summary>
        private readonly Dictionary<string, string> _validationErrors;

        /// <summary>
        /// The BinaryRepository dependency resolver.
        /// </summary>
        private IDependencyResolver _resolver;

        /// <summary>
        /// The collection which contains all available components.
        /// </summary>
        private ObservableCollection<string> _availableComponents;

        /// <summary>
        /// The collection which contains all available versions.
        /// </summary>
        private ObservableCollection<string> _availableVersions;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="FileShareDefinitionEditorViewModel"/> class.
        /// </summary>
        /// <param name="type">The resolver type for BinaryRepository.</param>
        /// <param name="xmlDependencyViewModel">The Xml dependency view model.</param>
        /// <param name="validDependencyDefinitonFilenameList">The list of valid dependency definition file names.</param>
        internal FileShareDefinitionEditorViewModel(FileShareResolverType type, IXmlDependencyViewModel xmlDependencyViewModel, string validDependencyDefinitonFilenameList)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            if (xmlDependencyViewModel == null)
            {
                throw new ArgumentNullException("xmlDependencyViewModel");
            }

            if (xmlDependencyViewModel.XmlDependency == null)
            {
                throw new ArgumentException(
                    "The argument xmlDependencyViewModel does not contain an IXmlDependency object.",
                    "xmlDependencyViewModel");
            }

            if (string.IsNullOrWhiteSpace(validDependencyDefinitonFilenameList))
            {
                throw new ArgumentNullException("validDependencyDefinitonFilenameList");
            }

            _resolverType = type;
            _xmlDependencyViewModel = xmlDependencyViewModel;
            _xmlDependency = xmlDependencyViewModel.XmlDependency;
            _validDependencyDefinitonFilenameList = validDependencyDefinitonFilenameList;

            _validationErrors = new Dictionary<string, string>();
            ValidateAll();
        }

        #endregion

        #region INotifyPropertyChanged Event

        /// <summary>
        /// Occurs when [property changed].
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the selected file share root path.
        /// </summary>
        public string SelectedFileShareRootPath
        {
            get
            {
                return _xmlDependency.ProviderConfiguration.Settings.GetSettingValue(DependencyProviderValidSettingName.FileShareRootPath);
            }

            set
            {
                if (SelectedFileShareRootPath != value)
                {
                    _xmlDependency.ProviderConfiguration.Settings.SetSettingValue(DependencyProviderValidSettingName.FileShareRootPath, value);
                    _xmlDependencyViewModel.SetChanged();

                    if (ValidateFileShareRootPath())
                    {
                        LoadAvailableComponents(true);
                    }

                    OnPropertyChanged("SelectedFileShareRootPath");
                }
            }
        }

        /// <summary>
        /// Gets or sets the available file share components.
        /// </summary>
        public ObservableCollection<string> AvailableComponents
        {
            get
            {
                if (_availableComponents == null)
                {
                    LoadAvailableComponents(false);
                }

                return _availableComponents;
            }

            set
            {
                if (_availableComponents != value)
                {
                    _availableComponents = value;
                    OnPropertyChanged("AvailableComponents");
                }
            }
        }

        /// <summary>
        /// Gets or sets the selected file share component.
        /// </summary>
        public string SelectedComponent
        {
            get
            {
                return _xmlDependency.ProviderConfiguration.Settings.GetSettingValue(DependencyProviderValidSettingName.ComponentName);
            }

            set
            {
                if (SelectedComponent != value)
                {
                    _xmlDependency.ProviderConfiguration.Settings.SetSettingValue(DependencyProviderValidSettingName.ComponentName, value);
                    _xmlDependencyViewModel.SetChanged();

                    if (ValidateComponent())
                    {
                        LoadAvailableVersions(true);
                    }

                    OnPropertyChanged("SelectedComponent");
                }
            }
        }

        /// <summary>
        /// Gets or sets the available file share component versions.
        /// </summary>
        public ObservableCollection<string> AvailableComponentVersions
        {
            get
            {
                if (_availableVersions == null)
                {
                    LoadAvailableVersions(false);
                }

                return _availableVersions;
            }

            set
            {
                if (_availableVersions != value)
                {
                    _availableVersions = value;
                    OnPropertyChanged("AvailableComponentVersions");
                }
            }
        }

        /// <summary>
        /// Gets or sets the selected file share component version.
        /// </summary>
        public string SelectedComponentVersion
        {
            get
            {
                return _xmlDependency.ProviderConfiguration.Settings.GetSettingValue(DependencyProviderValidSettingName.VersionNumber);
            }

            set
            {
                if (SelectedComponentVersion != value)
                {
                    _xmlDependency.ProviderConfiguration.Settings.SetSettingValue(DependencyProviderValidSettingName.VersionNumber, value);
                    _xmlDependencyViewModel.SetChanged();

                    ValidateComponentVersion();
                    OnPropertyChanged("SelectedComponentVersion");
                }
            }
        }

        /// <summary>
        /// Gets or sets the selected output path.
        /// </summary>
        public string SelectedOutputPath
        {
            get
            {
                return _xmlDependency.ProviderConfiguration.Settings.GetSettingValue(DependencyProviderValidSettingName.RelativeOutputPath);
            }

            set
            {
                if (SelectedOutputPath != value)
                {
                    _xmlDependency.ProviderConfiguration.Settings.SetSettingValue(DependencyProviderValidSettingName.RelativeOutputPath, value);
                    _xmlDependencyViewModel.SetChanged();

                    OnPropertyChanged("SelectedOutputPath");
                }
            }
        }


        /// <summary>
        /// Gets or sets the selected include filter unsing semicolon as separator.
        /// </summary>
        public string IncludeFilter
        {
            get
            {
                var value = _xmlDependency.ProviderConfiguration.Settings.GetSettingValue(DependencyProviderValidSettingName.IncludeFilter);
                return value ?? string.Empty;
            }

            set
            {
                if (IncludeFilter != value)
                {
                    _xmlDependency.ProviderConfiguration.Settings.SetSettingValue(DependencyProviderValidSettingName.IncludeFilter, value);
                    _xmlDependencyViewModel.SetChanged();

                    ValidateFilter(value, "IncludeFilter");
                    OnPropertyChanged("IncludeFilter");
                }
            }
        }

        /// <summary>
        /// Gets or sets the selected exclude filter unsing semicolon as separator.
        /// </summary>
        public string ExcludeFilter
        {
            get
            {
                var value = _xmlDependency.ProviderConfiguration.Settings.GetSettingValue(DependencyProviderValidSettingName.ExcludeFilter);
                return value ?? string.Empty;
            }

            set
            {
                if (ExcludeFilter != value)
                {
                    _xmlDependency.ProviderConfiguration.Settings.SetSettingValue(DependencyProviderValidSettingName.ExcludeFilter, value);
                    _xmlDependencyViewModel.SetChanged();

                    ValidateFilter(value, "ExcludeFilter");
                    OnPropertyChanged("ExcludeFilter");
                }
            }
        }

        /// <summary>
        /// Gets or sets the selected include filter.
        /// </summary>
        public string SelectedFolderMappings
        {
            get
            {
                return _xmlDependency.ProviderConfiguration.Settings.GetSettingValue(DependencyProviderValidSettingName.FolderMappings);
            }

            set
            {
                if (SelectedFolderMappings != value)
                {
                    _xmlDependency.ProviderConfiguration.Settings.SetSettingValue(DependencyProviderValidSettingName.FolderMappings, value);
                    _xmlDependencyViewModel.SetChanged();

                    ValidateFolderMappings();
                    OnPropertyChanged("SelectedFolderMappings");
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is a zipped component.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is zipped component; otherwise, <c>false</c>.
        /// </value>
        public bool IsZippedComponent
        {
            get
            {
                return
                    _xmlDependency.ProviderConfiguration.Settings.GetSettingValue(
                        DependencyProviderValidSettingName.CompressedDependency) == "True";
            }

            set
            {
                _xmlDependency.ProviderConfiguration.Settings.SetSettingValue(
                    DependencyProviderValidSettingName.CompressedDependency, value ? "True" : "False");
                _xmlDependencyViewModel.SetChanged();

                OnPropertyChanged("IsZippedComponent");
            }
        }

        /// <summary>
        /// Gets a value indicating whether the compressed dependency checkbox is enabled.
        /// </summary>
        /// <value>
        /// <c>true</c> if the compressed dependency checkbox is enabled; otherwise, <c>false</c>.
        /// </value>
        public bool IsCompressedDependencyEnabled
        {
            get
            {
                return DependencyManagerSettings.Instance.IsZippedDependencyAllowed;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating wheter the zipped archive files should be deleted
        /// </summary>
        /// <value>
        /// <c>true</c> if the archives should be deleted, otherwise <c>false</c>.
        /// </value>
        public bool IsDeletionOfZipFiles
        {
            get
            {
                return
                    _xmlDependency.ProviderConfiguration.Settings.GetSettingValue(
                        DependencyProviderValidSettingName.DeleteArchiveFiles) == "True";
            }

            set
            {
                _xmlDependency.ProviderConfiguration.Settings.SetSettingValue(
                    DependencyProviderValidSettingName.DeleteArchiveFiles, value ? "True" : "False");
                _xmlDependencyViewModel.SetChanged();

                OnPropertyChanged("IsDeletionOfZipFiles");
            }
        }

        #endregion

        #region IValidatingViewModel

        /// <summary>
        /// Gets a value indicating whether the data in this view model is valid.
        /// </summary>
        public bool IsValid
        {
            get
            {
                return _validationErrors == null || _validationErrors.Count == 0;
            }
        }

        #endregion

        #region IDataErrorInfo

        /// <summary>
        /// Gets an error message indicating what is wrong with this object.
        /// </summary>
        /// <returns>An error message indicating what is wrong with this object. The default is an empty string ("").</returns>
        string IDataErrorInfo.Error
        {
            get
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Gets the error message for the property with the given name.
        /// </summary>
        /// <param name="propertyName">The name of the property.</param>
        /// <returns>The error message.</returns>
        string IDataErrorInfo.this[string propertyName]
        {
            get
            {
                string errorString;

                if (_validationErrors.TryGetValue(propertyName, out errorString))
                {
                    return errorString;
                }

                return string.Empty;
            }
        }

        /// <summary>
        /// Adds the error message for the property to the error dictionary.
        /// </summary>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="errorMessage">The error message.</param>
        private void AddError(string propertyName, string errorMessage)
        {
            _validationErrors[propertyName] = errorMessage;
            _xmlDependencyViewModel.SetValid(IsValid);
        }

        /// <summary>
        /// Removes the error message for the property from the error dictionary.
        /// </summary>
        /// <param name="propertyName">The name of the property.</param>
        private void RemoveError(string propertyName)
        {
            _validationErrors.Remove(propertyName);
            _xmlDependencyViewModel.SetValid(IsValid);
        }

        #endregion

        #region INotifyPropertyChanged

        /// <summary>
        /// Called when [property changed].
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        private void OnPropertyChanged(string propertyName)
        {
            if (null != PropertyChanged)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion

        #region Validation methods

        /// <summary>
        /// Validates all drop down lists.
        /// </summary>
        private void ValidateAll()
        {
            ValidateFileShareRootPath();
            OnPropertyChanged("SelectedFileShareRootPath");
            ValidateComponent();
            OnPropertyChanged("SelectedComponent");
            ValidateComponentVersion();
            OnPropertyChanged("SelectedComponentVersion");
            ValidateFolderMappings();
            OnPropertyChanged("SelectedFolderMappings");
            ValidateFilter(IncludeFilter, "IncludeFilter");
            OnPropertyChanged("IncludeFilter");
            ValidateFilter(ExcludeFilter, "ExcludeFilter");
            OnPropertyChanged("ExcludeFilter");
        }

        /// <summary>
        /// Validates the file share root path.
        /// </summary>
        /// <returns>True if component version is valid. Otherwise false.</returns>
        private bool ValidateFileShareRootPath()
        {
            if (string.IsNullOrEmpty(SelectedFileShareRootPath))
            {
                AddError("SelectedFileShareRootPath", "File share root path is required.");
                return false;
            }

            RemoveError("SelectedFileShareRootPath");
            return true;
        }

        /// <summary>
        /// Validates the file share component.
        /// </summary>
        /// <returns>True if component version is valid. Otherwise false.</returns>
        private bool ValidateComponent()
        {
            if (string.IsNullOrEmpty(SelectedComponent))
            {
                AddError("SelectedComponent", "Component name is required.");
                return false;
            }

            RemoveError("SelectedComponent");
            return true;
        }

        /// <summary>
        /// Validates the file share component version.
        /// </summary>
        private void ValidateComponentVersion()
        {
            if (string.IsNullOrEmpty(SelectedComponentVersion))
            {
                AddError("SelectedComponentVersion", "Component version is required.");
            }
            else
            {
                RemoveError("SelectedComponentVersion");
            }
        }

        /// <summary>
        /// Validates a filter.
        /// </summary>
        private void ValidateFilter(string filter, string propertyName)
        {
            var filters = filter.Split(';').Select(x => x.Trim()).ToList();
            var wildCardInPath = filters.Any(x => x.LastIndexOfAny(new[] { '*', '?' }) < x.LastIndexOf('\\') - 1 && x.Contains('\\') && x.Contains("*"));
            var uncPath = filters.Any(x => x.StartsWith("\\"));

            RemoveError(propertyName);

            if (wildCardInPath)
            {
                AddError(propertyName, "A path cannot contain wildcards");
            }
            if (uncPath)
            {
                AddError(propertyName, "A path cannot be a UNC location");
            }
            if (filter.IndexOfAny(System.IO.Path.GetInvalidPathChars()) != -1)
            {
                AddError(propertyName, "Path contains invalid characters");
            }
        }

        /// <summary>
        /// Validates the include filter.
        /// </summary>
        private void ValidateFolderMappings()
        {
            if (string.IsNullOrEmpty(SelectedFolderMappings) ||
                Regex.IsMatch(SelectedFolderMappings, "^sourceoffset=([^/:*?\"<>|]+),localoffset=([^/:*?\"<>|]*)(;sourceoffset=([^/:*?\"<>|]+),localoffset=([^/:*?\"<>|]+))*$", RegexOptions.IgnoreCase))
            {
                RemoveError("SelectedFolderMappings");
            }
            else
            {
                AddError("SelectedFolderMappings", "Folder mapping definition is invalid");
            }
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Load available components and refresh AvailableComponents observable collection.
        /// </summary>
        /// <param name="clearDependingFields">True if depending fields should be cleared. False otherwise</param>
        private void LoadAvailableComponents(bool clearDependingFields)
        {
            if (!string.IsNullOrWhiteSpace(SelectedFileShareRootPath))
            {
                var resolverSettings = new Settings<ResolverValidSettings>();
                resolverSettings.AddSetting(new KeyValuePair<ResolverValidSettings, string>(ResolverValidSettings.FileShareUrl, SelectedFileShareRootPath));
                resolverSettings.AddSetting(new KeyValuePair<ResolverValidSettings, string>(ResolverValidSettings.DependencyDefinitionFileNameList, _validDependencyDefinitonFilenameList));

                try
                {
                    _resolver = _resolverType.CreateResolver(resolverSettings);
                    var components = _resolver.GetAvailableComponentNames().Select(x => x.GetName());
                    AvailableComponents = new ObservableCollection<string>(components);
                }
                catch (Exception)
                {
                    AvailableComponents = new ObservableCollection<string>();
                }
            }

            // Clear depending fields in case of:
            // a) Force clean
            // b) The selected team project is invalid (Todo: MRI Use validator instead of string check)
            if (clearDependingFields || string.IsNullOrWhiteSpace(SelectedFileShareRootPath))
            {
                ClearOnNewFileShareUrl();
            }
        }

        /// <summary>
        /// Load available versions and refresh AvailableVersions observable collection.
        /// </summary>
        /// <param name="clearDependingFields">True if depending fields should be cleared. False otherwise</param>
        private void LoadAvailableVersions(bool clearDependingFields)
        {
            if (!string.IsNullOrWhiteSpace(SelectedFileShareRootPath) && !string.IsNullOrWhiteSpace(SelectedComponent))
            {
                var resolverSettings = new Settings<ResolverValidSettings>();
                resolverSettings.AddSetting(new KeyValuePair<ResolverValidSettings, string>(ResolverValidSettings.FileShareUrl, SelectedFileShareRootPath));
                resolverSettings.AddSetting(new KeyValuePair<ResolverValidSettings, string>(ResolverValidSettings.DependencyDefinitionFileNameList, _validDependencyDefinitonFilenameList));

                try
                {
                    _resolver = _resolverType.CreateResolver(resolverSettings);
                    var versions = _resolver.GetAvailableVersions(new ComponentName(SelectedComponent)).Select(x => x.GetVersion());
                    AvailableComponentVersions = new ObservableCollection<string>(versions);
                }
                catch (Exception)
                {
                    AvailableComponentVersions = new ObservableCollection<string>();
                }
            }

            // Clear depending fields in case of:
            // a) Force clean
            // b) The selected file share root path is invalid
            // b) The selected component name is invalid (Todo: MRI Use validator instead of string check)
            if (string.IsNullOrWhiteSpace(SelectedFileShareRootPath))
            {
                ClearOnNewFileShareUrl();
            }

            if (clearDependingFields || string.IsNullOrWhiteSpace(SelectedComponent))
            {
                ClearOnNewComponent();
            }
        }

        /// <summary>
        /// Clears all fields which depend on the selected file share url.
        /// </summary>
        private void ClearOnNewFileShareUrl()
        {
            SelectedComponent = null;
            SelectedComponentVersion = null;
            AvailableComponentVersions = null;

            ValidateAll();
        }

        /// <summary>
        /// Clears all fields which depend on the selected file share url.
        /// </summary>
        private void ClearOnNewComponent()
        {
            SelectedComponentVersion = null;

            ValidateAll();
        }

        #endregion
    }
}
