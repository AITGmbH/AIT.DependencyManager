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
    internal static class VNextBuildResultPreparation
    {
        internal static void PrepareVNextBuildResultEnvironment(string username, string password, string tfsPath, string workspaceName, string buildLocation, string rootLocal)
        {
            Workspace workspace = null;
            try
            {
                NetworkCredential cred = new NetworkCredential(username, password);
                using (TfsTeamProjectCollection tfs = new TfsTeamProjectCollection(new Uri(tfsPath), cred))
                {
                    FileHelper.BuildDataToSharedFolder(rootLocal, buildLocation + "/TestData/JsonBuildData", "Shared2");
                    tfs.EnsureAuthenticated();

                    VersionControlServer vcs = (VersionControlServer)tfs.GetService(typeof(VersionControlServer));

                    workspace = vcs.GetWorkspace(workspaceName, vcs.AuthenticatedUser);
                    workspace.Get();
                    var targetContent = "<?xml version='1.0'?><Component xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmlns:xsd='http://www.w3.org/2001/XMLSchema' xmlns='http://schemas.aitgmbh.de/DependencyManager/2011/11'><Dependencies><Dependency Type='BinaryDependency'><Provider Type='BuildResultJSON'><Settings Type='VNextBuildResultSettings'><Setting Name='TeamProjectName' Value='FabrikamFiber' /><Setting Name='BuildDefinition' Value='MsBuild test' /><Setting Name='BuildStatus' Value='Completed' /></Settings></Provider></Dependency></Dependencies></Component>";
                    FileHelper.CheckInFolderWithFile(workspace, "VNextBuildResult", "component.targets", targetContent);
                }
            }
            catch (Exception e)
            {
                throw;
            }
        }
    }
}

