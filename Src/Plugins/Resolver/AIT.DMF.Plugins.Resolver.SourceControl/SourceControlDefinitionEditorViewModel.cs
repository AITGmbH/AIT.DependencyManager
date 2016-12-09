// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SourceControlDefinitionEditorViewModel.cs" company="AIT GmbH & Co. KG">
//   All rights reserved by AIT GmbH & Co. KG
// </copyright>
// <summary>
//   Defines the BinaryRepositoryDefinitionEditorViewModel type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace AIT.DMF.Plugins.Resolver.SourceControl
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
    using Contracts.Provider;

    using Microsoft.TeamFoundation.VersionControl.Client;
    using Microsoft.TeamFoundation.VersionControl.Common;

    /// <summary>
    /// The source control mapping definition editor view model.
    /// </summary>
    internal class SourceControlDefinitionEditorViewModel : IValidatingViewModel
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
        /// The valid dependency definition filename list.
        /// </summary>
        private readonly string _validDependencyDefinitonFilenameList;

        /// <summary>
        /// The team project collection url.
        /// </summary>
        private readonly string _tpcUrl;

        /// <summary>
        /// The access service.
        /// </summary>
        private readonly ITfsAccessService _accessService;

        /// <summary>
        /// The dictionary to save validation errors.
        /// </summary>
        private readonly Dictionary<string, string> _validationErrors;

        /// <summary>
        /// The standard options for version spec (as key) with display string.
        /// </summary>
        private readonly Dictionary<string, string> _versionSpecToDisplayStringDictionary;

        /// <summary>
        /// The reverse dictionary to versionSpecToDisplayStringDictionary.
        /// </summary>
        private readonly Dictionary<string, string> _displayStringToVersionSpecDictionary;

        /// <summary>
        /// The available version specs.
        /// </summary>
        private ObservableCollection<string> _availableVersionSpecs;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="SourceControlDefinitionEditorViewModel" /> class.
        /// </summary>
        /// <param name="accessService">The TFS access service.</param>
        /// <param name="type">The resolver type.</param>
        /// <param name="xmlDependencyViewModel">The XML dependency view model.</param>
        /// <param name="validDependencyDefinitonFilenameList">The list with valid dependency definition filenames.</param>
        /// <param name="tpcUrl">The team project collection url.</param>
        internal SourceControlDefinitionEditorViewModel(ITfsAccessService accessService, IDependencyResolverType type, IXmlDependencyViewModel xmlDependencyViewModel, string validDependencyDefinitonFilenameList, string tpcUrl)
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

            _tpcUrl = tpcUrl;
            _accessService = accessService;
            _accessService.Connect(new Uri(_tpcUrl));
            _xmlDependencyViewModel = xmlDependencyViewModel;
            _xmlDependency = xmlDependencyViewModel.XmlDependency;
            _validDependencyDefinitonFilenameList = validDependencyDefinitonFilenameList;
            _validationErrors = new Dictionary<string, string>();
            _versionSpecToDisplayStringDictionary = new Dictionary<string, string>
                {
                    { "T", "Latest version" }, { "C", "Changeset" }, { "D", "Date" }, { "L", "Label" }, { "W", "Workspace Version" }
                };
            _displayStringToVersionSpecDictionary = new Dictionary<string, string>();
            foreach (var e in _versionSpecToDisplayStringDictionary)
            {
                _displayStringToVersionSpecDictionary.Add(e.Value, e.Key);
            }

            LoadAvailableVersionSpecs();
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
        /// Gets or sets the selected source control path.
        /// </summary>
        public string SelectedSourceControlPath
        {
            get
            {
                return _xmlDependency.ProviderConfiguration.Settings.GetSettingValue(DependencyProviderValidSettingName.ServerRootPath);
            }

            set
            {
                if (SelectedSourceControlPath != value)
                {
                    _xmlDependency.ProviderConfiguration.Settings.SetSettingValue(DependencyProviderValidSettingName.ServerRootPath, value);
                    _xmlDependencyViewModel.SetChanged();

                    ClearSelectedVersionSpecAndVersionSpecString();
                    ValidateSelectedSourceControlPath();
                    OnPropertyChanged("SelectedSourceControlPath");
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
        /// Gets or sets the available version spec options.
        /// </summary>
        public ObservableCollection<string> AvailableVersionSpecs
        {
            get
            {
                if (_availableVersionSpecs == null)
                {
                    LoadAvailableVersionSpecs();
                }

                return _availableVersionSpecs;
            }

            set
            {
                if (_availableVersionSpecs != value)
                {
                    _availableVersionSpecs = value;
                    OnPropertyChanged("AvailableVersionSpecs");
                }
            }
        }

        /// <summary>
        /// Gets or sets the selected version spec option.
        /// </summary>
        public string SelectedVersionSpecs
        {
            get
            {
                var versionSpec = _xmlDependency.ProviderConfiguration.Settings.GetSettingValue(DependencyProviderValidSettingName.VersionSpec);

                if (!string.IsNullOrEmpty(versionSpec))
                {
                    var versionSpecOption = versionSpec.Substring(0, 1);

                    if (_versionSpecToDisplayStringDictionary.ContainsKey(versionSpecOption))
                    {
                        return _versionSpecToDisplayStringDictionary.FirstOrDefault(x => x.Key.Equals(versionSpecOption, StringComparison.OrdinalIgnoreCase)).Value;
                    }
                }

                return string.Empty;
            }

            set
            {
                if (SelectedVersionSpecs != value)
                {
                    string versionSpecForDisplayString;
                    _displayStringToVersionSpecDictionary.TryGetValue(value, out versionSpecForDisplayString);

                    _xmlDependency.ProviderConfiguration.Settings.SetSettingValue(DependencyProviderValidSettingName.VersionSpec, versionSpecForDisplayString);
                    ValidateSelectedVersionSpecs();
                    ValidateSelectedVersionSpecString();
                    _xmlDependencyViewModel.SetChanged();

                    // Saving only the version spec option into the string changes both fields
                    OnPropertyChanged("SelectedVersionSpecString");
                    OnPropertyChanged("SelectedVersionSpecs");
                    OnPropertyChanged("IsVersionSpecStringEnabled");
                }
            }
        }

        /// <summary>
        /// Gets or sets the selected version spec string.
        /// </summary>
        public string SelectedVersionSpecString
        {
            get
            {
                var versionSpec = _xmlDependency.ProviderConfiguration.Settings.GetSettingValue(DependencyProviderValidSettingName.VersionSpec);

                return !string.IsNullOrEmpty(versionSpec) ? versionSpec.Substring(1) : string.Empty;
            }

            set
            {
                if (SelectedVersionSpecString != value)
                {
                    _xmlDependency.ProviderConfiguration.Settings.SetSettingValue(
                        DependencyProviderValidSettingName.VersionSpec,
                        string.Concat(_xmlDependency.ProviderConfiguration.Settings.GetSettingValue(DependencyProviderValidSettingName.VersionSpec).Substring(0, 1), value));
                    _xmlDependencyViewModel.SetChanged();

                    ValidateSelectedVersionSpecString();
                    OnPropertyChanged("SelectedVersionSpecString");
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

                    ValidateFolderMappings();
                    OnPropertyChanged("SelectedFolderMappings");
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether VersionSpecsString field should be editable.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if version specs sting can be set; otherwise, <c>false</c>.
        /// </returns>
        public bool IsVersionSpecStringEnabled
        {
            get
            {
                string latestDisplayString;
                _versionSpecToDisplayStringDictionary.TryGetValue(VersionSpec.Latest.DisplayString, out latestDisplayString);

                return !SelectedVersionSpecs.Equals(latestDisplayString, StringComparison.OrdinalIgnoreCase);
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
        /// Validates all SourceControlMapping dependency settings.
        /// </summary>
        private void ValidateAll()
        {
            ValidateSelectedSourceControlPath();
            OnPropertyChanged("SelectedSourceControlPath");
            ValidateSelectedVersionSpecs();
            OnPropertyChanged("SelectedVersionSpecs");
            ValidateSelectedVersionSpecString();
            OnPropertyChanged("SelectedVersionSpecString");
            ValidateFilter(IncludeFilter, "IncludeFilter");
            OnPropertyChanged("IncludeFilter");
            ValidateFilter(ExcludeFilter, "ExcludeFilter");
            OnPropertyChanged("ExcludeFilter");
            ValidateFolderMappings();
            OnPropertyChanged("SelectedFolderMappings");
        }

        /// <summary>
        /// Validates the source control path by parsing the value and checking if source control folder with this value exists and contains an dependency definition file.
        /// </summary>
        private void ValidateSelectedSourceControlPath()
        {
            var value = SelectedSourceControlPath;
            string validationMessage = null;

            if (string.IsNullOrEmpty(value))
            {
                validationMessage = "Source control path is required!";
            }
            else if (!VersionControlPath.IsValidPath(value))
            {
                validationMessage = "Source control path is invalid!";
            }
            else
            {
                try
                {
                    if (!_accessService.IsServerPathValid(value))
                    {
                        validationMessage = "Source control folder does not exist!";
                    }

                    if (validationMessage == null)
                    {
                        try
                        {
                            if (!_accessService.IsDependencyDefinitionFilePresentInFolder(value, _validDependencyDefinitonFilenameList))
                            {
                                validationMessage = "Source control folder does not contain a valid SourceControl component!";
                            }
                        }
                        catch (Exception)
                        {
                            validationMessage = "Source control folder does not contain a valid SourceControl component!";
                        }
                    }
                }
                catch (Exception)
                {
                    validationMessage = "Source control folder does not exist!";
                }
            }

            if (validationMessage != null)
            {
                AddError("SelectedSourceControlPath", validationMessage);
            }
            else
            {
                RemoveError("SelectedSourceControlPath");
            }
        }

        /// <summary>
        /// Validates the version spec type.
        /// </summary>
        private void ValidateSelectedVersionSpecs()
        {
            var value = SelectedVersionSpecs;
            string validationMessage = null;

            if (string.IsNullOrEmpty(value))
            {
                validationMessage = "Version spec type is required!";
            }

            if (validationMessage != null)
            {
                AddError("SelectedVersionSpecs", validationMessage);
            }
            else
            {
                RemoveError("SelectedVersionSpecs");
            }
        }

        /// <summary>
        /// Validates the version spec string.
        /// </summary>
        private void ValidateSelectedVersionSpecString()
        {
            var value = SelectedVersionSpecString;
            string validationMessage = null;
            string latestVersionSpecOption;
            _versionSpecToDisplayStringDictionary.TryGetValue(VersionSpec.Latest.DisplayString, out latestVersionSpecOption);

            if (string.IsNullOrEmpty(value) && !SelectedVersionSpecs.Equals(latestVersionSpecOption, StringComparison.OrdinalIgnoreCase))
            {
                validationMessage = "Version spec is required!";
            }
            else
            {
                string versionSpecOption;
                // ReSharper disable AssignNullToNotNullAttribute
                _displayStringToVersionSpecDictionary.TryGetValue(SelectedVersionSpecs, out versionSpecOption);
                // ReSharper restore AssignNullToNotNullAttribute
                try
                {
                    // ReSharper disable PossibleNullReferenceException
                    if (versionSpecOption.Equals("W", StringComparison.OrdinalIgnoreCase))
                    // ReSharper restore PossibleNullReferenceException
                    {
                        var user = value.Split(';')[1];
                        VersionSpec.ParseSingleSpec(string.Concat(versionSpecOption, value), user);
                    }
                    else
                    {
                        VersionSpec.ParseSingleSpec(string.Concat(versionSpecOption, value), null);
                    }
                }
                catch (Exception)
                {
                    validationMessage = string.Format("{0} version spec is invalid!", SelectedVersionSpecs);
                }
            }

            if (validationMessage != null)
            {
                AddError("SelectedVersionSpecString", validationMessage);
            }
            else
            {
                RemoveError("SelectedVersionSpecString");
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

        /// <summary>
        /// Clears the selected version spec.
        /// </summary>
        private void ClearSelectedVersionSpecAndVersionSpecString()
        {
            string latestVersionSpecDisplayString;
            _versionSpecToDisplayStringDictionary.TryGetValue(VersionSpec.Latest.DisplayString, out latestVersionSpecDisplayString);

            SelectedVersionSpecs = latestVersionSpecDisplayString;
        }

        /// <summary>
        /// Loads the available version specs options by processing the versionSpecOptions dictionary.
        /// </summary>
        private void LoadAvailableVersionSpecs()
        {
            _availableVersionSpecs = new ObservableCollection<string>(_versionSpecToDisplayStringDictionary.Select(x => x.Value));
        }

        #endregion
    }
}
