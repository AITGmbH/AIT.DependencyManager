using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Deployment.WindowsInstaller;

namespace CustomAction
{

    enum SelectionMode
    {
        SevenzipExe
    }

    /// <summary>
    /// Custom Action that allows picking of a specific file
    /// </summary>
    public class CustomAction
    {
        private static string _pathToSevenZipExe;
        private static SelectionMode _mode;
        private const string SevenZipExeFileName = "\\7z.exe";
        private const string SevenZipExeErrorMessage = "The 7z.exe is missing or the filename is wrong.";

        /// <summary>
        /// The action
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        [CustomAction]
        public static ActionResult OpenFileChooser(Session session)
        {
            session.Log("Begin OpenFileChooser Custom Action");
            //Get the current mode
            if ("SEVENZIP".Equals(session.CustomActionData["FILE_SELECT_MODE"]))
            {
                _mode = SelectionMode.SevenzipExe;
            }

            session.Log("Custom Action Data Mode is:" + session.CustomActionData["FILE_SELECT_MODE"]);

            //Start the task that opens the file handler
            var task = new Thread(() => GetFile(session));
            task.SetApartmentState(ApartmentState.STA);
            task.Start();
            task.Join();

            if (_mode.Equals(SelectionMode.SevenzipExe))
            {
                session.Log(string.Format("Saving: {1} to CustomActionData: {0}", session.CustomActionData["FILE_SELECT_MODE"], _pathToSevenZipExe));

                session["SEVENZIP_EXE_DIR"] = _pathToSevenZipExe;
            }

            session.Log("Finished OpenFileChooser Custom Action");

            return ActionResult.Success;
        }

        /// <summary>
        /// The helper that opens the file picker dialog
        /// </summary>
        /// <param name="session"></param>
        private static void GetFile(Session session)
        {
            var fileDialog = new OpenFileDialog { Filter = "Exe File (*.exe)|*.exe" };
            DialogResult result = fileDialog.ShowDialog();
            if (result == DialogResult.Yes || result == DialogResult.OK)
            {
                session.Log("Dialog Finished. Setting the values");
                if (_mode.Equals(SelectionMode.SevenzipExe))
                {
                    session.Log("Set Path to 7-Zip:" + fileDialog.FileName);
                    _pathToSevenZipExe = fileDialog.FileName;
                }
            }
        }

        /// <summary>
        /// Custom Action that checks the existence of the files and returns the value over the session properties
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        [CustomAction]
        public static ActionResult ValidateFileExistence(Session session)
        {
            session.Log("7ZipSupport:" + session["DLLS_EXIST"]);

            //Seven Zip is disabled
            if ((("").Equals(session["ENABLE_SEVENZIP_SUPPORT"])) ||
                (session["ENABLE_SEVENZIP_SUPPORT"] == null) ||
                (("0").Equals(session["ENABLE_SEVENZIP_SUPPORT"]))
                )
            {
                session.Log("SevenZipSupport is disabled");
                session["DLLS_EXIST"] = "1";
                return ActionResult.Success;
            }

            if (("").Equals(session["SEVENZIP_EXE_DIR"]) || (session["SEVENZIP_EXE_DIR"] == null))
            {
                session["DLLS_EXIST"] = "0";
                session["DLL_ERROR_MESSAGE"] = SevenZipExeErrorMessage;
                return ActionResult.Success;
            }

            //Build the filepaths
            string sevenZipFilePath = Path.GetDirectoryName(session["SEVENZIP_EXE_DIR"]);


            //Build the filepaths
            string desiredSevenZipFullPath = sevenZipFilePath + SevenZipExeFileName;

            session.Log("Looking for file: " + desiredSevenZipFullPath);


            //Check the existence of the files and check that they meet the desired filename
            if (!((File.Exists(desiredSevenZipFullPath)) && (desiredSevenZipFullPath.Equals(session["SEVENZIP_EXE_DIR"]))))
            {
                session["DLLS_EXIST"] = "0";
                session["DLL_ERROR_MESSAGE"] = SevenZipExeErrorMessage;
                session.Log(File.Exists(desiredSevenZipFullPath) ? "Seven Zip Sharp Exe exists." : "Seven Zip Sharp Exe does not exist.");
                return ActionResult.Success;
            }
            else
            {
                session.Log(File.Exists(desiredSevenZipFullPath) ? "Seven Zip Sharp Exe exists." : "Seven Zip Sharp Exe does not exist.");
                session["DLLS_EXIST"] = "1";
                return ActionResult.Success;
            }

        }
    }
}
