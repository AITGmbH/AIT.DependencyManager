// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DependencyManagerSettings.cs" company="AIT GmbH & Co. KG">
//   All rights reserved by AIT GmbH & Co. KG
// </copyright>
// <summary>
//   Defines the DependencyManagerSettings type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using AIT.DMF.DependencyService;

namespace AIT.DMF.Common
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.Reflection;
    using Microsoft.TeamFoundation.Client;
    using Microsoft.TeamFoundation.Framework.Client;

    /// <summary>
    /// Internal class for registry settings (VSIX, MSBuild, Workflow). Because access is cluttered over the whole solution,
    /// settings are implemented as globally accessible singleton. To add a new setting that
    /// is automatically saved to and load from the team foundation server registry, add
    /// a property and annotate it with the <see cref="TfsRegistryEntryAttribute">registry entry attribute</see>.
    /// </summary>
    public sealed class DependencyManagerSettings
    {
        #region Constants

        /// <summary>
        /// The character that separates allowed dependency file extensions in a single string
        /// </summary>
        public const char FileExtensionSeparator = ',';

        /// <summary>
        /// The folder within the team foundation server registry in which settings are stored:
        /// - Team Project Collection specific: /AIT/DependencyManagement/Client/Settings/{TPC Name} (Future versions)
        /// - Global: /AIT/DependencyManagement/Client/Settings/Global
        /// Team members other than project collection administrators have only access to the
        /// /Service/Registration/RegistrationExtendedAttribute/ - node and its descendants: see
        /// http://pascoal.net/2011/11/using-team-foundation-server-registrypart-i-the-concepts/
        /// </summary>
        private const string TfsRegistryFolder = "/Service/Registration/RegistrationExtendedAttribute/AIT/DependencyManagement/Client/Settings/Global";

        /// <summary>
        /// The Default template to use when creating a dependency definition file.
        /// </summary>
        private const string DefaultDependencyDefinitionFileTemplate =
            "<?xml version=\"1.0\"?>\n<Component xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns=\"http://schemas.aitgmbh.de/DependencyManager/2011/11\">\n\t<Dependencies />\n</Component>";

        #endregion

        #region Fields

        /// <summary>
        /// The only instance.
        /// </summary>
        private static readonly DependencyManagerSettings _Instance = new DependencyManagerSettings();

        /// <summary>
        /// Team foundation server registry
        /// </summary>
        private ITeamFoundationRegistry _registry;

        #endregion

        #region Constructors

        /// <summary>
        /// Prevents a default instance of the <see cref="DependencyManagerSettings"/> class from being created.
        /// </summary>
        private DependencyManagerSettings()
        {
            Load(null);
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets the instance.
        /// </summary>
        public static DependencyManagerSettings Instance
        {
            get { return _Instance; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the addin automatically fetches dependencies when a local build is triggered
        /// </summary>
        [TfsRegistryEntry(defaultValue: false)]
        public bool FetchDependenciesOnLocalSolutionBuild { get; set; }

        /// <summary>
        /// Gets or sets the file extensions that indicate dependency definitions
        /// (Default value (string array with ".targets") is constructed in FileExtensions property)
        /// </summary>
        public string[] ValidDependencyDefinitionFileExtension { get; set; }

        /// <summary>
        /// Gets or sets the relative output path.
        /// </summary>
        [TfsRegistryEntry(defaultValue: "..\\Bin")]
        public string RelativeOutputPath { get; set; }

        /// <summary>
        /// Gets or sets the binary repository TP name currently set.
        /// </summary>
        [TfsRegistryEntry(defaultValue: "BinaryRepository")]
        public string BinaryRepositoryTeamProject { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether binary repository component settings are enabled for user input.
        /// </summary>
        [TfsRegistryEntry(defaultValue: true)]
        public bool IsBinaryRepositoryComponentSettingsEnabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to filter binary repository component list.
        /// </summary>
        [TfsRegistryEntry(defaultValue: false)]
        public bool BinaryRepositoryFilterComponentList { get; set; }

        /// <summary>
        /// Gets or sets the binary repository team project collection url.
        /// </summary>
        [TfsRegistryEntry(defaultValue: "")]
        public string BinaryRepositoryTeamProjectCollectionUrl { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether zipped dependencies are allowed.
        /// </summary>
        [TfsRegistryEntry(defaultValue: true)]
        public bool IsZippedDependencyAllowed { get; set; }

        /// <summary>
        /// Gets or sets the value indicating whether multi site path replacement is allowed
        /// </summary>
        [TfsRegistryEntry(defaultValue: false)]
        public bool IsMultiSiteAllowed { get; set; }

        public string[] MultiSiteList { get; set; }

        /// <summary>
        /// Gets or sets the forced mutli site entry
        /// </summary>
        [LocalRegistryEntry(defaultValue: ApplicationSettings.AutomaticSite)]
        public string SelectedMultiSiteEntry
        {
            get { return ApplicationSettings.Instance.SelectedMultiSiteEntry; }

            set { ApplicationSettings.Instance.SelectedMultiSiteEntry = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether users are allowed to create source control mapping dependencies.
        /// </summary>
        [TfsRegistryEntry(defaultValue: "")]
        public string DisabledResolvers { get; set; }

        /// <summary>
        /// Gets or sets the path to the seven zip dll
        /// </summary>
        [LocalRegistryEntry(defaultValue: "")]
        public string PathToSevenZipExe
        {
            get { return ApplicationSettings.Instance.InstallPathForSevenZip; }

            set { ApplicationSettings.Instance.InstallPathForSevenZip = value; }
        }

        /// <summary>
        /// Gets the component.targets template currently set.
        /// </summary>
        public string DependencyDefinitionFileTemplate
        {
            get { return DefaultDependencyDefinitionFileTemplate; }
        }


        #endregion

        #region Private members

        /// <summary>
        /// Gets or sets a comma separated list of allowed file extensions. This is a proxy to serialize the public
        /// string array of file extensions into a single string that can be stored in the registry
        /// </summary>
        [TfsRegistryEntry(defaultValue: ".targets")]
        // ReSharper disable UnusedMember.Local
        private string FileExtensions
        // ReSharper restore UnusedMember.Local
        {
            get
            {
                return string.Join(FileExtensionSeparator.ToString(CultureInfo.InvariantCulture), ValidDependencyDefinitionFileExtension);
            }

            set
            {
                ValidDependencyDefinitionFileExtension = value.Split(new[] { FileExtensionSeparator }, StringSplitOptions.RemoveEmptyEntries);
            }
        }

        /// <summary>
        ///
        /// Format: site,basepath,replacepath;site,basepath,replacepath
        /// </summary>
        [TfsRegistryEntry(defaultValue: "")]
        private string MultiSiteEntries
        {
            get
            {
                if (MultiSiteList != null)
                {
                    return string.Join(";", MultiSiteList);
                }
                else
                {
                    return null;
                }
            }
            set
            {
                MultiSiteList = value.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            }
        }

        /// <summary>
        /// Saves the current settings to the team foundation server registry
        /// </summary>
        /// <exception cref="InvalidOperationException">When trying to save without connection to team foundation server
        /// </exception>
        public void Save()
        {
            Logger.Instance().Log(TraceLevel.Verbose, "Saving settings to team foundation registry");
            if (_registry == null)
            {
                throw new InvalidOperationException("Settings can only be saved after a successful Load()");
            }

            foreach (var property in GetType().GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {

                //Save the settings to the tfs registry
                var registryInfo = property.GetCustomAttribute<TfsRegistryEntryAttribute>();

                // ReSharper disable InvertIf
                if (registryInfo != null)
                // ReSharper restore InvertIf
                {
                    var key = string.Format("{0}/{1}", TfsRegistryFolder, registryInfo.RegistryKeyOverride ?? property.Name);

                    var value = Convert.ToString(property.GetValue(this));
                    _registry.SetValue(key, value);
                    Logger.Instance().Log(TraceLevel.Verbose, "Saved key {0}, new value is: {1}", key, value);
                }
            }
        }

        /// <summary>
        /// Saves the current settings to the team the local registry
        /// </summary>

        public void SaveLocalRegistrySettings()
        {
            Logger.Instance().Log(TraceLevel.Verbose, "Saving settings to local registry");


            foreach (var property in GetType().GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                //Save the settings to the local registry
                var localRegistryInfo = property.GetCustomAttribute<LocalRegistryEntry>();

                if (localRegistryInfo != null)
                // ReSharper restore InvertIf
                {
                    var value = Convert.ToString(property.GetValue(this));

                    // TODO: The originally if-path is not a good implementation; rework, but create unit tests before
                    if ("PathToSevenZipExe".Equals(Convert.ToString(property.Name)))
                    {
                        ApplicationSettings.Instance.InstallPathForSevenZip = value;
                    }
                    else if ("SelectedMultiSiteEntry".Equals(Convert.ToString(property.Name)))
                    {
                        ApplicationSettings.Instance.SelectedMultiSiteEntry = value;
                    }
                    Logger.Instance().Log(TraceLevel.Verbose, "Saved key {0}, new value is: {1}", property.Name, value);
                }
            }
        }

        /// <summary>
        /// Load settings from registry or applies default values if registry values are either missing or
        /// this class was created without a connection to a team foundation server
        /// </summary>
        /// <param name="teamProjectCollectionUrl">Team project collection url</param>
        /// <returns>True if all keys were accessed successfully, false if at least on key could not be accessed</returns>
        public bool Load(string teamProjectCollectionUrl)
        {
            var successfulLoad = true;

            Logger.Instance().Log(TraceLevel.Verbose, "Loading settings from team foundation registry");

            if (teamProjectCollectionUrl != null)
            {
                var tfs = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(new Uri(teamProjectCollectionUrl));
                tfs.EnsureAuthenticated();
                _registry = tfs.GetService<ITeamFoundationRegistry>();
            }

            // Loads values from TFS registry
            foreach (var property in GetType().GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                var registryInfo = property.GetCustomAttribute<TfsRegistryEntryAttribute>();

                // ReSharper disable InvertIf
                if (registryInfo != null)
                // ReSharper restore InvertIf
                {
                    var key = string.Format("{0}/{1}", TfsRegistryFolder, registryInfo.RegistryKeyOverride ?? property.Name);

                    try
                    {
                        // Apply default value and after that, try to load value from registry
                        property.SetValue(this, registryInfo.DefaultValue);

                        var value = _registry.GetValue(key);
                        if (value == null)
                        {
                            // If we dont have read permission for security reasons null is returned which is the same for keys that don't exist
                            Logger.Instance().Log(TraceLevel.Verbose, "Key not present in registry or access denied: {0}. Using default value", key);
                            successfulLoad = false;
                        }
                        else
                        {
                            property.SetValue(this, Convert.ChangeType(value, property.PropertyType));
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Instance().Log(TraceLevel.Warning, "Failed to load value {0} from tfs registry: {1}", key, e.ToString());
                    }
                }
            }

            return successfulLoad;
        }

        #endregion
    }
}
