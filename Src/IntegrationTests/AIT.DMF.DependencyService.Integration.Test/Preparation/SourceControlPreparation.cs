using AIT.DMF.DependencyService.Integration.Test.Config;
using AIT.DMF.DependencyService.Integration.Test.Helpers;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using System;
using System.Net;

namespace AIT.DMF.DependencyService.Integration.Test.Preparation
{
    internal static class SourceControlPreparation
    {
        internal static void PrepareSourceControlTestEnvironment(string username, string password, string tfsPath, string tfsProject, string workspaceName, string localWorkspaceFolder, bool sourceControlMapping = false)
        {
            try
            {
                NetworkCredential cred = new NetworkCredential(username, password);
                using (TfsTeamProjectCollection tfs = new TfsTeamProjectCollection(new Uri(tfsPath), cred))
                {
                    tfs.EnsureAuthenticated();

                    VersionControlServer vcs = (VersionControlServer)tfs.GetService(typeof(VersionControlServer));
                    
                    var workspace = WorkspaceHelper.WorkspaceCreate(vcs, tfsProject, workspaceName, localWorkspaceFolder);
                    workspace.Get();

                    string targetContent;
                    if (!sourceControlMapping)
                    {
                        targetContent = $"<?xml version='1.0'?><Component xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmlns:xsd='http://www.w3.org/2001/XMLSchema' xmlns='http://schemas.aitgmbh.de/DependencyManager/2011/11'><Dependencies><Dependency Type='BinaryDependency'><Provider Type='SourceControlCopy'><Settings Type='SourceControlSettings'><Setting Name='ServerRootPath' Value='$/{Values.TeamProjectName}/DependencySource' /><Setting Name='VersionSpec' Value='T' /></Settings></Provider></Dependency></Dependencies></Component>";
                    }
                    else
                    {
                        targetContent = $"<?xml version='1.0'?><Component xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmlns:xsd='http://www.w3.org/2001/XMLSchema' xmlns='http://schemas.aitgmbh.de/DependencyManager/2011/11'><Dependencies><Dependency Type='SourceDependency'><Provider Type='SourceControl'><Settings Type='SourceControlSettings'><Setting Name='ServerRootPath' Value='$/{Values.TeamProjectName}/DependencySource' /><Setting Name='VersionSpec' Value='T' /></Settings></Provider></Dependency></Dependencies></Component>";
                    }
                    FileHelper.CheckInFolderWithFile(workspace, "SourceControlTarget", "component.targets", targetContent);

                    FileHelper.CheckInFolderWithFile(workspace, "DependencySource", "helloWorld.dll", "dummy content");

                    var emptyTargetContent = "<?xml version='1.0'?><Component xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmlns:xsd='http://www.w3.org/2001/XMLSchema' xmlns='http://schemas.aitgmbh.de/DependencyManager/2011/11'><Dependencies /></Component>";
                    FileHelper.CheckInFolderWithFile(workspace, "DependencySource", "component.targets", emptyTargetContent);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }      
    }
}
