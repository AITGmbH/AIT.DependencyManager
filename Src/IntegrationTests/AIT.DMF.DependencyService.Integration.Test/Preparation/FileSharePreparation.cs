using AIT.DMF.DependencyService.Integration.Test.Helpers;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using System;
using System.IO;
using System.Net;

namespace AIT.DMF.DependencyService.Integration.Test.Preparation
{
    internal static class FileSharePreparation
    {

        /// <summary>
        /// To run this preparation steps successfully a Windows File Share must be created at "fileShareFolder" (no content necessary).
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="tfsPath"></param>
        /// <param name="workspaceName"></param>
        /// <param name="tfsProject"></param>
        /// <param name="localWorkspaceFolder"></param>
        /// <param name="fileShareFolder"></param>
        internal static void PrepareFileShareTestEnvironment(string username, string password, string tfsPath, string workspaceName, string tfsProject, string localWorkspaceFolder, string fileShareFolder)
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

                    var targetContent = $"<?xml version='1.0'?><Component xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmlns:xsd='http://www.w3.org/2001/XMLSchema' xmlns='http://schemas.aitgmbh.de/DependencyManager/2011/11'><Dependencies><Dependency Type='BinaryDependency'><Provider Type='FileShare'><Settings Type='FileShareSettings'><Setting Name='FileShareRootPath' Value='{fileShareFolder}' /><Setting Name='ComponentName' Value='comp1' /><Setting Name='VersionNumber' Value='V1.0' /></Settings></Provider></Dependency></Dependencies></Component>";
                    FileHelper.CheckInFolderWithFile(workspace, "FileShareTarget", "component.targets", targetContent);
                    
                    var subPath = Path.Combine(fileShareFolder, "comp1");
                    var subSubPath = Path.Combine(subPath, "V1.0");
                    var subSubPath2 = Path.Combine(subPath, "V2.0");

                    Directory.CreateDirectory(subSubPath);
                    Directory.CreateDirectory(subSubPath2);

                    var emptyTarget = "<?xml version='1.0'?><Component xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmlns:xsd='http://www.w3.org/2001/XMLSchema' xmlns='http://schemas.aitgmbh.de/DependencyManager/2011/11'><Dependencies /></Component>";
                    FileHelper.AddFileInLocal(subSubPath, "component.targets", emptyTarget);
                    FileHelper.AddFileInLocal(subSubPath2, "component.targets", emptyTarget);
                    FileHelper.AddFileInLocal(subSubPath, "Example1.dll", "dummy content");
                    FileHelper.AddFileInLocal(subSubPath2, "Example2.dll", "dummy content");
                }
            }
            catch
            {
                throw;
            }
        }
    }
}
