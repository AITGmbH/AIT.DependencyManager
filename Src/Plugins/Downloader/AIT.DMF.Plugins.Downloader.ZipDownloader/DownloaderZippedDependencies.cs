// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DownloaderSourceControlMapping.cs" company="AIT GmbH & Co. KG">
//   All rights reserved by AIT GmbH & Co. KG
// </copyright>
// <summary>
//   Defines the zip downloader type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------


namespace AIT.DMF.Plugins.Downloader.ZippedDependency
{
    using Common;
    using Contracts.Common;
    using Contracts.Provider;
    using Contracts.Services;
    using DependencyService;
    using FileShareCopy;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;

    /// <summary>
    /// The zip downloader can be used to download zip files and extract it
    /// and then copy the extracted folders to the set destination folders as by the mapping configuration
    /// </summary>
    public class ZippedDependencyDownloader : IDependencyDownloader
    {

        #region Private Members

        private readonly IDependencyDownloader _downloader;

        private readonly bool _isDeleteArchiveFiles;

        #endregion

        /// <summary>
        /// Gets the downloader type.
        /// </summary>
        public string DownloadType
        {
            get
            {
                return "Downloader_ZippedDependency";
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ZippedDependencyDownloader"/> class.
        /// </summary>
        /// <param name="downloader">The download used to retrieve compressed files</param>
        public ZippedDependencyDownloader(IDependencyDownloader downloader, bool IsDeleteArchiveFiles)
        {
            _downloader = downloader;
            _isDeleteArchiveFiles = IsDeleteArchiveFiles;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ZippedDependencyDownloader"/> class for cleaning.
        /// </summary>
        public ZippedDependencyDownloader()
        {
        }

        /// <summary>
        /// Downloads a component to a local path.
        /// This method will download the zip files to a temporary folder
        /// Will extract the zip files there
        /// Will then move the content to the target directory, based on the mapping.
        /// </summary>
        /// <param name="source">Path to source location</param>
        /// <param name="destination">Path to destination folder</param>
        /// <param name="watermark">The watermark can be used to perform incremental updates and cleanup operations</param>
        /// <param name="force">Indicates that we want to force a get operation and all files have to be overwritten</param>
        /// <param name="settings">Settings which contains the pattern for directories and files to include ("Debug;Bin*")</param>
        [SuppressMessage("StyleCop.CSharp.LayoutRules", "SA1515:SingleLineCommentMustBePrecededByBlankLine", Justification = "Reviewed. Suppression is OK here.")]
        public void Download(string source, string destination, IDependencyDownloaderWatermark watermark, bool force, ISettings<DownloaderValidSettings> settings)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            if (destination == null)
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

            Logger.Instance().Log(TraceLevel.Info, "{0}: Downloading component {1} to {2} ...", DownloadType, source, destination);

            // Save compressed files to temp path and temporarily store watermarks. Turn of mappings as they only apply to the extracted folder structure
            var tempPath = Path.Combine(destination, "..\\", "DM_TEMP");

            //Create the temp Path
            if (!Directory.Exists(tempPath))
            {
                DirectoryInfo di = Directory.CreateDirectory(tempPath);
            }

            //save the folder mapping
            var tempFolderMapping = settings.GetSetting(DownloaderValidSettings.FolderMappings);
            var remainingArtefacts = new HashSet<string>(watermark.ArtifactsToClean);


            //Download the zip files and the rest of the contents ignore any mappings to get everything in the temppath
            settings.SettingsDictionary[DownloaderValidSettings.FolderMappings] = null;
            _downloader
                .Download(source, tempPath, watermark, force, settings);
            //var zipWatermarks = new Dictionary<string, object>(watermark.Watermarks);

            // The DownloaderFileShareCopy / DownloaderSourceControlCopy creates artifacts for cleanup,
            // which are deleted by the ZippedDependencyDownloader and must not be saved.
            watermark.ArtifactsToClean = remainingArtefacts;

            //Extract the zipped files in the temp path
            ExtractCompressedFiles(tempPath, tempPath, watermark, force);

            //Set the settings to a move operation
            settings.AddSetting(new KeyValuePair<DownloaderValidSettings, string>(DownloaderValidSettings.OperationType, "Move"));

            //Set the temporary saved mapping again
            settings.SettingsDictionary[DownloaderValidSettings.FolderMappings] = tempFolderMapping;

            //The SourceControlCopy-Provider needs the FileShareCopy-Provider to copy the extracted files from the temp folder to the
            //target folder, because the SourceControlCopy-Provider works not with local folders as source folder
            if (_downloader.DownloadType == "Downloader_SourceControlCopy")
            {
                IDependencyDownloader tmpDownloader = new DownloaderFileShareCopy();
                tmpDownloader.Download(tempPath, destination, watermark, force, settings);
            }
            // Provider: FileShare, BuildResult
            else
            {
                //Copy the files from the temp path to the final destination
                _downloader.Download(tempPath, destination, watermark, force, settings);
            }

            //Cleanup: Delete the temp path
            try
            {
                DeleteTempDirectory(tempPath);
            }
            catch (Exception e)
            {
                Logger.Instance().Log(TraceLevel.Error, "Deleting temp folder {0} did not succeed: {1}", tempPath, e.Message);
                Logger.Instance().Log(TraceLevel.Error, "Stacktrace: {0}", e.StackTrace);
            }
        }

        /// <summary>
        /// Extracts the compressed files in directory and recursively in subdirectories.
        /// Searches for zipped and 7-zipped files (LZMA)
        /// </summary>
        /// <param name="source">The directory with compressed files</param>
        /// <param name="target">The target direcotry of the zip files content</param>
        /// <param name="watermark">The watermark which is be used to perform incremental updates and cleanup operations.</param>
        /// <param name="force">Indicates that we want to force a get operation and all files have to be overwritten.</param>
        private void ExtractCompressedFiles(string source, string target, IDependencyDownloaderWatermark watermark, bool force)
        {
            string[] allowedExtensions = { ".zip", ".7z" };

            foreach (var compressedFile in Directory.EnumerateFiles(source, "*.*", SearchOption.AllDirectories)
            .Where(s => allowedExtensions.Any(ext => ext == Path.GetExtension(s))))
            {
                Logger.Instance().Log(TraceLevel.Info, "Start extracting compressed file {0}.", compressedFile);

                try
                {
                    // regard if the zip file is in a subdirectory
                    var targetSubdir = compressedFile.Replace(source, string.Empty);

                    if (targetSubdir.Contains("\\"))
                    {
                        targetSubdir = targetSubdir.Substring(0, targetSubdir.LastIndexOf("\\", StringComparison.Ordinal));

                        // cut the leading Backslash "\", if one exists
                        if ((targetSubdir.StartsWith("\\")) && (targetSubdir.Length > 0))
                        {
                            targetSubdir = targetSubdir.Substring(1);
                        }
                    }
                    else
                    {
                        // contains only the filename of the zip archive
                        targetSubdir = string.Empty;
                    }

                    //If Sevenzip is supported, extract and zip file also with sevenzip
                    if (compressedFile.EndsWith(".zip"))
                    {
                        if (string.IsNullOrWhiteSpace(ResolveSevenZipExecutable()))
                        {
                            ExtractStandardZipFile(target, targetSubdir, compressedFile, watermark, force);
                        }
                        else
                        {
                            ExtractSevenZipFile(target, targetSubdir, compressedFile, watermark, force);
                        }
                    }
                    else if (compressedFile.EndsWith(".7z"))
                    {
                        ExtractSevenZipFile(target, targetSubdir, compressedFile, watermark, force);
                    }
                    else
                    {
                        throw new Exception("The file you are trying to extract is not supported ");
                    }

                    //Delete
                    if (_isDeleteArchiveFiles)
                    {
                        DeleteArchiveFile(compressedFile);
                    }
                }
                catch (InvalidDataException)
                {
                    Logger.Instance().Log(TraceLevel.Warning, "Failed to extract file {0}. Format not supported!", compressedFile);
                    throw new Exception();
                }
            }
        }

        /// <summary>
        /// Delete an archive file
        ///
        /// </summary>
        /// <param name="compressedFile">The path to the file that should be deleted</param>
        private void DeleteArchiveFile(string compressedFile)
        {
            try
            {
                File.Delete(compressedFile);
            }
            catch (Exception)
            {
                Logger.Instance().Log(TraceLevel.Warning, "Failed to delete file{0}.", compressedFile);
                throw;
            }
        }

        /// <summary>
        /// Seven zip (lzma) support for the DM. The libraries for sevenzip sharp are referenced dynamically
        /// </summary>
        /// <param name="target"></param>
        /// <param name="targetSubdir"></param>
        /// <param name="archiveFile"></param>
        /// <param name="watermark"></param>
        /// <param name="force"></param>
        private void ExtractSevenZipFile(string target, string targetSubdir, string archiveFile, IDependencyDownloaderWatermark watermark, bool force)
        {
            var sevenZipExecutable = ResolveSevenZipExecutable();
            if (string.IsNullOrWhiteSpace(sevenZipExecutable))
            {
                Logger.Instance().Log(TraceLevel.Warning, "Unable to resolve the path to the 7 zip executable. Make sure that 7 zip is installed and that the dependency manager is configured properly");
                Logger.Instance().Log(TraceLevel.Warning, "Your dependecy contains *.7z archives. Please make sure the SevenZip support is enabled in the Dependency Manager settings.");
                return;
            }

            var process = new Process();
            process.StartInfo = new ProcessStartInfo();
            process.StartInfo.FileName = sevenZipExecutable;
            process.StartInfo.Arguments = string.Format("x \"{0}\" -y", archiveFile);
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.WorkingDirectory = Path.Combine(target,targetSubdir);

            process.Start();

            process.OutputDataReceived += (x, y) =>
                {
                    if(!string.IsNullOrWhiteSpace(y.Data) && y.Data.StartsWith("Extracting", StringComparison.OrdinalIgnoreCase))
                    {
                        var file = y.Data.Replace("Extracting", string.Empty).Trim();
                        var fullName = Path.Combine(target, file);
                        watermark.ArtifactsToClean.Add(fullName);
                    }
                };

            process.BeginOutputReadLine();
            process.WaitForExit();

            if(0 != process.ExitCode)
            {
                //Todo Replace by other and more specific exception
                Logger.Instance().Log(TraceLevel.Error, "An error occured while executing {0} {1}. The exit code is {2}", process.StartInfo.FileName, process.StartInfo.Arguments, process.ExitCode);
                throw new Exception(string.Format("An error occured while executing {0} {1}. The exit code is {2}", process.StartInfo.FileName, process.StartInfo.Arguments, process.ExitCode));
            }

            Logger.Instance().Log(TraceLevel.Info, "Extracted zipped file {0} finished successfully.", archiveFile);
        }

        /// <summary>
        /// Extract a standard zip file
        /// </summary>
        /// <param name="target"></param>
        /// <param name="targetSubdir"></param>
        /// <param name="archiveFile"></param>
        /// <param name="watermark"></param>
        /// <param name="force"></param>
        private void ExtractStandardZipFile(string target, string targetSubdir, string archiveFile, IDependencyDownloaderWatermark watermark, bool force)
        {
            using (var archive = ZipFile.OpenRead(archiveFile))
            {
                foreach (var file in archive.Entries)
                {
                    // Extract only files, not subdirectories. If a subdirectory not exists, create it;
                    if (file.FullName.EndsWith("/"))
                    {
                        var subdir = Path.Combine(target, targetSubdir, file.FullName.Substring(0, file.FullName.Length - 1));

                        subdir = subdir.Replace("/", "\\");

                        if (!Directory.Exists(subdir))
                        {
                            Directory.CreateDirectory(subdir);

                            // For the CreateDirectory method the folder depth its not relevant, but for the cleanup, so each subfolder must
                            // be logged separately
                            if (FolderDepth(subdir) > FolderDepth(target) + 1)
                            {
                                var tmp = subdir;

                                do
                                {
                                    watermark.ArtifactsToClean.Add(tmp);

                                    tmp = tmp.Substring(0, tmp.LastIndexOf("\\"));
                                } while (tmp.Length > target.Length);
                            }
                        }

                        continue;
                    }

                    var df = new FileInfo(Path.Combine(target, targetSubdir, file.FullName));
                    var wm = watermark.GetWatermark<DateTime>(file.FullName);

                    if (force || wm == DateTime.MinValue || wm != df.LastWriteTimeUtc || !df.Exists)
                    {
                        // Remove possible readonly flag
                        if (df.Exists)
                        {
                            df.Attributes = FileAttributes.Normal;
                        }

                        watermark.ArtifactsToClean.Add(df.FullName);

                        try
                        {
                            file.ExtractToFile(df.FullName, true);
                        }
                        catch (Exception ex)
                        {
                            Logger.Instance().Log(TraceLevel.Error, "{0}: The archive cannot be extracted. The compression method is not supported. {1}", ex.Message, file.FullName);
                            throw new Exception(String.Format("The file {0} cannot be extracted. If the file is compressed with a non-standard algorithm (e.g. LZMA), make sure the support for 7-zip is enabled and all dlls are set properly in the Dependecy Manager settings.", archiveFile));
                        }

                        df.Attributes = FileAttributes.Normal;

                        //TODO: Klaus will auch die enthaltenen Datien im Output Window sehen!
                        Logger.Instance().Log(TraceLevel.Verbose, "{0}: Downloading file {1}", DownloadType, file.FullName);
                    }
                    else
                    {
                        Logger.Instance().Log(TraceLevel.Verbose, "{0}: Skipping file {1}", DownloadType, file.FullName);
                    }
                }

            }

            Logger.Instance().Log(TraceLevel.Info, "Extracted zipped file {0} finished successfully.", archiveFile);
        }

        /// <summary>
        /// Performs a revert operation by removing all the files which have been downloaded previously. The watermark written during the Download will be provided
        /// </summary>
        /// <param name="watermark">The watermark that has been used for the download operation</param>
        public void RevertDownload(IDependencyDownloaderWatermark watermark)
        {
            // Todo MRI: Refactor to use PluginFactory
            (new DownloaderFileShareCopy()).RevertDownload(watermark);
        }

        #region Helpers

        /// <summary>
        /// Checks whether a seven zip installation is available on the system
        /// </summary>
        /// <returns>The full path to the 7z.exe if found; null otherwise</returns>
        private string ResolveSevenZipExecutable()
        {
            //If a file is set, use this one
            if (ApplicationSettings.Instance.InstallPathForSevenZip != null)
            {
                if (File.Exists(ApplicationSettings.Instance.InstallPathForSevenZip))
                {
                    return ApplicationSettings.Instance.InstallPathForSevenZip;
                }
                else
                {
                    //This is the case if a non existant file is referenced
                    return null;
                }
            }

            //This fallback solution - for determining the 7-zip folder without existing registry key - is necessary to
            //provide 7-zip-support, if a registry entry is not desired
            return ApplicationSettings.Instance.DetermineSevenZipFolder();
        }



        /// <summary>
        /// Determines the number of folders in a path
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private int FolderDepth(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return 0;
            }

            return Path.GetFullPath(path).Split(Path.DirectorySeparatorChar).Length;
        }

        /// <summary>
        /// Recursive Method to delete the tempPath
        /// </summary>
        /// <param name="tempPath"></param>

        private void DeleteTempDirectory(string tempPath)
        {
            string[] files = Directory.GetFiles(tempPath);
            string[] dirs = Directory.GetDirectories(tempPath);
            foreach (string file in files)
            {
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }

            foreach (string dir in dirs)
            {
                DeleteTempDirectory(dir);
            }

            Directory.Delete(tempPath, false);
        }

        #endregion
    }
}
