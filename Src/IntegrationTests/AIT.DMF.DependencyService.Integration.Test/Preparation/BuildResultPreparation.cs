using AIT.DMF.DependencyService.Integration.Test.Config;
using AIT.DMF.DependencyService.Integration.Test.Helpers;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using System;
using System.Net;

namespace AIT.DMF.DependencyService.Integration.Test.Preparation
{
    /// <summary>
    /// In progress..
    /// </summary>
    internal static class BuildResultPreparation
    {
        internal static void PrepareBuildResultEnvironment(string username, string password, string tfsPath, string workspaceName, string tfsProject, string localWorkspaceFolder)
        {
            NetworkCredential cred = new NetworkCredential(username, password);
            using (TfsTeamProjectCollection tfs = new TfsTeamProjectCollection(new Uri(tfsPath), cred))
            {
                tfs.EnsureAuthenticated();

                var vcs = tfs.GetService<VersionControlServer>();

                var workspace = WorkspaceHelper.WorkspaceCreate(vcs, tfsProject, workspaceName, localWorkspaceFolder);
                workspace.Get();

                var targetContent = $"<?xml version='1.0' ?><Component xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmlns:xsd='http://www.w3.org/2001/XMLSchema' xmlns='http://schemas.aitgmbh.de/DependencyManager/2011/11'><Dependencies><Dependency Type='BinaryDependency'><Provider Type='BuildResult'><Settings Type='BuildResultSettings'><Setting Name='TeamProjectName' Value='{Values.TeamProjectName}'/><Setting Name='BuildDefinition' Value='{Values.XamlBuildDefinitionName}'/><Setting Name='BuildStatus' Value='Succeeded'/></Settings></Provider></Dependency></Dependencies></Component>";
                FileHelper.CheckInFolderWithFile(workspace, "XAMLBuildResult", "component.targets", targetContent);
            }
        }
    }
}

