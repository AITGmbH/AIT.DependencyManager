// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PersonalSettingsEditorViewModel.cs" company="AIT GmbH & Co. KG">
//   All rights reserved by AIT GmbH & Co. KG
// </copyright>
// <summary>
//   Defines the PersonalSettingsEditorViewModel type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using AIT.DMF.DependencyService;

namespace AIT.DMF.DependencyManager.Controls.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Linq.Expressions;

    using Commands;
    using Common;
    using Microsoft.VisualStudio.TeamFoundation;
    using Services;

    /// <summary>
    /// ViewModel for the personal settings editor
    /// </summary>
    public class PersonalSettingsEditorViewModel : ViewModelBase, IDataErrorInfo
    {
        #region Fields

        private string _binaryRepositoryTeamProject;
        private string _binaryRepositoryTeamProjectCollectionUri;
        private string _pathToSevenZipExe;
        private string _sites;
        private SortableObservableCollection<string> _availableSites;
        private string _selectedSite;

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
        public PersonalSettingsEditorViewModel(TeamFoundationServerExt teamFoundationServer)
        {
            SaveCommand = new DelegateCommand(Save, CanSave);
            this._teamFoundationServer = teamFoundationServer;
            this._teamFoundationServer.ProjectContextChanged += (s, a) => LoadSettings();
            LoadSettings();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the binary repository team project.
        /// </summary>
        public string BinaryRepositoryTeamProject
        {
            get { return _binaryRepositoryTeamProject; }
            set { SetField(ref _binaryRepositoryTeamProject, value, () => BinaryRepositoryTeamProject); }
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
        
        public string Sites
        {
            get {return _sites;}
            set 
            { 
                // Reload corresponding combobox, if necessary
                if (SetField(ref _sites, value, () => Sites))
                {
                    LoadMultiSiteComboBox();
                }
            }
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
        /// Gets or sets the selected site
        /// </summary>
        public string SelectedSite
        {
            get { return _selectedSite; }

            set
            {
                if (this.SelectedSite!= value)
                {
                    _selectedSite = value;

                    RaiseNotifyPropertyChanged(() => SelectedSite);
                }
            }
        }
        
        /// <summary>
        /// Gets the resolver types that are registered with the dependency service
        /// </summary>
        //public ObservableCollection<ResolverTypeViewModel> ResolverTypes { get; private set; }

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
        /// Gets or sets the path to the seven zip dll
        /// </summary>
        public string PathToSevenZipExe
        {
            get
            {
                return _pathToSevenZipExe;
            }

            set
            {
                SetField(ref _pathToSevenZipExe, value, () => _pathToSevenZipExe);
            }
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
                ProjectContext.DomainUri != null && (
                this.PathToSevenZipExe != _settings.PathToSevenZipExe ||
                this.SelectedSite != _settings.SelectedMultiSiteEntry || LoadSettingsFailed);
        }

        /// <summary>
        /// Save settings to team foundation registry
        /// </summary>
        private void Save()
        {
            _settings.PathToSevenZipExe = PathToSevenZipExe;
            _settings.SelectedMultiSiteEntry = SelectedSite;

            try
            {
                _settings.SaveLocalRegistrySettings();
                SaveSettingsFailed = false;
                RaiseNotifyPropertyChanged(() => SaveSettingsFailed);
            }
            catch (Exception exception)
            {
                UserMessageService.ShowError("Failed to save settings to windows registry.");
                Logger.Instance().Log(TraceLevel.Error, "Failed to save settings to windows registry. Error was {0}", exception.ToString());
            }
            finally
            {
                LoadSettings();
            }
        }

        /// <summary>
        /// Loads the values of the combobox cbxSelectedMultiSite and selects the predefined value, if was set
        /// </summary>
        private void LoadMultiSiteComboBox()
        {
            var tmp = new List<string>();
            tmp.Add(ApplicationSettings.AutomaticSite);

            var multiSiteList = _sites.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var entry in multiSiteList)
            {
                var site = entry.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                tmp.Add(site[0]);
            }

            AvailableSites = new SortableObservableCollection<string>(tmp);
            AvailableSites.Sort(x => x.ToString(), ListSortDirection.Ascending);

            if ((ApplicationSettings.Instance.SelectedMultiSiteEntry != null) && (AvailableSites.Contains(ApplicationSettings.Instance.SelectedMultiSiteEntry)))
            {
                SelectedSite = ApplicationSettings.Instance.SelectedMultiSiteEntry;
            }
            else
            {
                SelectedSite = ApplicationSettings.AutomaticSite;
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

            BinaryRepositoryTeamProject = _settings.BinaryRepositoryTeamProject;
            BinaryRepositoryTeamProjectCollectionUri = _settings.BinaryRepositoryTeamProjectCollectionUrl;
            Sites = string.Join(";", _settings.MultiSiteList);

            LoadMultiSiteComboBox();

            //Load the settings from the local registry
            PathToSevenZipExe = ApplicationSettings.Instance.InstallPathForSevenZip;
            
            RaiseNotifyPropertyChanged(() => IsConnected);
            RaiseNotifyPropertyChanged(() => ProjectContext);
            RaiseNotifyPropertyChanged(() => LoadSettingsFailed);
        }

        #endregion

        #region Validation

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
    }
}
