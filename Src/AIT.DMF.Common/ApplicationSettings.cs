
using Microsoft.Win32;
using System;
using System.IO;
using System.Reflection;

namespace AIT.DMF.DependencyService
{
    /// <summary>
    /// Class to persist application specific settings into the registry
    /// </summary>
    public class ApplicationSettings
    {
        private const string _subkey = "SOFTWARE\\AIT\\DependencyManager";
        private const string _sevenzipDllKey = "SevenZipInstallPath";
        private const string _selectedMultiSiteEntry = "SelectedMultiSiteEntry";

        private static ApplicationSettings _instance;

        private ApplicationSettings() { }

        public const string AutomaticSite = "<Automatic>";

        /// <summary>
        /// The singleton class
        /// </summary>
        public static ApplicationSettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ApplicationSettings();
                }

                return _instance;
            }
        }

        /// <summary>
        /// Get or set the install path (exe) for SevenZip
        /// </summary>
        public string InstallPathForSevenZip
        {
            get
            {
                return GetValueFromRegistry(_sevenzipDllKey);
            }
            set
            {
                SetValueToRegistry(_sevenzipDllKey, value);
            }
        }

        /// <summary>
        /// Gets or set the forced multi site entry from Windows registry
        /// </summary>
        public string SelectedMultiSiteEntry
        {
            get
            {
                var ret = GetValueFromRegistry(_selectedMultiSiteEntry);

                // Does the registry value not exist in Windows Registry GetValueFromRegistry()
                // msbuild.exe:  returns null (unknown reason)
                // Team Build & Visual Studio: return "<Automatic>" (correct!)
                if (ret == null)
                {
                    ret = ApplicationSettings.AutomaticSite;
                }

                return ret;
            }
            set
            {
                SetValueToRegistry(_selectedMultiSiteEntry, value);
            }
        }

        /// <summary>
        /// Determines the folder of 7-zip.exe based on the standard install folders or the applications directory.
        /// </summary>
        /// <returns></returns>
        public string DetermineSevenZipFolder()
        {
            if (File.Exists(Path.Combine(AssemblyDirectory, "7z.exe")))
            {
                return Path.Combine(AssemblyDirectory, "7z.exe");
            }

            var programFiles = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles, Environment.SpecialFolderOption.None));
            if (!string.IsNullOrWhiteSpace(programFiles) && File.Exists(Path.Combine(programFiles, "7-Zip", "7z.exe")))
            {
                return Path.Combine(programFiles, "7-Zip", "7z.exe");
            }

            var programFiles32 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86, Environment.SpecialFolderOption.None);
            if (!string.IsNullOrWhiteSpace(programFiles32) && File.Exists(Path.Combine(programFiles32, "7-Zip", "7z.exe")))
            {
                return Path.Combine(programFiles32, "7-Zip", "7z.exe");
            }

            var programFilesW6432 = Environment.GetEnvironmentVariable("ProgramW6432");
            if (!string.IsNullOrWhiteSpace(programFilesW6432) && File.Exists(Path.Combine(programFilesW6432, "7-Zip", "7z.exe")))
            {
                return Path.Combine(programFilesW6432, "7-Zip", "7z.exe");
            }

            return null;
        }

        /// <summary>
        /// Persist a value in the registry
        /// </summary>
        /// <param name="valueName">The name of the vlaue</param>
        /// <param name="value">The value</param>
        /// <returns>Returns true if persisting was successfull, or false if not</returns>
        private bool SetValueToRegistry(string valueName, string value)
        {
            if (value == null)
            {
                value = string.Empty;
            }

            Registry.SetValue(Registry.CurrentUser + "\\" + _subkey, valueName, value);

            if (value.Equals(GetValueFromRegistry(valueName)) && GetValueFromRegistry(valueName) != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Get a persisted value from the registry, or null if the value does not exist
        /// </summary>
        /// <param name="valueName"></param>
        /// <returns></returns>
        private string GetValueFromRegistry(string valueName)
        {

            return Registry.GetValue(Registry.CurrentUser + "\\" + _subkey, valueName, null) as string;
        }

        /// <summary>
        /// Delete all values set in the registry by the dependecy manager
        /// </summary>
        /// <returns></returns>
        public void DeleteAllValues()
        {
            DeleteValue(_sevenzipDllKey);
        }

        /// <summary>
        /// Delete a single value in the registry
        /// </summary>
        /// <param name="valueName"></param>
        /// <returns></returns>
        private bool DeleteValue(string valueName)
        {
            var key = Registry.CurrentUser.OpenSubKey(_subkey, true);

            if (key == null)
            {
                return false;
            }
            else
            {
                if (key.GetValue(valueName) != null)
                {
                    key.DeleteValue(valueName);

                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        private static string AssemblyDirectory
        {
            get
            {
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }
    }
}
