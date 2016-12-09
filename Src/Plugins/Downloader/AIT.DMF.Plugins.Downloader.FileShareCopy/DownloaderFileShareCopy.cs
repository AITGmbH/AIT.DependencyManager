// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DownloaderFileShareCopy.cs" company="AIT GmbH & Co. KG">
//   All rights reserved by AIT GmbH & Co. KG
// </copyright>
// <summary>
//   Defines the DownloaderFileShareCopy type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace AIT.DMF.Plugins.Downloader.FileShareCopy
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Common;
    using Contracts.Common;
    using Contracts.Exceptions;
    using Contracts.Provider;
    using Contracts.Services;

    /// <summary>
    /// The downloader for a FileShare dependency. The dependency and its files are copied into the target destination.
    /// </summary>
    // TODO provide credentials to connect to the file share. On a plain system the credential dialog show up
    public class DownloaderFileShareCopy : IDependencyDownloader
    {
        #region Private Members

        /// <summary>
        /// The mapping regex match.
        /// </summary>
        private readonly Regex _mappingRegexMatch = new Regex("sourceoffset=([^/:*?\"<>|]+),localoffset=([^/:*?\"<>|]*)", RegexOptions.IgnoreCase);

        /// <summary>
        /// The retry download logic.
        /// </summary>
        private readonly RetryLogic _retryHelper = new RetryLogic(25);

        private OperationType _operationType;

        private enum OperationType
        {
            Move,
            Copy
        }

        /// <summary>
        /// Lists that hold all excluded and included files
        /// </summary>
        private List<DirectoryInfo> _includedFoldersList;
        private List<DirectoryInfo> _excludedFoldersList;
        private List<FileInfo> _includedFilesList;
        private List<FileInfo> _excludedFilesList;

        #endregion

        #region IDependencyDownloader

        /// <summary>
        /// Gets the download type of the file share downloader.
        /// </summary>
        public string DownloadType
        {
            get
            {
                return "Downloader_FileShareCopy";
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

            if (!Directory.Exists(source))
            {
                throw new DirectoryNotFoundException(string.Format("The directory {0} does not exit", source));
            }

            Logger.Instance().Log(TraceLevel.Info, "{0}: Downloading component {1} to {2} ...", DownloadType, source, destination);

            var sourceDi = new DirectoryInfo(source);
            var destinationDi = new DirectoryInfo(destination);
            var folderMappings = InitializeMappings(settings.GetSetting(DownloaderValidSettings.FolderMappings));
            var filters = InitializeIncludeFilter(settings.GetSetting(DownloaderValidSettings.IncludedFilesFilter));
            var excludedFiles = InitializeExcludeFilter(settings.GetSetting(DownloaderValidSettings.ExcludedFilesFilter));
            _operationType = InitializeOperationType(settings.GetSetting(DownloaderValidSettings.OperationType));

            // Reset the list of excluded and included files when a new download occurs
            _includedFoldersList = null;
            _excludedFoldersList = null;
            _includedFilesList = null;
            _excludedFilesList = null;

            // ReSharper disable PossibleMultipleEnumeration
            if (folderMappings.Any())
            // ReSharper restore PossibleMultipleEnumeration
            {
                // ReSharper disable PossibleMultipleEnumeration
                foreach (var mapping in folderMappings)
                // ReSharper restore PossibleMultipleEnumeration
                {
                    var subSourceDi = new DirectoryInfo(Path.Combine(sourceDi.ToString(), mapping.Key));
                    var subDestDi = new DirectoryInfo(Path.Combine(destinationDi.ToString(), mapping.Value));
                    Logger.Instance().Log(TraceLevel.Info, "{0}: Downloading component subfolder {1} to {2}", DownloadType, mapping.Key, subDestDi);
                    CopyDirectoryContent(sourceDi, subSourceDi, subDestDi, filters, excludedFiles, watermark, force);
                }

                Logger.Instance().Log(TraceLevel.Info, "{0}: Component {1} download finished successfully", DownloadType, source);
            }
            else
            {
                CopyDirectoryContent(sourceDi, sourceDi, destinationDi, filters, excludedFiles, watermark, force);
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
                    if (!di.EnumerateDirectories().Any() && !di.EnumerateFiles().Any())
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

        #region Helpers


        /// <summary>
        /// Copy files in directory and calls CopyDirectoryContent recursively for sub directories.
        /// </summary>
        /// <param name="root">The root of the recursive copy operation.</param>
        /// <param name="source">The directory to copy files from.</param>
        /// <param name="target">The directory to copy files to.</param>
        /// <param name="watermark">The watermark can be used to perform incremental updates and cleanup operations.</param>
        /// <param name="force">Indicates that we want to force a get operation and all files have to be overwritten.</param>
        /// <param name="includeFilters">Filter that defines what files and folders to include.</param>
        /// <param name="excludeFilters">Files that should be excluded</param>
        private void CopyDirectoryContent(DirectoryInfo root, DirectoryInfo source, DirectoryInfo target, IList<string> includeFilters, IList<string> excludeFilters, IDependencyDownloaderWatermark watermark, bool force)
        {
            if (string.Equals(source.FullName, target.FullName, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            //Get the folders only, if filters are provided and the list with excluded and included files and folders has not been initalized yet.
            // getting the folders only once, will increase the copy speed
            if (includeFilters != null || excludeFilters != null)
            {
                if (_includedFoldersList == null || _excludedFoldersList == null || _includedFilesList == null || _excludedFilesList == null)
                {
                    // explicitly and implicitly included and excluded folders
                    _includedFoldersList = includeFilters.Where(x => x.EndsWith("\\")).SelectMany(x => GetFoldersFromRoot(x, root)).ToList();
                    _excludedFoldersList = excludeFilters.Where(x => x.EndsWith("\\")).SelectMany(x => GetFoldersFromRoot(x, root)).ToList();
                    _includedFoldersList = _includedFoldersList.Concat(includeFilters.Where(x => x.EndsWith("\\")).Select(x => x.Trim('\\')).SelectMany(x => GetFoldersFromRoot(x, root)).ToList()).ToList();
                    _excludedFoldersList = _excludedFoldersList.Concat(excludeFilters.Where(x => x.EndsWith("\\")).Select(x => x.Trim('\\')).SelectMany(x => GetFoldersFromRoot(x, root)).ToList()).ToList();
                    //// generally and specifically included and excluded files
                    _includedFilesList = includeFilters.Where(x => x.EndsWith("\\") == false).SelectMany(x => GetFilesFromRoot(x, root)).ToList();
                    _excludedFilesList = excludeFilters.Where(x => x.EndsWith("\\") == false).SelectMany(x => GetFilesFromRoot(x, root)).ToList();


                    //Sort the list to move the root folder first
                    _includedFoldersList = _includedFoldersList.OrderBy(o => o.FullName.Length).ToList();

                    if (_excludedFoldersList.Any(x => source.FullName.StartsWith(x.FullName, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        return;
                    }
                }
            }

            watermark.ArtifactsToClean.Add(target.FullName);
            if (!target.Exists)
            {
                // Since preliminary deletes might lead to file or folder locks, we need to retry this
                var retries = _retryHelper.RetryAction(target.Create, 10, 1000);
                if (retries > 0)
                {
                    Logger.Instance().Log(TraceLevel.Verbose, "{0}: Folder {1} successfully created after {2} retries", DownloadType, target, retries);
                }
            }

            //The Move Operation Type.
            if (_operationType == OperationType.Move)
            {

                MoveFolder(source, target, watermark);
            }

            else
            {
                // get excluded and included files
                foreach (var sf in source.GetFiles())
                {
                    var isInIncludedFolder = _includedFoldersList.Any(x => sf.FullName.StartsWith(x.FullName, StringComparison.InvariantCultureIgnoreCase));
                    var isInExcludedFolder = _excludedFoldersList.Any(x => sf.FullName.StartsWith(x.FullName, StringComparison.InvariantCultureIgnoreCase));
                    var isIncludedFile = _includedFilesList.Any(x => IsSameFile(x.FullName, sf.FullName));
                    var isExcludedFile = _excludedFilesList.Any(x => IsSameFile(x.FullName, sf.FullName));

                    if ((isIncludedFile == false && isInIncludedFolder == false) || isExcludedFile || isInExcludedFolder)
                    {
                        continue;
                    }
                    var df = new FileInfo(Path.Combine(target.FullName, sf.Name));
                    var wm = watermark.GetWatermark<DateTime>(sf.FullName);

                    if (force || wm == DateTime.MinValue || wm != df.LastWriteTimeUtc || !df.Exists)
                    {
                        if (df.Exists)
                        {
                            df.Attributes = FileAttributes.Normal;
                        }
                        var localSourceFile = sf;

                        watermark.ArtifactsToClean.Add(df.FullName);

                        var retriesCopy = _retryHelper.RetryAction(() => localSourceFile.CopyTo(df.FullName, true), 5, 1000);
                        if (retriesCopy > 0)
                        {
                            Logger.Instance().Log(TraceLevel.Verbose, "{0}: File {1} successfully downloaded after {2} retries", DownloadType, df.FullName, retriesCopy);
                        }

                        //To determine the the LastWrite time the object must be refreshed after copy the file.
                        df.Refresh();

                        watermark.UpdateWatermark(sf.FullName, df.LastWriteTimeUtc);

                        df.Attributes = FileAttributes.Normal;
                        Logger.Instance().Log(TraceLevel.Verbose, "{0}: Downloading file {1}", DownloadType, sf.FullName);
                    }
                    else
                    {
                        Logger.Instance().Log(TraceLevel.Verbose, "{0}: Skipping file {1}", DownloadType, sf.FullName);
                    }
                }

                foreach (var subdirectory in source.GetDirectories())
                {
                    // a folder is included if one of its parents is included (unless it is explicitly exluded)
                    var files = _includedFilesList.Where(i => !_excludedFilesList.Any(e => IsSameFile(e.FullName, i.FullName))).ToList();
                    var isIncluded =
                        _includedFoldersList.Any(x => x.FullName.StartsWith(subdirectory.FullName, StringComparison.InvariantCultureIgnoreCase)) ||
                        !_excludedFoldersList.Any(x => x.FullName.EndsWith(subdirectory.FullName));


                    if (isIncluded)
                    {
                        var targetDi = new DirectoryInfo(Path.Combine(target.FullName, subdirectory.Name));
                        Logger.Instance()
                              .Log(TraceLevel.Verbose, "{0}: Downloading folder {1}", DownloadType,
                                   Path.Combine(source.FullName, subdirectory.Name));
                        CopyDirectoryContent(root, subdirectory, targetDi, includeFilters, excludeFilters, watermark,
                                                  force);
                    }
                }
            }
        }

        /// <summary>
        /// Recursive method to move a folder
        /// A folder is moved by moving its subfolders and all files within the folder
        /// If the target folder already exsist the method checks if the children of the target folder are the same.
        /// It will delete existing folders and will then move them to the folder
        ///
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <param name="watermark"></param>
        private void MoveFolder(DirectoryInfo source, DirectoryInfo target, IDependencyDownloaderWatermark watermark)
        {
            //Move the content of the current folder to the given output folder
            foreach (string tempPath in Directory.GetDirectories(source.FullName, "*", SearchOption.TopDirectoryOnly))
            {

                ////Check if the current folder exists on the target
                //if (Directory.Exists(tempPath.Replace(source.FullName, target.FullName)))
                //{
                //    //Check if the target contains any children. If there are no children, --> delete the folder
                //    //If the folder has children, check if the target children are equal to the source children --> If they are equal --> Delete
                //    var children = Directory.GetDirectories(tempPath.Replace(source.FullName, target.FullName), "*", SearchOption.TopDirectoryOnly);
                //    if (children.Count() == 0)
                //    {
                //        DeleteDirectory(tempPath.Replace(source.FullName, target.FullName));
                //    }
                //    else
                //    {
                //        //Delete each path that is similar within the children
                //        foreach (var tempPathChildren in Directory.GetDirectories(tempPath, "*", SearchOption.TopDirectoryOnly))
                //        {
                //            if (Directory.Exists(tempPathChildren.Replace(source.FullName, target.FullName)))
                //            {
                //                DeleteDirectory(tempPathChildren.Replace(source.FullName, target.FullName));
                //            }
                //        }
                //    }
                //}

                //If the target folder still exists, the children and files of the source can be moved
                //If the target does not exsist, the whole source folder can be moved
                if (Directory.Exists(tempPath.Replace(source.FullName, target.FullName)))
                {
                    MoveFolder(new DirectoryInfo(tempPath), new DirectoryInfo(tempPath.Replace(source.FullName, target.FullName)),watermark);
                }
                else
                {
                    var retriesMove = _retryHelper.RetryAction(() => Directory.Move(tempPath, tempPath.Replace(source.FullName, target.FullName)), 5, 1000);
                    if (retriesMove > 0)
                    {
                        Logger.Instance().Log(TraceLevel.Verbose, "{0}: File {1} successfully moved after {2} retries", DownloadType, tempPath, retriesMove);
                    }
                }

                //Get the file info and set the FileAttributes to Normal
                var fi = new DirectoryInfo(tempPath.Replace(source.FullName, target.FullName));
                fi.Attributes = FileAttributes.Normal;

                //Add all files in the folder to the watermarks
                foreach (var file in fi.GetFiles("*.*", SearchOption.AllDirectories))
                {
                    //Add the files to the watermark
                    watermark.UpdateWatermark(file.FullName, file.LastWriteTimeUtc);
                }
            }

            //Move all the files & Replaces any files with the same name by deleting them
            foreach (string tempPath in Directory.GetFiles(source.FullName, "*.*", SearchOption.TopDirectoryOnly))
            {
                if (File.Exists(tempPath.Replace(source.FullName, target.FullName)))
                {
                    File.SetAttributes(tempPath.Replace(source.FullName, target.FullName), FileAttributes.Normal);
                    File.Delete(tempPath.Replace(source.FullName, target.FullName));
                }

                var retriesMove = _retryHelper.RetryAction(() => File.Move(tempPath, tempPath.Replace(source.FullName, target.FullName)), 5, 1000);
                if (retriesMove > 0)
                {
                    Logger.Instance().Log(TraceLevel.Verbose, "{0}: File {1} successfully moved after {2} retries", DownloadType, tempPath, retriesMove);
                }

                //Get the file info and set the FileAttributes to Normal
                var fi = new FileInfo(tempPath.Replace(source.FullName, target.FullName));
                fi.Attributes = FileAttributes.Normal;

                //Add the files to the watermark
                watermark.UpdateWatermark(fi.FullName, fi.LastWriteTimeUtc);
            }

            Logger.Instance().Log(TraceLevel.Verbose, "{0}: Moving Directory {1} to {2}", DownloadType, source.FullName, target.FullName);
        }

        /// <summary>
        /// Compares the path of two files
        /// </summary>
        private static bool IsSameFile(string path, string otherPath)
        {
            path = path.Replace("\\.\\", "\\");
            otherPath = otherPath.Replace("\\.\\", "\\");

            return string.Compare(path, otherPath, StringComparison.OrdinalIgnoreCase) == 0;
        }

        /// <summary>
        /// Search files in root and subfolders
        /// </summary>
        private static IEnumerable<FileInfo> GetFilesFromRoot(string filter, DirectoryInfo root)
        {
            try
            {
                return root.GetFiles(filter, filter.StartsWith(".\\") ? SearchOption.TopDirectoryOnly : SearchOption.AllDirectories);
            }
            catch (DirectoryNotFoundException)
            {
                return Enumerable.Empty<FileInfo>();
            }
        }

        /// <summary>
        /// Search folders in root folder
        /// </summary>
        private static IEnumerable<DirectoryInfo> GetFoldersFromRoot(string filter, DirectoryInfo root)
        {
            try
            {
                return root.GetDirectories(filter, SearchOption.AllDirectories);
            }
            catch (IOException)
            {
                return Enumerable.Empty<DirectoryInfo>();
            }
            catch (ArgumentException)
            {
                return Enumerable.Empty<DirectoryInfo>();
            }
        }

        /// <summary>
        /// Initializes the filters.
        /// </summary>
        /// <param name="includePattern">The include pattern.</param>
        private IList<string> InitializeIncludeFilter(string includePattern)
        {
            var fileFilters = new List<string>();
            if (string.IsNullOrEmpty(includePattern))
            {
                Logger.Instance().Log(TraceLevel.Verbose, "{0}: No file filters are specified. Fetching all files.", DownloadType);
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
        /// <param name="excludePattern">The exclude pattern.</param>
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
        /// Initializes the operation type that determines if the requested operation is a move or a copy.
        /// </summary>
        /// <param name="operationType">The exclude pattern.</param>
        private OperationType InitializeOperationType(string operationType)
        {

            if (string.IsNullOrEmpty(operationType))
            {
                //Logger.Instance().Log(TraceLevel.Verbose, "{0}: No file exclude filters are specified. All file types are downloaded.", this.DownloadType);
                return OperationType.Copy;
            }

            if ("Move".Equals(operationType))
            {
                return OperationType.Move;
            }

            if ("Copy".Equals(operationType))
            {
                return OperationType.Copy;
            }

            return OperationType.Copy;
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
