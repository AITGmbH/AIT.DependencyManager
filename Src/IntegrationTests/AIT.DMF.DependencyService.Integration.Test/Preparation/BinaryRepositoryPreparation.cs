using AIT.DMF.DependencyService.Integration.Test.Config;
using AIT.DMF.DependencyService.Integration.Test.Helpers;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using System;
using System.IO;
using System.Net;

namespace AIT.DMF.DependencyService.Integration.Test.Preparation
{
    internal static class BinaryRepositoryPreparation
    {
        internal static void PrepareBinaryRepositoryEnvironment(string username, string password, string tfsPath, string workspaceName, string tfsProject, string localWorkspaceFolder)
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

                    var targetContent = "<?xml version='1.0'?><Component xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmlns:xsd='http://www.w3.org/2001/XMLSchema' xmlns='http://schemas.aitgmbh.de/DependencyManager/2011/11'><Dependencies><Dependency Type='BinaryDependency'><Provider Type='BinaryRepository'><Settings Type='BinaryRepositorySettings'><Setting Name='BinaryTeamProjectCollectionUrl' Value='{0}' /><Setting Name='BinaryRepositoryTeamProject' Value='{1}' /><Setting Name='ComponentName' Value='BinaryRepoSource' /><Setting Name='VersionNumber' Value='1.0' /></Settings></Provider></Dependency></Dependencies></Component>";
                    targetContent = string.Format(targetContent, Values.TeamProjectCollection, Values.TeamProjectName);
                    FileHelper.CheckInFolderWithFile(workspace, "BinaryRepo", "component.targets", targetContent);

                    var emptyTarget = "<?xml version='1.0'?><Component xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmlns:xsd='http://www.w3.org/2001/XMLSchema' xmlns='http://schemas.aitgmbh.de/DependencyManager/2011/11'><Dependencies /></Component>";
                    CheckInFolderSubFoldersWithFiles(workspace, "BinaryRepoSource", "1.0", "component.targets", emptyTarget);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
        private static void CheckInFolderSubFoldersWithFiles(Workspace workspace, string folderName, string subFolderName, string fileName, string targetContent)
        {
            string newDir = Path.Combine(workspace.Folders[0].LocalItem, folderName);
            Directory.CreateDirectory(newDir);

            string subDir1 = Path.Combine(workspace.Folders[0].LocalItem, folderName, subFolderName);
            Directory.CreateDirectory(subDir1);
            string filePath = Path.Combine(subDir1, fileName);
            string filePathTest = Path.Combine(subDir1, "test1.txt");
            using (StreamWriter sw = new StreamWriter(filePath))
            {
                sw.WriteLine(targetContent);
                sw.Close();
            }
            using (StreamWriter sw = new StreamWriter(filePathTest))
            {
                sw.WriteLine(Guid.NewGuid().ToString());
                sw.Close();
            }
            workspace.PendAdd(newDir, true);
            PendingChange[] changes = workspace.GetPendingChanges();
            if (changes.Length > 0)
            {
                int changeSetNumber = workspace.CheckIn(changes, $"Add file {fileName} - {DateTime.Now.ToString()}");
            }
        }
    }
}
