using AIT.DMF.DependencyService.Integration.Test.Helpers;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using System;
using System.Net;

namespace AIT.DMF.DependencyService.Integration.Test.Preparation
{
    internal static class SubversionPreparation
    {
        internal static void PrepareSubVersionTestEnvironment(string username, string password, string tfsPath, string workspaceName, string tfsProject, string localWorkspaceFolder, string subversionPath)
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

                    var targetContent = "<?xml version='1.0'?><Component xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmlns:xsd='http://www.w3.org/2001/XMLSchema' xmlns='http://schemas.aitgmbh.de/DependencyManager/2011/11'><Dependencies><Dependency Type='BinaryDependency'><Provider Type='Subversion'><Settings Type='SubversionSettings'><Setting Name='SubversionRootPath' Value='file:///{0}/trunk/lib/ComponentA/1.0' /><Setting Name='VersionSpec' Value='H' /></Settings></Provider></Dependency></Dependencies></Component>";
                    targetContent = string.Format(targetContent, subversionPath);
                    FileHelper.CheckInFolderWithFile(workspace, "Subversion", "component.targets", targetContent);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
