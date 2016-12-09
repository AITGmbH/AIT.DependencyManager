// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DownloaderSourceControlCopy.cs" company="AIT GmbH & Co. KG">
//   All rights reserved by AIT GmbH & Co. KG
// </copyright>
// <summary>
//   Defines the DownloaderSourceControlCopy type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace AIT.DMF.Plugins.Downloader.SourceControlCopy
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using Common;
    using Contracts.Common;
    using Contracts.Exceptions;
    using Contracts.Provider;
    using Contracts.Services;
    using Microsoft.TeamFoundation.Client;
    using Microsoft.TeamFoundation.VersionControl.Client;
    using Microsoft.TeamFoundation.VersionControl.Common;

    /// <summary>
    /// The downloader for a SourceControl/SourceControlMapping dependency. A workspace mapping is created and
    /// a get operation is triggered for all files and folder to get them into the target destination.
    /// </summary>
    public class DownloaderSourceControlCopy : IDependencyDownloader
    {
        #region Private Members

        /// <summary>
        /// The version control server.
        /// </summary>
        private readonly VersionControlServer _vcs;

        /// <summary>
        /// The workspace.
        /// </summary>
        private readonly Workspace _workspace;

        /// <summary>
        /// The mapping regex match.
        /// </summary>
        private readonly Regex _mappingRegexMatch = new Regex("sourceoffset=([^/:*?\"<>|]+),localoffset=([^/:*?\"<>|]*)", RegexOptions.IgnoreCase);

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="DownloaderSourceControlCopy"/> class and ensures it is connected to the team project collection uri.
        /// </summary>
        /// <param name="collectionUri">The collection URI.</param>
        /// <param name="workspaceName">The name of the workspace.</param>
        /// <param name="workspaceOwner">The workspace owner.</param>
        public DownloaderSourceControlCopy(string collectionUri, string workspaceName, string workspaceOwner)
        {
            if (collectionUri == null)
            {
                throw new InvalidProviderConfigurationException(
                    "Could not connect to version control on server (No collection url was provided)");
            }

            var tpc = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(new Uri(collectionUri));
            tpc.EnsureAuthenticated();

            _vcs = tpc.GetService<VersionControlServer>();
            if (_vcs == null)
            {
                throw new InvalidProviderConfigurationException(
                    string.Format("Could not connect to version control on server {0}", collectionUri));
            }

            if (string.IsNullOrEmpty(workspaceName))
            {
                throw new InvalidProviderConfigurationException(
                    string.Format(
                        "Could not fetch workspace information for workspace from tfs server {0} (No workspace name was provided)",
                        collectionUri));
            }

            if (string.IsNullOrEmpty(workspaceOwner))
            {
                throw new InvalidProviderConfigurationException(
                    string.Format(
                        "Could not fetch workspace information for workspace from tfs server {0} (No workspace owner was provided)",
                        collectionUri));
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
        /// Initializes a new instance of the <see cref="DownloaderSourceControlCopy"/> class which is used for Cleaner implementation.
        /// </summary>
        public DownloaderSourceControlCopy()
        {
            // TODO: Clean uses empty object here -> we should check this for null but the general approach for this is crappy. This object is useless and should be dropped at all.
        }

        #endregion

        #region IDependencyDownloader

        /// <summary>
        /// Gets the download type of the binary repository downloader.
        /// </summary>
        public string DownloadType
        {
            get
            {
                return "Downloader_SourceControlCopy";
            }
        }

        /// <summary>
        /// Downloads a component from source control to a local path.
        /// </summary>
        /// <param name="source">Path to source location</param>
        /// <param name="destination">Path to destination folder</param>
        /// <param name="watermark">The watermark can be used to perform incremental updates and cleanup operations</param>
        /// <param name="force">Indicates that we want to force a get operation and all files have to be overwritten</param>
        /// <param name="settings">Setting which contain the version to fetch</param>
        public void Download(string source, string destination, IDependencyDownloaderWatermark watermark, bool force, ISettings<DownloaderValidSettings> settings)
        {
            if (string.IsNullOrEmpty(source))
            {
                throw new InvalidComponentException(
                    "Source control path to component folder was empty in DownloaderSourceControlCopy");
            }

            if (string.IsNullOrEmpty(destination))
            {
                throw new InvalidComponentException(
                    "Destination path for component was empty in DownloaderSourceControlCopy");
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
                    "Version string for component was empty in DownloaderSourceControlCopy");
            }

            var folderMappings = InitializeMappings(settings.GetSetting(DownloaderValidSettings.FolderMappings));
            var includeFilter = InitializeIncludeFilter(settings.GetSetting(DownloaderValidSettings.IncludedFilesFilter));
            var excludeFilter = InitializeExcludeFilter(settings.GetSetting(DownloaderValidSettings.ExcludedFilesFilter));

            // ReSharper disable PossibleMultipleEnumeration
            if (folderMappings.Any())
            // ReSharper restore PossibleMultipleEnumeration
            {
                // ReSharper disable PossibleMultipleEnumeration
                foreach (var mapping in folderMappings)
                // ReSharper restore PossibleMultipleEnumeration
                {
                    var subSource = VersionControlPath.Combine(source, mapping.Key);
                    var subDest = Path.Combine(destination, mapping.Value);
                    Logger.Instance().Log(TraceLevel.Info, "{0}: Downloading component" +
                                                           " subfolder {1} with source control version '{2}' to {3}", DownloadType, mapping.Key, versionString, subDest);
                    DownloadFolder(source, subSource, subDest, versionString, includeFilter, excludeFilter, watermark, force);
                }

                Logger.Instance().Log(TraceLevel.Info, "{0}: Component {1} download finished successfully", DownloadType, source);
            }
            else
            {
                Logger.Instance().Log(TraceLevel.Info, "{0}: Downloading component {1} with source control version '{2}' to {3}", DownloadType, source, versionString, destination);
                DownloadFolder(source, source, destination, versionString, includeFilter, excludeFilter, watermark, force);
                Logger.Instance().Log(TraceLevel.Info, "{0}: Component {1} download finished successfully", DownloadType, source);
            }
        }

        /// <summary>
        /// Performs a revert operation by removing all the files which have been downloaded previously. The watermark write during the Download will be provided
        /// </summary>
        /// <param name="watermark">The watermark that has been used for the download operation</param>
        public void RevertDownload(IDependencyDownloaderWatermark watermark)
        {
            if (null == watermark)
            {
                throw new ArgumentNullException("watermark");
            }

            Logger.Instance().Log(TraceLevel.Info, "{0}: Reverting download via watermark ...", DownloadType);

            // We sort it by length. This means that we delete the deepest files first and we
            // hit the directories after all files have been deleted
            var itemsToClean = watermark.ArtifactsToClean.OrderByDescending(x => x.Length).ToList();
            foreach (var item in itemsToClean)
            {
                var fi = new FileInfo(item);
                if (fi.Exists)
                {
                    fi.Attributes = FileAttributes.Normal;
                    fi.Delete();
                    watermark.ArtifactsToClean.Remove(item);
                    Logger.Instance().Log(TraceLevel.Verbose, "{0}: Deleted file {1}", DownloadType, fi.FullName);
                    continue;
                }

                var di = new DirectoryInfo(item);
                if (di.Exists)
                {
                    if (!di.EnumerateDirectories().Any() && !di.EnumerateFiles().Any())
                    {
                        // Delete empty directories only
                        di.Delete();
                        Logger.Instance().Log(TraceLevel.Verbose, "{0}: Deleted empty directory {1}", DownloadType, di.FullName);
                    }

                    // But remove from artifacts anyway
                    watermark.ArtifactsToClean.Remove(item);
                }
            }

            // In the end, we will remove all watermarks in case someone else removed some files before
            watermark.ArtifactsToClean.Clear();
            watermark.Watermarks.Clear();
            Logger.Instance().Log(TraceLevel.Info, "{0}: Reverting download finished successfully", DownloadType);
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Downloads all items in current folder and items in subfolders by calling itself.
        /// </summary>
        /// <param name="root">Root of copy operation</param>
        /// <param name="source">A valid TFS version control path to a folder that shall be downloaded.</param>
        /// <param name="destination">A valid directory to which all the files will be downloaded to.</param>
        /// <param name="versionString">The version string to determine the version to download.</param>
        /// <param name="includeFilters">The filter for file types to include.</param>
        /// <param name="excludeFilters">List of filters to exclude files and folders.</param>
        /// <param name="watermark">The watermark can be used to perform incremental updates and cleanup operations.</param>
        /// <param name="force">Indicates that we want to force a get operation and all files have to be overwritten</param>
        private void DownloadFolder(string root, string source, string destination, string versionString, IEnumerable<string> includeFilters, IEnumerable<string> excludeFilters, IDependencyDownloaderWatermark watermark, bool force)
        {
            var version = VersionSpec.ParseSingleSpec(versionString, _workspace.OwnerName);

            var includes = ConvertFilterToRegex(includeFilters, root);
            var excludes = ConvertFilterToRegex(excludeFilters, root);

            var items = _vcs.GetItems(source, version, RecursionType.Full, DeletedState.NonDeleted, ItemType.File, true).Items;
            items = items.Where(x => includes.Any(y => y.IsMatch(x.ServerItem))).ToArray();
            items = items.Where(x => !excludes.Any(y => y.IsMatch(x.ServerItem))).ToArray();

            Parallel.ForEach(items, new ParallelOptions { MaxDegreeOfParallelism = 8 }, item =>
            {
                var subpath = item.ServerItem.Substring(source.Length).Trim(new[] { '/' }).Replace("//", "\\");
                var destinationpath = Path.Combine(destination, subpath);

                var wm = watermark.GetWatermark<int>(item.ServerItem);
                if (force || 0 == wm || item.ChangesetId != wm || !File.Exists(destinationpath))
                {
                    if (File.Exists(destinationpath))
                        File.SetAttributes(destinationpath, FileAttributes.Normal);

                    lock (this)
                    {
                        watermark.ArtifactsToClean.Add(destinationpath);
                    }

                    Logger.Instance().Log(TraceLevel.Verbose, "{0}: Downloading file {1}", DownloadType, item.ServerItem);
                    item.DownloadFile(destinationpath);

                    lock (this)
                    {
                        watermark.UpdateWatermark(item.ServerItem, item.ChangesetId);
                    }
                }
                else
                {
                    Logger.Instance().Log(TraceLevel.Verbose, "{0}: Skipping file {1}", DownloadType, item.ServerItem);
                }
            });
        }

        private IList<Regex> ConvertFilterToRegex(IEnumerable<string> filters, string root)
        {
            var result = new List<Regex>();

            root = root.Trim();
            foreach (var filter in filters)
            {
                //First of all we look for a trailing slash because that indicates a folder which has too be searched recursivley
                var pattern = filter.Trim();
                if (pattern.EndsWith("\\"))
                    pattern = pattern + "*";

                //Next we are going to analyze the prefixes. The normal case is that a * is used as prefix
                if (!pattern.StartsWith("*"))
                {
                    //There is one special case. If .\ is defined it indicates that it has to be in the root folder. In that case we prefix the branch root
                    if(pattern.StartsWith(".\\"))
                        pattern = root + pattern.Substring(1);
                    else
                        pattern = "*" + pattern;
                }

                //Now we have a canonicalized wildcard expression. Next we have to convert it to a regular expression
                pattern = pattern.Replace("\\", "/");
                pattern = pattern.Replace(".", "\\.");
                pattern = pattern.Replace("*", ".*");
                pattern = pattern.Replace("$", "\\$");

                //Now that we have the expression we have to ensure for proper pre and post fixes
                if (!pattern.EndsWith(".*"))
                    pattern = pattern + "$";
                if (!pattern.StartsWith(".*"))
                    pattern = "^" + pattern;

                try
                {
                    result.Add(new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant));
                }
                catch (ArgumentException)
                {
                    Logger.Instance().Log(TraceLevel.Error, "An error occured while converting the filter expression {0} to a regular expression", filter);
                }
            }

            return result;
        }

        /// <summary>
        /// Initializes the filters.
        /// </summary>
        /// <param name="includePattern">The include pattern.</param>
        /// <returns>List of filter strings</returns>
        private IList<string> InitializeIncludeFilter(string includePattern)
        {
            var fileFilters = new List<string>();
            if (string.IsNullOrEmpty(includePattern))
            {
                Logger.Instance().Log(TraceLevel.Verbose, "{0}: No file include filters are specified. Fetching all files.", DownloadType);
                fileFilters.Add("*");
                return fileFilters;
            }

            fileFilters.AddRange(includePattern.Split(new[] {';'}, StringSplitOptions.RemoveEmptyEntries));

            if (fileFilters.Count == 0)
            {
                fileFilters.Add("*");
            }

            return fileFilters;
        }

        /// <summary>
        /// Initializes the exclude filter.
        /// </summary>
        /// <param name="excludePattern">The include pattern.</param>
        private IList<string> InitializeExcludeFilter(string excludePattern)
        {
            var fileFilters = new List<string>();
            if (string.IsNullOrEmpty(excludePattern))
            {
                Logger.Instance().Log(TraceLevel.Verbose, "{0}: No file exclude filters are specified. All file types are downloaded.", DownloadType);
                return fileFilters;
            }

            fileFilters.AddRange(excludePattern.Split(new[] {';'}, StringSplitOptions.RemoveEmptyEntries));
            return fileFilters;
        }

        /// <summary>
        /// Initializes the mappings.
        /// </summary>
        /// <param name="mappingString">The mapping string.</param>
        /// <returns>List of subfolder mappings</returns>
        private IEnumerable<KeyValuePair<string, string>> InitializeMappings(string mappingString)
        {
            var folderMappings = new List<KeyValuePair<string, string>>();

            if (string.IsNullOrEmpty(mappingString))
            {
                Logger.Instance().Log(TraceLevel.Verbose, "{0}: No folder mappings are specified. Retrieving all folders.", DownloadType);
                return folderMappings;
            }

            foreach (var mapping in mappingString.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (_mappingRegexMatch.IsMatch(mapping.ToLower()))
                {
                    var mappingParts = mapping.Split(new[] { '=', ',' }, StringSplitOptions.None);

                    if (mappingParts.Length == 4)
                    {
                        folderMappings.Add(new KeyValuePair<string, string>(mappingParts[1], mappingParts[3]));
                    }
                    else
                    {
                        Logger.Instance().Log(TraceLevel.Error, "{0}: Mapping information could not be retrieved from mapping {1} in folder mapping string {2}", DownloadType, mapping, mappingString);
                        throw new InvalidProviderConfigurationException(string.Format("FolderMappings setting contains invalid part {0} (Pattern:{1})", mapping, mappingString));
                    }
                }
                else
                {
                    Logger.Instance().Log(TraceLevel.Error, "{0}: Skipping invalid mapping {1} in folder mapping string {2}", DownloadType, mapping, mappingString);
                    throw new InvalidProviderConfigurationException(string.Format("FolderMappings setting contains invalid part {0} (Pattern:{1})", mapping, mappingString));
                }
            }

            return folderMappings;
        }

        #endregion
    }
}