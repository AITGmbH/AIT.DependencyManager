using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using System;
using System.Net;

namespace AIT.DMF.DependencyService.Integration.Test.Helpers
{
    class WorkspaceHelper
    {
        internal static Workspace WorkspaceCreate(VersionControlServer vcs, string tfsProject, string workspaceName, string localWorkspaceFolder)
        {
            var workspaceParams = new CreateWorkspaceParameters(workspaceName)
            {
                Computer = Environment.MachineName,
                Folders = new[]
                        {
                            new WorkingFolder(string.Format(@"$/{0}", tfsProject), localWorkspaceFolder)
                        },
                Location = Microsoft.TeamFoundation.VersionControl.Common.WorkspaceLocation.Local,
                OwnerDisplayName = Environment.UserName
            };

            return vcs.CreateWorkspace(workspaceParams);
        }

        internal static void WorkspaceCleanup(string username, string password, string tfsPath, string workspaceName, string workspaceOwner)
        {
            NetworkCredential cred = new NetworkCredential(username, password);
            using (TfsTeamProjectCollection tfs = new TfsTeamProjectCollection(new Uri(tfsPath), cred))
            {
                tfs.EnsureAuthenticated();

                VersionControlServer vcs = (VersionControlServer)tfs.GetService(typeof(VersionControlServer));

                vcs.DeleteWorkspace(workspaceName, workspaceOwner);
            }
        }
    }
}
