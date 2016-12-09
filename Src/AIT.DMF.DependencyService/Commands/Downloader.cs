// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Downloader.cs" company="AIT GmbH & Co. KG">
//   All rights reserved by AIT GmbH & Co. KG
// </copyright>
// <summary>
//   This class implements the downloader scheduler.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace AIT.DMF.DependencyService.Commands
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using Common;
    using Contracts.Exceptions;
    using Contracts.Graph;
    using Contracts.Gui;
    using Contracts.Parser;
    using Contracts.Provider;
    using Contracts.Services;
    using PluginFactory;
    using Microsoft.TeamFoundation.Build.Client;
    using Microsoft.TeamFoundation.Client;
    using Microsoft.TeamFoundation.VersionControl.Client;
    using Microsoft.TeamFoundation.VersionControl.Common;
    using Microsoft.Win32;
    using Microsoft.VisualStudio.Services.Client;
    using Microsoft.TeamFoundation.Build.WebApi;
    using System.IO.Compression;

    /// <summary>
    /// This class implements the downloader scheduler.
    /// </summary>
    internal class Downloader
    {
        #region Private Members

        /// <summary>
        /// The logger.
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// The default download folder.
        /// </summary>
        private readonly string _defaultDownloadFolder;

        /// <summary>
        /// The default relative output path.
        /// </summary>
        private readonly string _defaultRelativeOutputPath;

        /// <summary>
        /// Specifies if silent mode should be used.
        /// </summary>
        private readonly bool _silentMode;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="Downloader" /> with a standard downloadFolder, a logger and the logging mode.
        /// </summary>
        /// <param name="downloadFolder">Standard download folder</param>
        /// <param name="preconfRelativeOutputPath">Standard relative output path</param>
        /// <param name="logger">Logger object</param>
        /// <param name="silent">Log with logger or log to debugging</param>
        public Downloader(string downloadFolder, string preconfRelativeOutputPath, ILogger logger, bool silent)
        {
            _logger = logger;
            if (_logger == null)
            {
                throw new DependencyServiceException("! Could not download dependencies (Invalid setting for logging)");
            }

            _defaultDownloadFolder = downloadFolder;
            if (string.IsNullOrEmpty(_defaultDownloadFolder))
            {
                throw new DependencyServiceException("! Could not download dependencies (Invalid setting for default download folder)");
            }

            _defaultRelativeOutputPath = preconfRelativeOutputPath;
            if (_defaultRelativeOutputPath == null)
            {
                throw new DependencyServiceException("! Could not download dependencies (Invalid setting for default relative output path)");
            }

            _silentMode = silent;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the command type.
        /// </summary>
        public string CommandType
        {
            get
            {
                return "Downloader";
            }
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Downloads the successors in the dependency graph.
        /// </summary>
        /// <param name="graph">Node to start with</param>
        /// <param name="recursive">Indicates if the dependencies should be fetched recursively or not.</param>
        /// <param name="force">Indicates that we want to force a get operation and all files have to be overwritten</param>
        internal void Download(IGraph graph, bool recursive, bool force)
        {
            if (graph.RootComponent == null)
            {
                throw new DependencyServiceException("! Dependency graph does have an invalid root node!");
            }

            if (!_silentMode)
            {
                _logger.LogMsg("Downloading components...");
            }

            Logger.Instance().Log(TraceLevel.Info, "{0}: {1} downloading components {2} ...", CommandType, recursive ? "Recursively " : "Non-recursively ", force ? "(Forced Get mode) ..." : "(Normal mode)");

            // Retrieve watermark service for currently called dependency definition (root component)
            var wms = new DownloadWatermarkService(graph.RootComponentTargetPath);

            // Clean previously downloaded components that are not part of the list anymore (removed or version changed)
            var previousWatermarks = wms.GetStoredDependencyWatermarks();
            var currentComponentList = graph.GetFlattenedGraph(false, recursive);

            // Copy currentComponentList into new Tuple<string, string> list
            var localComponentsToKeep = currentComponentList.Select(component => new Tuple<string, string>(component.Name.GetName(), component.Version.GetVersion())).ToList();
            var cleaner = new Cleaner(_logger, false);

            foreach (var previousWatermark in previousWatermarks)
            {
                if (!wms.IsComponentInList(localComponentsToKeep, previousWatermark))
                {
                    // Revert component which is not used anymore
                    cleaner.RevertComponent(wms, previousWatermark.Item1, previousWatermark.Item2);
                }
            }

            // Download current components
            foreach (var component in currentComponentList)
            {
                DownloadComponent(wms, component, recursive, force);
            }

            #region # Output detected problematic dependencies
            if (graph.SideBySideDependencies.Count() > 0 || graph.CircularDependencies.Count() > 0)
            {
                if (!_silentMode)
                {
                    _logger.LogMsg("- The following dependency anomalies have been detected:");
                }
                else
                {
                    Debug.WriteLine("- The following dependency anomalies have been detected:");
                }

                if (graph.SideBySideDependencies.Count() > 0)
                {
                    if (!_silentMode)
                    {
                        _logger.LogMsg("  x Side-by-side dependency warning:");
                    }

                    Logger.Instance().Log(TraceLevel.Info, "{0}: Side-by-side dependencies detected:", CommandType);

                    foreach (var validationError in graph.SideBySideDependencies)
                    {
                        if (!_silentMode)
                        {
                            _logger.LogMsg("   * " + validationError.Message);
                        }

                        Logger.Instance().Log(TraceLevel.Info, "{0}: {1}", CommandType, validationError.Message);
                    }
                }
                if (graph.CircularDependencies.Count() > 0)
                {
                    if (!_silentMode)
                    {
                        _logger.LogMsg("  x Circular dependency warning:");
                    }

                    Logger.Instance().Log(TraceLevel.Info, "{0}: Circular dependencies detected:", CommandType);

                    foreach (var validationError in graph.CircularDependencies)
                    {
                        if (!_silentMode)
                        {
                            _logger.LogMsg("   * " + validationError.Message);
                        }

                        Logger.Instance().Log(TraceLevel.Info, "{0}: {1}", CommandType, validationError.Message);
                    }
                }
            }
            #endregion

            // Finish documents
            if (!_silentMode)
            {
                _logger.LogMsg("Downloaded components successfully!\n");
            }
            Logger.Instance().Log(TraceLevel.Info, "{0}: Downloaded components successfully!", CommandType);
        }

        #endregion

        /// <summary>
        /// Determines the source, target location and worker settings and initializes download workers according to their type.
        /// </summary>
        /// <param name="dws">The downloader wartermark service.</param>
        /// <param name="component">The component node in graph to start download from.</param>
        /// <param name="recursive">Indicates if the dependencies should be fetched recursively or not.</param>
        /// <param name="force">If set to <c>true</c> it indicates that the get operation is forced and all files have to be overwritten. Otherwise false.</param>
        private void DownloadComponent(DownloadWatermarkService dws, IComponent component, bool recursive, bool force)
        {
            var df = new DownloaderFactory();
            var componentAlreadyDownloaded = false;
            var settings = new Settings<DownloaderValidSettings>();
            IDependencyDownloader worker;
            string destLocation;
            string targetLocation;
            bool removeTempFiles = false;
            Logger.Instance().Log(TraceLevel.Info, "{0}: Downloading component {1}#{2} ...", CommandType, component.Name, component.Version);

            // Create worker and settings according to type
            if (component.Type.Equals(ComponentType.FileShare))
            {
                var fileShareRootPath = component.GetFieldValue(DependencyProviderValidSettingName.FileShareRootPath);
                destLocation = Path.Combine(fileShareRootPath, component.Name.Path, component.Version.Version);

                var relativeTargetPath = component.GetFieldValue(DependencyProviderValidSettingName.RelativeOutputPath);
                targetLocation =
                    relativeTargetPath == null
                    ? Path.GetFullPath(Path.Combine(_defaultDownloadFolder, _defaultRelativeOutputPath))
                    : Path.GetFullPath(Path.Combine(_defaultDownloadFolder, relativeTargetPath));

                LoadFilterSettings(component, settings);
            }
            else if (component.Type.Equals(ComponentType.VNextBuildResult))
            {
                var url = component.GetFieldValue(DependencyProviderValidSettingName.BuildTeamProjectCollectionUrl);
                if (!Uri.IsWellFormedUriString(url, UriKind.Absolute))
                {
                    Logger.Instance().Log(
                        TraceLevel.Error,
                        "{0}: Invalid BuildTeamProjectCollectionUrl setting '{1}' was found for component {2}#{3}",
                        CommandType,
                        url,
                        component.Name,
                        component.Version);
                    throw new DependencyServiceException(
                        string.Format(
                            "  ! Invalid download information was found for build result control component {0} (BuildTeamProjectCollectionUrl setting)",
                            component.Name.GetName()));
                }

                // Connect to tfs server
                var tpc = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(new Uri(url));
                tpc.EnsureAuthenticated();

                // Connect to version control service
                var versionControl = tpc.GetService<VersionControlServer>();
                if (versionControl == null)
                {
                    Logger.Instance().Log(TraceLevel.Error, "{0}: Connection to version control server failed for component {1}#{2}", CommandType, component.Name, component.Version);
                    throw new DependencyServiceException(string.Format("  ! Could not connect to TFS version control server for team project collection {0}", component.GetFieldValue(DependencyProviderValidSettingName.BuildTeamProjectCollectionUrl)));
                }

                var tpcUrl = new Uri(url);
                var connection = new VssConnection(tpcUrl, new VssClientCredentials(true));
                var client = connection.GetClient<BuildHttpClient>();

                var builds = client.GetBuildsAsync(project: component.Name.TeamProject, type: DefinitionType.Build).Result;
                var buildResult = builds.FirstOrDefault(r => r.BuildNumber == component.Version.BuildNumber);
                if (buildResult == null)
                {
                    Logger.Instance().Log(TraceLevel.Error, "{0}: No vnext build result could not be determined for component {1}#{2}", CommandType, component.Name, component.Version);
                    throw new DependencyServiceException(string.Format("  ! Could not determine build result component {0}", component.Name.GetName()));
                }

                var artifacts = client.GetArtifactsAsync(component.Name.TeamProject, buildResult.Id).Result;

                destLocation = Path.Combine(Path.GetTempPath(), "VNextDM_" + Guid.NewGuid());
                Directory.CreateDirectory(destLocation);
                removeTempFiles = true;
                foreach (var artifact in artifacts)
                {
                    if (artifact.Resource.Type == "FilePath")
                    {
                        var sourceDirName = $"{artifact.Resource.Data}/{artifact.Name}";
                        DirectoryCopy(sourceDirName, destLocation, true);
                    }
                    else
                    {
                        var content = client.GetArtifactContentZipAsync(component.Name.TeamProject, buildResult.Id, artifact.Name);
                        using (var zipArchive = new ZipArchive(content.Result))
                        {
                            zipArchive.ExtractToDirectory(destLocation);
                        }
                    }
                }

                var relativeTargetPath = component.GetFieldValue(DependencyProviderValidSettingName.RelativeOutputPath);
                targetLocation =
                    relativeTargetPath == null
                        ? Path.GetFullPath(Path.Combine(_defaultDownloadFolder, _defaultRelativeOutputPath))
                        : Path.GetFullPath(Path.Combine(_defaultDownloadFolder, relativeTargetPath));

                LoadFilterSettings(component, settings);
            }
            else if (component.Type.Equals(ComponentType.BuildResult))
            {
                if (!Uri.IsWellFormedUriString(component.GetFieldValue(DependencyProviderValidSettingName.BuildTeamProjectCollectionUrl), UriKind.Absolute))
                {
                    Logger.Instance().Log(
                        TraceLevel.Error,
                        "{0}: Invalid BuildTeamProjectCollectionUrl setting '{1}' was found for component {2}#{3}",
                        CommandType,
                        component.GetFieldValue(DependencyProviderValidSettingName.BuildTeamProjectCollectionUrl),
                        component.Name,
                        component.Version);
                    throw new DependencyServiceException(
                        string.Format(
                            "  ! Invalid download information was found for build result control component {0} (BuildTeamProjectCollectionUrl setting)",
                            component.Name.GetName()));
                }

                // Connect to tfs server
                var tpc = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(new Uri(component.GetFieldValue(DependencyProviderValidSettingName.BuildTeamProjectCollectionUrl)));
                tpc.EnsureAuthenticated();

                // Connect to version control service
                var versionControl = tpc.GetService<VersionControlServer>();
                if (versionControl == null)
                {
                    Logger.Instance().Log(TraceLevel.Error, "{0}: Connection to version control server failed for component {1}#{2}", CommandType, component.Name, component.Version);
                    throw new DependencyServiceException(string.Format("  ! Could not connect to TFS version control server for team project collection {0}", component.GetFieldValue(DependencyProviderValidSettingName.BuildTeamProjectCollectionUrl)));
                }

                // Connect to build server
                var buildServer = tpc.GetService<IBuildServer>();
                if (buildServer == null)
                {
                    Logger.Instance().Log(TraceLevel.Error, "{0}: Connection to build server failed for component {1}#{2}", CommandType, component.Name, component.Version);
                    throw new DependencyServiceException(string.Format("  ! Could not connect to TFS build server for team project collection {0}", component.GetFieldValue(DependencyProviderValidSettingName.BuildTeamProjectCollectionUrl)));
                }

                var buildDef = buildServer.GetBuildDefinition(component.Name.TeamProject, component.Name.BuildDefinition);
                var buildDetailSpec = buildServer.CreateBuildDetailSpec(buildDef);
                buildDetailSpec.BuildNumber = component.Version.BuildNumber;
                buildDetailSpec.InformationTypes = new string[] { };
                var buildResult = buildServer.QueryBuilds(buildDetailSpec);

                if (buildResult == null || buildResult.Builds == null || buildResult.Builds.Length == 0)
                {
                    Logger.Instance().Log(TraceLevel.Error, "{0}: No build result could not be determined for component {1}#{2}", CommandType, component.Name, component.Version);
                    throw new DependencyServiceException(string.Format("  ! Could not determine drop location for build result component {0}", component.Name.GetName()));
                }

                // Determine source location
                var dropLocation = buildResult.Builds[0].DropLocation; // TODO dga: Is this a bug? it returns localhost for the computer name while we are not running this code on the server (I guess)?
                if (string.IsNullOrEmpty(dropLocation))
                {
                    Logger.Instance().Log(TraceLevel.Error, "{0}: Drop location could not be determined for component {1}#{2}", CommandType, component.Name, component.Version);
                    throw new DependencyServiceException(string.Format("! Could not determine drop location for build result component {0}", component.Name.GetName()));
                }

                destLocation = dropLocation;
                var relativeTargetPath = component.GetFieldValue(DependencyProviderValidSettingName.RelativeOutputPath);
                targetLocation =
                    relativeTargetPath == null
                        ? Path.GetFullPath(Path.Combine(_defaultDownloadFolder, _defaultRelativeOutputPath))
                        : Path.GetFullPath(Path.Combine(_defaultDownloadFolder, relativeTargetPath));

                LoadFilterSettings(component, settings);
            }
            else if (component.Type.Equals(ComponentType.SourceControl))
            {
                if (!Uri.IsWellFormedUriString(component.GetFieldValue(DependencyProviderValidSettingName.WorkspaceTeamProjectCollectionUrl), UriKind.Absolute))
                {
                    Logger.Instance().Log(
                        TraceLevel.Error,
                        "{0}: Invalid WorkspaceTeamProjectCollectionUrl setting '{1}' was found for component {2}#{3}",
                        CommandType,
                        component.GetFieldValue(DependencyProviderValidSettingName.WorkspaceTeamProjectCollectionUrl),
                        component.Name,
                        component.Version);
                    throw new DependencyServiceException(
                        string.Format(
                            "  ! Invalid download information was found for source control component {0} (WorkspaceTeamProjectCollectionUrl setting)",
                            component.Name.GetName()));
                }

                var workspaceName = component.GetFieldValue(DependencyProviderValidSettingName.WorkspaceName);
                if (string.IsNullOrEmpty(workspaceName))
                {
                    Logger.Instance().Log(TraceLevel.Error, "{0}: WorkspaceName setting was not specified for component {1}#{2}", CommandType, component.Name, component.Version);
                    throw new DependencyServiceException(
                        string.Format(
                            "  ! Invalid download information was found for source control component {0} (WorkspaceName setting)",
                            component.Name.GetName()));
                }

                var workspaceOwner = component.GetFieldValue(DependencyProviderValidSettingName.WorkspaceOwner);
                if (string.IsNullOrEmpty(workspaceOwner))
                {
                    Logger.Instance().Log(TraceLevel.Error, "{0}: WorkspaceOwner setting was not specified for component {1}#{2}", CommandType, component.Name, component.Version);
                    throw new DependencyServiceException(
                        string.Format(
                            "  ! Invalid download information was found for source control component {0} (WorkspaceOwner setting)",
                            component.Name.GetName()));
                }

                destLocation = component.Name.ToString();
                settings.AddSetting(new KeyValuePair<DownloaderValidSettings, string>(DownloaderValidSettings.VersionString, component.Version.ToString()));

                var relativeTargetPath = component.GetFieldValue(DependencyProviderValidSettingName.RelativeOutputPath);
                targetLocation =
                    Path.GetFullPath(
                        relativeTargetPath == null
                            ? Path.Combine(_defaultDownloadFolder, "..", VersionControlPath.GetFileName(component.Name.ToString()))
                            : Path.Combine(_defaultDownloadFolder, relativeTargetPath));
            }
            else if (component.Type.Equals(ComponentType.SourceControlCopy))
            {
                destLocation = component.Name.ToString();
                settings.AddSetting(new KeyValuePair<DownloaderValidSettings, string>(DownloaderValidSettings.VersionString, component.Version.ToString()));

                var relativeTargetPath = component.GetFieldValue(DependencyProviderValidSettingName.RelativeOutputPath);
                targetLocation =
                    Path.GetFullPath(
                        relativeTargetPath == null
                            ? Path.Combine(_defaultDownloadFolder, "..", VersionControlPath.GetFileName(component.Name.ToString()))
                            : Path.Combine(_defaultDownloadFolder, relativeTargetPath));

                LoadFilterSettings(component, settings);
            }
            else if (component.Type.Equals(ComponentType.BinaryRepository))
            {
                var repositoryTeamProject = component.GetFieldValue(DependencyProviderValidSettingName.BinaryRepositoryTeamProject);
                if (string.IsNullOrEmpty(repositoryTeamProject))
                {
                    Logger.Instance().Log(TraceLevel.Error, "{0}: BinaryRepositoryTeamProject setting was not specified for component {1}#{2}", CommandType, component.Name, component.Version);
                    throw new DependencyServiceException(
                        string.Format(
                            "  ! Invalid download information was found for binary repository component {0} (BinaryRepositoryTeamProject setting)",
                            component.Name.GetName()));
                }

                destLocation = VersionControlPath.Combine(VersionControlPath.Combine(VersionControlPath.Combine(VersionControlPath.RootFolder, repositoryTeamProject), component.Name.GetName()), component.Version.GetVersion());
                settings.AddSetting(new KeyValuePair<DownloaderValidSettings, string>(DownloaderValidSettings.VersionString, VersionSpec.Latest.DisplayString));

                var relativeOutputPath = component.GetFieldValue(DependencyProviderValidSettingName.RelativeOutputPath);
                targetLocation = Path.GetFullPath(
                    relativeOutputPath == null
                        ? Path.Combine(_defaultDownloadFolder, _defaultRelativeOutputPath)
                        : Path.Combine(_defaultDownloadFolder, relativeOutputPath));

                LoadFilterSettings(component, settings);
            }
            else if (component.Type.Equals(ComponentType.Subversion))
            {
                destLocation = string.Format("{0}", component.Name);

                settings.AddSetting(new KeyValuePair<DownloaderValidSettings, string>(DownloaderValidSettings.VersionString, component.Version.ToString()));

                var relativeTargetPath = component.GetFieldValue(DependencyProviderValidSettingName.RelativeOutputPath);
                targetLocation =
                    relativeTargetPath == null
                    ? Path.GetFullPath(Path.Combine(_defaultDownloadFolder, _defaultRelativeOutputPath))
                    : Path.GetFullPath(Path.Combine(_defaultDownloadFolder, relativeTargetPath));

                LoadFilterSettings(component, settings);
            }
            else
            {
                Logger.Instance().Log(TraceLevel.Error, "{0}: Unknown dependency type '{1}' was found in dependency graph", CommandType, component.Type.GetType());
                throw new DependencyServiceException("  ! Invalid dependency node found in graph!");
            }

            //Determine Multi-Site path for BuildResult and FileShare-Provider

            if (component.Type.Equals(ComponentType.FileShare) || (component.Type.Equals(ComponentType.BuildResult)))
            {
                destLocation = DetermineMultiSiteSourceLocation(destLocation);
            }

            if (string.IsNullOrEmpty(destLocation))
            {
                Logger.Instance().Log(TraceLevel.Error, "{0}: Source location was not set for component {1}#{2}", CommandType, component.Name, component.Version);
                throw new DependencyServiceException(string.Format("  ! Error occured while preparing to download component {0} (Source location was not set)", component.Name.GetName()));
            }

            if (string.IsNullOrEmpty(targetLocation))
            {
                Logger.Instance().Log(TraceLevel.Error, "{0}: Target location was not set for component {1}#{2}", CommandType, component.Name, component.Version);
                throw new DependencyServiceException(string.Format("  ! Error occured while preparing to download component {0} (Destination location was not set)", component.Name.GetName()));
            }

            // Download files
            try
            {
                IDependencyDownloaderWatermark wm = null;
                try
                {
                    worker = df.GetDownloader(component);
                    wm = dws.Load(component.Name.GetName(), component.Version.GetVersion()) ?? new DownloaderWatermark(worker);
                    worker.Download(destLocation, targetLocation, wm, force, settings);
                }
                finally
                {
                    if (null != wm)
                    {
                        dws.Save(wm, component.Name.GetName(), component.Version.GetVersion());
                    }
                    if (removeTempFiles)
                    {
                        Directory.Delete(destLocation, true);
                    }
                }
            }
            catch (Exception e)
            {
                if (e is DependencyServiceException)
                {
                    throw;
                }
                // ReSharper disable RedundantIfElseBlock
                else if (e is ComponentAlreadyDownloadedException)
                // ReSharper restore RedundantIfElseBlock
                {
                    componentAlreadyDownloaded = true;
                }
                else
                {
                    Logger.Instance().Log(TraceLevel.Error, "{0}: Exception {1} occured while downloading files: {2}", CommandType, e, e.Message);
                    throw new DependencyServiceException(string.Format("  ! Download of component {0} failed ({1})", component.Name.GetName(), e.Message));
                }
            }

            // Log download
            if (!_silentMode)
            {
                _logger.LogMsg(
                    !componentAlreadyDownloaded
                        ? string.Format(
                            "  * Component {0} (Version:{1}) downloaded to target directory {2}",
                            component.Name.GetName(),
                            component.Version.GetVersion(),
                            targetLocation)
                        : string.Format(
                            "  * Skipped component {0} (Version:{1}). Already present in target directory {2}",
                            component.Name.GetName(),
                            component.Version.GetVersion(),
                            targetLocation));
            }

            Logger.Instance().Log(
                TraceLevel.Info,
                !componentAlreadyDownloaded
                    ? "{0}: Component {1}#{2} successfully downloaded to target directory {3}"
                    : "{0}: Component {1}#{2} download was skipped. Already present in target directory {3}",
                CommandType,
                component.Name,
                component.Version,
                targetLocation);
        }

        private void LoadFilterSettings(IComponent component, Settings<DownloaderValidSettings> settings)
        {
            if (!string.IsNullOrEmpty(component.GetFieldValue(DependencyProviderValidSettingName.IncludeFilter)))
            {
                var value = component.GetFieldValue(DependencyProviderValidSettingName.IncludeFilter);
                var pair = new KeyValuePair<DownloaderValidSettings, string>(DownloaderValidSettings.IncludedFilesFilter, value);
                settings.AddSetting(pair);
            }

            if (!string.IsNullOrEmpty(component.GetFieldValue(DependencyProviderValidSettingName.ExcludeFilter)))
            {
                var value = component.GetFieldValue(DependencyProviderValidSettingName.ExcludeFilter);
                var pair = new KeyValuePair<DownloaderValidSettings, string>(DownloaderValidSettings.ExcludedFilesFilter, value);
                settings.AddSetting(pair);
            }

            if (!string.IsNullOrEmpty(component.GetFieldValue(DependencyProviderValidSettingName.IncludeFoldersFilter)))
            {
                var value = component.GetFieldValue(DependencyProviderValidSettingName.IncludeFoldersFilter);
                var pair = new KeyValuePair<DownloaderValidSettings, string>(DownloaderValidSettings.IncludedFoldersFilter, value);
                settings.AddSetting(pair);
            }

            if (!string.IsNullOrEmpty(component.GetFieldValue(DependencyProviderValidSettingName.ExcludeFoldersFilter)))
            {
                var value = component.GetFieldValue(DependencyProviderValidSettingName.ExcludeFoldersFilter);
                var pair = new KeyValuePair<DownloaderValidSettings, string>(DownloaderValidSettings.ExcludedFoldersFilter, value);
                settings.AddSetting(pair);
            }

            if (!string.IsNullOrEmpty(component.GetFieldValue(DependencyProviderValidSettingName.FolderMappings)))
            {
                var value = component.GetFieldValue(DependencyProviderValidSettingName.FolderMappings);
                var pair = new KeyValuePair<DownloaderValidSettings, string>(DownloaderValidSettings.FolderMappings, value);
                settings.AddSetting(pair);
            }
        }

        /// <summary>
        /// Determines the used domain based on activated Multi-Site-Feature.
        /// </summary>
        /// <param name="sourceLocation"></param>
        /// <returns></returns>
        private string DetermineMultiSiteSourceLocation(string sourceLocation)
        {
            var multiSiteConfig = string.Empty;

            if ((DependencyManagerSettings.Instance.IsMultiSiteAllowed) && (DependencyManagerSettings.Instance.MultiSiteList.Count() > 0))
            {
                var currentSite = string.Empty;

                if (ApplicationSettings.Instance.SelectedMultiSiteEntry == ApplicationSettings.AutomaticSite)
                {
                    var originalSite = GetAdSite();

                    if (string.IsNullOrEmpty(originalSite))
                    {
                        return sourceLocation;
                    }

                    currentSite = originalSite;
                }
                else
                {
                    currentSite = ApplicationSettings.Instance.SelectedMultiSiteEntry;
                }

                multiSiteConfig = DependencyManagerSettings.Instance.MultiSiteList.FirstOrDefault(entry => entry.ToLower().StartsWith(currentSite, StringComparison.OrdinalIgnoreCase));

                if (multiSiteConfig != null)
                {
                    var tmp = multiSiteConfig.Split(new[] { ',' });

                    // Replace basepath with replacepath by using lower case
                    sourceLocation = sourceLocation.ToLower().Replace(tmp[1].ToLower(), tmp[2].ToLower());

                    if (!_silentMode)
                    {
                        if (ApplicationSettings.Instance.SelectedMultiSiteEntry == ApplicationSettings.AutomaticSite)
                        {
                            _logger.LogMsg(string.Format("  * Mutli Site mapping is activated. AD-Site is determined automatically, used AD-Site is '{0}'. Using '{1}' as source directory instead of {2}'.", tmp[0], tmp[2], tmp[1]));
                        }
                        else
                        {
                            _logger.LogMsg(string.Format("  * Mutli Site mapping is activated. Defined AD-Site was '{0}'. Using '{1}' as source directory instead of {2}'.", tmp[0], tmp[2], tmp[1]));
                        }
                    }
                }
            }

            if ((multiSiteConfig == null) || ((DependencyManagerSettings.Instance.IsMultiSiteAllowed) && (DependencyManagerSettings.Instance.MultiSiteList.Count() == 0)))
            {
                if (!_silentMode)
                {
                    _logger.LogMsg(string.Format("  ! Multi Site mapping is activated, but no entry matches. Defined AD-Site was '{0}'. Path replacement will not be used.", ApplicationSettings.Instance.SelectedMultiSiteEntry));
                }

                Logger.Instance().Log(TraceLevel.Error, "{0}: Multi Site mapping is activated, but no entry matches.  Defined AD-Site was '{1}'. Path replacement will not be used.", CommandType, ApplicationSettings.Instance.SelectedMultiSiteEntry);
            }

            return sourceLocation;
        }


        private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();
            // If the destination directory doesn't exist, create it.
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, false);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath, copySubDirs);
                }
            }
        }

        /// <summary>
        /// Returns the AD-site of the computer.
        /// The first approach works only at customers infrastructure (e.g. Sivantos) and not inside AIT domain. Therefore the second
        /// approach is used.
        /// </summary>
        /// <returns></returns>
        string GetAdSite()
        {
            var exMsg = string.Empty;

            try
            {
                return System.DirectoryServices.ActiveDirectory.ActiveDirectorySite.GetComputerSite().Name;
            }
            catch (Exception ex)
            {
                exMsg = ex.Message;
            }

            var adSite = Registry.GetValue(Registry.LocalMachine + @"\SYSTEM\CurrentControlSet\services\Netlogon\Parameters", "DynamicSiteName", null) as string;
            if (!string.IsNullOrEmpty(adSite))
            {
                return adSite.Replace("\0", string.Empty);
            }
            else
            {
                _logger.LogMsg(string.Format("  ! Mutli Site mapping is set to <Automatic>, but current AD-Site could not be determined. Path replacement will not be used. Error: {0}", exMsg));
                _logger.LogMsg(string.Format("  ! Mutli Site mapping is set to <Automatic>, but current AD-Site could not be determined. Path replacement will not be used. Error: Expected Windows Registry entry does not exist."));
            }

            return string.Empty;
        }
    }
}
