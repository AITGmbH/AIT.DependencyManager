// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DownloaderFactory.cs" company="AIT GmbH & Co. KG.">
//   All rights reserved by AIT GmbH & Co. KG.
// </copyright>
// <summary>
//   Defines the DownloaderFactory factory.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;

namespace AIT.DMF.PluginFactory
{
    using Contracts.Exceptions;
    using Contracts.Graph;
    using Contracts.Parser;
    using Contracts.Provider;
    using Plugins.Downloader.FileShareCopy;
    using Plugins.Downloader.SourceControlCopy;
    using Plugins.Downloader.SourceControlMapping;
    using Plugins.Downloader.ZippedDependency;
    using Plugins.Downloader.Subversion;

    /// <summary>
    /// The downloader factory.
    /// </summary>
    public class DownloaderFactory : IDependencyDownloaderFactory
    {
        /// <summary>
        /// Returns the IDependencyDownloader object according to the name provided.
        /// If a provider with this name cannot be found a DependencyManagementFoundationPluginNotFoundException is thrown.
        /// </summary>
        /// <param name="component">The component to download</param>
        /// <returns>Downloader object</returns>
        public IDependencyDownloader GetDownloader(IComponent component)
        {
            IDependencyDownloader downloader;

            switch (component.Type)
            {
                case ComponentType.FileShare:
                case ComponentType.BuildResult:
                case ComponentType.VNextBuildResult:
                    downloader = new DownloaderFileShareCopy();
                    break;
                case ComponentType.SourceControl:
                    downloader =
                        new DownloaderSourceControlMapping(
                            component.GetFieldValue(DependencyProviderValidSettingName.WorkspaceTeamProjectCollectionUrl),
                            component.GetFieldValue(DependencyProviderValidSettingName.WorkspaceName),
                            component.GetFieldValue(DependencyProviderValidSettingName.WorkspaceOwner));
                    break;
                case ComponentType.BinaryRepository:
                    downloader =
                        new DownloaderSourceControlCopy(
                            component.GetFieldValue(DependencyProviderValidSettingName.BinaryTeamProjectCollectionUrl),
                            component.GetFieldValue(DependencyProviderValidSettingName.WorkspaceName),
                            component.GetFieldValue(DependencyProviderValidSettingName.WorkspaceOwner));
                    break;
                case ComponentType.SourceControlCopy:
                    downloader =
                        new DownloaderSourceControlCopy(
                            component.GetFieldValue(DependencyProviderValidSettingName.WorkspaceTeamProjectCollectionUrl),
                            component.GetFieldValue(DependencyProviderValidSettingName.WorkspaceName),
                            component.GetFieldValue(DependencyProviderValidSettingName.WorkspaceOwner));
                    break;
                case ComponentType.Subversion:
                    downloader = new DownloaderSubversion();
                    break;
                default:
                    throw new DependencyManagementFoundationPluginNotFoundException();
            }

            // Add zipper facade if component is a compressen component (and not a source control mapping). In addition get the value that determines if the downloaded archives should be deleted
            if (component.Type != ComponentType.SourceControl &&
                component.GetFieldValue(DependencyProviderValidSettingName.CompressedDependency) == "True")
            {
                var originalDownloader = downloader;
                //Try to parse the value from the settings. if this failes show an error and
                bool deleteArchives = false;
                try
                {
                    if (component.GetFieldValue(DependencyProviderValidSettingName.DeleteArchiveFiles) == "True")
                    {
                        deleteArchives = bool.Parse(component.GetFieldValue(DependencyProviderValidSettingName.DeleteArchiveFiles));
                    }
                }
                catch (Exception)
                {
                    throw new Exception("The settings is missing that determines if the archive files should be deleted, please make sure it is set accordingly");
                }
                downloader = new ZippedDependencyDownloader(originalDownloader, deleteArchives);
            }

            return downloader;
        }

        /// <summary>
        /// Returns the IDependencyDownloader object according to the type provided. Used by clean only
        /// </summary>
        /// <param name="downloadername">Name of the downloader</param>
        /// <returns>Downloader object</returns>
        public IDependencyDownloader GetDownloader(string downloadername)
        {
            IDependencyDownloader downl;

            // Todo MRI: After migration: Remove old Downloader types
            switch (downloadername)
            {
                case "Downloader_FileShare":
                case "Downloader_FileShareCopy":
                    downl = new DownloaderFileShareCopy();
                    break;
                case "Downloader_SourceControl":
                case "Downloader_SourceControlMapping":
                    downl = new DownloaderSourceControlMapping();
                    break;
                case "Downloader_BinaryRepository":
                case "Downloader_SourceControlCopy":
                    downl = new DownloaderSourceControlCopy();
                    break;
                case "Downloader_ZippedDependency":
                    downl = new ZippedDependencyDownloader();
                    break;
                case "Downloader_Subversion":
                    downl = new DownloaderSubversion();
                    break;
                default:
                    throw new DependencyManagementFoundationPluginNotFoundException();
            }

            return downl;
        }
    }
}
