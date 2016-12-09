// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DownloaderSourceControlMapping.cs" company="AIT GmbH & Co. KG">
//   All rights reserved by AIT GmbH & Co. KG
// </copyright>
// <summary>
//   Defines the DownloaderSourceControlMapping type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace AIT.DMF.Plugins.Downloader.SourceControlMapping
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using Common;
    using Contracts.Common;
    using Contracts.Exceptions;
    using Contracts.Provider;
    using Contracts.Services;
    using Microsoft.TeamFoundation.Client;
    using Microsoft.TeamFoundation.VersionControl.Client;

    /// <summary>
    /// The downloader source control mapping.
    /// </summary>
    public class DownloaderSourceControlMapping : IDependencyDownloader
    {
        #region Private Members

        /// <summary>
        /// The incremental get options.
        /// </summary>
        private const GetOptions IncrementalGetOptions = GetOptions.Overwrite | GetOptions.Remap;

        /// <summary>
        /// The force get options.
        /// </summary>
        private const GetOptions ForceGetOptions = GetOptions.GetAll;

        /// <summary>
        /// The workspace.
        /// </summary>
        private readonly Workspace _workspace;

        /// <summary>
        /// The version control server.
        /// </summary>
        private readonly VersionControlServer _vcs;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="DownloaderSourceControlMapping"/> class.
        /// </summary>
        /// <param name="collectionUri">The team project collection uri.</param>
        /// <param name="workspaceName">The workspace name.</param>
        /// <param name="workspaceOwner">The workspace owner.</param>
        public DownloaderSourceControlMapping(string collectionUri, string workspaceName, string workspaceOwner)
        {
            var tpc = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(new Uri(collectionUri));
            tpc.EnsureAuthenticated();

            _vcs = tpc.GetService<VersionControlServer>();
            if (_vcs == null)
            {
                throw new InvalidProviderConfigurationException(
                    string.Format("Could not connect to version control on server {0}", collectionUri));
            }

            Workstation.Current.EnsureUpdateWorkspaceInfoCache(_vcs, ".", new TimeSpan(0, 1, 0));
            _workspace = _vcs.GetWorkspace(workspaceName, workspaceOwner);
            if (_workspace == null)
            {
                throw new InvalidProviderConfigurationException(
                    string.Format(
                        "Could not fetch workspace information for workspace '{0};{1}'from tfs server {2}",
                        workspaceName,
                        workspaceOwner,
                        collectionUri));
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DownloaderSourceControlMapping"/> class.
        /// </summary>
        public DownloaderSourceControlMapping()
        {
            // TODO: Clean uses empty object here -> we should check this for null but the general approach for this is crappy. This object is useless and should be dropped at all.
        }

        #endregion

        #region IDependencyDownloader

        /// <summary>
        /// Gets the download type of the source control downloader.
        /// </summary>
        public string DownloadType
        {
            get { return "Downloader_SourceControlMapping"; }
        }

        /// <summary>
        /// Downloads a component from source control to a local path.
        /// </summary>
        /// <param name="source">Path to source location</param>
        /// <param name="destination">Path to destination folder</param>
        /// <param name="watermark">The watermark to adapt for clean support</param>
        /// <param name="force">Indicates that we want to force a get operation and all files have to be overwritten</param>
        /// <param name="settings">Setting which contain the version to fetch</param>
        public void Download(string source, string destination, IDependencyDownloaderWatermark watermark, bool force, ISettings<DownloaderValidSettings> settings)
        {
            if (string.IsNullOrEmpty(source))
            {
                throw new ArgumentNullException(
                    "source", "Source control path to component folder was empty in DownloaderSourceControl");
            }

            if (string.IsNullOrEmpty(destination))
            {
                throw new ArgumentNullException(
                    "destination", "Destination path for component was empty in DownloaderSourceControl");
            }

            if (null == watermark)
            {
                throw new ArgumentNullException("watermark");
            }

            if (null == settings)
            {
                throw new ArgumentNullException("settings");
            }

            var versionString = settings.GetSetting(DownloaderValidSettings.VersionString);
            if (string.IsNullOrEmpty(versionString))
            {
                throw new InvalidComponentException(
                    "VersionSpec string for component was empty in DownloaderSourceControl");
            }

            var version = VersionSpec.ParseSingleSpec(versionString, _workspace.OwnerName);
            if (version == null)
            {
                throw new InvalidComponentException("VersionSpec for component was invalid in DownloaderSourceControl");
            }

            // Adapt workspace mapping
            // The mapping we want to end up with: source -> destination

            // The currently active workspace mappings targeting our local root paths
            var workspaceMappings = _workspace.Folders.Where(item => item.LocalItem != null) // ignore cloaked
                .Where(mapping => mapping.LocalItem.StartsWith(destination, StringComparison.OrdinalIgnoreCase));

            // Items mapped in earlier runs of this tool, maybe configuration has changed or items no longer belong to the dependencies.
            var outdatedMappings =
                workspaceMappings.Where(
                    mapping => !source.Equals(mapping.ServerItem, StringComparison.OrdinalIgnoreCase));

            // Map and Get operations

            // Remove outdated mappings
            foreach (var mapping in outdatedMappings)
            {
                _workspace.DeleteMapping(mapping);
                Logger.Instance().Log(TraceLevel.Verbose, "{0}: Deleted old workspace folder mapping {1} -> {2}", DownloadType, mapping.ServerItem, mapping.LocalItem);
            }

            // Ensure desired mappings are mapped.
            var localPath = _workspace.TryGetLocalItemForServerItem(source);
            if (!string.Equals(localPath, destination))
            {
                _workspace.Map(source, destination);
                Logger.Instance().Log(TraceLevel.Verbose, "{0}: Created workspace folder mapping {1} -> {2}", DownloadType, source, destination);
            }

            // Build a get request for every mapping to ensure we have the desired version. Always need to do a full get since the mapping might
            // not have changed but only the label, or we map to latest version and there is a newer version available.
            var getRequest = new GetRequest(source, RecursionType.Full, version);

            try
            {
                _vcs.Getting += GettingHandler;
                _workspace.Get(getRequest, force ? ForceGetOptions : IncrementalGetOptions);
                watermark.ArtifactsToClean.Add(destination);
            }
            finally
            {
                _vcs.Getting -= GettingHandler;
            }
        }

        /// <summary>
        /// This will remove any previously downloaded local folders, so in case a mapping was changed,
        /// the clean will remove the previously mapped folders also.
        /// The currently active mapping can be retrieved using the watermark (server -> local path)
        /// </summary>
        /// <param name="watermark">The watermark to use.</param>
        public void RevertDownload(IDependencyDownloaderWatermark watermark)
        {
            if (null == watermark)
            {
                throw new ArgumentNullException("watermark");
            }

            Logger.Instance().Log(TraceLevel.Info, "{0}: Reverting download via watermark ...", DownloadType);

            var itemsToClean = watermark.ArtifactsToClean.OrderByDescending(x => x.Length).ToList();

            foreach (var localFolder in itemsToClean)
            {
                // Check local folder
                var di = new DirectoryInfo(localFolder);

                if (!di.Exists)
                {
                    continue;
                }

                // Determine workspace
                var wi = Workstation.Current.GetLocalWorkspaceInfo(localFolder);

                if (wi == null)
                {
                    continue;
                }

                var localCollection = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(wi.ServerUri);
                var localWorkspace = wi.GetWorkspace(localCollection);
                var localVcs = localWorkspace.VersionControlServer;

                // Clean
                // ReSharper disable AccessToForEachVariableInClosure
                var mappingsToRemove = localWorkspace.Folders.Where(item => item.LocalItem != null) // ignore cloaked
                    .Where(mapping => mapping.LocalItem.StartsWith(localFolder, StringComparison.OrdinalIgnoreCase));
                // ReSharper restore AccessToForEachVariableInClosure

                // Remove outdated mappings
                foreach (var mapping in mappingsToRemove)
                {
                    // Get version C1 in order to remove all downloaded files without edits (Conflict resolution: "Keep Local Version")
                    var getRequest = new GetRequest(mapping.ServerItem, RecursionType.Full, new ChangesetVersionSpec(1));
                    localVcs.Getting += RemovingHandler;
                    localWorkspace.Get(getRequest, GetOptions.None);
                    localVcs.Getting -= RemovingHandler;

                    // Remove from workspace
                    localWorkspace.DeleteMapping(mapping);
                    Logger.Instance().Log(TraceLevel.Verbose, "{0}: Removed workspace folder mapping {1} -> {2}", DownloadType, mapping.ServerItem, mapping.LocalItem);
                    watermark.ArtifactsToClean.Remove(localFolder);
                }
            }
        }

        /// <summary>
        /// Handler to handle the get operation result.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event args.</param>
        protected void GettingHandler(object sender, GettingEventArgs e)
        {
            // We don't need to adapt watermark here on file level since TFS Map and Get operations do it all for us

            // Log fetched files
            if (e.ItemType == ItemType.Folder)
            {
                Logger.Instance().Log(TraceLevel.Verbose, "{0}: Downloading folder {1}", DownloadType, e.ServerItem);
            }
            else if (e.ItemType == ItemType.File)
            {
                Logger.Instance().Log(TraceLevel.Verbose, "{0}: Downloading file {1}", DownloadType, e.ServerItem);
            }
        }

        /// <summary>
        /// Handler to handle the get operation result during removal.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event args.</param>
        protected void RemovingHandler(object sender, GettingEventArgs e)
        {
            // Log fetched files
            if (e.ItemType == ItemType.Folder)
            {
                Logger.Instance().Log(TraceLevel.Verbose, "{0}: Deleted empty folder {1}", DownloadType, e.SourceLocalItem);
            }
            else if (e.ItemType == ItemType.File)
            {
                Logger.Instance().Log(TraceLevel.Verbose, "{0}: Deleted file {1}", DownloadType, e.SourceLocalItem);
            }
        }

        #endregion
    }
}
