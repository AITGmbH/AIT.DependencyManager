// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BuildResultDefinitionEditorViewModel.cs" company="AIT GmbH & Co. KG">
//   All rights reserved by AIT GmbH & Co. KG
// </copyright>
// <summary>
//   Defines the BinaryRepositoryDefinitionEditorViewModel type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace AIT.DMF.Plugins.Resolver.BuildResult
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Common;
    using Contracts.GUI;
    using Contracts.Parser;
    using Microsoft.TeamFoundation.Build.Client;

    /// <summary>
    /// Model responsible for
    ///  - loading and storing properties to Xml object
    ///  - providing properties to editor
    ///  - validation of properties
    ///  - provide allowed values
    /// </summary>
    internal class BuildResultDefinitionEditorViewModel : IValidatingViewModel
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
        /// The build status read from build service.
        /// </summary>
        private readonly IEnumerable<BuildStatus> _importedBuildStatus;

        /// <summary>
        /// The build service.
        /// </summary>
        private readonly TfsBuildHelper _buildService;

        /// <summary>
        /// The dictionary to save validation errors.
        /// </summary>
        private readonly Dictionary<string, string> _validationErrors;

        /// <summary>
        /// The available team projects.
        /// </summary>
        private ObservableCollection<string> _availableTeamProjects;

        /// <summary>
        /// The available build definitions.
        /// </summary>
        private ObservableCollection<string> _availableBuildDefinitions;

        /// <summary>
        /// The available build numbers.
        /// </summary>
        private ObservableCollection<string> _availableBuildNumbers;

        /// <summary>
        /// The available build qualities.
        /// </summary>
        private ObservableCollection<ComboBoxItem<string>> _availableBuildQualities;

        /// <summary>
        /// The available build numbers.
        /// </summary>
        private ObservableCollection<ComboBoxItem<string>> _availableBuildStatus;

        /// <summary>
        /// The boolean if filter by status and quality or build number.
        /// </summary>
        private bool _filterByStatusAndQuality;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="BuildResultDefinitionEditorViewModel"/> class.
        /// </summary>
        /// <param name="buildService">The helper service used to get team foundation server specific information.</param>
        /// <param name="type">The resolver type for BuildResult.</param>
        /// <param name="xmlDependencyViewModel">The Xml dependency view model.</param>
        /// <param name="validDependencyDefinitonFilenameList">The list of valid dependency definition file names.</param>
        internal BuildResultDefinitionEditorViewModel(TfsBuildHelper buildService, BuildResultResolverType type, IXmlDependencyViewModel xmlDependencyViewModel, string validDependencyDefinitonFilenameList)
        {
            if (null == buildService)
            {
                throw new ArgumentNullException("buildService");
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

            _buildService = buildService;
            _xmlDependencyViewModel = xmlDependencyViewModel;
            _xmlDependency = xmlDependencyViewModel.XmlDependency;

            _validationErrors = new Dictionary<string, string>();
            _importedBuildStatus = _buildService.GetBuildStatus();

            if (!string.IsNullOrEmpty(SelectedBuildNumber))
            {
                _filterByStatusAndQuality = false;
            }
            else
            {
                _filterByStatusAndQuality = true;
            }

            OnPropertyChanged("IsCheckedFilterByBuildNumber");
            OnPropertyChanged("IsCheckedFilterByStatusAndQuality");

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
        /// Gets or sets the available team projects.
        /// </summary>
        public ObservableCollection<string> AvailableTeamProjects
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
                    OnPropertyChanged("AvailableTeamProjects");
                }
            }
        }

        /// <summary>
        /// Gets or sets the selected team project.
        /// </summary>
        public string SelectedTeamProject
        {
            get
            {
                return _xmlDependency.ProviderConfiguration.Settings.GetSettingValue(DependencyProviderValidSettingName.TeamProjectName);
            }

            set
            {
                if (SelectedTeamProject != value)
                {
                    _xmlDependency.ProviderConfiguration.Settings.SetSettingValue(DependencyProviderValidSettingName.TeamProjectName, value);
                    _xmlDependencyViewModel.SetChanged();

                    if (ValidateTeamProject())
                    {
                        LoadAvailableBuildDefinitions(true);
                    }

                    OnPropertyChanged("SelectedTeamProject");
                }
            }
        }

        /// <summary>
        /// Gets or sets the available build definitions.
        /// </summary>
        public ObservableCollection<string> AvailableBuildDefinitions
        {
            get
            {
                if (_availableBuildDefinitions == null)
                {
                    LoadAvailableBuildDefinitions(false);
                }

                return _availableBuildDefinitions;
            }

            set
            {
                if (_availableBuildDefinitions != value)
                {
                    _availableBuildDefinitions = value;
                    OnPropertyChanged("AvailableBuildDefinitions");
                }
            }
        }

        /// <summary>
        /// Gets or sets the selected build definition.
        /// </summary>
        public string SelectedBuildDefinition
        {
            get
            {
                return _xmlDependency.ProviderConfiguration.Settings.GetSettingValue(DependencyProviderValidSettingName.BuildDefinition);
            }

            set
            {
                if (SelectedBuildDefinition != value)
                {
                    _xmlDependency.ProviderConfiguration.Settings.SetSettingValue(DependencyProviderValidSettingName.BuildDefinition, value);
                    _xmlDependencyViewModel.SetChanged();

                    if (ValidateBuildDefinition())
                    {
                        LoadAvailableBuildNumbers();
                        LoadAvailableBuildQualities();
                        LoadAvailableBuildStatus();
                        ClearBuildNumber();
                        ClearBuildStatusAndQuality();
                    }

                    OnPropertyChanged("SelectedBuildDefinition");
                }
            }
        }

        /// <summary>
        /// Gets or sets the available build numbers.
        /// </summary>
        public ObservableCollection<string> AvailableBuildNumbers
        {
            get
            {
                if (_availableBuildNumbers == null)
                {
                    LoadAvailableBuildNumbers();
                }

                return _availableBuildNumbers;
            }

            set
            {
                if (_availableBuildNumbers != value)
                {
                    _availableBuildNumbers = value;
                    OnPropertyChanged("AvailableBuildNumbers");
                }
            }
        }

        /// <summary>
        /// Gets or sets the selected build number.
        /// </summary>
        public string SelectedBuildNumber
        {
            get
            {
                return _xmlDependency.ProviderConfiguration.Settings.GetSettingValue(DependencyProviderValidSettingName.BuildNumber);
            }

            set
            {
                if (SelectedBuildNumber != value)
                {
                    _xmlDependency.ProviderConfiguration.Settings.SetSettingValue(DependencyProviderValidSettingName.BuildNumber, value);
                    _xmlDependencyViewModel.SetChanged();

                    ValidateBuildNumber();
                    OnPropertyChanged("SelectedBuildNumber");
                }
            }
        }

        /// <summary>
        /// Gets or sets the available build status.
        /// </summary>
        public ObservableCollection<ComboBoxItem<string>> AvailableBuildStatus
        {
            get
            {
                if (_availableBuildStatus == null)
                {
                    LoadAvailableBuildStatus();
                }

                return _availableBuildStatus;
            }

            set
            {
                if (_availableBuildStatus != value)
                {
                    _availableBuildStatus = value;
                    OnPropertyChanged("AvailableBuildStatus");
                }
            }
        }

        /// <summary>
        /// Gets the build status string to display.
        /// </summary>
        /// <value>
        /// The multiple build status.
        /// </value>
        public string MultipleBuildStatus
        {
            get
            {
                var multipleBuildStatusString = string.Empty;

                if (AvailableBuildStatus != null)
                {
                    var multipleBuildStatus = AvailableBuildStatus.Where(x => x.IsChecked).Select(x => x.Content);

                    // ReSharper disable PossibleMultipleEnumeration
                    if (multipleBuildStatus.Any())
                    // ReSharper restore PossibleMultipleEnumeration
                    {
                        // ReSharper disable PossibleMultipleEnumeration
                        multipleBuildStatusString = string.Join(",", multipleBuildStatus);
                        // ReSharper restore PossibleMultipleEnumeration
                    }
                }

                return multipleBuildStatusString;
            }
            set
            {
                if (MultipleBuildStatus != value)
                {
                    OnPropertyChanged("MultipleBuildStatus");
                }
            }
        }

        /// <summary>
        /// Gets or sets the available build qualities.
        /// </summary>
        public ObservableCollection<ComboBoxItem<string>> AvailableBuildQualities
        {
            get
            {
                if (_availableBuildQualities == null)
                {
                    LoadAvailableBuildQualities();
                }

                return _availableBuildQualities;
            }

            set
            {
                if (_availableBuildQualities != value)
                {
                    _availableBuildQualities = value;
                    OnPropertyChanged("AvailableBuildQualities");
                }
            }
        }

        /// <summary>
        /// Gets the build quality string to display.
        /// </summary>
        /// <value>
        /// The multiple build qualities.
        /// </value>
        public string MultipleBuildQualities
        {
            get
            {
                var multipleBuildQualitiesString = string.Empty;

                if (_availableBuildQualities != null)
                {
                    var multipleBuildQualities = _availableBuildQualities.Where(x => x.IsChecked).Select(x => x.Content);
                    var buildQualities = multipleBuildQualities as List<string> ?? multipleBuildQualities.ToList();

                    if (buildQualities.Any())
                    {
                        multipleBuildQualitiesString = string.Join(",", buildQualities);
                    }
                }

                return multipleBuildQualitiesString;
            }
            set
            {
                if (MultipleBuildQualities != value)
                {
                    OnPropertyChanged("MultipleBuildQualities");
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
        /// Gets or sets a value indicating whether the state for filter by status and quality and the build number radio box is enabled/disabled/unknown.
        /// </summary>
        public bool IsCheckedFilterByStatusAndQuality
        {
            get
            {
                return _filterByStatusAndQuality;
            }

            set
            {
                if (IsCheckedFilterByStatusAndQuality != value)
                {
                    _filterByStatusAndQuality = value;
                    LoadAvailableBuildQualities();
                    LoadAvailableBuildStatus();
                    ClearBuildNumber();
                    OnPropertyChanged("MultipleBuildStatus");
                    OnPropertyChanged("MultipleBuildQualities");
                    OnPropertyChanged("SelectedBuildNumber");
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the state for filter by build number radio box is enabled/disabled.
        /// </summary>
        public bool IsCheckedFilterByBuildNumber
        {
            get
            {
                return !_filterByStatusAndQuality;
            }

            set
            {
                if (IsCheckedFilterByBuildNumber != value)
                {
                    _filterByStatusAndQuality = !value;
                    LoadAvailableBuildNumbers();
                    ClearBuildStatusAndQuality();
                    OnPropertyChanged("MultipleBuildStatus");
                    OnPropertyChanged("MultipleBuildQualities");
                    OnPropertyChanged("SelectedBuildNumber");
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
            ValidateTeamProject();
            OnPropertyChanged("SelectedTeamProject");
            ValidateBuildDefinition();
            OnPropertyChanged("SelectedBuildDefinition");
            ValidateBuildNumber();
            OnPropertyChanged("SelectedBuildNumber");
            ValidateFilter(IncludeFilter, "IncludeFilter");
            OnPropertyChanged("IncludeFilter");
            ValidateFilter(ExcludeFilter, "ExcludeFilter");
            OnPropertyChanged("ExcludeFilter");
            ValidateFolderMappings();
            OnPropertyChanged("SelectedFolderMappings");
            ValidateBuildStatus();
            OnPropertyChanged("MultipleBuildStatus");
            OnPropertyChanged("MultipleBuildQualities");
        }

        /// <summary>
        /// Validates the team project.
        /// </summary>
        /// <returns>True if team project is valid. Otherwise false.</returns>
        private bool ValidateTeamProject()
        {
            if (string.IsNullOrEmpty(SelectedTeamProject))
            {
                AddError("SelectedTeamProject", "Team project is required.");
                return false;
            }

            RemoveError("SelectedTeamProject");
            return true;
        }

        /// <summary>
        /// Validates the build definition.
        /// </summary>
        /// <returns>True if build definition is valid. Otherwise false.</returns>
        private bool ValidateBuildDefinition()
        {
            if (string.IsNullOrEmpty(SelectedBuildDefinition))
            {
                AddError("SelectedBuildDefinition", "Build definition name is required.");
                return false;
            }

            RemoveError("SelectedBuildDefinition");
            return true;
        }

        /// <summary>
        /// Validates the build number.
        /// </summary>
        private void ValidateBuildNumber()
        {
            if (string.IsNullOrEmpty(SelectedBuildNumber) && IsCheckedFilterByBuildNumber)
            {
                AddError("SelectedBuildNumber", "Build number is required.");
            }
            else
            {
                RemoveError("SelectedBuildNumber");
            }
        }

        /// <summary>
        /// Validates the build number.
        /// </summary>
        private void ValidateBuildStatus()
        {
            if (string.IsNullOrEmpty(MultipleBuildStatus) && IsCheckedFilterByStatusAndQuality)
            {
                AddError("MultipleBuildStatus", "Build status is required.");
            }
            else
            {
                RemoveError("MultipleBuildStatus");
            }
        }

        /// <summary>
        /// Validates a filter.
        /// </summary>
        private void ValidateFilter(string filter, string propertyName)
        {
            var filters = filter.Split(';').Select(x => x.Trim()).ToList();
            var wildCardInPath = filters.Any(x => x.LastIndexOfAny(new[] {'*', '?'}) < x.LastIndexOf('\\') - 1 && x.Contains('\\') && x.Contains("*"));
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
            if(filter.IndexOfAny(System.IO.Path.GetInvalidPathChars()) != -1)
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
        /// Loads the available team projects and refresh AvailableTeamProjects observable collection.
        /// </summary>
        /// <param name="clearDependingFields">True if depending fields should be cleared. False otherwise</param>
        private void LoadAvailableTeamProjects(bool clearDependingFields)
        {
            AvailableTeamProjects = new ObservableCollection<string>(_buildService.GetTeamProjects().Select(x => x.Name));

            // Clear depending fields in case of:
            // a) Force clean
            if (clearDependingFields)
            {
                ClearOnNewTeamProject();
            }
        }

        /// <summary>
        /// Load available build definitions and refresh AvailableBuildDefinitions observable collection.
        /// </summary>
        /// <param name="clearDependingFields">True if depending fields should be cleared. False otherwise</param>
        private void LoadAvailableBuildDefinitions(bool clearDependingFields)
        {
            if (!string.IsNullOrWhiteSpace(SelectedTeamProject))
            {
                AvailableBuildDefinitions = new ObservableCollection<string>(_buildService.GetBuildDefinitionsFromTeamProject(SelectedTeamProject).Select(x => x.Name));
            }

            // Clear depending fields in case of:
            // a) Force clean
            if (clearDependingFields)
            {
                ClearOnNewBuildDefinition();
            }
        }

        /// <summary>
        /// Load available build numbers and refresh AvailableBuildNumbers observable collection.
        /// </summary>
        private void LoadAvailableBuildNumbers()
        {
            if (!string.IsNullOrWhiteSpace(SelectedTeamProject) && !string.IsNullOrWhiteSpace(SelectedBuildDefinition))
            {
                AvailableBuildNumbers = new ObservableCollection<string>(_buildService.GetAvailableBuildNumbers(SelectedTeamProject, SelectedBuildDefinition));
            }

            if (string.IsNullOrEmpty(SelectedTeamProject))
            {
                ClearOnNewTeamProject();
            }

            if (string.IsNullOrEmpty(SelectedBuildDefinition) || string.IsNullOrEmpty(SelectedTeamProject))
            {
                ClearOnNewBuildDefinition();
            }
        }

        /// <summary>
        /// Load available build qualities and refresh AvailableBuildQualities observable collection.
        /// </summary>
        private void LoadAvailableBuildQualities()
        {
            AvailableBuildQualities = new ObservableCollection<ComboBoxItem<string>>();

            if (!string.IsNullOrWhiteSpace(SelectedTeamProject))
            {
                foreach (var buildQuality in _buildService.GetBuildQuality(SelectedTeamProject))
                {
                    var element = new ComboBoxItem<string> { IsChecked = false, Content = buildQuality };
                    element.PropertyChanged += BuildQualityOnPropertyChanged;
                    _availableBuildQualities.Add(element);
                }

                var selectedBuildQualities = _xmlDependency.ProviderConfiguration.Settings.GetSettingValue(DependencyProviderValidSettingName.BuildQuality);
                if (!string.IsNullOrEmpty(selectedBuildQualities))
                {
                    var selectedBuildQualitiesList = selectedBuildQualities.Split(',');

                    foreach (var qualityToCheck in AvailableBuildQualities)
                    {
                        if (selectedBuildQualitiesList.Any(x => x.Equals(qualityToCheck.Content, StringComparison.OrdinalIgnoreCase)))
                        {
                            qualityToCheck.IsChecked = true;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Handle build quality state checkboxes changed and set BuildQuality setting.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="propertyChangedEventArgs">The <see cref="PropertyChangedEventArgs" /> instance containing the event data.</param>
        private void BuildQualityOnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            if (propertyChangedEventArgs.PropertyName == "IsChecked")
            {
                if (!MultipleBuildQualities.Equals(_xmlDependency.ProviderConfiguration.Settings.GetSettingValue(DependencyProviderValidSettingName.BuildQuality), StringComparison.OrdinalIgnoreCase))
                {
                    _xmlDependency.ProviderConfiguration.Settings.SetSettingValue(DependencyProviderValidSettingName.BuildQuality, MultipleBuildQualities);
                    _xmlDependencyViewModel.SetChanged();
                    OnPropertyChanged("MultipleBuildQualities");
                }
            }
        }

        /// <summary>
        /// Load available build status and refresh AvailableBuildStatus observable collection.
        /// </summary>
        private void LoadAvailableBuildStatus()
        {
            AvailableBuildStatus = new ObservableCollection<ComboBoxItem<string>>();

            foreach (var buildStatus in _importedBuildStatus)
            {
                var buildStatusString = buildStatus.ToString();

                if (buildStatusString.Equals("None", StringComparison.OrdinalIgnoreCase) ||
                    buildStatusString.Equals("All", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var element = new ComboBoxItem<string> { IsChecked = false, Content = buildStatusString };
                element.PropertyChanged += BuildStatusOnPropertyChanged;
                _availableBuildStatus.Add(element);
            }

            var selectedBuildStatus = _xmlDependency.ProviderConfiguration.Settings.GetSettingValue(DependencyProviderValidSettingName.BuildStatus);
            if (!string.IsNullOrEmpty(selectedBuildStatus))
            {
                var selectedBuildStatusList = selectedBuildStatus.Split(',');

                foreach (var statusToCheck in AvailableBuildStatus)
                {
                    if (selectedBuildStatusList.Any(x => x.Equals(statusToCheck.Content, StringComparison.OrdinalIgnoreCase)))
                    {
                        statusToCheck.IsChecked = true;
                    }
                }
            }
        }

        /// <summary>
        /// Handle build status state checkboxes changed and set BuildStatus setting.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="propertyChangedEventArgs">The <see cref="PropertyChangedEventArgs" /> instance containing the event data.</param>
        private void BuildStatusOnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            if (propertyChangedEventArgs.PropertyName == "IsChecked")
            {
                if (!MultipleBuildStatus.Equals(_xmlDependency.ProviderConfiguration.Settings.GetSettingValue(DependencyProviderValidSettingName.BuildStatus), StringComparison.OrdinalIgnoreCase))
                {
                    _xmlDependency.ProviderConfiguration.Settings.SetSettingValue(
                    DependencyProviderValidSettingName.BuildStatus, MultipleBuildStatus);
                    _xmlDependencyViewModel.SetChanged();
                    ValidateBuildStatus();
                    OnPropertyChanged("MultipleBuildStatus");
                }
            }
        }

        /// <summary>
        /// Clears all fields which depend on the selected team project property.
        /// </summary>
        private void ClearOnNewTeamProject()
        {
            SelectedBuildDefinition = null;
            AvailableBuildQualities = null;
            AvailableBuildStatus = null;
            AvailableBuildNumbers = null;
            SelectedBuildNumber = null;

            ValidateAll();
        }

        /// <summary>
        /// Clears all fields which depend on the selected build definition property.
        /// </summary>
        private void ClearOnNewBuildDefinition()
        {
            ClearBuildNumber();
            ClearBuildStatusAndQuality();
        }

        /// <summary>
        /// Clears all fields which depend on the selected build definition property.
        /// </summary>
        private void ClearBuildNumber()
        {
            SelectedBuildNumber = null;

            ValidateAll();
        }

        /// <summary>
        /// Clears available build status and quality.
        /// </summary>
        private void ClearBuildStatusAndQuality()
        {
            // Clear selected build qualities and build status
            if (_availableBuildQualities != null)
            {
                foreach (var quality in _availableBuildQualities)
                {
                    quality.IsChecked = false;
                }
            }

            if (_availableBuildStatus != null)
            {
                foreach (var status in _availableBuildStatus)
                {
                    status.IsChecked = false;
                }
            }

            ValidateAll();
        }

        #endregion
    }
}
