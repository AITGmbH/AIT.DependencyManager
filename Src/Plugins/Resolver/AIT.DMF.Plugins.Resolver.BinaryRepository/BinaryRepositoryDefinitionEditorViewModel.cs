// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BinaryRepositoryDefinitionEditorViewModel.cs" company="AIT GmbH & Co. KG">
//   All rights reserved by AIT GmbH & Co. KG
// </copyright>
// <summary>
//   Defines the BinaryRepositoryDefinitionEditorViewModel type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace AIT.DMF.Plugins.Resolver.BinaryRepository
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Common;
    using Common.Trash;
    using Contracts.Filters;
    using Contracts.GUI;
    using Contracts.Parser;
    using Contracts.Provider;
    using Contracts.Services;

    /// <summary>
    /// The view model for the BinaryRepository definition editor.
    /// </summary>
    internal class BinaryRepositoryDefinitionEditorViewModel : IValidatingViewModel
    {
        #region Private Members

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
        private readonly BinaryRepositoryResolverType _resolverType;

        /// <summary>
        /// The valid dependency definition filename list.
        /// </summary>
        private readonly string _validDependencyDefinitonFilenameList;

        /// <summary>
        /// The access service.
        /// </summary>
        private readonly ITfsAccessService _accessService;

        /// <summary>
        /// The dictionary to save validation errors.
        /// </summary>
        private readonly Dictionary<string, string> _validationErrors;

        /// <summary>
        /// The component filter.
        /// </summary>
        private readonly IComponentFilter _filter;

        /// <summary>
        /// The BinaryRepository dependency resolver.
        /// </summary>
        private IDependencyResolver _resolver;

        /// <summary>
        /// The collection which contains all available team projects.
        /// </summary>
        private ObservableCollection<string> _availableTeamProjects;

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
        /// Initializes a new instance of the <see cref="BinaryRepositoryDefinitionEditorViewModel"/> class.
        /// </summary>
        /// <param name="accessService">The helper service used to get team foundation server specific information.</param>
        /// <param name="type">The resolver type for BinaryRepository.</param>
        /// <param name="xmlDependencyViewModel">The Xml dependency view model.</param>
        /// <param name="validDependencyDefinitonFilenameList">The list of valid dependency definition file names.</param>
        /// <param name="filter">The filter to use to filter the available components.</param>
        /// <param name="tpcUrl">The team project collection url.</param>
        internal BinaryRepositoryDefinitionEditorViewModel(ITfsAccessService accessService, BinaryRepositoryResolverType type, IXmlDependencyViewModel xmlDependencyViewModel, string validDependencyDefinitonFilenameList, IComponentFilter filter, string tpcUrl)
        {
            if (null == accessService)
            {
                throw new ArgumentNullException("accessService");
            }

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

            if (string.IsNullOrWhiteSpace(tpcUrl))
            {
                throw new ArgumentNullException("tpcUrl");
            }

            _accessService = accessService;
            _resolverType = type;
            _xmlDependencyViewModel = xmlDependencyViewModel;
            _xmlDependency = xmlDependencyViewModel.XmlDependency;
            _validDependencyDefinitonFilenameList = validDependencyDefinitonFilenameList;
            _filter = filter;

            _validationErrors = new Dictionary<string, string>();

            if (string.IsNullOrEmpty(SelectedBinaryTeamProjectCollection))
            {
                SelectedBinaryTeamProjectCollection = tpcUrl;
            }

            ValidateAll();

            // TODO: Use setting to determine whether outputpath should be set automatically
            PropertyChanged += BinaryRepositoryDefinitionEditorViewModel_PropertyChanged;
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
        /// Gets or sets the selected team project collection.
        /// </summary>
        public string SelectedBinaryTeamProjectCollection
        {
            get
            {
                return _xmlDependency.ProviderConfiguration.Settings.GetSettingValue(DependencyProviderValidSettingName.BinaryTeamProjectCollectionUrl);
            }

            set
            {
                if (SelectedBinaryTeamProjectCollection != value)
                {
                    _xmlDependency.ProviderConfiguration.Settings.SetSettingValue(DependencyProviderValidSettingName.BinaryTeamProjectCollectionUrl, value);
                    _xmlDependencyViewModel.SetChanged();

                    if (ValidateBinaryTeamProjectCollection())
                    {
                        LoadAvailableTeamProjects(true);
                    }

                    OnPropertyChanged("SelectedBinaryTeamProjectCollection");
                }
            }
        }

        /// <summary>
        /// Gets or sets the available binary repositories.
        /// </summary>
        public ObservableCollection<string> AvailableBinaryRepositoryTeamProjects
        {
            get
            {
                if (_availableTeamProjects == null)
                {
                    LoadAvailableTeamProjects(false);
                }

                return _availableTeamProjects;
            }

            set
            {
                if (_availableTeamProjects != value)
                {
                    _availableTeamProjects = value;
                    OnPropertyChanged("AvailableBinaryRepositoryTeamProjects");
                }
            }
        }

        /// <summary>
        /// Gets or sets the selected binary repository.
        /// </summary>
        public string SelectedBinaryRepositoryTeamProject
        {
            get
            {
                return _xmlDependency.ProviderConfiguration.Settings.GetSettingValue(DependencyProviderValidSettingName.BinaryRepositoryTeamProject);
            }

            set
            {
                if (SelectedBinaryRepositoryTeamProject != value)
                {
                    _xmlDependency.ProviderConfiguration.Settings.SetSettingValue(DependencyProviderValidSettingName.BinaryRepositoryTeamProject, value);
                    _xmlDependencyViewModel.SetChanged();

                    if (ValidateBinaryRepositoryTeamProject())
                    {
                        LoadAvailableComponents(true);
                    }

                    OnPropertyChanged("SelectedBinaryRepositoryTeamProject");
                }
            }
        }

        /// <summary>
        /// Gets or sets the available components.
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
        /// Gets or sets the selected components.
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
        /// Gets or sets the available component versions.
        /// </summary>
        public ObservableCollection<string> AvailableVersions
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
                    OnPropertyChanged("AvailableVersions");
                }
            }
        }

        /// <summary>
        /// Gets or sets the selected component version.
        /// </summary>
        public string SelectedVersion
        {
            get
            {
                return _xmlDependency.ProviderConfiguration.Settings.GetSettingValue(DependencyProviderValidSettingName.VersionNumber);
            }

            set
            {
                // Refresh only if value was changed
                if (SelectedVersion != value)
                {
                    _xmlDependency.ProviderConfiguration.Settings.SetSettingValue(DependencyProviderValidSettingName.VersionNumber, value);
                    _xmlDependencyViewModel.SetChanged();

                    if (ValidateVersion())
                    {
                    }

                    OnPropertyChanged("SelectedVersion");
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
                // Refresh only if value was changed
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
                // Refresh only if value was changed
                if (SelectedFolderMappings != value)
                {
                    _xmlDependency.ProviderConfiguration.Settings.SetSettingValue(DependencyProviderValidSettingName.FolderMappings, value);
                    _xmlDependencyViewModel.SetChanged();

                    if (ValidateFolderMappings())
                    {
                    }

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

        /// <summary>
        /// Gets a value indicating whether the output path textbox is enabled.
        /// </summary>
        /// <value>
        /// <c>true</c> if the output path textbox is enabled; otherwise, <c>false</c>.
        /// </value>
        public bool IsOutputPathEnabled
        {
            get
            {
                return !string.IsNullOrEmpty(SelectedBinaryTeamProjectCollection) && DependencyManagerSettings.Instance.IsBinaryRepositoryComponentSettingsEnabled;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the folder mappings textbox is enabled.
        /// </summary>
        /// <value>
        /// <c>true</c> if the folder mappings textbox is enabled; otherwise, <c>false</c>.
        /// </value>
        public bool IsFolderMappingsEnabled
        {
            get
            {
                return DependencyManagerSettings.Instance.IsBinaryRepositoryComponentSettingsEnabled;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the include filter textbox is enabled.
        /// </summary>
        /// <value>
        /// <c>true</c> if the include filter textbox is enabled; otherwise, <c>false</c>.
        /// </value>
        public bool IsIncludeFilterEnabled
        {
            get
            {
                return DependencyManagerSettings.Instance.IsBinaryRepositoryComponentSettingsEnabled;
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

        #region Private Methods

        /// <summary>
        /// Validates the binary team project collection.
        /// </summary>
        /// <returns>True if binary team project collection is valid. Otherwise false.</returns>
        private bool ValidateBinaryTeamProjectCollection()
        {
            var value = SelectedBinaryTeamProjectCollection;
            string validationMessage = null;

            if (string.IsNullOrEmpty(value))
            {
                validationMessage = "Binary repository team project collection url is required!";
            }
            else if (!Uri.IsWellFormedUriString(value, UriKind.Absolute))
            {
                validationMessage = "Binary repository team project collection url is invalid!";
            }
            else
            {
                try
                {
                    _accessService.Connect(new Uri(value));
                }
                catch (Exception)
                {
                    validationMessage = "Couldn't conntect to team project collection!";
                }
            }

            if (validationMessage != null)
            {
                AddError("SelectedBinaryTeamProjectCollection", validationMessage);
            }
            else
            {
                RemoveError("SelectedBinaryTeamProjectCollection");
            }

            return validationMessage == null;
        }

        /// <summary>
        /// Validates the binary repository team project.
        /// </summary>
        /// <returns>True if binary team project is valid. Otherwise false.</returns>
        private bool ValidateBinaryRepositoryTeamProject()
        {
            if (string.IsNullOrEmpty(SelectedBinaryRepositoryTeamProject))
            {
                AddError("SelectedBinaryRepositoryTeamProject", "Binary repository team project is required.");
                return false;
            }

            RemoveError("SelectedBinaryRepositoryTeamProject");
            return true;
        }

        /// <summary>
        /// Validates the component.
        /// </summary>
        /// <returns>True if component name is valid. Otherwise false.</returns>
        private bool ValidateComponent()
        {
            if (string.IsNullOrEmpty(SelectedComponent))
            {
                AddError("SelectedComponent", "Component is required");
                return false;
            }

            RemoveError("SelectedComponent");
            return true;
        }

        /// <summary>
        /// Validates the version.
        /// </summary>
        /// <returns>True if component version is valid. Otherwise false.</returns>
        private bool ValidateVersion()
        {
            if (string.IsNullOrEmpty(SelectedVersion))
            {
                AddError("SelectedVersion", "Component version is required");
                return false;
            }

            RemoveError("SelectedVersion");
            return true;
        }

        /// <summary>
        /// Validates a filter.
        /// </summary>
        private void ValidateFilter(string filterString, string propertyName)
        {
            var filters = filterString.Split(';').Select(x => x.Trim()).ToList();
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
            if (filterString.IndexOfAny(System.IO.Path.GetInvalidPathChars()) != -1)
            {
                AddError(propertyName, "Path contains invalid characters");
            }
        }

        /// <summary>
        /// Validates the include filter.
        /// </summary>
        /// <returns>True if include filter expression is valid. Otherwise false.</returns>
        private bool ValidateFolderMappings()
        {
            if (string.IsNullOrEmpty(SelectedFolderMappings) ||
                Regex.IsMatch(SelectedFolderMappings, "^sourceoffset=([^/:*?\"<>|]+),localoffset=([^/:*?\"<>|]*)(;sourceoffset=([^/:*?\"<>|]+),localoffset=([^/:*?\"<>|]+))*$", RegexOptions.IgnoreCase))
            {
                RemoveError("SelectedFolderMappings");
                return true;
            }

            AddError("SelectedFolderMappings", "Folder mapping definition is invalid");
            return false;
        }

        /// <summary>
        /// The binary repository definition editor view model_ property changed.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event args.</param>
        [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1400:AccessModifierMustBeDeclared", Justification = "Reviewed. Suppression is OK here.")]
        // ReSharper disable InconsistentNaming
        void BinaryRepositoryDefinitionEditorViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        // ReSharper restore InconsistentNaming
        {
            // HACK: Homag specific outputpath calculation
            if (e.PropertyName == "SelectedComponent")
            {
                if (!string.IsNullOrEmpty(SelectedBinaryTeamProjectCollection) && !DependencyManagerSettings.Instance.IsBinaryRepositoryComponentSettingsEnabled)
                {
                    SelectedOutputPath = @"..\root\Packages\" + SelectedComponent;
                }
            }

            if (e.PropertyName == "SelectedBinaryTeamProjectCollection")
            {
                // Raised in order to set the enabled property correctly
                OnPropertyChanged("IsOutputPathEnabled");
            }
        }

        /// <summary>
        /// Validates all drop down lists.
        /// </summary>
        private void ValidateAll()
        {
            ValidateBinaryTeamProjectCollection();
            OnPropertyChanged("SelectedBinaryTeamProjectCollection");
            ValidateBinaryRepositoryTeamProject();
            OnPropertyChanged("SelectedBinaryRepositoryTeamProject");
            ValidateComponent();
            OnPropertyChanged("SelectedComponent");
            ValidateVersion();
            OnPropertyChanged("SelectedVersion");
            ValidateFilter(IncludeFilter, "IncludeFilter");
            OnPropertyChanged("IncludeFilter");
            ValidateFilter(ExcludeFilter, "ExcludeFilter");
            OnPropertyChanged("ExcludeFilter");
            ValidateFolderMappings();
            OnPropertyChanged("SelectedFolderMappings");
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Load available team projects and refresh AvailableBinaryRepositoryTeamProject observable collection.
        /// </summary>
        /// <param name="clearDependingFields">True if depending fields should be cleared. False otherwise</param>
        private void LoadAvailableTeamProjects(bool clearDependingFields)
        {
            if (!string.IsNullOrWhiteSpace(SelectedBinaryTeamProjectCollection))
            {
                _accessService.Connect(new Uri(SelectedBinaryTeamProjectCollection));
                AvailableBinaryRepositoryTeamProjects = new ObservableCollection<string>(_accessService.GetTeamProjects().Select(x => x.Name));
            }

            // Clear depending fields in case of:
            // a) Force clean
            // b) The selected team project collection is invalid (Todo: MRI Use validator instead of string check)
            if (clearDependingFields || string.IsNullOrWhiteSpace(SelectedBinaryTeamProjectCollection))
            {
                ClearOnNewTeamProjectCollection();
            }
        }

        /// <summary>
        /// Load available components and refresh AvailableComponents observable collection.
        /// </summary>
        /// <param name="clearDependingFields">True if depending fields should be cleared. False otherwise</param>
        private void LoadAvailableComponents(bool clearDependingFields)
        {
            if (!string.IsNullOrWhiteSpace(SelectedBinaryRepositoryTeamProject))
            {
                var resolverSettings = new Settings<ResolverValidSettings>();
                resolverSettings.AddSetting(new KeyValuePair<ResolverValidSettings, string>(ResolverValidSettings.BinaryTeamProjectCollectionUrl, SelectedBinaryTeamProjectCollection));
                resolverSettings.AddSetting(new KeyValuePair<ResolverValidSettings, string>(ResolverValidSettings.BinaryRepositoryTeamProject, SelectedBinaryRepositoryTeamProject));
                resolverSettings.AddSetting(new KeyValuePair<ResolverValidSettings, string>(ResolverValidSettings.DependencyDefinitionFileNameList, _validDependencyDefinitonFilenameList));

                _resolver = _resolverType.CreateResolver(resolverSettings);

                var components = _resolver.GetAvailableComponentNames().Select(x => x.GetName());
                if (_filter != null && DependencyManagerSettings.Instance.BinaryRepositoryFilterComponentList)
                {
                    components = _filter.Filter(_resolverType.DependencyType, components, _resolver.ResolverSettings, SelectedComponent);
                }

                AvailableComponents = new ObservableCollection<string>(components);
            }

            // Clear depending fields in case of:
            // a) Force clean
            // b) The selected team project is invalid (Todo: MRI Use validator instead of string check)
            if (clearDependingFields || string.IsNullOrWhiteSpace(SelectedBinaryRepositoryTeamProject))
            {
                ClearOnNewTeamProject();
            }
        }

        /// <summary>
        /// Load available versions and refresh AvailableVersions observable collection.
        /// </summary>
        /// <param name="clearDependingFields">True if depending fields should be cleared. False otherwise</param>
        private void LoadAvailableVersions(bool clearDependingFields)
        {
            // Renew available versions collection only if a new component was selected
            if (!string.IsNullOrWhiteSpace(SelectedComponent))
            {
                AvailableVersions =
                    new ObservableCollection<string>(
                        _resolver.GetAvailableVersions(new ComponentName(SelectedComponent)).Select(
                            x => x.GetVersion()));
            }

            // Clear depending fields in case of:
            // a) Force clean
            // b) The selected component is invalid (Todo: MRI Use validator instead of string check)
            if (clearDependingFields || string.IsNullOrWhiteSpace(SelectedComponent))
            {
                ClearOnNewComponent();
            }
        }

        /// <summary>
        /// Clears all fields which depend on the selected team project collection property.
        /// </summary>
        private void ClearOnNewTeamProjectCollection()
        {
            SelectedBinaryRepositoryTeamProject = null;
            SelectedComponent = null;
            SelectedVersion = null;
            AvailableComponents = null;
            AvailableVersions = null;

            ValidateAll();
        }

        /// <summary>
        /// Clears all fields which depend on the selected team project property.
        /// </summary>
        private void ClearOnNewTeamProject()
        {
            SelectedComponent = null;
            SelectedVersion = null;
            AvailableVersions = null;

            ValidateAll();
        }

        /// <summary>
        /// Clears all fields which depend on the selected component property.
        /// </summary>
        private void ClearOnNewComponent()
        {
            SelectedVersion = null;

            ValidateAll();
        }

        #endregion
    }
}
