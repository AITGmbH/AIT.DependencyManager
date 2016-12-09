namespace AIT.DMF.Contracts.Services
{
    /// <summary>
    /// Contains all valid settings which can be added to the key value dictionary (ServiceSettings) for a DependencyResolver.
    /// </summary>
    public enum ResolverValidSettings
    {
        FileShareUrl,
        TeamProjectCollectionUrl,
        WorkspaceName,
        WorkspaceOwner,
        BinaryTeamProjectCollectionUrl,
        BinaryRepositoryTeamProject,
        DependencyDefinitionFileNameList,
        SubversionUrl
    }
}
