// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DependencyGraphCreator.cs" company="AIT GmbH & Co. KG">
//   All rights reserved by AIT GmbH & Co. KG.
// </copyright>
// <summary>
//   Defines the DependencyGraphCreator type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace AIT.DMF.DependencyService
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Xml;
    using System.Xml.Linq;

    using Common;
    using Contracts.Common;
    using Contracts.Enums;
    using Contracts.Exceptions;
    using Contracts.Graph;
    using Contracts.Gui;
    using Contracts.Parser;
    using Contracts.Services;
    using Microsoft.TeamFoundation.Client;
    using Microsoft.TeamFoundation.VersionControl.Client;
    using Microsoft.TeamFoundation.VersionControl.Common;
    using SharpSvn;
    using Contracts.Provider;
    /// <summary>
    /// The DependencyGraphCreator class creates the dependency graph by reading several dependency definition files.
    /// </summary>
    public class DependencyGraphCreator
    {
        #region Private Members

        /// <summary>
        /// The component list.
        /// </summary>
        private readonly List<IComponent> _compList;

        /// <summary>
        /// The dependency definition file name.
        /// </summary>
        private readonly string _dependencyDefinitionFileName;

        /// <summary>
        /// The logger to log messages which are user visible.
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// Determines if messages should be logged which are visible for the user.
        /// </summary>
        private readonly bool _silentMode = true;

        /// <summary>
        /// A dictionary with all available provider types.
        /// </summary>
        private readonly IDictionary<DependencyType, IEnumerable<string>> _providerTypeDictionary;

        /// <summary>
        /// A dictionary with all available provider settings types.
        /// </summary>
        private readonly IDictionary<string, DependencyProviderSettingsType> _providerSettingsTypeDictionary;

        /// <summary>
        /// A dictionary with all available provider configuration settings types.
        /// </summary>
        private readonly IDictionary<string, IEnumerable<ResolverValidSettings>> _providerConfigurationSettingsDictionary;

        /// <summary>
        /// A dictionary with all valid provider settings for each provider type.
        /// </summary>
        private readonly IDictionary<string, IEnumerable<DependencyProviderValidSettingName>> _providerValidSettingsDictionary;

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="DependencyGraphCreator"/> class by initializing the logger and calling the basic constructor.
        /// </summary>
        /// <param name="dependencyDefinitionFileName">The dependency definition file name.</param>
        /// <param name="initalizedLogger">The logger.</param>
        /// <param name="silentMode">If silent mode is enabled the messages are traced in the debug log but not written via logger.</param>
        public DependencyGraphCreator(string dependencyDefinitionFileName, ILogger initalizedLogger, bool silentMode) : this(dependencyDefinitionFileName)
        {
            _logger = initalizedLogger;
            if (_logger == null)
            {
                throw new DependencyServiceException("No logger supplied for logging");
            }

            _compList = new List<IComponent>();
            _silentMode = silentMode;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DependencyGraphCreator"/> class.
        /// This class is initialized with a new resolver factory, with dependency provider and settings information
        /// and the dependency definition filename (Minimal initialization for Visual Editor support).
        /// </summary>
        /// <param name="dependencyDefinitionFileName">
        /// The filename of dependency definition file.
        /// </param>
        public DependencyGraphCreator(string dependencyDefinitionFileName)
        {
            if (string.IsNullOrEmpty(dependencyDefinitionFileName))
            {
                throw new DependencyServiceException("Could not fetch name of dependency definition file");
            }

            _dependencyDefinitionFileName = dependencyDefinitionFileName;

            // Supported provider types
            // MIND_THE_GAP: Add information for new providers here
            _providerTypeDictionary = new Dictionary<DependencyType, IEnumerable<string>>();
            var providersForBinaryDependencyType = new List<string>
                                                        {
                                                            "BuildResultJSON",
                                                            "BuildResult",
                                                            "FileShare",
                                                            "BinaryRepository",
                                                            "Subversion"
                                                        };
            _providerTypeDictionary.Add(new KeyValuePair<DependencyType, IEnumerable<string>>(DependencyType.BinaryDependency, providersForBinaryDependencyType));
            var providersForSourceDependencyType = new List<string>
                                                        {
                                                            "SourceControl",
                                                            "SourceControlCopy"
                                                        };
            _providerTypeDictionary.Add(new KeyValuePair<DependencyType, IEnumerable<string>>(DependencyType.SourceDependency, providersForSourceDependencyType));

            // Supported provider settings type
            // MIND_THE_GAP: Add information for new provider settings types here
            _providerSettingsTypeDictionary = new Dictionary<string, DependencyProviderSettingsType>();
            _providerSettingsTypeDictionary.Add(new KeyValuePair<string, DependencyProviderSettingsType>("BuildResultJSON", DependencyProviderSettingsType.VNextBuildResultSettings));
            _providerSettingsTypeDictionary.Add(new KeyValuePair<string, DependencyProviderSettingsType>("BuildResult", DependencyProviderSettingsType.BuildResultSettings));
            _providerSettingsTypeDictionary.Add(new KeyValuePair<string, DependencyProviderSettingsType>("FileShare", DependencyProviderSettingsType.FileShareSettings));
            _providerSettingsTypeDictionary.Add(new KeyValuePair<string, DependencyProviderSettingsType>("BinaryRepository", DependencyProviderSettingsType.BinaryRepositorySettings));
            _providerSettingsTypeDictionary.Add(new KeyValuePair<string, DependencyProviderSettingsType>("SourceControl", DependencyProviderSettingsType.SourceControlSettings));
            _providerSettingsTypeDictionary.Add(new KeyValuePair<string, DependencyProviderSettingsType>("SourceControlCopy", DependencyProviderSettingsType.SourceControlCopySettings));
            _providerSettingsTypeDictionary.Add(new KeyValuePair<string, DependencyProviderSettingsType>("Subversion", DependencyProviderSettingsType.SubversionSettings));

            // Required resolver configuration settings
            // MIND_THE_GAP: Add settings if provider configuration changes or new providers are added
            _providerConfigurationSettingsDictionary = new Dictionary<string, IEnumerable<ResolverValidSettings>>();
            //VNextBuild result
            var vnextBuildResultConfigurationSettings = new List<ResolverValidSettings> { ResolverValidSettings.BinaryTeamProjectCollectionUrl };// TODO dga: Investigate if we should extend enum with resolver for vnext settings.
            _providerConfigurationSettingsDictionary.Add(new KeyValuePair<string, IEnumerable<ResolverValidSettings>>("BuildResultJSON", vnextBuildResultConfigurationSettings));
            //Build Result
            var buildResultConfigurationSettings = new List<ResolverValidSettings> { ResolverValidSettings.TeamProjectCollectionUrl };
            _providerConfigurationSettingsDictionary.Add(new KeyValuePair<string, IEnumerable<ResolverValidSettings>>("BuildResult", buildResultConfigurationSettings));
            // File Share
            var fileShareConfigurationSettings = new List<ResolverValidSettings> { ResolverValidSettings.FileShareUrl };
            _providerConfigurationSettingsDictionary.Add(new KeyValuePair<string, IEnumerable<ResolverValidSettings>>("FileShare", fileShareConfigurationSettings));
            var binaryRepositoryConfigurationSettings = new List<ResolverValidSettings>
                                                            {
                                                                ResolverValidSettings.BinaryTeamProjectCollectionUrl,
                                                                ResolverValidSettings.BinaryRepositoryTeamProject
                                                            };
            _providerConfigurationSettingsDictionary.Add(new KeyValuePair<string, IEnumerable<ResolverValidSettings>>("BinaryRepository", binaryRepositoryConfigurationSettings));
            // Source Control
            var sourceControlConfigurationSettings = new List<ResolverValidSettings>
                                                        {
                                                            ResolverValidSettings.TeamProjectCollectionUrl
                                                        };
            _providerConfigurationSettingsDictionary.Add(new KeyValuePair<string, IEnumerable<ResolverValidSettings>>("SourceControl", sourceControlConfigurationSettings));
            // Subversion
            var subversionConfigurationSettings = new List<ResolverValidSettings>
                                                        {
                                                            ResolverValidSettings.SubversionUrl
                                                        };
            _providerConfigurationSettingsDictionary.Add(new KeyValuePair<string, IEnumerable<ResolverValidSettings>>("Subversion", subversionConfigurationSettings));

            // Required provider valid settings
            // MIND_THE_GAP: Add settings if provider valid settings changes or new providers are added
            _providerValidSettingsDictionary = new Dictionary<string, IEnumerable<DependencyProviderValidSettingName>>();
            // Build Result
            var buildResultValidSettings = new List<DependencyProviderValidSettingName>
                                               {
                                                   DependencyProviderValidSettingName.TeamProjectName,
                                                   DependencyProviderValidSettingName.BuildDefinition,
                                                   DependencyProviderValidSettingName.BuildNumber,
                                                   DependencyProviderValidSettingName.BuildQuality,
                                                   DependencyProviderValidSettingName.BuildStatus,
                                                   DependencyProviderValidSettingName.IncludeFilter,
                                                   DependencyProviderValidSettingName.RelativeOutputPath,
                                                   DependencyProviderValidSettingName.FolderMappings
                                               };
            _providerValidSettingsDictionary.Add(new KeyValuePair<string, IEnumerable<DependencyProviderValidSettingName>>("BuildResult", buildResultValidSettings));

            var vNextBuildResultValidSettings = new List<DependencyProviderValidSettingName>
                                               {
                                                   DependencyProviderValidSettingName.TeamProjectName,
                                                   DependencyProviderValidSettingName.BuildDefinition,
                                                   DependencyProviderValidSettingName.BuildNumber,
                                                   DependencyProviderValidSettingName.BuildStatus,
                                                   DependencyProviderValidSettingName.BuildTags,
                                                   DependencyProviderValidSettingName.IncludeFilter,
                                                   DependencyProviderValidSettingName.RelativeOutputPath,
                                                   DependencyProviderValidSettingName.FolderMappings
                                               };

            _providerValidSettingsDictionary.Add(new KeyValuePair<string, IEnumerable<DependencyProviderValidSettingName>>("BuildResultJSON", vNextBuildResultValidSettings));
            // File Share
            var fileShareValidSettings = new List<DependencyProviderValidSettingName>
                                             {
                                                 DependencyProviderValidSettingName.FileShareRootPath,
                                                 DependencyProviderValidSettingName.ComponentName,
                                                 DependencyProviderValidSettingName.VersionNumber,
                                                 DependencyProviderValidSettingName.IncludeFilter,
                                                 DependencyProviderValidSettingName.RelativeOutputPath,
                                                 DependencyProviderValidSettingName.FolderMappings
                                             };
            _providerValidSettingsDictionary.Add(new KeyValuePair<string, IEnumerable<DependencyProviderValidSettingName>>("FileShare", fileShareValidSettings));
            // Binary Repository
            var binaryRepositoryValidSettings = new List<DependencyProviderValidSettingName>
                                                    {
                                                        DependencyProviderValidSettingName.BinaryTeamProjectCollectionUrl,
                                                        DependencyProviderValidSettingName.BinaryRepositoryTeamProject,
                                                        DependencyProviderValidSettingName.ComponentName,
                                                        DependencyProviderValidSettingName.VersionNumber,
                                                        DependencyProviderValidSettingName.RelativeOutputPath,
                                                        DependencyProviderValidSettingName.IncludeFilter,
                                                        DependencyProviderValidSettingName.FolderMappings
                                                    };
            _providerValidSettingsDictionary.Add(new KeyValuePair<string, IEnumerable<DependencyProviderValidSettingName>>("BinaryRepository", binaryRepositoryValidSettings));
            // Source Control
            var sourceControlValidSettings = new List<DependencyProviderValidSettingName>
                                                 {
                                                     DependencyProviderValidSettingName.ServerRootPath,
                                                     DependencyProviderValidSettingName.VersionSpec,
                                                     DependencyProviderValidSettingName.RelativeOutputPath
                                                 };
            _providerValidSettingsDictionary.Add(new KeyValuePair<string, IEnumerable<DependencyProviderValidSettingName>>("SourceControl", sourceControlValidSettings));
            // Source Control Copy
            var sourceControlCopyValidSettings = new List<DependencyProviderValidSettingName>
                                                 {
                                                     DependencyProviderValidSettingName.ServerRootPath,
                                                     DependencyProviderValidSettingName.VersionSpec,
                                                     DependencyProviderValidSettingName.RelativeOutputPath,
                                                     DependencyProviderValidSettingName.IncludeFilter,
                                                     DependencyProviderValidSettingName.FolderMappings
                                                 };
            _providerValidSettingsDictionary.Add(new KeyValuePair<string, IEnumerable<DependencyProviderValidSettingName>>("SourceControlCopy", sourceControlCopyValidSettings));
            // Subversion
            var subversionValidSettings = new List<DependencyProviderValidSettingName>
                                                 {
                                                     DependencyProviderValidSettingName.SubversionRootPath,
                                                     DependencyProviderValidSettingName.ComponentName,
                                                     DependencyProviderValidSettingName.VersionSpec,
                                                     DependencyProviderValidSettingName.RelativeOutputPath,
                                                     DependencyProviderValidSettingName.IncludeFilter,
                                                     DependencyProviderValidSettingName.FolderMappings
                                                 };
            _providerValidSettingsDictionary.Add(new KeyValuePair<string, IEnumerable<DependencyProviderValidSettingName>>("Subversion", subversionValidSettings));
        }

        #region Public Properties

        /// <summary>
        /// Gets the command type.
        /// </summary>
        public string CommandType
        {
            get
            {
                return "GraphCreator";
            }
        }

        #endregion

        /// <summary>
        /// Returns the dependency graph based on the component.targets specified by the user.
        /// </summary>
        /// <param name="serviceSettings">Settings with connection description etc.</param>
        /// <param name="localPath">Component.targets folder</param>
        /// <returns>Returns the dependency graph</returns>
        public IGraph GetDependencyGraph(ISettings<ServiceValidSettings> serviceSettings, string localPath)
        {
            if (string.IsNullOrWhiteSpace(localPath) || string.IsNullOrWhiteSpace(Path.GetDirectoryName(localPath)) || !File.Exists(localPath))
            {
                Logger.Instance().Log(TraceLevel.Info, "{0}: Dependency definition file {1} could not be found!", CommandType, localPath);
                throw new FileNotFoundException("No dependency definition file found!", localPath);
            }

            Logger.Instance().Log(TraceLevel.Info, "{0}: Generating dependency graph ...", CommandType);

            // Parsing component.targets file
            var dependencyDefinitionFileXDoc = LoadDependencyDefinitionFile(localPath);
            var parser = new ParserXml();
            var parsedCompTargets = (XmlComponent)parser.ReadDependencyFile(dependencyDefinitionFileXDoc);

            // ReSharper disable PossibleNullReferenceException
            var name = string.IsNullOrWhiteSpace(parsedCompTargets.Name) ? Path.GetDirectoryName(localPath).Substring(Path.GetDirectoryName(localPath).LastIndexOf(Path.DirectorySeparatorChar)) : parsedCompTargets.Name;
            // ReSharper restore PossibleNullReferenceException

            // Try to get local workspace info of the file
            var wi = Workstation.Current.GetLocalWorkspaceInfo(localPath);
            var serverPath = string.Empty;
            var addedToSourceControl = false;
            VersionControlServer vcs = null;

            if (wi != null)
            {
                // TODO inject the team project collection in the constructor. On a plain system, the dialog to enter the credentials is shown
                var collection = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(wi.ServerUri);
                var ws = wi.GetWorkspace(collection);
                serverPath = ws.TryGetServerItemForLocalItem(localPath);
                vcs = ws.VersionControlServer;
                var pendingChanges = ws.GetPendingChangesEnumerable(serverPath);
                foreach (var pendingChange in pendingChanges)
                {
                    if (pendingChange.ChangeType == ChangeType.Add)
                    {
                        addedToSourceControl = true;
                    }
                }
            }

            var config = new DependencyProviderConfig
            {
                Settings = new DependencyProviderSettings()
            };

            if (wi == null || vcs == null || !(vcs.ServerItemExists(serverPath, ItemType.File) || addedToSourceControl))
            {
                // support component.targets that aren't checked-in yet
                config.Type = "Local";
            }

            if (wi == null)
            {
                // Local component.targets without workspace
                var nameSetting = new DependencyProviderSetting
                {
                    Name = DependencyProviderValidSettingName.ComponentName,
                    Value = name
                };
                var tpcSetting = new DependencyProviderSetting
                {
                    Name = DependencyProviderValidSettingName.WorkspaceTeamProjectCollectionUrl,
                    Value = serviceSettings.GetSetting(ServiceValidSettings.DefaultTeamProjectCollection)
                };
                var workspaceNameSetting = new DependencyProviderSetting
                {
                    Name = DependencyProviderValidSettingName.WorkspaceName,
                    Value = serviceSettings.GetSetting(ServiceValidSettings.DefaultWorkspaceName)
                };
                var workspaceOwnerSetting = new DependencyProviderSetting
                {
                    Name = DependencyProviderValidSettingName.WorkspaceOwner,
                    Value = serviceSettings.GetSetting(ServiceValidSettings.DefaultWorkspaceOwner)
                };
                var versionSetting = new DependencyProviderSetting
                {
                    Name = DependencyProviderValidSettingName.VersionNumber,
                    Value = string.IsNullOrWhiteSpace(parsedCompTargets.Version) ? "Local" : parsedCompTargets.Version
                };
                config.Settings.SettingsList = new List<IDependencyProviderSetting> { nameSetting, versionSetting, tpcSetting, workspaceNameSetting, workspaceOwnerSetting };
            }
            else
            {
                // Checked in
                // Or pending add to source control
                config.Type = "SourceControl";
                serverPath = VersionControlPath.GetFolderName(serverPath);
                var nameSetting = new DependencyProviderSetting
                {
                    Name = DependencyProviderValidSettingName.ServerRootPath,
                    Value = serverPath
                };

                var tpcSetting = new DependencyProviderSetting
                {
                    Name = DependencyProviderValidSettingName.WorkspaceTeamProjectCollectionUrl,
                    Value = wi.ServerUri.ToString()
                };
                var workspaceNameSetting = new DependencyProviderSetting
                {
                    Name = DependencyProviderValidSettingName.WorkspaceName,
                    Value = wi.Name
                };
                var workspaceOwnerSetting = new DependencyProviderSetting
                {
                    Name = DependencyProviderValidSettingName.WorkspaceOwner,
                    Value = wi.OwnerName
                };

                config.Settings.SettingsList = new List<IDependencyProviderSetting> { nameSetting, tpcSetting, workspaceNameSetting, workspaceOwnerSetting };
            }

            var rootComp = new Component(config, new List<IDependency>(), new List<IDependency>());
            _compList.Add(rootComp);
            Logger.Instance().Log(TraceLevel.Info, "{0}: Root component {1}#{2} (Type {3}) added to graph", CommandType, rootComp.Name, rootComp.Version, rootComp.Type);

            // Handle root node dependencies
            Logger.Instance().Log(TraceLevel.Info, "{0}: Searching dependencies for root component ...", CommandType);
            foreach (var dep in parsedCompTargets.Dependencies)
            {
                var succDep = HandleDependency(dep, rootComp, serviceSettings);
                rootComp.AddSuccessor(succDep);
            }

            Logger.Instance().Log(TraceLevel.Info, "{0}: Searching dependencies for root component finished", CommandType);
            Logger.Instance().Log(TraceLevel.Info, "{0}: Dependency graph created successfully!", CommandType);

            return new Graph(rootComp, localPath);
        }

        /// <summary>
        /// Loads the dependency definition file.
        /// </summary>
        /// <param name="localPathToDependencyDefinitionFile">The local path to dependency definition file.</param>
        /// <returns>XDocument object containing the defined dependencies as XML</returns>
        private static XDocument LoadDependencyDefinitionFile(string localPathToDependencyDefinitionFile)
        {
            XDocument compTargetsXDoc;

            try
            {
                using (var fs = new FileStream(localPathToDependencyDefinitionFile, FileMode.Open, FileAccess.Read))
                {
                    compTargetsXDoc = XDocument.Load(fs);
                    if (compTargetsXDoc == null)
                    {
                        throw new InvalidComponentException(string.Format("Error while parsing local file '{0}'.", localPathToDependencyDefinitionFile));
                    }
                }
            }
            catch (XmlException)
            {
                throw new InvalidComponentException(string.Format("Local file '{0}' generated xml exception while parsing.", localPathToDependencyDefinitionFile));
            }

            return compTargetsXDoc;
        }

        /// <summary>
        /// Handles dependencies according to provider configuration type by calling the appropriate helper method.
        /// </summary>
        /// <param name="dependency">Dependency description</param>
        /// <param name="sourceComp">Dependency source component</param>
        /// <param name="serviceSettings">Settings to use</param>
        /// <returns>Constructed dependency</returns>
        private IDependency HandleDependency(IXmlDependency dependency, IComponent sourceComp, ISettings<ServiceValidSettings> serviceSettings)
        {
            var depProvConfig = dependency.ProviderConfiguration;
            Logger.Instance().Log(TraceLevel.Info, "{0}: Processing {1} dependency ...", CommandType, depProvConfig.Type);

            switch (depProvConfig.Type)
            {
                case "BuildResult":
                    return HandleBuildResultDependency(depProvConfig, sourceComp, serviceSettings);
                case "BuildResultJSON":
                    return HandleVNextBuildResultDependency(depProvConfig, sourceComp, serviceSettings);
                case "FileShare":
                    return HandleFileShareDependency(depProvConfig, sourceComp, serviceSettings);
                case "SourceControl":
                    return HandleSourceControlDependency("Resolver_SourceControlMapping", "SourceControl", depProvConfig, sourceComp, serviceSettings);
                case "SourceControlCopy":
                    return HandleSourceControlDependency("Resolver_SourceControlCopy", "SourceControlCopy", depProvConfig, sourceComp, serviceSettings);
                case "BinaryRepository":
                    return HandleBinaryRepositoryDependency(depProvConfig, sourceComp, serviceSettings);
                case "Subversion":
                    return HandleSubversionDependency(depProvConfig, sourceComp, serviceSettings);
                default:
                    {
                        Logger.Instance().Log(TraceLevel.Info, "{0}: Unsupported dependency type '{1}' found. Please check dependency definition file {2} for component {3}!", CommandType, depProvConfig.Type, _dependencyDefinitionFileName, sourceComp.Name.GetName());
                        throw new DependencyServiceException(string.Format("Unsupported dependency type '{0}' found. Please check dependency definition file {1} for component {2}!", depProvConfig.Type, _dependencyDefinitionFileName, sourceComp.Name.GetName()));
                    }
            }
        }

        private IDependency HandleVNextBuildResultDependency(IDependencyProviderConfig config, IComponent sourceComp, ISettings<ServiceValidSettings> serviceSettings)
        {
            var resolverType = "Resolver_BuildResultJSON";
            var componentType = ComponentType.VNextBuildResult;
            // Only used to temporarily calculate name and version to look for
            var tempComponent = new Component(config);
            var compName = tempComponent.Name;
            var expectedVersion = tempComponent.Version;

            Logger.Instance().Log(TraceLevel.Info, "{0}: Start VNextBuildResult dependency {1}#{2} processing ...", CommandType, compName, expectedVersion);

            var tpc = serviceSettings.GetSetting(ServiceValidSettings.DefaultTeamProjectCollection);
            if (string.IsNullOrEmpty(tpc))
            {
                Logger.Instance().Log(TraceLevel.Error, "{0}: Team project collection url was not set for VNextBuildResult dependency {1}#{2}", CommandType, compName, expectedVersion);
                throw new DependencyServiceException("Invalid source control connection description found (Version control address does not exist)");
            }

            var dependencyDefinitionFileNameList = serviceSettings.GetSetting(ServiceValidSettings.DefaultDependencyDefinitionFilename);
            if (string.IsNullOrEmpty(dependencyDefinitionFileNameList))
            {
                Logger.Instance().Log(TraceLevel.Error, "{0}: Dependency definition file list is not present for VNextBuildResult dependency {1}#{2}", CommandType, tempComponent.Name, tempComponent.Version);
                throw new DependencyServiceException("Invalid build result connection settings found (Dependency definition file list does not exist)");
            }

            try
            {
                ISettings<ResolverValidSettings> resolverSettings = new Settings<ResolverValidSettings>();
                resolverSettings.AddSetting(new KeyValuePair<ResolverValidSettings, string>(ResolverValidSettings.TeamProjectCollectionUrl, tpc));
                resolverSettings.AddSetting(new KeyValuePair<ResolverValidSettings, string>(ResolverValidSettings.DependencyDefinitionFileNameList, dependencyDefinitionFileNameList));

                var rt = DependencyResolverFactory.GetResolverType(resolverType);
                var resolver = rt.CreateResolver(resolverSettings);

                if (resolver.ComponentExists(compName))
                {
                    var existing = false;
                    IComponent comp = null;
                    IComponentVersion versionNumberUsed = null;

                    if (expectedVersion.BuildNumber == null && expectedVersion.BuildStatus == null && expectedVersion.BuildTags == null)
                    {
                        // No specific build required -> Get latest build
                        var availableVersions = resolver.GetAvailableVersions(compName);
                        var componentVersions = availableVersions as List<IComponentVersion> ?? availableVersions.ToList();
                        if (availableVersions != null && componentVersions.Any())
                        {
                            versionNumberUsed = componentVersions.Last();
                            comp = CreateGraphNodeOrReturnExisting(compName, versionNumberUsed, componentType, config, out existing);
                        }
                        else
                        {
                            Logger.Instance().Log(TraceLevel.Error, "{0}: VNextBuildResult component {1} was not found in any version on build server for team project collection {2}", CommandType, compName, tpc);
                            throw new DependencyServiceException(string.Format("Component {0} was not found in any version (Team Project Collection: {1})", compName, tpc));
                        }
                    }
                    else if (resolver.ComponentExists(compName, expectedVersion))
                    {
                        versionNumberUsed = expectedVersion;
                        comp = CreateGraphNodeOrReturnExisting(compName, versionNumberUsed, componentType, config, out existing);
                    }
                    else
                    {
                        var availableVersions = resolver.GetAvailableVersions(compName);

                        // ReSharper disable PossibleMultipleEnumeration
                        if (availableVersions != null && availableVersions.Any())
                        // ReSharper restore PossibleMultipleEnumeration
                        {
                            // Search from latest build result to earliest
                            // ReSharper disable PossibleMultipleEnumeration
                            foreach (var version in availableVersions)
                            // ReSharper restore PossibleMultipleEnumeration
                            {
                                if (expectedVersion.BuildStatus != null && expectedVersion.BuildStatus.Contains(version.BuildStatus.First().ToLower()))
                                {
                                    // No build tags defined
                                    if (expectedVersion.BuildTags == null || !expectedVersion.BuildTags.Any())
                                    {
                                        versionNumberUsed = version;
                                        comp = CreateGraphNodeOrReturnExisting(compName, versionNumberUsed, componentType, config, out existing);
                                        break;
                                    }

                                    // Look for defined build tags
                                    if (version.BuildTags != null && expectedVersion.BuildTags.Contains(version.BuildTags.First().ToLower()))
                                    {
                                        versionNumberUsed = version;
                                        comp = CreateGraphNodeOrReturnExisting(compName, versionNumberUsed, componentType, config, out existing);
                                        break;
                                    }
                                }
                                else if (expectedVersion.BuildStatus == null && expectedVersion.BuildTags != null && version.BuildTags != null && expectedVersion.BuildTags.Contains(version.BuildTags.First().ToLower()))
                                {
                                    versionNumberUsed = version;
                                    comp = CreateGraphNodeOrReturnExisting(compName, versionNumberUsed, componentType, config, out existing);
                                    break;
                                }
                            }

                            if (comp == null)
                            {
                                Logger.Instance().Log(TraceLevel.Error, "{0}: VNextBuildResult component {1} was not found in a compatible version to expected version {2} on build server for team project collection {3}", CommandType, compName, expectedVersion, tpc);
                                throw new DependencyServiceException(string.Format("Component {0} not found in a compatible version to expected version {1} (Team Project Collection: {2})", compName, expectedVersion, tpc));
                            }
                        }
                        else
                        {
                            Logger.Instance().Log(TraceLevel.Error, "{0}: No build results could be found for VNextBuildResult component {1} on build server for team project collection {2}", CommandType, compName, tpc);
                            throw new DependencyServiceException(string.Format("No build results could be found for component {0} (Team Project Collection: {1})", compName, tpc));
                        }
                    }

                    Logger.Instance().Log(TraceLevel.Info, "{0}: VNextBuildResult dependency {1}#{2} resolved to component {1}#{3} on build server for team project collection {4}", CommandType, compName, expectedVersion, versionNumberUsed, tpc);
                    if (!_silentMode)
                    {
                        _logger.LogMsg(string.Format("* Found VNextBuildResult dependency: {0} ({1}) [Team Project Collection: {2}]", compName, versionNumberUsed, tpc));
                    }

                    ReadComponentTargets("VNextBuildResult", existing, resolver, compName, versionNumberUsed, comp, serviceSettings, expectedVersion, tpc);

                    // Create the dependency edge, add to own List as predecessor and return as successor edge for source component
                    // Use AddFallbackFieldValue of IComponent
                    comp.AddFallbackFieldValue(DependencyProviderValidSettingName.BuildTeamProjectCollectionUrl, serviceSettings.GetSetting(ServiceValidSettings.DefaultTeamProjectCollection));

                    IDependency dep = new Dependency(sourceComp, comp, expectedVersion);
                    comp.AddPredecessor(dep);

                    Logger.Instance().Log(TraceLevel.Verbose, "{0}: Creating dependency graph edge {1}#{2} --> {3}#{4}", CommandType, sourceComp.Name, sourceComp.Version, comp.Name, expectedVersion);
                    Logger.Instance().Log(TraceLevel.Info, "{0}: Resolving VNextBuildResult dependency {1}#{2} finished successfully", CommandType, compName, expectedVersion);

                    return dep;
                }

                Logger.Instance().Log(TraceLevel.Error, "{0}: Component {1} not found (Team Project Collection: {2})", CommandType, compName, tpc);
                throw new DependencyServiceException(string.Format("Component {0} not found (Team Project Collection: {1})", compName, tpc));
            }
            catch (UriFormatException ufe)
            {
                Logger.Instance().Log(TraceLevel.Error, "{0}: Error while processing VNextBuildResult dependency {1}#{2}: {3}", CommandType, compName, expectedVersion, ufe.Message);
                throw new DependencyServiceException(string.Format("Invalid url to build server [Team Project Collection: {0}]", tpc));
            }
            catch (InvalidProviderConfigurationException ipce)
            {
                Logger.Instance().Log(TraceLevel.Error, "{0}: Error while processing VNextBuildResult dependency {1}#{2}: {3}", CommandType, compName, expectedVersion, ipce.Message);
                throw new DependencyServiceException(string.Format("Invalid url to build server [Team Project Collection: {0}]", tpc));
            }
        }

        private void ReadComponentTargets(string buildResult, bool existing, IDependencyResolver resolver, IComponentName compName, IComponentVersion versionNumberUsed, IComponent comp, ISettings<ServiceValidSettings> serviceSettings, IComponentVersion expectedVersion, string tpc)
        {
            // Read component.targets from fileshare for component
            try
            {
                // We can stop traversing if the component already exists in the tree
                if (!existing)
                {
                    var dependencyDefinitionXDoc = resolver.LoadComponentTarget(compName, versionNumberUsed);

                    // DesignDecision MRI: Dependency definition files are optional!
                    if (dependencyDefinitionXDoc != null)
                    {
                        var parser = new ParserXml();
                        var dependencyDefinitionXml = (XmlComponent)parser.ReadDependencyFile(dependencyDefinitionXDoc);

                        Logger.Instance().Log(TraceLevel.Verbose, "{0}: Starting resolving dependencies for {1} component {2}#{3} on build server for team project collection {4} ...", CommandType, buildResult, compName, versionNumberUsed, tpc);
                        foreach (var dependency in dependencyDefinitionXml.Dependencies)
                        {
                            var succDep = HandleDependency(dependency, comp, serviceSettings);
                            comp.AddSuccessor(succDep);
                        }

                        Logger.Instance().Log(TraceLevel.Verbose, "{0}: Resolving dependencies for {1} component {2}#{3} on build server for team project collection {4} finished successfully", CommandType, buildResult, compName, versionNumberUsed, tpc);
                    }
                    else
                    {
                        Logger.Instance().Log(TraceLevel.Verbose, "{0}: Dependency definition file for component {1}#{2} was not found in DropLocation. Skipping search for dependencies.", CommandType, compName, versionNumberUsed);
                    }
                }
            }
            catch (DependencyServiceException dse)
            {
                Logger.Instance().Log(TraceLevel.Error, "{0}: Error while processing {2} dependency {3}#{4}: {4}", CommandType, buildResult, compName, expectedVersion, dse.Message);
                throw new DependencyServiceException(dse.Message);
            }
            catch (InvalidComponentException ice)
            {
                Logger.Instance().Log(TraceLevel.Error, "{0}: Error while processing {1} dependency {2}#{3}: {4}", CommandType, buildResult, compName, expectedVersion, ice.Message);
                throw new DependencyServiceException(string.Format("No {0} was found in {1}\\{2} [Team Project Collection:{3}]", _dependencyDefinitionFileName, compName, versionNumberUsed, tpc));
            }
        }
        /// <summary>
        /// Handles a build result dependency by searching for sub dependencies, creating a new IComponent and IDependency object and returning the created IDependency object.
        /// </summary>
        /// <param name="config">The configuration for build result.</param>
        /// <param name="sourceComp">The source component.</param>
        /// <param name="serviceSettings">The service settings.</param>
        /// <returns>The build result dependency.</returns>
        private IDependency HandleBuildResultDependency(IDependencyProviderConfig config, IComponent sourceComp, ISettings<ServiceValidSettings> serviceSettings)
        {
            var resolverType = "Resolver_BuildResult";
            var componentType = ComponentType.BuildResult;
            // Only used to temporarily calculate name and version to look for
            var tempComponent = new Component(config);
            var compName = tempComponent.Name;
            var expectedVersion = tempComponent.Version;

            Logger.Instance().Log(TraceLevel.Info, "{0}: Start BuildResult dependency {1}#{2} processing ...", CommandType, compName, expectedVersion);

            var tpc = serviceSettings.GetSetting(ServiceValidSettings.DefaultTeamProjectCollection);
            if (string.IsNullOrEmpty(tpc))
            {
                Logger.Instance().Log(TraceLevel.Error, "{0}: Team project collection url was not set for BuildResult dependency {1}#{2}", CommandType, compName, expectedVersion);
                throw new DependencyServiceException("Invalid source control connection description found (Version control address does not exist)");
            }

            var dependencyDefinitionFileNameList = serviceSettings.GetSetting(ServiceValidSettings.DefaultDependencyDefinitionFilename);
            if (string.IsNullOrEmpty(dependencyDefinitionFileNameList))
            {
                Logger.Instance().Log(TraceLevel.Error, "{0}: Dependency definition file list is not present for BuildResult dependency {1}#{2}", CommandType, tempComponent.Name, tempComponent.Version);
                throw new DependencyServiceException("Invalid build result connection settings found (Dependency definition file list does not exist)");
            }

            try
            {
                ISettings<ResolverValidSettings> resolverSettings = new Settings<ResolverValidSettings>();
                resolverSettings.AddSetting(new KeyValuePair<ResolverValidSettings, string>(ResolverValidSettings.TeamProjectCollectionUrl, tpc));
                resolverSettings.AddSetting(new KeyValuePair<ResolverValidSettings, string>(ResolverValidSettings.DependencyDefinitionFileNameList, dependencyDefinitionFileNameList));

                var rt = DependencyResolverFactory.GetResolverType(resolverType);
                var resolver = rt.CreateResolver(resolverSettings);

                if (resolver.ComponentExists(compName))
                {
                    var existing = false;
                    IComponent comp = null;
                    IComponentVersion versionNumberUsed = null;

                    if (expectedVersion.BuildNumber == null && expectedVersion.BuildStatus == null && expectedVersion.BuildQuality == null)
                    {
                        // No specific build required -> Get latest build
                        var availableVersions = resolver.GetAvailableVersions(compName);
                        var componentVersions = availableVersions as List<IComponentVersion> ?? availableVersions.ToList();
                        if (availableVersions != null && componentVersions.Any())
                        {
                            versionNumberUsed = componentVersions.Last();
                            comp = CreateGraphNodeOrReturnExisting(compName, versionNumberUsed, componentType, config, out existing);
                        }
                        else
                        {
                            Logger.Instance().Log(TraceLevel.Error, "{0}: BuildResult component {1} was not found in any version on build server for team project collection {2}", CommandType, compName, tpc);
                            throw new DependencyServiceException(string.Format("Component {0} was not found in any version (Team Project Collection: {1})", compName, tpc));
                        }
                    }
                    else if (resolver.ComponentExists(compName, expectedVersion))
                    {
                        versionNumberUsed = expectedVersion;
                        comp = CreateGraphNodeOrReturnExisting(compName, versionNumberUsed, componentType, config, out existing);
                    }
                    else
                    {
                        var availableVersions = resolver.GetAvailableVersions(compName);

                        // ReSharper disable PossibleMultipleEnumeration
                        if (availableVersions != null && availableVersions.Any())
                        // ReSharper restore PossibleMultipleEnumeration
                        {
                            // Search from latest build result to earliest
                            // ReSharper disable PossibleMultipleEnumeration
                            foreach (var version in availableVersions.Reverse())
                            // ReSharper restore PossibleMultipleEnumeration
                            {
                                if (expectedVersion.BuildStatus != null && expectedVersion.BuildStatus.Contains(version.BuildStatus.First().ToLower()))
                                {
                                    // No build quality defined
                                    if (expectedVersion.BuildQuality == null || !expectedVersion.BuildQuality.Any())
                                    {
                                        versionNumberUsed = version;
                                        comp = CreateGraphNodeOrReturnExisting(compName, versionNumberUsed, componentType, config, out existing);
                                        break;
                                    }

                                    // Look for defined build quality
                                    if (version.BuildQuality != null && expectedVersion.BuildQuality.Contains(version.BuildQuality.First().ToLower()))
                                    {
                                        versionNumberUsed = version;
                                        comp = CreateGraphNodeOrReturnExisting(compName, versionNumberUsed, componentType, config, out existing);
                                        break;
                                    }
                                }
                                else if (expectedVersion.BuildStatus == null && expectedVersion.BuildQuality != null && version.BuildQuality != null && expectedVersion.BuildQuality.Contains(version.BuildQuality.First().ToLower()))
                                {
                                    versionNumberUsed = version;
                                    comp = CreateGraphNodeOrReturnExisting(compName, versionNumberUsed, componentType, config, out existing);
                                    break;
                                }
                            }

                            if (comp == null)
                            {
                                Logger.Instance().Log(TraceLevel.Error, "{0}: BuildResult component {1} was not found in a compatible version to expected version {2} on build server for team project collection {3}", CommandType, compName, expectedVersion, tpc);
                                throw new DependencyServiceException(string.Format("Component {0} not found in a compatible version to expected version {1} (Team Project Collection: {2})", compName, expectedVersion, tpc));
                            }
                        }
                        else
                        {
                            Logger.Instance().Log(TraceLevel.Error, "{0}: No build results could be found for BuildResult component {1} on build server for team project collection {2}", CommandType, compName, tpc);
                            throw new DependencyServiceException(string.Format("No build results could be found for component {0} (Team Project Collection: {1})", compName, tpc));
                        }
                    }

                    Logger.Instance().Log(TraceLevel.Info, "{0}: BuildResult dependency {1}#{2} resolved to component {1}#{3} on build server for team project collection {4}", CommandType, compName, expectedVersion, versionNumberUsed, tpc);
                    if (!_silentMode)
                    {
                        _logger.LogMsg(string.Format("* Found BuildResult dependency: {0} ({1}) [Team Project Collection: {2}]", compName, versionNumberUsed, tpc));
                    }

                    ReadComponentTargets("BuildResult", existing, resolver, compName, versionNumberUsed, comp, serviceSettings, expectedVersion, tpc);

                    // Create the dependency edge, add to own List as predecessor and return as successor edge for source component
                    // Use AddFallbackFieldValue of IComponent
                    comp.AddFallbackFieldValue(DependencyProviderValidSettingName.BuildTeamProjectCollectionUrl, serviceSettings.GetSetting(ServiceValidSettings.DefaultTeamProjectCollection));

                    IDependency dep = new Dependency(sourceComp, comp, expectedVersion);
                    comp.AddPredecessor(dep);

                    Logger.Instance().Log(TraceLevel.Verbose, "{0}: Creating dependency graph edge {1}#{2} --> {3}#{4}", CommandType, sourceComp.Name, sourceComp.Version, comp.Name, expectedVersion);
                    Logger.Instance().Log(TraceLevel.Info, "{0}: Resolving BuildResult dependency {1}#{2} finished successfully", CommandType, compName, expectedVersion);

                    return dep;
                }

                Logger.Instance().Log(TraceLevel.Error, "{0}: Component {1} not found (Team Project Collection: {2})", CommandType, compName, tpc);
                throw new DependencyServiceException(string.Format("Component {0} not found (Team Project Collection: {1})", compName, tpc));
            }
            catch (UriFormatException ufe)
            {
                Logger.Instance().Log(TraceLevel.Error, "{0}: Error while processing BuildResult dependency {1}#{2}: {3}", CommandType, compName, expectedVersion, ufe.Message);
                throw new DependencyServiceException(string.Format("Invalid url to build server [Team Project Collection: {0}]", tpc));
            }
            catch (InvalidProviderConfigurationException ipce)
            {
                Logger.Instance().Log(TraceLevel.Error, "{0}: Error while processing BuildResult dependency {1}#{2}: {3}", CommandType, compName, expectedVersion, ipce.Message);
                throw new DependencyServiceException(string.Format("Invalid url to build server [Team Project Collection: {0}]", tpc));
            }
        }

        /// <summary>
        /// Handles a file share dependency by searching for sub dependencies, creating a new IComponent and IDependency objects and returning the created IDependency object.
        /// </summary>
        /// <param name="config">The configuration for file share.</param>
        /// <param name="sourceComp">The source component.</param>
        /// <param name="serviceSettings">The service settings.</param>
        /// <returns>The file share dependency.</returns>
        private IDependency HandleFileShareDependency(IDependencyProviderConfig config, IComponent sourceComp, ISettings<ServiceValidSettings> serviceSettings)
        {
            // Only used to temporarily calculate name and version to look for
            var tempComponent = new Component(config);
            var compName = tempComponent.Name;
            var expectedVersion = tempComponent.Version;
            var filesharePath = tempComponent.GetFieldValue(DependencyProviderValidSettingName.FileShareRootPath);

            Logger.Instance().Log(TraceLevel.Info, "{0}: Start FileShare dependency {1}#{2} processing ...", CommandType, compName, expectedVersion);

            var dependencyDefinitionFileNameList = serviceSettings.GetSetting(ServiceValidSettings.DefaultDependencyDefinitionFilename);
            if (string.IsNullOrEmpty(dependencyDefinitionFileNameList))
            {
                Logger.Instance().Log(TraceLevel.Error, "{0}: Dependency definition file list is not present for FileShare dependency {1}#{2}", CommandType, tempComponent.Name, tempComponent.Version);
                throw new DependencyServiceException("Invalid file share connection settings found (Dependency definition file list does not exist)");
            }

            // Resolve file share component
            try
            {
                ISettings<ResolverValidSettings> fileShareResolverSettings = new Settings<ResolverValidSettings>();
                fileShareResolverSettings.AddSetting(new KeyValuePair<ResolverValidSettings, string>(ResolverValidSettings.FileShareUrl, filesharePath));
                fileShareResolverSettings.AddSetting(new KeyValuePair<ResolverValidSettings, string>(ResolverValidSettings.DependencyDefinitionFileNameList, dependencyDefinitionFileNameList));

                var rt = DependencyResolverFactory.GetResolverType("Resolver_FileShare");
                var resolver = rt.CreateResolver(fileShareResolverSettings);

                if (resolver.ComponentExists(compName))
                {
                    bool existing;
                    IComponent comp;
                    IComponentVersion versionNumberUsed;

                    if (resolver.ComponentExists(compName, expectedVersion))
                    {
                        // Create the new component and add to the list of already created components
                        versionNumberUsed = expectedVersion;
                        comp = CreateGraphNodeOrReturnExisting(compName, expectedVersion, ComponentType.FileShare, config, out existing);
                    }
                    else
                    {
                        if (expectedVersion.Version.Equals("*"))
                        {
                            // Use highest version available
                            try
                            {
                                versionNumberUsed = resolver.GetAvailableVersions(compName).Last();
                                comp = CreateGraphNodeOrReturnExisting(compName, versionNumberUsed, ComponentType.FileShare, config, out existing);
                            }
                            catch (InvalidOperationException)
                            {
                                Logger.Instance().Log(TraceLevel.Error, "{0}: Component {1} was not found in expected version {2} on file share {3}", CommandType, compName, expectedVersion, filesharePath);
                                throw new DependencyServiceException(string.Format("Component {0} not found in expected version {1} [File share:{2}]", compName, expectedVersion, filesharePath));
                            }
                        }
                        else if (expectedVersion.Version.Contains("*"))
                        {
                            var allVersions = resolver.GetAvailableVersions(compName);
                            try
                            {
                                // ReSharper disable StringIndexOfIsCultureSpecific.1
                                var versionStringPart = expectedVersion.Version.Substring(0, expectedVersion.Version.IndexOf("*"));
                                // ReSharper restore StringIndexOfIsCultureSpecific.1
                                var availableVersions = allVersions.Where(x => x.Version.StartsWith(versionStringPart));
                                // ReSharper disable PossibleMultipleEnumeration
                                if (availableVersions.Any())
                                // ReSharper restore PossibleMultipleEnumeration
                                {
                                    // ReSharper disable PossibleMultipleEnumeration
                                    versionNumberUsed = availableVersions.Last();
                                    // ReSharper restore PossibleMultipleEnumeration
                                    comp = CreateGraphNodeOrReturnExisting(compName, versionNumberUsed, ComponentType.FileShare, config, out existing);
                                }
                                else
                                {
                                    Logger.Instance().Log(TraceLevel.Error, "{0}: Component {1} was not found in expected version {2} on file share {3}", CommandType, compName, expectedVersion, filesharePath);
                                    throw new DependencyServiceException(string.Format("Component {0} not found in expected version {1} [File share:{2}]", compName, expectedVersion, filesharePath));
                                }
                            }
                            catch (IndexOutOfRangeException ioore)
                            {
                                Logger.Instance().Log(TraceLevel.Error, "{0}: Error while parsing file share version {1} for component {2}: {3}", CommandType, expectedVersion, compName, ioore.Message);
                                throw new DependencyServiceException(string.Format("Could not parse file share dependency version {0} for component {1}", expectedVersion, compName));
                            }
                        }
                        else
                        {
                            Logger.Instance().Log(TraceLevel.Error, "{0}: Component {1} was not found in expected version {2} on file share {3}", CommandType, compName, expectedVersion, filesharePath);
                            throw new DependencyServiceException(string.Format("Component {0} not found in expected version {1} [File share: {2}]", compName, expectedVersion, filesharePath));
                        }
                    }

                    Logger.Instance().Log(TraceLevel.Info, "{0}: FileShare dependency {1}#{2} resolved to component {1}#{3} on file share {4}", CommandType, compName, expectedVersion, versionNumberUsed, filesharePath);
                    if (!_silentMode)
                    {
                        _logger.LogMsg(string.Format("* Found FileShare dependency: {0} ({1}) [File share:{2}]", compName, expectedVersion, filesharePath));
                    }

                    // Read component.targets from fileshare for component
                    try
                    {
                        // We can stop traversing if the component already exists in the tree
                        if (!existing)
                        {
                            var compTargetsXDoc = resolver.LoadComponentTarget(compName, versionNumberUsed);
                            var parser = new ParserXml();
                            var compTargets = (XmlComponent)parser.ReadDependencyFile(compTargetsXDoc);

                            Logger.Instance().Log(TraceLevel.Verbose, "{0}: Starting resolving dependencies for FileShare component {1}#{2} on file share {3} ...", CommandType, compName, versionNumberUsed, filesharePath);
                            foreach (var dependency in compTargets.Dependencies)
                            {
                                var succDep = HandleDependency(dependency, comp, serviceSettings);
                                comp.AddSuccessor(succDep);
                            }

                            Logger.Instance().Log(TraceLevel.Verbose, "{0}: Resolving dependencies for FileShare component {1}#{2} on file share {3} finished successfully", CommandType, compName, versionNumberUsed, filesharePath);
                        }
                    }
                    catch (DependencyServiceException dse)
                    {
                        Logger.Instance().Log(TraceLevel.Error, "{0}: Error while processing FileShare dependency {1}#{2}: {3}", CommandType, compName, expectedVersion, dse.Message);
                        throw new DependencyServiceException(dse.Message);
                    }
                    catch (InvalidComponentException ice)
                    {
                        Logger.Instance().Log(TraceLevel.Error, "{0}: Error while processing FileShare dependency {1}#{2}: {3}", CommandType, compName, expectedVersion, ice.Message);
                        throw new DependencyServiceException(string.Format("No {0} was found in {1}\\{2} [File share:{3}]", _dependencyDefinitionFileName, compName, versionNumberUsed, filesharePath));
                    }

                    // Create the dependency edge, add to own List as predecessor and return as successor edge for source component
                    var dep = new Dependency(sourceComp, comp, expectedVersion);
                    comp.AddPredecessor(dep);
                    Logger.Instance().Log(TraceLevel.Verbose, "{0}: Creating dependency graph edge {1}#{2} --> {3}#{4}", CommandType, sourceComp.Name, sourceComp.Version, comp.Name, expectedVersion);
                    Logger.Instance().Log(TraceLevel.Info, "{0}: Resolving FileShare dependency {1}#{2} finished successfully", CommandType, compName, expectedVersion);

                    return dep;
                }

                Logger.Instance().Log(TraceLevel.Error, "{0}: Component {1} was not found on file share {2}", CommandType, compName, filesharePath);
                throw new DependencyServiceException(string.Format("Component {0} not found on file share {1}", compName, filesharePath));
            }
            catch (UriFormatException uri)
            {
                Logger.Instance().Log(TraceLevel.Error, "{0}: Error while processing FileShare dependency {1}#{2}: {3}", CommandType, compName, expectedVersion, uri.Message);
                throw new DependencyServiceException(string.Format("Invalid url to file share [Url:{0}]", filesharePath), uri);
            }
            catch (InvalidProviderConfigurationException ipce)
            {
                Logger.Instance().Log(TraceLevel.Error, "{0}: Error while processing FileShare dependency {1}#{2}: {3}", CommandType, compName, expectedVersion, ipce.Message);
                throw new DependencyServiceException(string.Format("Invalid url to file share [Url:{0}]", filesharePath), ipce);
            }
        }

        /// <summary>
        /// Handles a source control dependency by searching for sub dependencies, creating a new IComponent and IDependency objects and returning the created IDependency object.
        /// </summary>
        /// <param name="resolverType">The resolver type.</param>
        /// <param name="dependencyType">The dependency type.</param>
        /// <param name="config">The configuration for source control.</param>
        /// <param name="sourceComp">The source component.</param>
        /// <param name="serviceSettings">The service settings to use.</param>
        /// <returns>The source control dependency.</returns>
        private IDependency HandleSourceControlDependency(string resolverType, string dependencyType, IDependencyProviderConfig config, IComponent sourceComp, ISettings<ServiceValidSettings> serviceSettings)
        {
            // Only used to temporarily calculate name and version to look for
            var tempComponent = new Component(config);
            var compName = tempComponent.Name;
            var expectedVersion = tempComponent.Version;

            Logger.Instance().Log(TraceLevel.Info, "{0}: Start {1} dependency {2}#{3} processing ...", CommandType, dependencyType, compName, expectedVersion);

            // Validate version control connection settings
            var tpc = serviceSettings.GetSetting(ServiceValidSettings.DefaultTeamProjectCollection);
            if (string.IsNullOrEmpty(tpc))
            {
                Logger.Instance().Log(TraceLevel.Error, "{0}: Team project collection url was not set for {1} dependency {2}#{3}", CommandType, dependencyType, compName, expectedVersion);
                throw new DependencyServiceException("Invalid source control connection description found (Version control address does not exist)");
            }

            var workspaceName = serviceSettings.GetSetting(ServiceValidSettings.DefaultWorkspaceName);
            if (string.IsNullOrEmpty(workspaceName))
            {
                Logger.Instance().Log(TraceLevel.Error, "{0}: Workspace name was not set for {1} dependency {2}#{3}", CommandType, dependencyType, compName, expectedVersion);
                throw new DependencyServiceException("Invalid source control connection description found (WorkspaceName does not exist)");
            }

            var workspaceOwner = serviceSettings.GetSetting(ServiceValidSettings.DefaultWorkspaceOwner);
            if (string.IsNullOrEmpty(workspaceOwner))
            {
                Logger.Instance().Log(TraceLevel.Error, "{0}: Workspace owner was not set for {1} dependency {2}#{3}", CommandType, dependencyType, compName, expectedVersion);
                throw new DependencyServiceException("Invalid source control connection description found (WorkspaceOwner does not exist)");
            }

            var dependencyDefinitionFileNameList = serviceSettings.GetSetting(ServiceValidSettings.DefaultDependencyDefinitionFilename);
            if (string.IsNullOrEmpty(dependencyDefinitionFileNameList))
            {
                Logger.Instance().Log(TraceLevel.Error, "{0}: Dependency definition file list is not present for {1} dependency {2}#{3}", CommandType, dependencyType, tempComponent.Name, tempComponent.Version);
                throw new DependencyServiceException("Invalid source control connection description found (Dependency definition file list does not exist)");
            }

            tempComponent.AddFallbackFieldValue(DependencyProviderValidSettingName.WorkspaceTeamProjectCollectionUrl, tpc);
            tempComponent.AddFallbackFieldValue(DependencyProviderValidSettingName.WorkspaceName, workspaceName);
            tempComponent.AddFallbackFieldValue(DependencyProviderValidSettingName.WorkspaceOwner, workspaceOwner);

            // Resolve source control component
            try
            {
                ISettings<ResolverValidSettings> sourceControlResolverSettings = new Settings<ResolverValidSettings>();
                sourceControlResolverSettings.AddSetting(new KeyValuePair<ResolverValidSettings, string>(ResolverValidSettings.TeamProjectCollectionUrl, tpc));
                sourceControlResolverSettings.AddSetting(new KeyValuePair<ResolverValidSettings, string>(ResolverValidSettings.WorkspaceName, workspaceName));
                sourceControlResolverSettings.AddSetting(new KeyValuePair<ResolverValidSettings, string>(ResolverValidSettings.WorkspaceOwner, workspaceOwner));
                sourceControlResolverSettings.AddSetting(new KeyValuePair<ResolverValidSettings, string>(ResolverValidSettings.DependencyDefinitionFileNameList, dependencyDefinitionFileNameList));

                var rt = DependencyResolverFactory.GetResolverType(resolverType);
                var resolver = rt.CreateResolver(sourceControlResolverSettings);

                if (resolver.ComponentExists(compName))
                {
                    bool existing;
                    IComponent comp;
                    IComponentVersion versionNumberUsed;

                    try
                    {
                        if (resolver.ComponentExists(compName, expectedVersion))
                        {
                            // Create the new component and add to the list of already created components
                            versionNumberUsed = expectedVersion;
                            comp = CreateGraphNodeOrReturnExisting(compName, expectedVersion, ComponentType.SourceControl, config, out existing);
                        }
                        else
                        {
                            Logger.Instance().Log(TraceLevel.Error, "{0}: Component {1} was not found in expected version {2} for team project collection {3}", CommandType, compName, expectedVersion, tpc);
                            throw new DependencyServiceException(string.Format("Component {0} not found in expected version {1} [Team Project Collection:{2}]", compName, expectedVersion, tpc));
                        }
                    }
                    catch (InvalidComponentException ice)
                    {
                        // Todo: MRI Do not hide exception informations
                        Logger.Instance().Log(TraceLevel.Error, "{0}: Error while processing {1} dependency {2}#{3}: {4}", CommandType, dependencyType, compName, expectedVersion, ice.Message);
                        throw new DependencyServiceException(ice.Message);
                    }

                    comp.AddFallbackFieldValue(DependencyProviderValidSettingName.WorkspaceTeamProjectCollectionUrl, tpc);
                    comp.AddFallbackFieldValue(DependencyProviderValidSettingName.WorkspaceName, workspaceName);
                    comp.AddFallbackFieldValue(DependencyProviderValidSettingName.WorkspaceOwner, workspaceOwner);

                    Logger.Instance().Log(TraceLevel.Info, "{0}: {1} dependency {2}#{3} resolved to component {2}#{4} on team project collection {5}", CommandType, dependencyType, compName, expectedVersion, versionNumberUsed, tpc);
                    if (!_silentMode)
                    {
                        _logger.LogMsg(string.Format("* Found {0} dependency: {1} ({2}) [Team Project Collection:{3}]", dependencyType, compName, expectedVersion, tpc));
                    }

                    // Read component.targets from source control for component
                    try
                    {
                        // We can stop traversing if the component is already afailable in the graph
                        if (!existing)
                        {
                            var compTargetsXDoc = resolver.LoadComponentTarget(compName, versionNumberUsed);
                            var parser = new ParserXml();
                            var compTargets = (XmlComponent)parser.ReadDependencyFile(compTargetsXDoc);

                            Logger.Instance().Log(TraceLevel.Verbose, "{0}: Starting resolving dependencies for {1} component {2}#{3} on team project collection {4} ...", CommandType, dependencyType, compName, versionNumberUsed, tpc);
                            foreach (var dependency in compTargets.Dependencies)
                            {
                                var succDep = HandleDependency(dependency, comp, serviceSettings);
                                comp.AddSuccessor(succDep);
                            }

                            Logger.Instance().Log(TraceLevel.Verbose, "{0}: Resolving dependencies for {1} component {2}#{3} on team project collection {4} finished successfully", CommandType, dependencyType, compName, versionNumberUsed, tpc);
                        }
                    }
                    catch (DependencyServiceException dse)
                    {
                        Logger.Instance().Log(TraceLevel.Error, "{0}: Error while processing {1} dependency {2}#{3}: {4}", CommandType, dependencyType, compName, expectedVersion, dse.Message);
                        throw new DependencyServiceException(dse.Message);
                    }
                    catch (InvalidComponentException ice)
                    {
                        Logger.Instance().Log(TraceLevel.Error, "{0}: Error while processing {1} dependency {2}#{3}: {4}", CommandType, dependencyType, compName, expectedVersion, ice.Message);
                        throw new DependencyServiceException(string.Format("No {0} was present for component {1} [Team Project Collection:{2}]", _dependencyDefinitionFileName, compName, tpc));
                    }

                    // Create the dependency edge, add to own List as predecessor and return as successor edge for source component
                    var dep = new Dependency(sourceComp, comp, expectedVersion);
                    comp.AddPredecessor(dep);

                    Logger.Instance().Log(TraceLevel.Verbose, "{0}: Creating dependency graph edge {1}#{2} --> {3}#{4}", CommandType, sourceComp.Name, sourceComp.Version, comp.Name, expectedVersion);
                    Logger.Instance().Log(TraceLevel.Info, "{0}: Resolving {1} dependency {2}#{3} finished successfully", CommandType, dependencyType, compName, expectedVersion);

                    return dep;
                }

                Logger.Instance().Log(TraceLevel.Error, "{0}: Component {1} was not found on version control server for team project collection {2}", CommandType, compName, tpc);
                throw new DependencyServiceException(string.Format("Component {0} not found on version control server for team project collection {1}", compName, tpc));
            }
            catch (UriFormatException ufe)
            {
                Logger.Instance().Log(TraceLevel.Error, "{0}: Error while processing {1} dependency {2}#{3}: {4}", CommandType, dependencyType, compName, expectedVersion, ufe.Message);
                throw new DependencyServiceException(string.Format("Invalid url specifying team project collection [Team project collection:{0}]", tpc));
            }
            catch (InvalidProviderConfigurationException ipce)
            {
                Logger.Instance().Log(TraceLevel.Error, "{0}: Error while processing {1} dependency {2}#{3}: {4}", CommandType, dependencyType, compName, expectedVersion, ipce.Message);
                throw new DependencyServiceException(string.Format("Invalid url specifying team project collection [Team project collection:{0}]", tpc));
            }
        }

        /// <summary>
        /// Handles a binary repository dependency by searching for sub dependencies, creating a new IComponent and IDependency objects and returning the created IDependency object.
        /// </summary>
        /// <param name="config">The configuration for binary repository.</param>
        /// <param name="sourceComp">The source component.</param>
        /// <param name="serviceSettings">The service settings.</param>
        /// <returns>The binary repository dependency.</returns>
        private IDependency HandleBinaryRepositoryDependency(IDependencyProviderConfig config, IComponent sourceComp, ISettings<ServiceValidSettings> serviceSettings)
        {
            // Only used to temporarily calculate name and version to look for
            var tempComponent = new Component(config);

            Logger.Instance().Log(TraceLevel.Info, "{0}: Start BinaryRepository dependency {1}#{2} processing ...", CommandType, tempComponent.Name, tempComponent.Version);

            var binaryTpc = serviceSettings.GetSetting(ServiceValidSettings.BinaryTeamProjectCollectionUrl);
            if (string.IsNullOrEmpty(binaryTpc))
            {
                Logger.Instance().Log(TraceLevel.Error, "{0}: Binary team project collection url was not set for BinaryRepository dependency {1}#{2}", CommandType, tempComponent.Name, tempComponent.Version);
                throw new DependencyServiceException("Invalid binary dependency connection settings found (Binary team project collection url does not exist)");
            }

            var repositoryTp = serviceSettings.GetSetting(ServiceValidSettings.BinaryRepositoryTeamProject);
            if (string.IsNullOrEmpty(repositoryTp))
            {
                Logger.Instance().Log(TraceLevel.Error, "{0}: Binary team project was not set for BinaryRepository dependency {1}#{2}", CommandType, tempComponent.Name, tempComponent.Version);
                throw new DependencyServiceException("Invalid binary dependency connection settings found (Repository team project name does not exist)");
            }

            tempComponent.AddFallbackFieldValue(DependencyProviderValidSettingName.BinaryTeamProjectCollectionUrl, binaryTpc);
            tempComponent.AddFallbackFieldValue(DependencyProviderValidSettingName.BinaryRepositoryTeamProject, repositoryTp);

            var dependencyDefinitionFileNameList = serviceSettings.GetSetting(ServiceValidSettings.DefaultDependencyDefinitionFilename);
            if (string.IsNullOrEmpty(dependencyDefinitionFileNameList))
            {
                Logger.Instance().Log(TraceLevel.Error, "{0}: Dependency definition file list is not present for BinaryRepository dependency {1}#{2}", CommandType, tempComponent.Name, tempComponent.Version);
                throw new DependencyServiceException("Invalid binary dependency connection settings found (Dependency definition file list does not exist)");
            }

            var workspaceName = serviceSettings.GetSetting(ServiceValidSettings.DefaultWorkspaceName);
            if (string.IsNullOrEmpty(workspaceName))
            {
                Logger.Instance().Log(TraceLevel.Error, "{0}: Workspace name was not set for BinaryRepository dependency {1}#{2}", CommandType, tempComponent.Name, tempComponent.Version);
                throw new DependencyServiceException("Invalid binary dependency connection settings found (Workspace name does not exist)");
            }

            var workspaceOwner = serviceSettings.GetSetting(ServiceValidSettings.DefaultWorkspaceOwner);
            if (string.IsNullOrEmpty(workspaceOwner))
            {
                Logger.Instance().Log(TraceLevel.Error, "{0}: Workspace owner was not set for BinaryRepository dependency {1}#{2}", CommandType, tempComponent.Name, tempComponent.Version);
                throw new DependencyServiceException("Invalid binary dependency connection settings found (Workspace owner name does not exist)");
            }

            tempComponent.AddFallbackFieldValue(DependencyProviderValidSettingName.WorkspaceName, workspaceName);
            tempComponent.AddFallbackFieldValue(DependencyProviderValidSettingName.WorkspaceOwner, workspaceOwner);

            try
            {
                var binaryRepositoryResolverSettings = new Settings<ResolverValidSettings>();
                binaryRepositoryResolverSettings.AddSetting(new KeyValuePair<ResolverValidSettings, string>(ResolverValidSettings.BinaryTeamProjectCollectionUrl, tempComponent.GetFieldValue(DependencyProviderValidSettingName.BinaryTeamProjectCollectionUrl)));
                binaryRepositoryResolverSettings.AddSetting(new KeyValuePair<ResolverValidSettings, string>(ResolverValidSettings.BinaryRepositoryTeamProject, tempComponent.GetFieldValue(DependencyProviderValidSettingName.BinaryRepositoryTeamProject)));
                binaryRepositoryResolverSettings.AddSetting(new KeyValuePair<ResolverValidSettings, string>(ResolverValidSettings.WorkspaceName, tempComponent.GetFieldValue(DependencyProviderValidSettingName.WorkspaceName)));
                binaryRepositoryResolverSettings.AddSetting(new KeyValuePair<ResolverValidSettings, string>(ResolverValidSettings.WorkspaceOwner, tempComponent.GetFieldValue(DependencyProviderValidSettingName.WorkspaceOwner)));
                binaryRepositoryResolverSettings.AddSetting(new KeyValuePair<ResolverValidSettings, string>(ResolverValidSettings.DependencyDefinitionFileNameList, dependencyDefinitionFileNameList));

                var rt = DependencyResolverFactory.GetResolverType("Resolver_BinaryRepository");
                var resolver = rt.CreateResolver(binaryRepositoryResolverSettings);

                if (resolver.ComponentExists(tempComponent.Name))
                {
                    bool existing;
                    IComponent comp;

                    // Read
                    try
                    {
                        if (resolver.ComponentExists(tempComponent.Name, tempComponent.Version))
                        {
                            // Create the new component and add to the list of already created components
                            comp = CreateGraphNodeOrReturnExisting(tempComponent.Name, tempComponent.Version, ComponentType.BinaryRepository, config, out existing);
                        }
                        else
                        {
                            Logger.Instance().Log(TraceLevel.Error, "{0}: Component {1} was not found in expected version {2} on binary team project collection {3} and project {4}", CommandType, tempComponent.Name, tempComponent.Version, binaryTpc, repositoryTp);
                            throw new DependencyServiceException(string.Format("Component {0} not found in expected version {1} [Team Project Collection:{2}, TeamProject:{3}]", tempComponent.Name, tempComponent.Version, binaryTpc, repositoryTp));
                        }
                    }
                    catch (InvalidComponentException ice)
                    {
                        // Todo: MRI Do not hide exception informations
                        Logger.Instance().Log(TraceLevel.Error, "{0}: Error while processing BinaryRepository dependency {1}#{2}: {3}", CommandType, tempComponent.Name, tempComponent.Version, ice.Message);
                        throw new DependencyServiceException(ice.Message);
                    }

                    comp.AddFallbackFieldValue(DependencyProviderValidSettingName.BinaryTeamProjectCollectionUrl, binaryTpc);
                    comp.AddFallbackFieldValue(DependencyProviderValidSettingName.BinaryRepositoryTeamProject, repositoryTp);
                    comp.AddFallbackFieldValue(DependencyProviderValidSettingName.WorkspaceName, workspaceName);
                    comp.AddFallbackFieldValue(DependencyProviderValidSettingName.WorkspaceOwner, workspaceOwner);

                    Logger.Instance().Log(TraceLevel.Info, "{0}: BinaryRepository dependency {1}#{2} resolved to component {1}#{3} on binary team project collection {4}", CommandType, comp.Name, tempComponent.Version, comp.Version, binaryTpc);
                    if (!_silentMode)
                    {
                        _logger.LogMsg(string.Format("* Found BinaryRepository dependency: {0} ({1}) [Team Project Collection:{2}]", comp.Name, comp.Version, binaryTpc));
                    }

                    try
                    {
                        // We can stop traversing because the item is already available in the tree. Simply update the edge later on
                        if (!existing)
                        {
                            var compTargetsXDoc = resolver.LoadComponentTarget(comp.Name, comp.Version);
                            var parser = new ParserXml();
                            var compTargets = (XmlComponent)parser.ReadDependencyFile(compTargetsXDoc);

                            Logger.Instance().Log(TraceLevel.Verbose, "{0}: Starting resolving dependencies for BinaryRepository component {1}#{2} on binary team project collection {3} ...", CommandType, comp.Name, comp.Version, binaryTpc);
                            foreach (var dependency in compTargets.Dependencies)
                            {
                                var succDep = HandleDependency(dependency, comp, serviceSettings);
                                comp.AddSuccessor(succDep);
                            }

                            Logger.Instance().Log(TraceLevel.Verbose, "{0}: Resolving dependencies for BinaryRepository component {1}#{2} on binary team project collection {3} finished successfully", CommandType, comp.Name, comp.Version, binaryTpc);
                        }
                    }
                    catch (InvalidComponentException ice)
                    {
                        // Todo: MRI Do not hide exception informations
                        Logger.Instance().Log(TraceLevel.Error, "{0}: Error while processing BinaryRepository dependency {1}#{2}: {3}", CommandType, tempComponent.Name, tempComponent.Version, ice.Message);
                        throw new DependencyServiceException(string.Format("No {0} was present for component {1} [Team Project Collection:{2}]", _dependencyDefinitionFileName, comp.Name, binaryTpc));
                    }

                    // Create the dependency edge, add to own List as predecessor and return as successor edge for source component
                    var dep = new Dependency(sourceComp, comp, comp.Version);
                    comp.AddPredecessor(dep);

                    Logger.Instance().Log(TraceLevel.Verbose, "{0}: Creating dependency graph edge {1}#{2} --> {3}#{4}", CommandType, sourceComp.Name, sourceComp.Version, comp.Name, comp.Version);
                    Logger.Instance().Log(TraceLevel.Info, "{0}: Resolving FileShare dependency {1}#{2} finished successfully", CommandType, tempComponent.Name, tempComponent.Version);

                    return dep;
                }

                Logger.Instance().Log(TraceLevel.Error, "{0}: Component {1} was not found on version control server for binary team project collection {2}", CommandType, tempComponent.Name, binaryTpc);
                throw new DependencyServiceException(string.Format("Component {0} not found on version control server for team project collection {1}", tempComponent.Name, binaryTpc));
            }
            catch (UriFormatException ufe)
            {
                Logger.Instance().Log(TraceLevel.Error, "{0}: Error while processing BinaryRepository dependency {1}#{2}: {3}", CommandType, tempComponent.Name, tempComponent.Version, ufe.Message);
                throw new DependencyServiceException(string.Format("Invalid url specifying team project collection [Team project collection:{0}]", binaryTpc));
            }
            catch (InvalidProviderConfigurationException ipce)
            {
                Logger.Instance().Log(TraceLevel.Error, "{0}: Error while processing BinaryRepository dependency {1}#{2}: {3}", CommandType, tempComponent.Name, tempComponent.Version, ipce.Message);
                throw new DependencyServiceException(ipce.Message);
            }
        }

        /// <summary>
        /// Creates the graph node or return existing.
        /// </summary>
        /// <param name="name">The component name.</param>
        /// <param name="version">The component version.</param>
        /// <param name="type">The component type.</param>
        /// <param name="config">The settings of the component (from xml).</param>
        /// <param name="existing">if set to <c>true</c> [existing].</param>
        /// <returns>Existing node or newly created node</returns>
        private IComponent CreateGraphNodeOrReturnExisting(IComponentName name, IComponentVersion version, ComponentType type, IDependencyProviderConfig config, out bool existing)
        {
            try
            {
                IComponent comp;
                existing = true;

                switch (type)
                {
                    case ComponentType.FileShare:
                        comp = _compList.SingleOrDefault(x => x.Type.Equals(ComponentType.FileShare) && x.Name.Path.Equals(name.Path) && x.Version.Version.Equals(version.Version));
                        break;
                    case ComponentType.VNextBuildResult:
                        comp = _compList.SingleOrDefault(x => x.Type.Equals(ComponentType.VNextBuildResult) && x.Name.TeamProject.Equals(name.TeamProject)
                                                && x.Name.BuildDefinition.Equals(name.BuildDefinition) && x.Version.BuildNumber.Equals(version.BuildNumber));
                        break;
                    case ComponentType.BuildResult:
                        comp = _compList.SingleOrDefault(x => x.Type.Equals(ComponentType.BuildResult) && x.Name.TeamProject.Equals(name.TeamProject)
                                                && x.Name.BuildDefinition.Equals(name.BuildDefinition) && x.Version.BuildNumber.Equals(version.BuildNumber));
                        break;
                    case ComponentType.SourceControl:
                        comp = _compList.SingleOrDefault(x => x.Type.Equals(ComponentType.SourceControl) && x.Name.Path.Equals(name.Path) && x.Version.TfsVersionSpec.Equals(version.TfsVersionSpec));
                        break;
                    case ComponentType.BinaryRepository:
                        comp = _compList.SingleOrDefault(x => x.Type.Equals(ComponentType.BinaryRepository) && x.Name.Path.Equals(name.Path) && x.Version.Version.Equals(version.Version));
                        break;
                    case ComponentType.Subversion:
                        comp = _compList.SingleOrDefault(x => x.Type.Equals(ComponentType.Subversion) && x.Name.Path.Equals(name.Path) && x.Version.Equals(version.Version));
                        break;
                    default:
                        {
                            Logger.Instance().Log(TraceLevel.Error, "{0}: Unsupported component type {1} found!", CommandType, type);
                            throw new DependencyServiceException(string.Format("Unsupported component type \"{0}\" found!", type));
                        }
                }

                if (comp == null)
                {
                    existing = false;
                    comp = new Component(version, config, new List<IDependency>(), new List<IDependency>());
                    Logger.Instance().Log(TraceLevel.Verbose, "{0}: Adding a new dependency graph node {1}#{2} to the dependency graph", CommandType, name, version);
                    _compList.Add(comp);
                }

                return comp;
            }
            catch (ArgumentNullException)
            {
                existing = false;
                Logger.Instance().Log(TraceLevel.Verbose, "{0}: Creating a new dependency graph node {1}#{2}", CommandType, name, version);
                return new Component(version, config, new List<IDependency>(), new List<IDependency>());
            }
            catch (InvalidOperationException ioe)
            {
                Logger.Instance().Log(TraceLevel.Error, "{0}: Error while creating a dependency graph node: {1}", CommandType, ioe.Message);
                throw new DependencyServiceException("Duplicate graph component found!");
            }
        }

        private IDependency HandleSubversionDependency(IDependencyProviderConfig config, IComponent sourceComp, ISettings<ServiceValidSettings> serviceSettings)
        {
            // Only used to temporarily calculate name and version to look for
            var tempComponent = new Component(config);
            var compName = tempComponent.Name;
            var expectedVersion = tempComponent.Version;
            var subversionRootPath = tempComponent.GetFieldValue(DependencyProviderValidSettingName.SubversionRootPath);

            Logger.Instance().Log(TraceLevel.Info, "{0}: Start Subversion dependency {1}#{2} processing ...", CommandType, compName, expectedVersion);

            var dependencyDefinitionFileNameList = serviceSettings.GetSetting(ServiceValidSettings.DefaultDependencyDefinitionFilename);
            if (string.IsNullOrEmpty(dependencyDefinitionFileNameList))
            {
                Logger.Instance().Log(TraceLevel.Error, "{0}: Dependency definition file list is not present for Subversion dependency {1}#{2}", CommandType, tempComponent.Name, tempComponent.Version);
                throw new DependencyServiceException("Invalid Subversion connection settings found (Dependency definition file list does not exist)");
            }

            // Resolve Subversion component
            try
            {
                ISettings<ResolverValidSettings> subversionResolverSettings = new Settings<ResolverValidSettings>();
                subversionResolverSettings.AddSetting(new KeyValuePair<ResolverValidSettings, string>(ResolverValidSettings.SubversionUrl, subversionRootPath));
                subversionResolverSettings.AddSetting(new KeyValuePair<ResolverValidSettings, string>(ResolverValidSettings.DependencyDefinitionFileNameList, dependencyDefinitionFileNameList));

                var rt = DependencyResolverFactory.GetResolverType("Resolver_Subversion");
                var resolver = rt.CreateResolver(subversionResolverSettings);

                if (resolver.ComponentExists(compName))
                {
                    bool existing;
                    IComponent comp;
                    IComponentVersion versionNumberUsed;

                    if (resolver.ComponentExists(compName, expectedVersion))
                    {
                        // Create the new component and add to the list of already created components
                        versionNumberUsed = expectedVersion;
                        comp = CreateGraphNodeOrReturnExisting(compName, expectedVersion, ComponentType.Subversion, config, out existing);
                    }
                    else
                    {
                        Logger.Instance().Log(TraceLevel.Error, "{0}: Component {1} was not found in expected version {2} on Subversion", CommandType, compName, expectedVersion);
                        throw new DependencyServiceException(string.Format("Component {0} not found in expected version {1} on Subversion", compName, expectedVersion));
                    }

                    Logger.Instance().Log(TraceLevel.Info, "{0}: Subversion dependency {1}#{2} resolved to component {1}#{3} on Subversion", CommandType, compName, expectedVersion, versionNumberUsed);
                    if (!_silentMode)
                    {
                        _logger.LogMsg(string.Format("* Found Subversion dependency: {0} ({1}) on Subversion", compName, expectedVersion));
                    }

                    // Read component.targets from Subversion for component
                    try
                    {
                        // We can stop traversing if the component already exists in the tree
                        if (!existing)
                        {
                            var compTargetsXDoc = resolver.LoadComponentTarget(compName, versionNumberUsed);
                            var parser = new ParserXml();
                            var compTargets = (XmlComponent)parser.ReadDependencyFile(compTargetsXDoc);

                            Logger.Instance().Log(TraceLevel.Verbose, "{0}: Starting resolving dependencies for Subversion component {1}#{2} on Subversion {3} ...", CommandType, compName, versionNumberUsed, subversionRootPath);
                            foreach (var dependency in compTargets.Dependencies)
                            {
                                var succDep = HandleDependency(dependency, comp, serviceSettings);
                                comp.AddSuccessor(succDep);
                            }

                            Logger.Instance().Log(TraceLevel.Verbose, "{0}: Resolving dependencies for Subversion component {1}#{2} on Subversion {3} finished successfully", CommandType, compName, versionNumberUsed, subversionRootPath);
                        }
                    }
                    catch (DependencyServiceException dse)
                    {
                        Logger.Instance().Log(TraceLevel.Error, "{0}: Error while processing Subversion dependency {1}#{2}: {3}", CommandType, compName, expectedVersion, dse.Message);
                        throw new DependencyServiceException(dse.Message);
                    }
                    catch (InvalidComponentException ice)
                    {
                        Logger.Instance().Log(TraceLevel.Error, "{0}: Error while processing Subversion dependency {1}#{2}: {3}", CommandType, compName, expectedVersion, ice.Message);
                        throw new DependencyServiceException(string.Format("No {0} was found in {1}\\{2} [Subversion:{3}]", _dependencyDefinitionFileName, compName, versionNumberUsed, subversionRootPath));
                    }

                    // Create the dependency edge, add to own List as predecessor and return as successor edge for source component
                    var dep = new Dependency(sourceComp, comp, expectedVersion);
                    comp.AddPredecessor(dep);
                    Logger.Instance().Log(TraceLevel.Verbose, "{0}: Creating dependency graph edge {1}#{2} --> {3}#{4}", CommandType, sourceComp.Name, sourceComp.Version, comp.Name, expectedVersion);
                    Logger.Instance().Log(TraceLevel.Info, "{0}: Resolving Subversion dependency {1}#{2} finished successfully", CommandType, compName, expectedVersion);

                    return dep;
                }

                Logger.Instance().Log(TraceLevel.Error, "{0}: Component {1} was not found on Subversion {2}", CommandType, compName, subversionRootPath);
                throw new DependencyServiceException(string.Format("Component {0} not found on Subversion {1}", compName, subversionRootPath));
            }
            catch (UriFormatException uri)
            {
                Logger.Instance().Log(TraceLevel.Error, "{0}: Error while processing Subversion dependency {1}#{2}: {3}", CommandType, compName, expectedVersion, uri.Message);
                throw new DependencyServiceException(string.Format("Invalid url to Subversion [Url:{0}]", subversionRootPath), uri);
            }
            catch (InvalidProviderConfigurationException ipce)
            {
                Logger.Instance().Log(TraceLevel.Error, "{0}: Error while processing Subversion dependency {1}#{2}: {3}", CommandType, compName, expectedVersion, ipce.Message);
                throw new DependencyServiceException(string.Format("Invalid url to Subversion [Url:{0}]", subversionRootPath), ipce);
            }
            catch (SvnAuthenticationException)
            {
                Logger.Instance().Log(TraceLevel.Error, "{0}: Unable to connect to repository {1}, because authentication failed.", CommandType, subversionRootPath);
                throw new InvalidProviderConfigurationException(string.Format("Could not connect to Subversion {0}, because authentication failed. Please login once at your your Subversion client to store the credentials locally.", subversionRootPath));
            }
        }
    }
}
