namespace AIT.DMF.Contracts.Services
{
    /// <summary>
    /// Contains all valid settings which can be added to the key value dictionary (ServiceSettings) for a DependencyDownloader.
    /// </summary>
    public enum DownloaderValidSettings
    {
        IncludedFilesFilter,
        ExcludedFilesFilter,
        IncludedFoldersFilter,
        ExcludedFoldersFilter,
        VersionString,
        Configuration,
        FolderMappings,
        OperationType,
        MultiSiteEntries,
        SubversionUser
    }
}
