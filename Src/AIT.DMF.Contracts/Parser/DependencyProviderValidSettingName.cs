namespace AIT.DMF.Contracts.Parser
{
    public enum DependencyProviderValidSettingName
    {
        IncludeFilter,
        ExcludeFilter,
        ExcludeFoldersFilter,
        IncludeFoldersFilter,
        ComponentName,
        VersionNumber,
        RelativeOutputPath,
        FolderMappings,
        CompressedDependency,
        //Delete archivefile
        DeleteArchiveFiles,
        IgnoreInSideBySideAnomalyChecks,
        // File share
        FileShareRootPath,
        /// Build result
        BuildTeamProjectCollectionUrl,
        TeamProjectName,
        BuildDefinition,
        BuildNumber,
        BuildQuality,
        BuildStatus,
        // Source Control
        WorkspaceName,
        WorkspaceOwner,
        WorkspaceTeamProjectCollectionUrl,
        ServerRootPath,
        VersionSpec,
        // Binary Repository
        BinaryTeamProjectCollectionUrl,
        BinaryRepositoryTeamProject,
        // Subversion Repository
        SubversionRootPath,
        BuildTags
    }
}
