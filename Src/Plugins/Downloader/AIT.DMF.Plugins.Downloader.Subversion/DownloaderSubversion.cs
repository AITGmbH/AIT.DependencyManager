// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DownloaderFileShareCopy.cs" company="AIT GmbH & Co. KG">
//   All rights reserved by AIT GmbH & Co. KG
// </copyright>
// <summary>
//   Defines the DownloaderSubversion type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using AIT.DMF.Contracts.Provider;
using AIT.DMF.Contracts.Common;
using AIT.DMF.Contracts.Services;
using AIT.DMF.Contracts.Exceptions;
using AIT.DMF.Provider.Subversion;
using AIT.DMF.Common;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace AIT.DMF.Plugins.Downloader.Subversion
{
    public class DownloaderSubversion : IDependencyDownloader
    {
        #region Private Members

        private readonly Regex _mappingRegexMatch = new Regex("sourceoffset=([^/:*?\"<>|]+),localoffset=([^/:*?\"<>|]*)", RegexOptions.IgnoreCase);

        #endregion

        #region IDependencyDownloader

        /// <summary>
        /// Gets the download type of the file share downloader.
        /// </summary>
        public string DownloadType
        {
            get
            {
                return "Downloader_Subversion";
            }
        }

        /// <summary>
        /// Downloads a component to a local path.
        /// </summary>
        /// <param name="source">The path to source folder.</param>
        /// <param name="destination">The path to destination folder.</param>
        /// <param name="watermark">The watermark which is be used to perform incremental updates and cleanup operations.</param>
        /// <param name="force">Indicates that we want to force a get operation and all files have to be overwritten.</param>
        /// <param name="settings">The settings which include the pattern for files/directories to include and folder mappings.</param>
        public void Download(string source, string destination, IDependencyDownloaderWatermark watermark, bool force, ISettings<DownloaderValidSettings> settings)
        {
            if (string.IsNullOrEmpty(source))
            {
                throw new ArgumentNullException("source");
            }

            if (string.IsNullOrEmpty(destination))
            {
                throw new ArgumentNullException("destination");
            }

            if (watermark == null)
            {
                throw new ArgumentNullException("watermark");
            }

            if (settings == null)
            {
                throw new ArgumentNullException("settings");
            }

            var versionSpec = settings.GetSetting(DownloaderValidSettings.VersionString);

            if (string.IsNullOrEmpty(versionSpec))
            {
                throw new InvalidComponentException("Version string for component was empty in DownloaderSubversion");
            }

            try
            {
                if (!ProviderSubversion.Instance.ItemExists(source, versionSpec))
                {
                    Logger.Instance().Log(TraceLevel.Error, "{0}: Folder not exists in Subversion {1}", DownloadType, source);
                    throw new InvalidProviderConfigurationException(string.Format("Could not connect to Subversion {0}", source));
                }
            }
            catch (SvnAuthenticationException)
            {
                throw new SvnAuthenticationException();
            }

            Logger.Instance().Log(TraceLevel.Info, "{0}: Downloading component {1} to {2} ...", DownloadType, source, destination);

            var folderMappings = InitializeMappings(settings.GetSetting(DownloaderValidSettings.FolderMappings));
            var includeFilter = InitializeIncludeFilter(settings.GetSetting(DownloaderValidSettings.IncludedFilesFilter));
            var excludeFilter = InitializeExcludeFilter(settings.GetSetting(DownloaderValidSettings.ExcludedFilesFilter));

            if (folderMappings.Any())
            {
                foreach (var mapping in folderMappings)
                {
                    //var subSource = VersionControlPath.Combine(source, mapping.Key);
                    var subSource = string.Format("{0}/{1}", source, mapping.Key);
                    var subDest = Path.Combine(destination, mapping.Value);
                    Logger.Instance().Log(TraceLevel.Info, "{0}: Downloading component subfolder {1} with source control version to folder {2}", DownloadType, mapping.Key, subDest);
                    Export(source, subSource, subDest, versionSpec, includeFilter, excludeFilter, watermark, force);
                }

                Logger.Instance().Log(TraceLevel.Info, "{0}: Component {1} download finished successfully", DownloadType, source);
            }
            else
            {
                Logger.Instance().Log(TraceLevel.Info, "{0}: Downloading component {1} to folder {2}", DownloadType, source, destination);
                Export(source, source, destination, versionSpec, includeFilter, excludeFilter, watermark, force);
                Logger.Instance().Log(TraceLevel.Info, "{0}: Component {1} download finished successfully", DownloadType, source);
            }
        }

        /// <summary>
        /// Performs a revert operation by removing all the files which have been downloaded previously.
        /// The watermark written during the Download will be provided.
        /// </summary>
        /// <param name="watermark">The watermark that has been used for the download operation.</param>
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
                    watermark.Watermarks.Remove(item);
                    fi.Attributes = FileAttributes.Normal;
                    fi.Delete();
                    watermark.ArtifactsToClean.Remove(item);
                    Logger.Instance().Log(TraceLevel.Verbose, "{0}: Deleted file {1}", DownloadType, fi.FullName);
                    continue;
                }

                var di = new DirectoryInfo(item);
                if (di.Exists)
                {
                    if (!di.EnumerateFiles().Any())
                    {
                        di.Delete();
                        watermark.ArtifactsToClean.Remove(item);
                        Logger.Instance().Log(TraceLevel.Verbose, "{0}: Deleted empty directory {1}", DownloadType, di.FullName);
                    }
                }
            }

            Logger.Instance().Log(TraceLevel.Info, "{0}: Reverting download finished successfully", DownloadType);
        }

        #endregion

        #region Helper

        private void Export(string root, string source, string destination, string versionSpec, IEnumerable<string> includeFilters, IEnumerable<string> excludeFilters, IDependencyDownloaderWatermark watermark, bool force)
        {
            var includes = ConvertFilterToRegex(includeFilters, root);
            var excludes = ConvertFilterToRegex(excludeFilters, root);

            var items = new Dictionary<string, string>();
            var allItems = ProviderSubversion.Instance.GetItems(source, ProviderSubversion.ItemType.File, true, versionSpec);

            // apply include and exclude filter
            foreach (var item in allItems)
            {
                if ((includes.Any(x => x.IsMatch(item.Key))) && (!excludes.Any(x => x.IsMatch(item.Key))))
                {
                    items.Add(item.Key, item.Value);
                }
            }

            var revision = 0L;

            foreach (var item in items)
            {
                var destinationpath = destination;

                var subpath = item.Value.Substring(source.Length).Trim(new[] { '/' }).Replace("/", "\\");

                //Remove filename
                if (subpath.Contains("\\"))
                {
                    subpath = subpath.Remove(subpath.LastIndexOf("\\"));
                    destinationpath = Path.Combine(destination, subpath);
                }

                // Determine revision one-time. All items have same revision.
                if (revision == 0)
                {
                    revision = ProviderSubversion.Instance.GetHeadRevision(item.Key);
                }

                var wm = watermark.GetWatermark<long>(item.Key);

                var targetFile = Path.Combine(destinationpath, Path.GetFileName(item.Value));

                if (force || 0 == wm || revision != wm || !File.Exists(targetFile))
                {
                    Logger.Instance().Log(TraceLevel.Verbose, "{0}: Downloading file {1}", DownloadType, item.Key);

                    ProviderSubversion.Instance.GetFile(item.Key, destinationpath, force, versionSpec);

                    //Save files and folders, which included in the item
                    watermark.ArtifactsToClean.Add(destinationpath);

                    foreach (var file in Directory.GetFiles(destinationpath))
                    {
                        watermark.ArtifactsToClean.Add(file);
                    }

                    //Update information about source (path and revision)
                    watermark.UpdateWatermark(item.Key, revision);
                }
                else
                {
                    Logger.Instance().Log(TraceLevel.Verbose, "{0}: Skipping file {1}", DownloadType, item);
                }
            }
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
                    if (pattern.StartsWith(".\\"))
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

            fileFilters.AddRange(includePattern.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));

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

            fileFilters.AddRange(excludePattern.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
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
