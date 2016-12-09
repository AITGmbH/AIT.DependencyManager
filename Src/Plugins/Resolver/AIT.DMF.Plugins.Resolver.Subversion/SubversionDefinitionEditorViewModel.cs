// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SubversionDefinitionEditorViewModel.cs" company="AIT GmbH & Co. KG">
//   All rights reserved by AIT GmbH & Co. KG
// </copyright>
// <summary>
//   Defines the SubversionDefinitionEditorViewModel type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace AIT.DMF.Plugins.Resolver.Subversion
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Linq;
    using Contracts.GUI;
    using Contracts.Parser;
    using Contracts.Provider;

    /// <summary>
    /// The subversion mapping definition editor view model.
    /// </summary>
    internal class SubversionDefinitionEditorViewModel : IValidatingViewModel
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
        /// The subversion repository url.
        /// </summary>
        private readonly string _svnUrl;

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
        /// <param name="type">The resolver type.</param>
        /// <param name="xmlDependencyViewModel">The XML dependency view model.</param>
        /// <param name="validDependencyDefinitonFilenameList">The list with valid dependency definition filenames.</param>
        /// <param name="svnUrl">The team project collection url.</param>
        internal SubversionDefinitionEditorViewModel(IDependencyResolverType type, IXmlDependencyViewModel xmlDependencyViewModel, string validDependencyDefinitonFilenameList, string svnUrl)
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

            if (string.IsNullOrWhiteSpace(svnUrl))
            {
                throw new ArgumentNullException("svnUrl");
            }

            _svnUrl = svnUrl;
            _xmlDependencyViewModel = xmlDependencyViewModel;
            _xmlDependency = xmlDependencyViewModel.XmlDependency;
            _validDependencyDefinitonFilenameList = validDependencyDefinitonFilenameList;
            _validationErrors = new Dictionary<string, string>();
            _versionSpecToDisplayStringDictionary = new Dictionary<string, string>
                {
                    { "H", "Head revision" }, { "R", "Revision" }
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
        /// Gets or sets the selected subversion path.
        /// </summary>
        public string SelectedSubversionPath
        {
            get
            {
                return _xmlDependency.ProviderConfiguration.Settings.GetSettingValue(DependencyProviderValidSettingName.SubversionRootPath);
            }

            set
            {
                if (SelectedSubversionPath != value)
                {
                    _xmlDependency.ProviderConfiguration.Settings.SetSettingValue(DependencyProviderValidSettingName.SubversionRootPath, value);
                    _xmlDependencyViewModel.SetChanged();

                    ClearSelectedVersionSpecAndVersionSpecString();
                    ValidateSelectedSubversionPath();
                    OnPropertyChanged("SelectedSubversionPath");
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
        /// Gets a value indicating whether VersionSpecsString field should be editable.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if version specs sting can be set; otherwise, <c>false</c>.
        /// </returns>
        public bool IsVersionSpecStringEnabled
        {
            get
            {
                string latestDisplayString = _versionSpecToDisplayStringDictionary["H"];

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
        /// Validates all Subversion dependency settings.
        /// </summary>
        private void ValidateAll()
        {
            ValidateSelectedSubversionPath();
            OnPropertyChanged("SelectedSubversionPath");
            ValidateSelectedVersionSpecs();
            OnPropertyChanged("SelectedVersionSpecs");
            ValidateSelectedVersionSpecString();
            OnPropertyChanged("SelectedVersionSpecString");
            ValidateFilter(IncludeFilter, "IncludeFilter");
            OnPropertyChanged("IncludeFilter");
            ValidateFilter(ExcludeFilter, "ExcludeFilter");
            OnPropertyChanged("ExcludeFilter");
            ValidateUserAccount();
            OnPropertyChanged("SelectedUserAccount");
        }

        /// <summary>
        /// Validates the subversion path by parsing the value and checking if subversion folder with this value exists and contains an dependency definition file.
        /// </summary>
        private void ValidateSelectedSubversionPath()
        {
            var value = SelectedSubversionPath;

             if (string.IsNullOrEmpty(SelectedSubversionPath))
            {
                AddError("SelectedSubversionRootPath", "Subversion path is required.");
            }

            RemoveError("SelectedSubversionRootPath");
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
            string latestVersionSpecOption = _versionSpecToDisplayStringDictionary["H"];

            if (string.IsNullOrEmpty(value) && !SelectedVersionSpecs.Equals(latestVersionSpecOption, StringComparison.OrdinalIgnoreCase))
            {
                validationMessage = "Revision is required!";
            }
            else
            {
                string versionSpecOption;
                // ReSharper disable AssignNullToNotNullAttribute
                _displayStringToVersionSpecDictionary.TryGetValue(SelectedVersionSpecs, out versionSpecOption);
                // ReSharper restore AssignNullToNotNullAttribute

                int tmp;

                if ((versionSpecOption.StartsWith("R")) && (!int.TryParse(value.Trim(), out tmp)))
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
        private void ValidateUserAccount()
        {
            //if (string.IsNullOrEmpty(this.SelectedUserAccount) ||
            //    Regex.IsMatch(this.SelectedUserAccount, "^sourceoffset=([^/:*?\"<>|]+),localoffset=([^/:*?\"<>|]*)(;sourceoffset=([^/:*?\"<>|]+),localoffset=([^/:*?\"<>|]+))*$", RegexOptions.IgnoreCase))
            //{
            //    this.RemoveError("SelectedUserAccount");
            //}
            //else
            //{
            //    this.AddError("SelectedUserAccount", "User Account definition is invalid");
            //}
        }

        /// <summary>
        /// Clears the selected version spec.
        /// </summary>
        private void ClearSelectedVersionSpecAndVersionSpecString()
        {
            string latestVersionSpecDisplayString = _versionSpecToDisplayStringDictionary["H"];

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
