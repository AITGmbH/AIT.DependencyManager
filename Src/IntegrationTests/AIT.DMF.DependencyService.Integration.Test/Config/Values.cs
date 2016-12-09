using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AIT.DMF.DependencyService.Integration.Test.Config
{
    /// <summary>
    /// global cofiguration for all integration tests.
    /// </summary>
    internal static class Values
    {
        private static readonly Lazy<string> PasswordLazy = new Lazy<string>(() => GetEnvironmentVariableValue("DMF_PASSWORD"));
        private static readonly Lazy<string> FileShareFolderLazy = new Lazy<string>(() => GetEnvironmentVariableValue("DMF_FILESHAREFOLDER"));
        private static readonly Lazy<string> WorkSpaceOwnerLazy = new Lazy<string>(() => GetEnvironmentVariableValue("DMF_WORKSPACEOWNER"));
        private static readonly Lazy<string> WorkSpaceNameLazy = new Lazy<string>(() => GetEnvironmentVariableValue("DMF_WORKSPACENAME"));
        private static readonly Lazy<string> TeamProjectCollectionLazy = new Lazy<string>(() => GetEnvironmentVariableValue("DMF_TEAMPROJECTCOLLECTIONURL"));
        private static readonly Lazy<string> TeamProjectNameLazy = new Lazy<string>(() => GetEnvironmentVariableValue("DMF_TEAMPROJECTNAME"));
        private static readonly Lazy<string> XamlBuildDefinitionNameLazy = new Lazy<string>(() => GetEnvironmentVariableValue("DMF_XAMLBUILDDEFINITIONNAME"));

        /* TFS 2013 test system */
        internal static readonly string TeamProjectCollection = TeamProjectCollectionLazy.Value;
        internal static readonly string WorkspaceOwner = WorkSpaceOwnerLazy.Value;
        internal static readonly string Password = PasswordLazy.Value;
        internal static readonly string FileShareFolder = FileShareFolderLazy.Value;

        /* system-independent parameters */
        internal static readonly string LocalRootDisk = @"C:\";
        internal static readonly string TeamProjectName = TeamProjectNameLazy.Value;
        internal static readonly string WorkSpaceName = WorkSpaceNameLazy.Value;
        internal static readonly string PathToTeamProject = Path.Combine(Path.GetTempPath(), WorkSpaceName);
        internal static readonly string LocalWorkspaceFolder = Path.Combine(Environment.CurrentDirectory, WorkSpaceName);
        internal static readonly string DependencyOutputPath = Path.Combine(Environment.CurrentDirectory, WorkSpaceName, "Bin");
        internal static readonly string SvnRepoFile = "SVNRepo.zip";
        internal static readonly string XamlBuildDefinitionName = XamlBuildDefinitionNameLazy.Value;

        private static string GetEnvironmentVariableValue(string environmentVariable)
        {
            if (environmentVariable == null)
            {
                throw new ArgumentNullException(nameof(environmentVariable));
            }

            if (string.IsNullOrWhiteSpace(environmentVariable))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(environmentVariable));
            }

            var value = Environment.GetEnvironmentVariable(environmentVariable);
            if (value == null)
            {
                Assert.Fail($"The test can not be run, because the environment variable '{environmentVariable}' is not set. Please set this variable on the machine where the tests are executed. For further information read the section Testing of README.md in the root of this repository");
            }

            return value;
        }
    }
}
