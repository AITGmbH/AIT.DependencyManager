// --------------------------------------------------------------------------------------------------------------------
// <copyright file="GeneralSettingsEditorViewModel.cs" company="AIT GmbH & Co. KG">
//   All rights reserved by AIT GmbH & Co. KG
// </copyright>
// <summary>
//   Defines the GeneralSettingsEditorViewModel type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace AIT.DMF.DependencyManager.Controls.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Linq.Expressions;

    using Commands;
    using Common;
    using Contracts.Provider;
    using Microsoft.TeamFoundation.Framework.Client;
    using Microsoft.VisualStudio.TeamFoundation;
    using Services;

    /// <summary>
    /// ViewModel for the general settings editor
    /// </summary>
    public class GeneralSettingsEditorViewModel : ViewModelBase, IDataErrorInfo
    {
        #region Fields

        private bool _fetchDependenciesOnLocalSolutionBuild;
        private string _filenameExtension;
        private string _relativeOutputPath;
        private string _binaryRepositoryTeamProject;
        private bool _isBinaryRepositoryComponentSettingsEnabled;
        private bool _binaryRepositoryFilterComponentList;
        private string _binaryRepositoryTeamProjectCollectionUri;
        private bool _isZippedDependencyAllowed;
        private bool _isMultiSiteAllowed;
        private string _sites;
        private SortableObservableCollection<string> _availableSites;

        private DependencyManagerSettings _settings;
        private TeamFoundationServerExt _teamFoundationServer;
        private Dictionary<string, string> _validationErrors = new Dictionary<string, string>();  

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="GeneralSettingsEditorViewModel"/> class.
        /// </summary>
        /// <param name="teamFoundationServer">
        /// The team foundation server.
        /// </param>
        /// <param name="resolverTypes">
        /// The resolver Types.
        /// </param>
        public GeneralSettingsEditorViewModel(TeamFoundationServerExt teamFoundationServer, IEnumerable<IDependencyResolverType> resolverTypes)
        {
            ResolverTypes = new ObservableCollection<ResolverTypeViewModel>(resolverTypes.Select(x => new ResolverTypeViewModel(x)));

            SaveCommand = new DelegateCommand(Save, CanSave);
            this._teamFoundationServer = teamFoundationServer;
            this._teamFoundationServer.ProjectContextChanged += (s, a) => LoadSettings();
            LoadSettings();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets a value indicating whether to fetch dependencies on local solution build.
        /// </summary>
        /// <value>
        /// <c>true</c> if the add on should fetch dependencies on local solution build; otherwise, <c>false</c>.
        /// </value>
        public bool FetchDependenciesOnLocalSolutionBuild
        {
            get { return _fetchDependenciesOnLocalSolutionBuild; }
            set { SetField(ref _fetchDependenciesOnLocalSolutionBuild, value, () => FetchDependenciesOnLocalSolutionBuild); }
        }

        /// <summary>
        /// Gets or sets the allowed filename extensions of dependency definition files.
        /// </summary>
        /// <value>
        /// The filename extensions.
        /// </value>
        public string FilenameExtension
        {
            get
            {
                return _filenameExtension;
            }

            set
            {
                SetField(ref _filenameExtension, value, () => FilenameExtension);
                ValidateFilenameExtensions();
            }
        }

        /// <summary>
        /// Gets or sets the relative output path of dependencies when downloading.
        /// </summary>
        public string RelativeOutputPath
        {
            get
            { 
                return _relativeOutputPath; 
            }

            set
            {
                SetField(ref _relativeOutputPath, value, () => RelativeOutputPath);
                ValidateRelativeOutputPath();
            }
        }

        /// <summary>
        /// Gets or sets the binary repository team project.
        /// </summary>
        public string BinaryRepositoryTeamProject
        {
            get { return _binaryRepositoryTeamProject; }
            set { SetField(ref _binaryRepositoryTeamProject, value, () => BinaryRepositoryTeamProject); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether binary repository component settings are enabled for user input.
        /// </summary>
        public bool IsBinaryRepositoryComponentSettingsEnabled
        {
            get { return _isBinaryRepositoryComponentSettingsEnabled; }
            set { SetField(ref _isBinaryRepositoryComponentSettingsEnabled, value, () => IsBinaryRepositoryComponentSettingsEnabled); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to filter binary repository component list.
        /// </summary>
        public bool BinaryRepositoryFilterComponentList
        {
            get { return _binaryRepositoryFilterComponentList; }
            set { SetField(ref _binaryRepositoryFilterComponentList, value, () => BinaryRepositoryFilterComponentList); }
        }

        /// <summary>
        /// Gets or sets the binary repository team project collection url.
        /// </summary>
        public string BinaryRepositoryTeamProjectCollectionUri
        {
            get
            {
                return _binaryRepositoryTeamProjectCollectionUri;
            }

            set
            {
                SetField(ref _binaryRepositoryTeamProjectCollectionUri, value, () => BinaryRepositoryTeamProjectCollectionUri);
                ValidateTeamProjectUri();
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether multi site path replacement is allowed
        /// </summary>
        public bool IsMultiSiteAllowed
        {
            get { return _isMultiSiteAllowed; }
            set { SetField(ref _isMultiSiteAllowed, value, () => IsMultiSiteAllowed); }
        }

        public string Sites
        {
            get {return _sites;}
            set { SetField(ref _sites, value, () => Sites); }
        }

        /// <summary>
        /// Gets or sets all available site entries
        /// </summary>
        public SortableObservableCollection<string> AvailableSites
        {
            get { return _availableSites; }
            set 
            {
                SetField(ref _availableSites, value, () => AvailableSites);
            }
        }
        
        /// <summary>
        /// Gets or sets a value indicating whether zipped dependencies can defined by users
        /// </summary>
        public bool IsZippedDependencyAllowed
        {
            get { return _isZippedDependencyAllowed; }
            set
            {
                SetField(ref _isZippedDependencyAllowed, value, () => IsZippedDependencyAllowed);
               //Raise the Property Changed Notification
                RaiseNotifyPropertyChanged(() => IsZippedDependencyAllowedAndConnected);
               
            }
        }
        
        /// <summary>
        /// Private property that checks if zipped dependecies are allowed and the DM is connected to a server
        /// </summary>
        public bool IsZippedDependencyAllowedAndConnected
        {
            get
            {
                if (IsZippedDependencyAllowed && IsConnected)
                {
                    _isZippedDependencyAllowed =  true;
                }
                else
                {
                    _isZippedDependencyAllowed =  false;
                }

                return _isZippedDependencyAllowed;
            }

        }
        /// <summary>
        /// Gets the resolver types that are registered with the dependency service
        /// </summary>
        public ObservableCollection<ResolverTypeViewModel> ResolverTypes { get; private set; }

        /// <summary>
        /// Gets the project context.
        /// </summary>
        public ProjectContextExt ProjectContext
        {
            get { return _teamFoundationServer.ActiveProjectContext; }
        }

        /// <summary>
        /// Gets a value indicating whether we are connected to a team foundation server connected.
        /// </summary>
        public bool IsConnected
        {
            get { return _teamFoundationServer.ActiveProjectContext.DomainUri != null; }
        }
        
        /// <summary>
        /// Gets a value indicating whether load settings failed. This is the case if we don't have permission to access keys or they are simply not present.
        /// </summary>
        public bool LoadSettingsFailed { get; private set; }

        /// <summary>
        /// Gets the save command.
        /// </summary>
        public DelegateCommand SaveCommand { get; private set; }

        /// <summary>
        /// Gets a value indicating whether save settings failed. This is the case if we don't have permission to update keys.
        /// </summary>
        public bool SaveSettingsFailed { get; private set; }

        #endregion

        #region IDataError Implementation

        /// <summary>
        /// Gets an error message indicating what is wrong with this object.
        /// </summary>
        /// <returns>An error message indicating what is wrong with this object. The default is an empty string ("").</returns>
        string IDataErrorInfo.Error
        {
            get { return string.Empty; }
        }

        /// <summary>
        /// Gets the error message for the property with the given name.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <returns>Validation error message if any or an empty string</returns>
        string IDataErrorInfo.this[string propertyName]
        {
            get
            {
                return _validationErrors.ContainsKey(propertyName) ? _validationErrors[propertyName] : string.Empty;
            }
        }

        #endregion

        #region Save and Load

        /// <summary>
        /// Can save if connected to team foundation server and at least one settings has changed.
        /// </summary>
        /// <returns>
        /// whether save is possible
        /// </returns>
        private bool CanSave()
        {
            return
                ProjectContext.DomainUri != null && ((
                this.BinaryRepositoryFilterComponentList != _settings.BinaryRepositoryFilterComponentList ||
                this.BinaryRepositoryTeamProject != _settings.BinaryRepositoryTeamProject ||
                this.BinaryRepositoryTeamProjectCollectionUri != _settings.BinaryRepositoryTeamProjectCollectionUrl ||
                this.IsBinaryRepositoryComponentSettingsEnabled != _settings.IsBinaryRepositoryComponentSettingsEnabled ||
                this.FetchDependenciesOnLocalSolutionBuild != _settings.FetchDependenciesOnLocalSolutionBuild ||
                this.FilenameExtension != string.Join(",", _settings.ValidDependencyDefinitionFileExtension) ||
                this.IsZippedDependencyAllowed != _settings.IsZippedDependencyAllowed ||
                this.ResolverTypes.Any(x => x.IsEnabled == _settings.DisabledResolvers.Contains(x.ReferenceName)) ||
                this.RelativeOutputPath != _settings.RelativeOutputPath) || 
                this.IsMultiSiteAllowed != _settings.IsMultiSiteAllowed ||
                this.Sites != string.Join(";", _settings.MultiSiteList) || LoadSettingsFailed);
        }

        /// <summary>
        /// Save settings to team foundation registry
        /// </summary>
        private void Save()
        {
            _settings.FetchDependenciesOnLocalSolutionBuild = FetchDependenciesOnLocalSolutionBuild;
            _settings.ValidDependencyDefinitionFileExtension = FilenameExtension.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            _settings.RelativeOutputPath = RelativeOutputPath;
            _settings.BinaryRepositoryTeamProject = BinaryRepositoryTeamProject;
            _settings.IsBinaryRepositoryComponentSettingsEnabled = IsBinaryRepositoryComponentSettingsEnabled;
            _settings.BinaryRepositoryTeamProjectCollectionUrl = BinaryRepositoryTeamProjectCollectionUri;
            _settings.BinaryRepositoryFilterComponentList = BinaryRepositoryFilterComponentList;
            _settings.IsZippedDependencyAllowed = IsZippedDependencyAllowed;
            _settings.IsMultiSiteAllowed = IsMultiSiteAllowed;
            _settings.MultiSiteList = Sites.Split(new[] {';'}, StringSplitOptions.RemoveEmptyEntries);

            // create a comma separated list of disabled resolvers
            _settings.DisabledResolvers = string.Join(",", ResolverTypes.Where(x => x.IsEnabled == false).Select(x => x.ReferenceName));

            try
            {
                _settings.Save();
                SaveSettingsFailed = false;
                RaiseNotifyPropertyChanged(() => SaveSettingsFailed);
            }
            catch (AccessCheckException)
            {
                Logger.Instance().Log(TraceLevel.Error, "You don't have permission to save to the registry.");
                SaveSettingsFailed = true;
                RaiseNotifyPropertyChanged(() => SaveSettingsFailed);
            }
            catch (Exception exception)
            {
                UserMessageService.ShowError("Failed to save settings to team foundation server registry.");
                Logger.Instance().Log(TraceLevel.Error, "Failed to save settings to team foundation server registry. Error was {0}", exception.ToString());
            }
            finally
            {
                LoadSettings();
            }
        }
        
        /// <summary>
        /// Load settings from server if project context has changed. This refreshes the user
        /// interface in case the tool window is open when connecting / disconnecting
        /// </summary>
        public void LoadSettings()
        {
            _settings = DependencyManagerSettings.Instance;
            
            // The load is triggered anyways but the load is registered to the same event and not
            // guaranteed to be executed before the ui refresh, so we load it manually before refreshing.
            LoadSettingsFailed = !_settings.Load(_teamFoundationServer.ActiveProjectContext.DomainUri) && IsConnected;

            FetchDependenciesOnLocalSolutionBuild = _settings.FetchDependenciesOnLocalSolutionBuild;
            FilenameExtension = string.Join(",", _settings.ValidDependencyDefinitionFileExtension);
            RelativeOutputPath = _settings.RelativeOutputPath;
            BinaryRepositoryTeamProject = _settings.BinaryRepositoryTeamProject;
            IsBinaryRepositoryComponentSettingsEnabled = _settings.IsBinaryRepositoryComponentSettingsEnabled;
            BinaryRepositoryFilterComponentList = _settings.BinaryRepositoryFilterComponentList;
            BinaryRepositoryTeamProjectCollectionUri = _settings.BinaryRepositoryTeamProjectCollectionUrl;
            IsZippedDependencyAllowed = _settings.IsZippedDependencyAllowed;
            IsMultiSiteAllowed = _settings.IsMultiSiteAllowed;
            Sites = string.Join(";", _settings.MultiSiteList);

            // check or uncheck resolver depending on whether their internal name is in the list of disabled resolvers
            foreach (var resolverTypeViewModel in ResolverTypes)
            {
                resolverTypeViewModel.IsEnabled = _settings.DisabledResolvers.Contains(resolverTypeViewModel.ReferenceName) == false;
            }

            RaiseNotifyPropertyChanged(() => IsConnected);
            RaiseNotifyPropertyChanged(() => IsZippedDependencyAllowedAndConnected);
            RaiseNotifyPropertyChanged(() => IsMultiSiteAllowed);
            RaiseNotifyPropertyChanged(() => ProjectContext);
            RaiseNotifyPropertyChanged(() => LoadSettingsFailed);
        }



        #endregion

        #region Validation

        /// <summary>
        /// Validates the filename extensions. Makes sure each one starts with a '.' and does not contain invalid characters
        /// </summary>
        private void ValidateFilenameExtensions()
        {
            var extensions = FilenameExtension.Split(new[] { DependencyManagerSettings.FileExtensionSeparator }, StringSplitOptions.RemoveEmptyEntries);
            var invalidChars = System.IO.Path.GetInvalidFileNameChars();

            if (extensions.Any(x => x.IndexOfAny(invalidChars) != -1 || !x.StartsWith(".")))
            {
                SetError(() => FilenameExtension, "Extensions must begin with '.' and must not contain illegal characters");
            }
            else
            {
                RemoveError(() => FilenameExtension);
            }
        }

        /// <summary>
        /// Validates the team project URI.
        /// </summary>
        private void ValidateTeamProjectUri()
        {
            if (Uri.IsWellFormedUriString(BinaryRepositoryTeamProjectCollectionUri, UriKind.Absolute) || BinaryRepositoryTeamProjectCollectionUri.Equals(string.Empty))
            {
                RemoveError(() => BinaryRepositoryTeamProjectCollectionUri);
            }
            else
            {
                SetError(() => BinaryRepositoryTeamProjectCollectionUri, "The uri is not well-formed");
            }
        }

        /// <summary>
        /// Validates the relative output path.
        /// </summary>
        private void ValidateRelativeOutputPath()
        {
            var invalidChars = System.IO.Path.GetInvalidPathChars();

            if (RelativeOutputPath.IndexOfAny(invalidChars) != -1)
            {
                SetError(() => RelativeOutputPath, "Path must not illegal characters");
            }
            else
            {
                RemoveError(() => RelativeOutputPath);
            }
        }

        /// <summary>
        /// Sets the validation error message for a property
        /// </summary>
        /// <typeparam name="T">Type of the property</typeparam>
        /// <param name="selector">Expression that selects the property that failed to validate.</param>
        /// <param name="errorMessage">The error message.</param>
        private void SetError<T>(Expression<Func<T>> selector, string errorMessage)
        {
            _validationErrors[((MemberExpression)selector.Body).Member.Name] = errorMessage;
        }

        /// <summary>
        /// Removes the validation error for a given property
        /// </summary>
        /// <typeparam name="T">Type of the property</typeparam>
        /// <param name="selector">An expression that selects the property.</param>
        private void RemoveError<T>(Expression<Func<T>> selector)
        {
            var propertyName = ((MemberExpression)selector.Body).Member.Name;

            if (_validationErrors.ContainsKey(propertyName))
            {
                _validationErrors.Remove(propertyName);
            }
        }
        #endregion

        #region Nested class: ResolverTypeViewModel

        /// <summary>
        /// Simple ViewModel for registered resolver types that allows disabling
        /// </summary>
        public class ResolverTypeViewModel : ViewModelBase
        {
            private bool _isChecked;
            private readonly IDependencyResolverType _resolverType;

            /// <summary>
            /// Initializes a new instance of the <see cref="ResolverTypeViewModel"/> class.
            /// </summary>
            /// <param name="resolverType">The registered resolver type</param>
            internal ResolverTypeViewModel(IDependencyResolverType resolverType)
            {
                _resolverType = resolverType;
            }

            /// <summary>
            /// Gets or sets a value indicating whether this instance is checked.
            /// </summary>
            public bool IsEnabled
            {
                get { return _isChecked; }
                set { SetField(ref _isChecked, value, () => IsEnabled); }
            }

            /// <summary>
            /// Gets the display name of this resolver type.
            /// </summary>
            public string DisplayName
            {
                get { return _resolverType.DisplayName; }
            }

            /// <summary>
            /// Gets the reference name of the resolver type.
            /// </summary>
            internal string ReferenceName
            {
                get { return _resolverType.ReferenceName; }
            }
        }

        #endregion
    }
}
