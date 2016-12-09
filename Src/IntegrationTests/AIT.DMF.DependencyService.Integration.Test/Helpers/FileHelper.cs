using Microsoft.TeamFoundation.VersionControl.Client;
using System;
using System.IO;
using System.Management;
using System.Security.AccessControl;
using System.Security.Principal;


namespace AIT.DMF.DependencyService.Integration.Test.Helpers
{
    internal static class FileHelper
    {
        internal static void QshareFolder(string FolderPath, string ShareName, string Description)
        {
            // Create a ManagementClass object
            using (ManagementClass managementClass = new ManagementClass("Win32_Share"))
            // Create ManagementBaseObjects for in and out parameters
            using (ManagementBaseObject inParams = managementClass.GetMethodParameters("Create"))
            {
                //ManagementBaseObject outParams;
                // Set the input parameters
                inParams["Description"] = Description;
                inParams["Name"] = ShareName;
                inParams["Path"] = FolderPath;
                inParams["Type"] = 0x0; // Disk Drive
                using (var outParams = managementClass.InvokeMethod("Create", inParams, null)) { }
            }
        }

        internal static string AddFolderInLocal(string rootLocal, string folderName)
        {
            string newDir = Path.Combine(rootLocal, folderName);
            Directory.CreateDirectory(newDir);
            return newDir;
        }

        internal static void CheckInFolderWithFile(Workspace workspace, string folderName, string fileName, string targetContent)
        {
            string newDir = Path.Combine(workspace.Folders[0].LocalItem, folderName);
            Directory.CreateDirectory(newDir);
            string filePath = Path.Combine(newDir, fileName);
            using (StreamWriter sw = new StreamWriter(filePath))
            {
                sw.WriteLine(targetContent);
                sw.Close();
            }
            workspace.PendAdd(newDir, true);
            PendingChange[] changes = workspace.GetPendingChanges();
            if (changes.Length > 0)
            {
                int changeSetNumber = workspace.CheckIn(changes, $"Add file {fileName} - {DateTime.Now.ToString()}");
            }
        }

        internal static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, false);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath, copySubDirs);
                }
            }
        }

        internal static void AddFileInLocal(string path, string fileName, string targetContent)
        {
            string filePath = Path.Combine(path, fileName);
            using (StreamWriter sw = new StreamWriter(filePath))
            {
                sw.WriteLine(targetContent);
                sw.Close();
            }
        }

        internal static void FolderNetworkAccessEveryone(string path)
        {
            DirectorySecurity sec = Directory.GetAccessControl(path);
            DirectoryInfo dInfo = new DirectoryInfo(path);

            // Using this instead of the "Everyone" string means we work on non-English systems.
            SecurityIdentifier everyone = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
            sec.AddAccessRule(new FileSystemAccessRule(everyone, FileSystemRights.FullControl | FileSystemRights.Synchronize, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow));
            Directory.SetAccessControl(path, sec);

        }

        internal static void BuildDataToSharedFolder(string rootLocal, string buildLocation, string folderName)
        {
            var folderPath = FileHelper.AddFolderInLocal(rootLocal, folderName);
            if (Directory.Exists(folderPath))
            {
                Directory.Delete(folderPath, true);
            }
            FileHelper.QshareFolder(folderPath, "Shared2", "some descr...");
            FileHelper.DirectoryCopy(buildLocation, folderPath, true);
        }
    }
}
