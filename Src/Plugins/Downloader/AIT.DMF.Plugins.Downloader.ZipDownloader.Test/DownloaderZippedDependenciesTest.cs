using System.Collections.Generic;
using AIT.DMF.Contracts.Provider;
using AIT.DMF.Contracts.Services;
using AIT.DMF.DependencyService;
using AIT.DMF.Plugins.Downloader.FileShareCopy;
using AIT.DMF.Plugins.Downloader.ZippedDependency;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;

namespace AIT.DMF.Plugins.Downloader.ZipDownloader.Test
{
    [TestClass]
    [DeploymentItem("Resources", "Resources")]
    public class DownloaderZippedDependenciesTest
    {
        private static string _workingDir = string.Empty;
        private static string _sourceDir = string.Empty;
        private static string _targetDir = string.Empty;
        private static string _resourcestDir = "Resources";

        private static void CopySevenZipFilesToTestDirectory()
        {
            var directory = default(string);
            var programFiles = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles, Environment.SpecialFolderOption.None));
            if (!string.IsNullOrWhiteSpace(programFiles) && File.Exists(Path.Combine(programFiles, "7-Zip", "7z.exe")))
            {
                directory = Path.Combine(programFiles, "7-Zip");
            }

            var programFiles32 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86, Environment.SpecialFolderOption.None);
            if (!string.IsNullOrWhiteSpace(programFiles32) && File.Exists(Path.Combine(programFiles32, "7-Zip", "7z.exe")))
            {
                directory = Path.Combine(programFiles32, "7-Zip");
            }

            var programFilesW6432 = Environment.GetEnvironmentVariable("ProgramW6432");
            if (!string.IsNullOrWhiteSpace(programFilesW6432) && File.Exists(Path.Combine(programFilesW6432, "7-Zip", "7z.exe")))
            {
                directory = Path.Combine(programFilesW6432, "7-Zip");
            }

            File.Copy(directory + "\\7z.exe", Environment.CurrentDirectory + "\\Resources\\7z.exe");
            File.Copy(directory + "\\7z.dll", Environment.CurrentDirectory + "\\Resources\\7z.dll");
        }

        [ClassInitialize()]
        public static void Initialize(TestContext testContext)
        {
            // Generate necessary directory names
            var tempPath = Path.GetTempPath();
            var guid = Guid.NewGuid().ToString();

            _workingDir = Path.Combine(tempPath, guid);
            _sourceDir = Path.Combine(tempPath, guid, "ZippedDependency", "1.0");
            // A source dir must be a network share
            _sourceDir = @"\\" + _sourceDir.Replace("C:", Environment.MachineName);
            _targetDir = Path.Combine(tempPath, guid, "targetDir");

            // Create necessary directories
            Directory.CreateDirectory(_workingDir);
            Directory.CreateDirectory(_sourceDir);
            Directory.CreateDirectory(_targetDir);

            CopySevenZipFilesToTestDirectory();

            // Copy necessary files (a component.targets file)
            File.Copy(Path.Combine(Environment.CurrentDirectory, _resourcestDir,"component.targets"), Path.Combine(_sourceDir, "component.targets"));
        }

        [ClassCleanup()]
        public static void Cleanup()
        {
            Directory.Delete(_workingDir, true);
            File.Delete(Path.Combine(Environment.CurrentDirectory, "7z.dll"));
            File.Delete(Path.Combine(Environment.CurrentDirectory, "SevenZipSharp.dll"));
        }

        [TestCleanup()]
        public void TestCleanup()
        {
            //Remove all DLL path from registry
            ApplicationSettings.Instance.DeleteAllValues();

            //Remove all files
            DeleteFile(Path.Combine(_sourceDir, "lzmaExample.7z"));
            DeleteFile(Path.Combine(_targetDir, "lzmaExample.7z"));
            DeleteFile(Path.Combine(_sourceDir, "lzmaDifferentBaseFolder.7z"));
            DeleteFile(Path.Combine(_targetDir, "lzmaDifferentBaseFolder.7z"));
            DeleteFile(Path.Combine(_sourceDir, "lzmaDifferentBaseFolder2.7z"));
            DeleteFile(Path.Combine(_targetDir, "lzmaDifferentBaseFolder2.7z"));

            DeleteFile(Path.Combine(_sourceDir, "example.zip"));
            DeleteFile(Path.Combine(_targetDir, "example.zip"));
            DeleteFile(Path.Combine(_targetDir, "lzmareadme.txt"));
            DeleteFile(Path.Combine(_targetDir, "readme.txt"));
            DeleteFile(Path.Combine(_targetDir, "sample.xml"));
            DeleteFile(Path.Combine(_targetDir, "lzmasubfolder", "lzmareadme.txt"));
            DeleteFile(Path.Combine(_targetDir, "lzmasubfolder", "sampleSubfolder.xml"));
            DeleteFile(Path.Combine(_targetDir, "subfolder", "readme.txt"));

            DeleteFile(Path.Combine(_targetDir, "lzmaDifferentBaseFolder", "lzmasubfolder", "AnotherFolder", "lzmareadme.xml"));
            DeleteFile(Path.Combine(_targetDir, "lzmaDifferentBaseFolder", "lzmasubfolder", "AnotherFolder", "sampleSubfolder.xml"));
            DeleteFolder(Path.Combine(_targetDir, "lzmaDifferentBaseFolder","lzmasubfolder", "AnotherFolder"));

            DeleteFile(Path.Combine(_targetDir, "lzmaDifferentBaseFolder2", "lzmasubfolder", "AnotherFolder2", "lzmareadme.xml"));
            DeleteFile(Path.Combine(_targetDir, "lzmaDifferentBaseFolde2r", "lzmasubfolder", "AnotherFolder2", "sampleSubfolder.xml"));
            DeleteFolder(Path.Combine(_targetDir, "lzmaDifferentBaseFolder2", "lzmasubfolder", "AnotherFolder2"));

            DeleteFolder(Path.Combine(_targetDir, "lzmasubfolder"));
            DeleteFolder(Path.Combine(_targetDir, "subfolder"));
            DeleteFolder(Path.Combine(_targetDir, "lzmaDifferentBaseFolder"));
            DeleteFolder(Path.Combine(_targetDir, "lzmaDifferentBaseFolder2"));
        }

        /// <summary>
        /// Helper to delete a file if it exists
        /// </summary>
        /// <param name="path"></param>
        private void DeleteFile(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        /// <summary>
        /// Helper to delete a folder if it exists
        /// </summary>
        /// <param name="folder"></param>
        private void DeleteFolder(string folder)
        {
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder);
            }
        }

        /// <summary>
        /// Helper class that enables seven zip and set the path to registry
        /// </summary>
        private static void EnableSevenZipSupportAndSetPath()
        {
            ApplicationSettings.Instance.InstallPathForSevenZip = Path.Combine(Environment.CurrentDirectory, _resourcestDir, "7z.exe");
        }

        /// <summary>
        /// Test of the basic zipping functionality, which simulates a zipped file from a network share
        /// </summary>
        [TestMethod]
        public void Download_StandardZippedDependenciesDeletionFalse_Test()
        {
            File.Delete(Path.Combine(_sourceDir, "example.zip"));
            File.Copy(Path.Combine(Environment.CurrentDirectory, _resourcestDir, "example.zip"), Path.Combine(_sourceDir, "example.zip"));

            IDependencyDownloader downloader = new DownloaderFileShareCopy();
            var originalDownloader = downloader;
            downloader = new ZippedDependencyDownloader(originalDownloader,false);

            downloader.Download(_sourceDir, _targetDir, new DummyWatermark(), false, new Settings<DownloaderValidSettings>());

            //The archive should still be there
            Assert.IsTrue(File.Exists(Path.Combine(_targetDir, "example.zip")));

            Assert.IsTrue(File.Exists(Path.Combine(_targetDir, "readme.txt")));
            Assert.IsTrue(File.Exists(Path.Combine(_targetDir, "subfolder", "readme.txt")));
        }

        /// <summary>
        /// Test of the basic zipping functionality, which simulates a zipped file from a network share
        /// </summary>
        [TestMethod]
        public void Download_StandardZippedDependenciesDeletionTrue_Test()
        {
            File.Delete(Path.Combine(_sourceDir, "example.zip"));
            File.Copy(Path.Combine(Environment.CurrentDirectory, _resourcestDir,"example.zip"), Path.Combine(_sourceDir, "example.zip"));

            IDependencyDownloader downloader = new DownloaderFileShareCopy();
            var originalDownloader = downloader;
            downloader = new ZippedDependencyDownloader(originalDownloader, true);

            downloader.Download(_sourceDir, _targetDir, new DummyWatermark(), false, new Settings<DownloaderValidSettings>());

            //The archive should still be there
            Assert.IsFalse(File.Exists(Path.Combine(_targetDir, "example.zip")));

            Assert.IsTrue(File.Exists(Path.Combine(_targetDir, "readme.txt")));
            Assert.IsTrue(File.Exists(Path.Combine(_targetDir, "subfolder", "readme.txt")));
        }

        [TestMethod]
        public void Download_SevenZippedDependenciesDeletionFalse_Test()
        {
            //Arrange - Set 7zip DLL path in registry
            EnableSevenZipSupportAndSetPath();
            File.Copy(Path.Combine(Environment.CurrentDirectory, _resourcestDir, "lzmaExample.7z"), Path.Combine(_sourceDir, "lzmaExample.7z"));

            IDependencyDownloader downloader = new DownloaderFileShareCopy();
            var originalDownloader = downloader;
            downloader = new ZippedDependencyDownloader(originalDownloader,false);

            downloader.Download(_sourceDir, _targetDir, new DummyWatermark(), false, new Settings<DownloaderValidSettings>());

            Assert.IsTrue(File.Exists(Path.Combine(_targetDir, "lzmareadme.txt")));
            Assert.IsTrue(File.Exists(Path.Combine(_targetDir, "lzmasubfolder", "lzmareadme.txt")));

            //The lzmaFile should still be there
            Assert.IsTrue(File.Exists(Path.Combine(_targetDir, "lzmaExample.7z")));
        }

        /// <summary>
        /// Test the deletion functionalitiy
        /// </summary>
        [TestMethod]
        public void Download_SevenZippedDependenciesDeletionTrue_Test()
        {
            //Arrange - Set 7zip and sevensharpzip DLL path in registry
            EnableSevenZipSupportAndSetPath();
            //ApplicationSettings.Instance.InstallPathForSevenZipSharp = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, @"..\..\..\..\..\..\Lib\SevenZipSharp\0.64\SevenZipSharp.dll"));
            File.Copy(Path.Combine(Environment.CurrentDirectory, _resourcestDir, "lzmaExample.7z"), Path.Combine(_sourceDir, "lzmaExample.7z"));

            IDependencyDownloader downloader = new DownloaderFileShareCopy();
            var originalDownloader = downloader;
            downloader = new ZippedDependencyDownloader(originalDownloader, true);

            downloader.Download(_sourceDir, _targetDir, new DummyWatermark(), false, new Settings<DownloaderValidSettings>());

            Assert.IsTrue(File.Exists(Path.Combine(_targetDir, "lzmareadme.txt")));
            Assert.IsTrue(File.Exists(Path.Combine(_targetDir, "sample.xml")));
            Assert.IsTrue(File.Exists(Path.Combine(_targetDir, "lzmasubfolder", "lzmareadme.txt")));
            Assert.IsTrue(File.Exists(Path.Combine(_targetDir, "lzmasubfolder", "sampleSubfolder.xml")));
            //The lzmaFile should not be there
            Assert.IsFalse(File.Exists(Path.Combine(_targetDir, "lzmaExample.7z")));
        }

        /// <summary>
        /// Test if the exclusion of files work properly
        /// exclude the folder "lzmasubfolder"
        /// </summary>
        ///         /// Test currently ignored, as inclusion and exclusion is not used with  zipped dependencies
        [TestMethod]
        [Ignore]
        public void Download_SevenZippedDependenciesDeletionTrueExcludedFolders_Test()
        {
            //Arrange - Set 7zip and sevensharpzip DLL path in registry
            EnableSevenZipSupportAndSetPath();
            File.Copy(Path.Combine(Environment.CurrentDirectory, _resourcestDir, "lzmaExample.7z"), Path.Combine(_sourceDir, "lzmaExample.7z"));

            //Create the excludefilter
            var pair = new KeyValuePair<DownloaderValidSettings, string>(DownloaderValidSettings.ExcludedFilesFilter, "lzmasubfolder\\");
            var settings = new Settings<DownloaderValidSettings>();
            settings.AddSetting(pair);

            IDependencyDownloader downloader = new DownloaderFileShareCopy();
            var originalDownloader = downloader;
            downloader = new ZippedDependencyDownloader(originalDownloader, true);

            downloader.Download(_sourceDir, _targetDir, new DummyWatermark(), false, settings);

            Assert.IsTrue(File.Exists(Path.Combine(_targetDir, "lzmareadme.txt")));
            Assert.IsTrue(File.Exists(Path.Combine(_targetDir, "sample.xml")));
            //The lzma subfolder and its files should not be there
            Assert.IsFalse(Directory.Exists(Path.Combine(_targetDir, "lzmasubfolder")));
            Assert.IsFalse(File.Exists(Path.Combine(_targetDir, "lzmasubfolder", "lzmareadme.txt")));

            //The lzmaFile should not be there
            Assert.IsFalse(File.Exists(Path.Combine(_targetDir, "lzmaExample.7z")));
        }

        /// <summary>
        /// Test if the exclusion of files work properly
        /// exclude the folder all txt files
        /// </summary>
        /// Test currently ignored, as inclusion and exclusion is not used with  zipped dependencies
        [TestMethod]
        [Ignore]
        public void Download_SevenZippedDependenciesDeletionTrueExcludedFiles_Test()
        {

            //Arrange - Set 7zip and sevensharpzip DLL path in registry
            EnableSevenZipSupportAndSetPath();
            File.Copy(Path.Combine(Environment.CurrentDirectory, _resourcestDir, "lzmaExample.7z"), Path.Combine(_sourceDir, "lzmaExample.7z"));

            //Create the excludefilter
            var pair = new KeyValuePair<DownloaderValidSettings, string>(DownloaderValidSettings.ExcludedFilesFilter, "*.txt");
            var settings = new Settings<DownloaderValidSettings>();
            settings.AddSetting(pair);

            IDependencyDownloader downloader = new DownloaderFileShareCopy();
            var originalDownloader = downloader;
            downloader = new ZippedDependencyDownloader(originalDownloader, true);

            downloader.Download(_sourceDir, _targetDir, new DummyWatermark(), false, settings);

            Assert.IsFalse(File.Exists(Path.Combine(_targetDir, "lzmareadme.txt")));
            Assert.IsTrue(File.Exists(Path.Combine(_targetDir, "sample.xml")));

            //The lzma subfolder should be there with xml files
            Assert.IsTrue(Directory.Exists(Path.Combine(_targetDir, "lzmasubfolder")));
            Assert.IsFalse(File.Exists(Path.Combine(_targetDir, "lzmasubfolder", "lzmareadme.txt")));
            Assert.IsTrue(File.Exists(Path.Combine(_targetDir, "lzmasubfolder", "sampleSubfolder.xml")));

            //The lzmaFile should not be there
            Assert.IsFalse(File.Exists(Path.Combine(_targetDir, "lzmaExample.7z")));
        }

        /// <summary>
        /// Nothing shold be there, as no zips are copied to test the unzipping
        /// exclude the folder exclude the lzma subfolder and copy only txt files. Nothing should be in the target dir, as no zip files are copied (therefore no content is copied)
        /// </summary>
        [TestMethod]
        public void Download_SevenZippedDependenciesDeletionTrueExcludedFoldersAndIncludeFilesNoZip_Test()
        {

            //Arrange - Set 7zip and sevensharpzip DLL path in registry
            EnableSevenZipSupportAndSetPath();
            File.Copy(Path.Combine(Environment.CurrentDirectory, _resourcestDir, "lzmaExample.7z"), Path.Combine(_sourceDir, "lzmaExample.7z"));

            var settings = new Settings<DownloaderValidSettings>();
            //Create the excludefilter
            var excludepair = new KeyValuePair<DownloaderValidSettings, string>(DownloaderValidSettings.ExcludedFilesFilter, "lzmasubfolder\\");
            settings.AddSetting(excludepair);

            //Create the include filter for txt files
            var includedPair = new KeyValuePair<DownloaderValidSettings, string>(DownloaderValidSettings.IncludedFilesFilter, "*.txt");
            settings.AddSetting(includedPair);

            IDependencyDownloader downloader = new DownloaderFileShareCopy();
            var originalDownloader = downloader;
            downloader = new ZippedDependencyDownloader(originalDownloader, true);

            downloader.Download(_sourceDir, _targetDir, new DummyWatermark(), false, settings);

            Assert.IsFalse(File.Exists(Path.Combine(_targetDir, "lzmareadme.txt")));

            //The lzma subfolder should be there with txt files
            Assert.IsFalse(Directory.Exists(Path.Combine(_targetDir, "lzmasubfolder")));
            Assert.IsFalse(File.Exists(Path.Combine(_targetDir, "lzmasubfolder", "lzmareadme.txt")));
            Assert.IsFalse(File.Exists(Path.Combine(_targetDir, "lzmasubfolder", "sampleSubfolder.xml")));

            //The lzmaFile should not be there
            Assert.IsFalse(File.Exists(Path.Combine(_targetDir, "lzmaExample.7z")));
        }

        /// <summary>
        ///
        /// exclude the folder "lzmasubfolder", copy all zip files and all txt files
        /// </summary>
        ///         /// Test currently ignored, as inclusion and exclusion is not used with  zipped dependencies
        [TestMethod]
        [Ignore]
        public void Download_SevenZippedDependenciesDeletionTrueExcludedFoldersAndIncludeFilesWithZip_Test()
        {

            //Arrange - Set 7zip and sevensharpzip DLL path in registry
            EnableSevenZipSupportAndSetPath();
            File.Copy(Path.Combine(Environment.CurrentDirectory, _resourcestDir, "lzmaExample.7z"), Path.Combine(_sourceDir, "lzmaExample.7z"));

            var settings = new Settings<DownloaderValidSettings>();
            //Create the excludefilter
            var excludepair = new KeyValuePair<DownloaderValidSettings, string>(DownloaderValidSettings.ExcludedFilesFilter, "lzmasubfolder\\");
            settings.AddSetting(excludepair);

            //Create the include filter for txt files
            var includedPair = new KeyValuePair<DownloaderValidSettings, string>(DownloaderValidSettings.IncludedFilesFilter, "*.txt;*.zip;*.7z");
            settings.AddSetting(includedPair);

            IDependencyDownloader downloader = new DownloaderFileShareCopy();
            var originalDownloader = downloader;
            downloader = new ZippedDependencyDownloader(originalDownloader, true);

            downloader.Download(_sourceDir, _targetDir, new DummyWatermark(), false, settings);

            Assert.IsTrue(File.Exists(Path.Combine(_targetDir, "lzmareadme.txt")));
            Assert.IsFalse(File.Exists(Path.Combine(_targetDir, "sample.xml")));

            //The lzma subfolder should not be there as it was excluded
            Assert.IsFalse(Directory.Exists(Path.Combine(_targetDir, "lzmasubfolder")));
            Assert.IsFalse(File.Exists(Path.Combine(_targetDir, "lzmasubfolder", "lzmareadme.txt")));
            Assert.IsFalse(File.Exists(Path.Combine(_targetDir, "lzmasubfolder", "sampleSubfolder.xml")));

            //The lzmaFile should not be there
            Assert.IsFalse(File.Exists(Path.Combine(_targetDir, "lzmaExample.7z")));
        }


        /// <summary>
        ///
        /// exclude and include the folder "lzmasubfolder", copy all zip files and all txt files
        /// The folder should not exists
        /// </summary>
        ///         /// Test currently ignored, as inclusion and exclusion is not used with  zipped dependencies
        [TestMethod]
        [Ignore]
        public void Download_SevenZippedDependenciesDeletionTrueExcludedFoldersAndFoldersWithZip_Test()
        {
            //Arrange - Set 7zip and sevensharpzip DLL path in registry
            EnableSevenZipSupportAndSetPath();
            File.Copy(Path.Combine(Environment.CurrentDirectory, _resourcestDir, "lzmaExample.7z"), Path.Combine(_sourceDir, "lzmaExample.7z"));

            var settings = new Settings<DownloaderValidSettings>();
            //Create the excludefilter
            var excludepair = new KeyValuePair<DownloaderValidSettings, string>(DownloaderValidSettings.ExcludedFilesFilter, "lzmasubfolder\\");
            settings.AddSetting(excludepair);

            //Create the include for the lzma subbfolder
            var includedPair = new KeyValuePair<DownloaderValidSettings, string>(DownloaderValidSettings.IncludedFilesFilter, "lzmasubfolder\\;*.txt;*.zip;*.7z");
            settings.AddSetting(includedPair);

            IDependencyDownloader downloader = new DownloaderFileShareCopy();
            var originalDownloader = downloader;
            downloader = new ZippedDependencyDownloader(originalDownloader, true);

            downloader.Download(_sourceDir, _targetDir, new DummyWatermark(), false, settings);

            Assert.IsTrue(File.Exists(Path.Combine(_targetDir, "lzmareadme.txt")));
            Assert.IsFalse(File.Exists(Path.Combine(_targetDir, "sample.xml")));

            //The lzma subfolder should not be there as it was excluded
            Assert.IsFalse(Directory.Exists(Path.Combine(_targetDir, "lzmasubfolder")));
            Assert.IsFalse(File.Exists(Path.Combine(_targetDir, "lzmasubfolder", "lzmareadme.txt")));
            Assert.IsFalse(File.Exists(Path.Combine(_targetDir, "lzmasubfolder", "sampleSubfolder.xml")));

            //The lzmaFile should not be there
            Assert.IsFalse(File.Exists(Path.Combine(_targetDir, "lzmaExample.7z")));
        }

        /// <summary>
        ///
        /// exclude exclude the 7z and zip files --> This will prevent the download these--> They will not extracted
        /// The folder should not exists
        /// </summary>
        [TestMethod]
        public void Download_SevenZippedDependenciesDeletionTrueExcludeZipAnd7z_Test()
        {

            //Arrange - Set 7zip and sevensharpzip DLL path in registry
            EnableSevenZipSupportAndSetPath();
            File.Copy(Path.Combine(Environment.CurrentDirectory, _resourcestDir, "lzmaExample.7z"), Path.Combine(_sourceDir, "lzmaExample.7z"));

            var settings = new Settings<DownloaderValidSettings>();
            //Create the excludefilter
            var excludepair = new KeyValuePair<DownloaderValidSettings, string>(DownloaderValidSettings.ExcludedFilesFilter, "*.zip;*.7z");
            settings.AddSetting(excludepair);

            IDependencyDownloader downloader = new DownloaderFileShareCopy();
            var originalDownloader = downloader;
            downloader = new ZippedDependencyDownloader(originalDownloader, true);

            downloader.Download(_sourceDir, _targetDir, new DummyWatermark(), false, settings);

            //NO Files should be there as all files are excluded
            Assert.IsFalse(File.Exists(Path.Combine(_targetDir, "lzmareadme.txt")));
            Assert.IsFalse(File.Exists(Path.Combine(_targetDir, "sample.xml")));

            //The lzma subfolder should not be there as it was excluded
            Assert.IsFalse(Directory.Exists(Path.Combine(_targetDir, "lzmasubfolder")));
            Assert.IsFalse(File.Exists(Path.Combine(_targetDir, "lzmasubfolder", "lzmareadme.txt")));
            Assert.IsFalse(File.Exists(Path.Combine(_targetDir, "lzmasubfolder", "sampleSubfolder.xml")));

            //The folder from the zip should be there
            Assert.IsFalse(Directory.Exists(Path.Combine(_targetDir, "subfolder")));
            Assert.IsFalse(File.Exists(Path.Combine(_targetDir, "subfolder", "readme.txt")));

            //The lzmaFile should not be there
            Assert.IsFalse(File.Exists(Path.Combine(_targetDir, "lzmaExample.7z")));
        }

        /// <summary>
        ///
        /// exclude exclude only zip files --> no example zip
        /// The folder should not exists
        /// </summary>
        [TestMethod]
        public void Download_SevenZippedDependenciesDeletionTrueExcludeZip_Test()
        {

            //Arrange - Set 7zip and sevensharpzip DLL path in registry
            EnableSevenZipSupportAndSetPath();
            File.Copy(Path.Combine(Environment.CurrentDirectory, _resourcestDir, "lzmaExample.7z"), Path.Combine(_sourceDir, "lzmaExample.7z"));

            var settings = new Settings<DownloaderValidSettings>();
            //Create the excludefilter
            var excludepair = new KeyValuePair<DownloaderValidSettings, string>(DownloaderValidSettings.ExcludedFilesFilter, "*.zip;");
            settings.AddSetting(excludepair);

            IDependencyDownloader downloader = new DownloaderFileShareCopy();
            var originalDownloader = downloader;
            downloader = new ZippedDependencyDownloader(originalDownloader, true);

            downloader.Download(_sourceDir, _targetDir, new DummyWatermark(), false, settings);

            //NO Files should be there as all files are excluded
            Assert.IsTrue(File.Exists(Path.Combine(_targetDir, "lzmareadme.txt")));
            Assert.IsTrue(File.Exists(Path.Combine(_targetDir, "sample.xml")));

            //The lzma subfolder should not be there as it was excluded
            Assert.IsTrue(Directory.Exists(Path.Combine(_targetDir, "lzmasubfolder")));
            Assert.IsTrue(File.Exists(Path.Combine(_targetDir, "lzmasubfolder", "lzmareadme.txt")));
            Assert.IsTrue(File.Exists(Path.Combine(_targetDir, "lzmasubfolder", "sampleSubfolder.xml")));

            //The folder from the zip should be there
            Assert.IsFalse(Directory.Exists(Path.Combine(_targetDir, "subfolder")));
            Assert.IsFalse(File.Exists(Path.Combine(_targetDir, "subfolder", "readme.txt")));

            //The lzmaFile should not be there
            Assert.IsFalse(File.Exists(Path.Combine(_targetDir, "lzmaExample.7z")));
        }

        /// <summary>
        ///
        /// exclude exclude only 7z files --> no 7z files
        /// The folder should not exists
        /// </summary>
        [TestMethod]
        public void Download_SevenZippedDependenciesDeletionTrueExclude7z_Test()
        {

            //Arrange - Set 7zip and sevensharpzip DLL path in registry
            EnableSevenZipSupportAndSetPath();
            File.Copy(Path.Combine(Environment.CurrentDirectory, _resourcestDir, "lzmaExample.7z"), Path.Combine(_sourceDir, "lzmaExample.7z"));
            File.Copy(Path.Combine(Environment.CurrentDirectory, _resourcestDir, "example.zip"), Path.Combine(_sourceDir, "example.zip"));

            var settings = new Settings<DownloaderValidSettings>();
            //Create the excludefilter
            var excludepair = new KeyValuePair<DownloaderValidSettings, string>(DownloaderValidSettings.ExcludedFilesFilter, "*.7z;");
            settings.AddSetting(excludepair);

            IDependencyDownloader downloader = new DownloaderFileShareCopy();
            var originalDownloader = downloader;
            downloader = new ZippedDependencyDownloader(originalDownloader, true);

            downloader.Download(_sourceDir, _targetDir, new DummyWatermark(), false, settings);

            //NO Files should be there as all files are excluded
            Assert.IsFalse(File.Exists(Path.Combine(_targetDir, "lzmareadme.txt")));
            Assert.IsFalse(File.Exists(Path.Combine(_targetDir, "sample.xml")));

            //The lzma subfolder should not be there as it was excluded
            Assert.IsFalse(Directory.Exists(Path.Combine(_targetDir, "lzmasubfolder")));
            Assert.IsFalse(File.Exists(Path.Combine(_targetDir, "lzmasubfolder", "lzmareadme.txt")));
            Assert.IsFalse(File.Exists(Path.Combine(_targetDir, "lzmasubfolder", "sampleSubfolder.xml")));

            //The folder from the zip should be there
            Assert.IsTrue(Directory.Exists(Path.Combine(_targetDir, "subfolder")));
            Assert.IsTrue(File.Exists(Path.Combine(_targetDir, "subfolder", "readme.txt")));

            //The lzmaFile should not be there
            Assert.IsFalse(File.Exists(Path.Combine(_targetDir, "lzmaExample.7z")));
        }


        /// <summary>
        ///
        /// include the folder "lzmasubfolder", copy all zip files and all txt files
        /// The folder should exists
        /// </summary>
        ///         /// Test currently ignored, as inclusion and exclusion is not used with  zipped dependencies
        [TestMethod]
        [Ignore]
        public void Download_SevenZippedDependenciesDeletionTrueIncludeFoldersWithZip_Test()
        {

            //Arrange - Set 7zip and sevensharpzip DLL path in registry
            EnableSevenZipSupportAndSetPath();
            File.Copy(Path.Combine(Environment.CurrentDirectory, _resourcestDir, "lzmaExample.7z"), Path.Combine(_sourceDir, "lzmaExample.7z"));

            var settings = new Settings<DownloaderValidSettings>();

            //Create the include for the lzma subbfolder
            var includedPair = new KeyValuePair<DownloaderValidSettings, string>(DownloaderValidSettings.IncludedFilesFilter, "lzmasubfolder\\;*.zip;*.7z");
            settings.AddSetting(includedPair);

            IDependencyDownloader downloader = new DownloaderFileShareCopy();
            var originalDownloader = downloader;
            downloader = new ZippedDependencyDownloader(originalDownloader, true);

            downloader.Download(_sourceDir, _targetDir, new DummyWatermark(), false, settings);

            Assert.IsFalse(File.Exists(Path.Combine(_targetDir, "lzmareadme.txt")));
            Assert.IsFalse(File.Exists(Path.Combine(_targetDir, "sample.xml")));

            //The lzma subfolder should not be there as it was excluded
            Assert.IsTrue(Directory.Exists(Path.Combine(_targetDir, "lzmasubfolder")));
            Assert.IsTrue(File.Exists(Path.Combine(_targetDir, "lzmasubfolder", "lzmareadme.txt")));
            Assert.IsTrue(File.Exists(Path.Combine(_targetDir, "lzmasubfolder", "sampleSubfolder.xml")));

            //The lzmaFile should not be there
            Assert.IsFalse(File.Exists(Path.Combine(_targetDir, "lzmaExample.7z")));
        }

        /// <summary>
        ///
        /// include the folder "lzmasubfolder", copy all zip files and all txt files
        /// The folder should exists
        /// </summary>
        /// Test currently ignored, as inclusion and exclusion is not used with  zipped dependencies
        [TestMethod]
        [Ignore]
        public void Download_SevenZippedDependenciesDeletionTrueIncludeMultipleSubFoldersWithZip_Test()
        {
            //Arrange - Set 7zip and sevensharpzip DLL path in registry
            EnableSevenZipSupportAndSetPath();
            File.Copy(Path.Combine(Environment.CurrentDirectory, _resourcestDir, "lzmaExample.7z"), Path.Combine(_sourceDir, "lzmaExample.7z"));

            var settings = new Settings<DownloaderValidSettings>();

            //Create the include for the lzma subbfolder
            var includedPair = new KeyValuePair<DownloaderValidSettings, string>(DownloaderValidSettings.IncludedFilesFilter, "lzmamultiplesubfolders\\;*.zip;*.7z");
            settings.AddSetting(includedPair);

            IDependencyDownloader downloader = new DownloaderFileShareCopy();
            var originalDownloader = downloader;
            downloader = new ZippedDependencyDownloader(originalDownloader, true);

            downloader.Download(_sourceDir, _targetDir, new DummyWatermark(), false, settings);

            Assert.IsFalse(File.Exists(Path.Combine(_targetDir, "lzmareadme.txt")));
            Assert.IsFalse(File.Exists(Path.Combine(_targetDir, "sample.xml")));

            //The lzma subfolder should not be there as it was excluded
            Assert.IsFalse(Directory.Exists(Path.Combine(_targetDir, "lzmasubfolder")));
            Assert.IsFalse(File.Exists(Path.Combine(_targetDir, "lzmasubfolder", "lzmareadme.txt")));
            Assert.IsFalse(File.Exists(Path.Combine(_targetDir, "lzmasubfolder", "sampleSubfolder.xml")));


            //The lzma subfolder should not be there as it was excluded
            Assert.IsTrue(Directory.Exists(Path.Combine(_targetDir, "lzmamultiplesubfolders")));
            Assert.IsTrue(File.Exists(Path.Combine(_targetDir, "lzmamultiplesubfolders", "sub1.txt")));

            //The lzma subfolder should not be there as it was excluded
            Assert.IsTrue(Directory.Exists(Path.Combine(_targetDir, "lzmamultiplesubfolders", "subfolder1")));
            Assert.IsTrue(File.Exists(Path.Combine(_targetDir, "lzmamultiplesubfolders","subfolder1", "sub2.txt")));

            //The lzmaFile should not be there
            Assert.IsFalse(File.Exists(Path.Combine(_targetDir, "lzmaExample.7z")));
        }

        /// <summary>
        ///
        /// include the folder "lzmasubfolder", copy all zip files and all txt files
        /// The folder should exists
        /// </summary>
        /// Test currently ignored, as inclusion and exclusion is not used with  zipped dependencies
        [TestMethod]
        [Ignore]
        public void Download_SevenZippedDependenciesDeletionTrueMultipleSubFoldersWithZip_Test()
        {

            //Arrange - Set 7zip and sevensharpzip DLL path in registry
            EnableSevenZipSupportAndSetPath();
            File.Copy(Path.Combine(Environment.CurrentDirectory, _resourcestDir, "lzmaExample.7z"), Path.Combine(_sourceDir, "lzmaExample.7z"));

            var settings = new Settings<DownloaderValidSettings>();

            //Create the include for the lzma subbfolder
            var includedPair = new KeyValuePair<DownloaderValidSettings, string>(DownloaderValidSettings.IncludedFilesFilter, "lzmamultiplesubfolders\\;*.zip;*.7z");
            settings.AddSetting(includedPair);

            IDependencyDownloader downloader = new DownloaderFileShareCopy();
            var originalDownloader = downloader;
            downloader = new ZippedDependencyDownloader(originalDownloader, true);

            downloader.Download(_sourceDir, _targetDir, new DummyWatermark(), false, settings);

            Assert.IsFalse(File.Exists(Path.Combine(_targetDir, "lzmareadme.txt")));
            Assert.IsFalse(File.Exists(Path.Combine(_targetDir, "sample.xml")));

            //The lzma subfolder should not be there as it was excluded
            Assert.IsFalse(Directory.Exists(Path.Combine(_targetDir, "lzmasubfolder")));
            Assert.IsFalse(File.Exists(Path.Combine(_targetDir, "lzmasubfolder", "lzmareadme.txt")));
            Assert.IsFalse(File.Exists(Path.Combine(_targetDir, "lzmasubfolder", "sampleSubfolder.xml")));


            //The lzma subfolder should not be there as it was excluded
            Assert.IsTrue(Directory.Exists(Path.Combine(_targetDir, "lzmamultiplesubfolders")));
            Assert.IsTrue(File.Exists(Path.Combine(_targetDir, "lzmamultiplesubfolders", "sub1.txt")));

            //The lzma subfolder should not be there as it was excluded
            Assert.IsTrue(Directory.Exists(Path.Combine(_targetDir, "lzmamultiplesubfolders", "subfolder1")));
            Assert.IsTrue(File.Exists(Path.Combine(_targetDir, "lzmamultiplesubfolders", "subfolder1", "sub2.txt")));

            //The lzmaFile should not be there
            Assert.IsFalse(File.Exists(Path.Combine(_targetDir, "lzmaExample.7z")));
        }





        /// <summary>
        ///
        /// Map the folder "lzmasubfolder" to a different folder
        /// </summary>
        [TestMethod]
        public void Download_SevenZippedDependenciesFolderMappings_Test()
        {

            EnableSevenZipSupportAndSetPath();
            File.Copy(Path.Combine(Environment.CurrentDirectory, _resourcestDir, "lzmaExample.7z"), Path.Combine(_sourceDir, "lzmaExample.7z"));
            string newMappedTargetFolder = "SomeNewMappedTargetFolder";
            var settings = new Settings<DownloaderValidSettings>();
            //Create the excludefilter
            var mappings = new KeyValuePair<DownloaderValidSettings, string>(DownloaderValidSettings.FolderMappings, "SourceOffset=lzmasubfolder,LocalOffset=" + newMappedTargetFolder+";");
            settings.AddSetting(mappings);

            IDependencyDownloader downloader = new DownloaderFileShareCopy();
            var originalDownloader = downloader;
            downloader = new ZippedDependencyDownloader(originalDownloader, true);

            downloader.Download(_sourceDir, _targetDir, new DummyWatermark(), false, settings);

            //Only Mapped Folders are copied
            Assert.IsFalse(File.Exists(Path.Combine(_targetDir, "lzmareadme.txt")));
            Assert.IsFalse(File.Exists(Path.Combine(_targetDir, "sample.xml")));

            //The lzma subfolder should not be there as it name was changed
            Assert.IsFalse(Directory.Exists(Path.Combine(_targetDir, "lzmasubfolder")));
            Assert.IsTrue(Directory.Exists(Path.Combine(_targetDir, newMappedTargetFolder)));
            Assert.IsTrue(File.Exists(Path.Combine(_targetDir, newMappedTargetFolder, "lzmareadme.txt")));
            Assert.IsTrue(File.Exists(Path.Combine(_targetDir, newMappedTargetFolder, "sampleSubfolder.xml")));

            Assert.IsFalse(File.Exists(Path.Combine(_targetDir, "lzmasubfolder", "lzmareadme.txt")));
            Assert.IsFalse(File.Exists(Path.Combine(_targetDir, "lzmasubfolder", "sampleSubfolder.xml")));

            //The lzmaFile should not be there
            Assert.IsFalse(File.Exists(Path.Combine(_targetDir, "lzmaExample.7z")));
        }

        /// <summary>
        ///
        /// Map the folder "lzmasubfolder" to a different folder
        /// </summary>
        [TestMethod]
        public void Download_SevenZippedDependenciesFolderMappingsSameSubfolders_Test()
        {

            EnableSevenZipSupportAndSetPath();

            File.Copy(Path.Combine(Environment.CurrentDirectory, _resourcestDir, "lzmaExample.7z"), Path.Combine(_sourceDir, "lzmaExample.7z"));
            File.Copy(Path.Combine(Environment.CurrentDirectory, _resourcestDir, "lzmaDifferentBaseFolder.7z"), Path.Combine(_sourceDir, "lzmaDifferentBaseFolder.7z"));
            File.Copy(Path.Combine(Environment.CurrentDirectory, _resourcestDir, "lzmaDifferentBaseFolder2.7z"), Path.Combine(_sourceDir, "lzmaDifferentBaseFolder2.7z"));

            string newMappedTargetFolder = "SomeNewMappedTargetFolder";
            var settings = new Settings<DownloaderValidSettings>();
            //Create the excludefilter
            var mappings = new KeyValuePair<DownloaderValidSettings, string>(DownloaderValidSettings.FolderMappings, "SourceOffset=lzmasubfolder,LocalOffset=" + newMappedTargetFolder + ";SourceOffset=.\\lzmaDifferentBaseFolder\\lzmasubfolder,LocalOffset=.\\" + newMappedTargetFolder + ";" + ";SourceOffset=.\\lzmaDifferentBaseFolder2\\lzmasubfolder,LocalOffset=.\\" + newMappedTargetFolder + ";");

            settings.AddSetting(mappings);

            IDependencyDownloader downloader = new DownloaderFileShareCopy();
            var originalDownloader = downloader;
            downloader = new ZippedDependencyDownloader(originalDownloader, true);

            downloader.Download(_sourceDir, _targetDir, new DummyWatermark(), false, settings);

            //Only Mapped Folders are copied
            Assert.IsFalse(File.Exists(Path.Combine(_targetDir, "lzmareadme.txt")));
            Assert.IsFalse(File.Exists(Path.Combine(_targetDir, "sample.xml")));

            //The lzma subfolder should not be there as it name was changed
            Assert.IsFalse(Directory.Exists(Path.Combine(_targetDir, "lzmasubfolder")));
            Assert.IsTrue(Directory.Exists(Path.Combine(_targetDir, newMappedTargetFolder)));
            Assert.IsTrue(File.Exists(Path.Combine(_targetDir, newMappedTargetFolder, "lzmareadme.txt")));
            Assert.IsTrue(File.Exists(Path.Combine(_targetDir, newMappedTargetFolder, "sampleSubfolder.xml")));

            //The Newly mapped folder should contain a new folder "AnotherFolder"
            Assert.IsTrue(Directory.Exists(Path.Combine(_targetDir, newMappedTargetFolder, "AnotherFolder")));
            Assert.IsTrue(File.Exists(Path.Combine(_targetDir, newMappedTargetFolder, "AnotherFolder", "lzmareadme.txt")));
            Assert.IsTrue(File.Exists(Path.Combine(_targetDir, newMappedTargetFolder, "AnotherFolder","sampleSubfolder.xml")));

            //The Newly mapped folder should contain a new folder "AnotherFolder"
            Assert.IsTrue(Directory.Exists(Path.Combine(_targetDir, newMappedTargetFolder, "AnotherFolder2")));
            Assert.IsTrue(File.Exists(Path.Combine(_targetDir, newMappedTargetFolder, "AnotherFolder2", "lzmareadme.txt")));
            Assert.IsTrue(File.Exists(Path.Combine(_targetDir, newMappedTargetFolder, "AnotherFolder2", "sampleSubfolder.xml")));

            Assert.IsFalse(File.Exists(Path.Combine(_targetDir, "lzmasubfolder", "lzmareadme.txt")));
            Assert.IsFalse(File.Exists(Path.Combine(_targetDir, "lzmasubfolder", "sampleSubfolder.xml")));

            //The lzmaFile should not be there
            Assert.IsFalse(File.Exists(Path.Combine(_targetDir, "lzmaExample.7z")));
        }


        /// <summary>
        /// Map the folder "lzmasubfolder" to a different folder
        /// </summary>
        [TestMethod]
        public void Download_SevenZippedDependenciesFolderMappingsSameRootFoldersMapped_Test()
        {

            EnableSevenZipSupportAndSetPath();

            File.Copy(Path.Combine(Environment.CurrentDirectory, _resourcestDir, "lzmaExample.7z"), Path.Combine(_sourceDir, "lzmaExample.7z"));
            File.Copy(Path.Combine(Environment.CurrentDirectory, _resourcestDir, "lzmaDifferentBaseFolder.7z"), Path.Combine(_sourceDir, "lzmaDifferentBaseFolder.7z"));
            File.Copy(Path.Combine(Environment.CurrentDirectory, _resourcestDir, "lzmaDifferentBaseFolder2.7z"), Path.Combine(_sourceDir, "lzmaDifferentBaseFolder2.7z"));

            string newMappedTargetFolder = "SomeNewMappedTargetFolder";
            var settings = new Settings<DownloaderValidSettings>();
            //Create the excludefilter
            var mappings = new KeyValuePair<DownloaderValidSettings, string>(DownloaderValidSettings.FolderMappings, "SourceOffset=lzmasubfolder,LocalOffset=" + newMappedTargetFolder + ";SourceOffset=.\\lzmaDifferentBaseFolder,LocalOffset=.\\" + newMappedTargetFolder + ";" + ";SourceOffset=.\\lzmaDifferentBaseFolder2,LocalOffset=.\\" + newMappedTargetFolder + ";");

            settings.AddSetting(mappings);

            IDependencyDownloader downloader = new DownloaderFileShareCopy();
            var originalDownloader = downloader;
            downloader = new ZippedDependencyDownloader(originalDownloader, true);

            downloader.Download(_sourceDir, _targetDir, new DummyWatermark(), false, settings);

            //Only Mapped Folders are copied
            Assert.IsFalse(File.Exists(Path.Combine(_targetDir, "lzmareadme.txt")));
            Assert.IsFalse(File.Exists(Path.Combine(_targetDir, "sample.xml")));

            //The lzma subfolder should not be there as it name was changed
            Assert.IsFalse(Directory.Exists(Path.Combine(_targetDir, "lzmasubfolder")));
            Assert.IsTrue(Directory.Exists(Path.Combine(_targetDir, newMappedTargetFolder)));
            Assert.IsTrue(File.Exists(Path.Combine(_targetDir, newMappedTargetFolder, "lzmareadme.txt")));
            Assert.IsTrue(File.Exists(Path.Combine(_targetDir, newMappedTargetFolder, "sampleSubfolder.xml")));

            //The Newly mapped folder should contain a new folder "AnotherFolder"
            Assert.IsTrue(Directory.Exists(Path.Combine(_targetDir, newMappedTargetFolder,"lzmaSubfolder", "AnotherFolder")));
            Assert.IsTrue(File.Exists(Path.Combine(_targetDir, newMappedTargetFolder, "lzmaSubfolder", "AnotherFolder", "lzmareadme.txt")));
            Assert.IsTrue(File.Exists(Path.Combine(_targetDir, newMappedTargetFolder, "lzmaSubfolder", "AnotherFolder", "sampleSubfolder.xml")));

            //The Newly mapped folder should contain a new folder "AnotherFolder"
            Assert.IsTrue(Directory.Exists(Path.Combine(_targetDir, newMappedTargetFolder, "lzmaSubfolder", "AnotherFolder2")));
            Assert.IsTrue(File.Exists(Path.Combine(_targetDir, newMappedTargetFolder, "lzmaSubfolder", "AnotherFolder2", "lzmareadme.txt")));
            Assert.IsTrue(File.Exists(Path.Combine(_targetDir, newMappedTargetFolder, "lzmaSubfolder", "AnotherFolder2", "sampleSubfolder.xml")));

            Assert.IsFalse(File.Exists(Path.Combine(_targetDir, "lzmasubfolder", "lzmareadme.txt")));
            Assert.IsFalse(File.Exists(Path.Combine(_targetDir, "lzmasubfolder", "sampleSubfolder.xml")));

            //The lzmaFile should not be there
            Assert.IsFalse(File.Exists(Path.Combine(_targetDir, "lzmaExample.7z")));
        }



        /// <summary>
        /// This test will simuate that a subfolder aleady exists
        /// The subfolder will in addition contain a subfolder with a new subfolder which is not contained in the zip files
        /// THis folder should exists after the extraction
        /// Map the folder "lzmasubfolder" to a different folder
        /// </summary>
        [TestMethod]
        public void Download_SevenZippedDependencies_FolderMappings_SameRootFoldersMapped_SubFolderDifferent_Test()
        {

            EnableSevenZipSupportAndSetPath();

            File.Copy(Path.Combine(Environment.CurrentDirectory, _resourcestDir, "lzmaExample.7z"), Path.Combine(_sourceDir, "lzmaExample.7z"));
            File.Copy(Path.Combine(Environment.CurrentDirectory, _resourcestDir, "lzmaDifferentBaseFolder.7z"), Path.Combine(_sourceDir, "lzmaDifferentBaseFolder.7z"));
            File.Copy(Path.Combine(Environment.CurrentDirectory, _resourcestDir, "lzmaDifferentBaseFolder2.7z"), Path.Combine(_sourceDir, "lzmaDifferentBaseFolder2.7z"));

            string nameOfFolderThatIsNotInZipFiles = "ANonZipFolder";
            string newMappedTargetFolder = "SomeNewMappedTargetFolder";

            //Create the mapped folder
            if (!Directory.Exists(Path.Combine(_targetDir, newMappedTargetFolder)))
                Directory.CreateDirectory(Path.Combine(_targetDir, newMappedTargetFolder));
            //Create a Subfolder within this folder that is not contained in the zip files
                 if (!Directory.Exists(Path.Combine(_targetDir, newMappedTargetFolder,nameOfFolderThatIsNotInZipFiles )))
                 Directory.CreateDirectory(Path.Combine(_targetDir, newMappedTargetFolder, nameOfFolderThatIsNotInZipFiles));

            var settings = new Settings<DownloaderValidSettings>();
            //Create the excludefilter
            var mappings = new KeyValuePair<DownloaderValidSettings, string>(DownloaderValidSettings.FolderMappings, "SourceOffset=lzmasubfolder,LocalOffset=" + newMappedTargetFolder + ";SourceOffset=.\\lzmaDifferentBaseFolder,LocalOffset=.\\" + newMappedTargetFolder + ";" + ";SourceOffset=.\\lzmaDifferentBaseFolder2,LocalOffset=.\\" + newMappedTargetFolder + ";");

            settings.AddSetting(mappings);

            IDependencyDownloader downloader = new DownloaderFileShareCopy();
            var originalDownloader = downloader;
            downloader = new ZippedDependencyDownloader(originalDownloader, true);

            downloader.Download(_sourceDir, _targetDir, new DummyWatermark(), false, settings);

            //Only Mapped Folders are copied
            Assert.IsFalse(File.Exists(Path.Combine(_targetDir, "lzmareadme.txt")));
            Assert.IsFalse(File.Exists(Path.Combine(_targetDir, "sample.xml")));

            //The lzma subfolder should not be there as it name was changed
            Assert.IsFalse(Directory.Exists(Path.Combine(_targetDir, "lzmasubfolder")));
            Assert.IsTrue(Directory.Exists(Path.Combine(_targetDir, newMappedTargetFolder)));
            Assert.IsTrue(File.Exists(Path.Combine(_targetDir, newMappedTargetFolder, "lzmareadme.txt")));
            Assert.IsTrue(File.Exists(Path.Combine(_targetDir, newMappedTargetFolder, "sampleSubfolder.xml")));

            //The Newly mapped folder should contain a new folder "AnotherFolder"
            Assert.IsTrue(Directory.Exists(Path.Combine(_targetDir, newMappedTargetFolder, "lzmaSubfolder", "AnotherFolder")));
            Assert.IsTrue(File.Exists(Path.Combine(_targetDir, newMappedTargetFolder, "lzmaSubfolder", "AnotherFolder", "lzmareadme.txt")));
            Assert.IsTrue(File.Exists(Path.Combine(_targetDir, newMappedTargetFolder, "lzmaSubfolder", "AnotherFolder", "sampleSubfolder.xml")));

            //The Newly mapped folder should contain a new folder "AnotherFolder"
            Assert.IsTrue(Directory.Exists(Path.Combine(_targetDir, newMappedTargetFolder, "lzmaSubfolder", "AnotherFolder2")));
            Assert.IsTrue(File.Exists(Path.Combine(_targetDir, newMappedTargetFolder, "lzmaSubfolder", "AnotherFolder2", "lzmareadme.txt")));
            Assert.IsTrue(File.Exists(Path.Combine(_targetDir, newMappedTargetFolder, "lzmaSubfolder", "AnotherFolder2", "sampleSubfolder.xml")));

            //The Newly mapped folder should contain the folder which was created before
            Assert.IsTrue(Directory.Exists(Path.Combine(_targetDir, newMappedTargetFolder, nameOfFolderThatIsNotInZipFiles)));


            Assert.IsFalse(File.Exists(Path.Combine(_targetDir, "lzmasubfolder", "lzmareadme.txt")));
            Assert.IsFalse(File.Exists(Path.Combine(_targetDir, "lzmasubfolder", "sampleSubfolder.xml")));

            //The lzmaFile should not be there
            Assert.IsFalse(File.Exists(Path.Combine(_targetDir, "lzmaExample.7z")));
        }


        /// <summary>
        /// This test will simuate that a subfolder aleady exists
        /// The subfolder will in addition contain a subfolder with a new subfolder which is not contained in the zip files
        /// THis folder should exists after the extraction
        /// In addition the lzmasubfolder will already contain a folder wiht a different file
        /// ToDo: Understand and redign this test
        /// </summary>
        [TestMethod]
        [Ignore]
        public void Download_SevenZippedDependencies_FolderMappings_SameRootFoldersMapped_SubFolderSameWithFile_Test()
        {

            EnableSevenZipSupportAndSetPath();

            File.Copy(Path.Combine(Environment.CurrentDirectory, _resourcestDir, "lzmaExample.7z"), Path.Combine(_sourceDir, "lzmaExample.7z"));
            File.Copy(Path.Combine(Environment.CurrentDirectory, _resourcestDir, "lzmaDifferentBaseFolder.7z"), Path.Combine(_sourceDir, "lzmaDifferentBaseFolder.7z"));
            File.Copy(Path.Combine(Environment.CurrentDirectory, _resourcestDir, "lzmaDifferentBaseFolder2.7z"), Path.Combine(_sourceDir, "lzmaDifferentBaseFolder2.7z"));

            string nameOfFolderThatIsNotInZipFiles = "ANonZipFolder";
            string newMappedTargetFolder = "SomeNewMappedTargetFolder";

            //Create the mapped folder
            if (!Directory.Exists(Path.Combine(_targetDir, newMappedTargetFolder)))
            Directory.CreateDirectory(Path.Combine(_targetDir, newMappedTargetFolder));
            //Create a Subfolder within this folder that is not contained in the zip files
            if (!Directory.Exists(Path.Combine(_targetDir, newMappedTargetFolder, nameOfFolderThatIsNotInZipFiles)))
            Directory.CreateDirectory(Path.Combine(_targetDir, newMappedTargetFolder, nameOfFolderThatIsNotInZipFiles));

            //Create a Subfolder which is included in the zip files and create a file in the lzmaSubfolder (Which should be gone after extraction, because the folder will be deleted
            if (!Directory.Exists(Path.Combine(_targetDir, newMappedTargetFolder, "lzmaSubfolder", "AnotherFolder")))
            {
                Directory.CreateDirectory(Path.Combine(_targetDir, newMappedTargetFolder, "lzmaSubfolder", "AnotherFolder"));

            }
            File.Copy(Path.Combine(Environment.CurrentDirectory, _resourcestDir, "lzmaExample.7z"), Path.Combine(_targetDir, newMappedTargetFolder, "lzmaSubfolder", "AnotherFolder", "lzmaExample.7z"));


            var settings = new Settings<DownloaderValidSettings>();
             //Create the mappings
            var mappings = new KeyValuePair<DownloaderValidSettings, string>(DownloaderValidSettings.FolderMappings, "SourceOffset=lzmasubfolder,LocalOffset=" + newMappedTargetFolder + ";SourceOffset=.\\lzmaDifferentBaseFolder,LocalOffset=.\\" + newMappedTargetFolder + ";" + ";SourceOffset=.\\lzmaDifferentBaseFolder2,LocalOffset=.\\" + newMappedTargetFolder + ";");

            settings.AddSetting(mappings);

            //The file should now be in the folder before downloading starts
            Assert.IsTrue(File.Exists(Path.Combine(_targetDir, newMappedTargetFolder, "lzmaSubfolder", "AnotherFolder", "lzmaExample.7z")));


            IDependencyDownloader downloader = new DownloaderFileShareCopy();
            var originalDownloader = downloader;
            downloader = new ZippedDependencyDownloader(originalDownloader, true);

            downloader.Download(_sourceDir, _targetDir, new DummyWatermark(), false, settings);

            //Only Mapped Folders are copied
            Assert.IsFalse(File.Exists(Path.Combine(_targetDir, "lzmareadme.txt")));
            Assert.IsFalse(File.Exists(Path.Combine(_targetDir, "sample.xml")));

            //The lzma subfolder should not be there as it name was changed
            Assert.IsFalse(Directory.Exists(Path.Combine(_targetDir, "lzmasubfolder")));
            Assert.IsTrue(Directory.Exists(Path.Combine(_targetDir, newMappedTargetFolder)));
            Assert.IsTrue(File.Exists(Path.Combine(_targetDir, newMappedTargetFolder, "lzmareadme.txt")));
            Assert.IsTrue(File.Exists(Path.Combine(_targetDir, newMappedTargetFolder, "sampleSubfolder.xml")));

            //The Newly mapped folder should contain a new folder "AnotherFolder"
            Assert.IsTrue(Directory.Exists(Path.Combine(_targetDir, newMappedTargetFolder, "lzmaSubfolder", "AnotherFolder")));
            Assert.IsTrue(File.Exists(Path.Combine(_targetDir, newMappedTargetFolder, "lzmaSubfolder", "AnotherFolder", "lzmareadme.txt")));
            Assert.IsTrue(File.Exists(Path.Combine(_targetDir, newMappedTargetFolder, "lzmaSubfolder", "AnotherFolder", "sampleSubfolder.xml")));

            //The Newly mapped folder should contain a new folder "AnotherFolder2"
            Assert.IsTrue(Directory.Exists(Path.Combine(_targetDir, newMappedTargetFolder, "lzmaSubfolder", "AnotherFolder2")));
            Assert.IsTrue(File.Exists(Path.Combine(_targetDir, newMappedTargetFolder, "lzmaSubfolder", "AnotherFolder2", "lzmareadme.txt")));
            Assert.IsTrue(File.Exists(Path.Combine(_targetDir, newMappedTargetFolder, "lzmaSubfolder", "AnotherFolder2", "sampleSubfolder.xml")));

            //The Newly mapped folder should contain the folder which was created before
            Assert.IsTrue(Directory.Exists(Path.Combine(_targetDir, newMappedTargetFolder, nameOfFolderThatIsNotInZipFiles)));

            //The Newly mapped folder should not contain a 7z. file which was copied before
            Assert.IsFalse(File.Exists(Path.Combine(_targetDir, newMappedTargetFolder, "lzmaSubfolder", "AnotherFolder","lzmaExample.7z")));

            Assert.IsFalse(File.Exists(Path.Combine(_targetDir, "lzmasubfolder", "lzmareadme.txt")));
            Assert.IsFalse(File.Exists(Path.Combine(_targetDir, "lzmasubfolder", "sampleSubfolder.xml")));

            //The lzmaFile should not be there
            Assert.IsFalse(File.Exists(Path.Combine(_targetDir, "lzmaExample.7z")));
        }



        /// <summary>
        ///
        /// Map the folder "lzmasubfolder" to a different folder
        /// </summary>
        [TestMethod]
        public void Download_SevenZippedDependenciesFolderMappingsSameName_Test()
        {

            EnableSevenZipSupportAndSetPath();
            File.Copy(Path.Combine(Environment.CurrentDirectory, _resourcestDir, "lzmaExample.7z"), Path.Combine(_sourceDir, "lzmaExample.7z"));

            string originalMappedTargetFolder = "lzmasubfolder";
            var settings = new Settings<DownloaderValidSettings>();
            //Create the excludefilter
            var mappings = new KeyValuePair<DownloaderValidSettings, string>(DownloaderValidSettings.FolderMappings, "SourceOffset=" + originalMappedTargetFolder + ",LocalOffset=" + originalMappedTargetFolder + ";");
            settings.AddSetting(mappings);

            IDependencyDownloader downloader = new DownloaderFileShareCopy();
            var originalDownloader = downloader;
            downloader = new ZippedDependencyDownloader(originalDownloader, true);

            downloader.Download(_sourceDir, _targetDir, new DummyWatermark(), false, settings);

            //Only Mapped Folders are copied
            Assert.IsFalse(File.Exists(Path.Combine(_targetDir, "lzmareadme.txt")));
            Assert.IsFalse(File.Exists(Path.Combine(_targetDir, "sample.xml")));

            //The lzma subfolder should not be there as it name was changed
            Assert.IsTrue(Directory.Exists(Path.Combine(_targetDir, "lzmasubfolder")));
            Assert.IsTrue(Directory.Exists(Path.Combine(_targetDir, originalMappedTargetFolder)));
            Assert.IsTrue(File.Exists(Path.Combine(_targetDir, originalMappedTargetFolder, "lzmareadme.txt")));
            Assert.IsTrue(File.Exists(Path.Combine(_targetDir, originalMappedTargetFolder, "sampleSubfolder.xml")));

            Assert.IsTrue(File.Exists(Path.Combine(_targetDir, "lzmasubfolder", "lzmareadme.txt")));
            Assert.IsTrue(File.Exists(Path.Combine(_targetDir, "lzmasubfolder", "sampleSubfolder.xml")));

            //The lzmaFile should not be there
            Assert.IsFalse(File.Exists(Path.Combine(_targetDir, "lzmaExample.7z")));
        }

        /// <summary>
        ///
        /// Map the folder "lzmasubfolder" to a different folder
        /// </summary>
        [TestMethod]
        public void Download_SevenZippedDependenciesFolderMappingsSubFolders_Test()
        {

            EnableSevenZipSupportAndSetPath();
            File.Copy(Path.Combine(Environment.CurrentDirectory, _resourcestDir, "lzmaExample.7z"), Path.Combine(_sourceDir, "lzmaExample.7z"));
            string newMappedTargetFolder = "SomeNewMappedTargetFolder\\WithSubFolder";
            var settings = new Settings<DownloaderValidSettings>();
            //Create the excludefilter
            var mappings = new KeyValuePair<DownloaderValidSettings, string>(DownloaderValidSettings.FolderMappings, "SourceOffset=lzmasubfolder,LocalOffset=" + newMappedTargetFolder + ";");
            settings.AddSetting(mappings);

            IDependencyDownloader downloader = new DownloaderFileShareCopy();
            var originalDownloader = downloader;
            downloader = new ZippedDependencyDownloader(originalDownloader, true);

            downloader.Download(_sourceDir, _targetDir, new DummyWatermark(), false, settings);

            //Only Mapped Folders are copied
            Assert.IsFalse(File.Exists(Path.Combine(_targetDir, "lzmareadme.txt")));
            Assert.IsFalse(File.Exists(Path.Combine(_targetDir, "sample.xml")));

            //The lzma subfolder should not be there as it name was changed
            Assert.IsFalse(Directory.Exists(Path.Combine(_targetDir, "lzmasubfolder")));
            Assert.IsTrue(Directory.Exists(Path.Combine(_targetDir, newMappedTargetFolder)));
            Assert.IsTrue(File.Exists(Path.Combine(_targetDir, newMappedTargetFolder, "lzmareadme.txt")));
            Assert.IsTrue(File.Exists(Path.Combine(_targetDir, newMappedTargetFolder, "sampleSubfolder.xml")));

            Assert.IsFalse(File.Exists(Path.Combine(_targetDir, "lzmasubfolder", "lzmareadme.txt")));
            Assert.IsFalse(File.Exists(Path.Combine(_targetDir, "lzmasubfolder", "sampleSubfolder.xml")));

            //The lzmaFile should not be there
            Assert.IsFalse(File.Exists(Path.Combine(_targetDir, "lzmaExample.7z")));
        }

        /// <summary>
        /// Map the folder "lzmasubfolder" to a different folder
        /// </summary>
        [TestMethod]
        public void Download_SevenZippedDependenciesFolderMappingsWithInclude_Test()
        {

            EnableSevenZipSupportAndSetPath();
            File.Copy(Path.Combine(Environment.CurrentDirectory, _resourcestDir, "lzmaExample.7z"), Path.Combine(_sourceDir, "lzmaExample.7z"));
            string newMappedTargetFolder = ".\\SomeNewMappedTargetFolder\\lzmasubfolder";
            var settings = new Settings<DownloaderValidSettings>();
            //Create the excludefilter
            var mappings = new KeyValuePair<DownloaderValidSettings, string>(DownloaderValidSettings.FolderMappings, "SourceOffset=lzmasubfolder,LocalOffset=" + newMappedTargetFolder + ";");
            settings.AddSetting(mappings);

            //Create the include filter for txt files
            var includedPair = new KeyValuePair<DownloaderValidSettings, string>(DownloaderValidSettings.IncludedFilesFilter, "lzmasubfolder\\;*.txt;*.7z");
            settings.AddSetting(includedPair);

            IDependencyDownloader downloader = new DownloaderFileShareCopy();
            var originalDownloader = downloader;
            downloader = new ZippedDependencyDownloader(originalDownloader, true);

            downloader.Download(_sourceDir, _targetDir, new DummyWatermark(), false, settings);

            //Only Mapped Folders are copied
            Assert.IsFalse(File.Exists(Path.Combine(_targetDir, "lzmareadme.txt")));
            Assert.IsFalse(File.Exists(Path.Combine(_targetDir, "sample.xml")));

            //The lzma subfolder should not be there as it name was changed
            Assert.IsFalse(Directory.Exists(Path.Combine(_targetDir, "lzmasubfolder")));
            Assert.IsTrue(Directory.Exists(Path.Combine(_targetDir, newMappedTargetFolder)));
            Assert.IsTrue(File.Exists(Path.Combine(_targetDir, newMappedTargetFolder, "lzmareadme.txt")));
            Assert.IsTrue(File.Exists(Path.Combine(_targetDir, newMappedTargetFolder, "sampleSubfolder.xml")));

            Assert.IsFalse(File.Exists(Path.Combine(_targetDir, "lzmasubfolder", "lzmareadme.txt")));
            Assert.IsFalse(File.Exists(Path.Combine(_targetDir, "lzmasubfolder", "sampleSubfolder.xml")));

            //The lzmaFile should not be there
            Assert.IsFalse(File.Exists(Path.Combine(_targetDir, "lzmaExample.7z")));
        }



        /// Obsolete Test.
        [Ignore]
        [TestMethod]
        public void Download_SevenZippedDependencies_WithoutSevenZipDll_ShouldFail()
        {
            //Arrange - Set only th sevensharp  DLL path in registry
            ApplicationSettings.Instance.DeleteAllValues();
            //ApplicationSettings.Instance.InstallPathForSevenZipSharp = Path.Combine(Environment.CurrentDirectory, @"..\..\..\..\Lib\SevenZipSharp\SevenZipSharp.dll");

            File.Copy(Path.Combine(Environment.CurrentDirectory,_resourcestDir, "lzmaExample.7z"), Path.Combine(_sourceDir, "lzmaExample.7z"));

            IDependencyDownloader downloader = new DownloaderFileShareCopy();
            var originalDownloader = downloader;
            downloader = new ZippedDependencyDownloader(originalDownloader,false);

            downloader.Download(_sourceDir, _targetDir, new DummyWatermark(), false, new Settings<DownloaderValidSettings>());

            Assert.IsFalse(File.Exists(Path.Combine(_targetDir, "lzmareadme.txt")));
            Assert.IsFalse(File.Exists(Path.Combine(_targetDir, "lzmasubfolder", "lzmareadme.txt")));
        }

        /// <summary>
        /// A seven zip file that does not exists
        /// </summary>
        [TestMethod]
        public void Download_SevenZippedDependencies_NonExistantExeFile_ShouldFail()
        {
            //Arrange - Set a wrong Dll in the registry
            ApplicationSettings.Instance.DeleteAllValues();
            ApplicationSettings.Instance.InstallPathForSevenZip = Path.Combine(Environment.CurrentDirectory, @"C:\Program Files\SomeWrongDirectory\Wrong7z.exe");

            File.Delete(Path.Combine(_sourceDir, "lzmaExample.7z"));
            File.Copy(Path.Combine(Environment.CurrentDirectory, _resourcestDir, "lzmaExample.7z"), Path.Combine(_sourceDir, "lzmaExample.7z"));

            IDependencyDownloader downloader = new DownloaderFileShareCopy();
            var originalDownloader = downloader;
            downloader = new ZippedDependencyDownloader(originalDownloader,false);

            downloader.Download(_sourceDir, _targetDir, new DummyWatermark(), false, new Settings<DownloaderValidSettings>());

            //The lzmaFile should still be there
            Assert.IsTrue(File.Exists(Path.Combine(_targetDir, "lzmaExample.7z")));

            Assert.IsFalse(File.Exists(Path.Combine(_targetDir, "lzmareadme.txt")));
            Assert.IsFalse(File.Exists(Path.Combine(_targetDir, "lzmasubfolder", "lzmareadme.txt")));

        }


        /// <summary>
        /// An exe file that is not a 7z.exe
        /// </summary>
        [TestMethod]
        public void Download_SevenZippedDependencies_ExistingButWrongFile_ShouldFail()
        {
            //Arrange - Set a wrong Dll in the registry
            ApplicationSettings.Instance.DeleteAllValues();
            ApplicationSettings.Instance.InstallPathForSevenZip = Path.Combine(Environment.CurrentDirectory, @"%windir%\system32\cmd.exe");

            File.Delete(Path.Combine(_sourceDir, "lzmaExample.7z"));
            File.Copy(Path.Combine(Environment.CurrentDirectory, _resourcestDir, "lzmaExample.7z"), Path.Combine(_sourceDir, "lzmaExample.7z"));

            IDependencyDownloader downloader = new DownloaderFileShareCopy();
            var originalDownloader = downloader;
            downloader = new ZippedDependencyDownloader(originalDownloader, false);

            downloader.Download(_sourceDir, _targetDir, new DummyWatermark(), false, new Settings<DownloaderValidSettings>());

            //The lzmaFile should still be there
            Assert.IsTrue(File.Exists(Path.Combine(_targetDir, "lzmaExample.7z")));

            Assert.IsFalse(File.Exists(Path.Combine(_targetDir, "lzmareadme.txt")));
            Assert.IsFalse(File.Exists(Path.Combine(_targetDir, "lzmasubfolder", "lzmareadme.txt")));

        }


        /// <summary>
        /// The files should not be there, as the exe is referenced wrong
        /// </summary>
        [TestMethod]
        public void Download_SevenZippedDependencies_WithWrongReferencedExe_AndDisabledSevenzipSupport()
        {
            //Arrange - Reference a wrong path and dsiable the support
            ApplicationSettings.Instance.InstallPathForSevenZip = Path.Combine(Environment.CurrentDirectory, @"..\..\..\..\Lib\");

            File.Copy(Path.Combine(Environment.CurrentDirectory, _resourcestDir, "lzmaExample.7z"), Path.Combine(_sourceDir, "lzmaExample.7z"));

            IDependencyDownloader downloader = new DownloaderFileShareCopy();
            var originalDownloader = downloader;
            downloader = new ZippedDependencyDownloader(originalDownloader,false);

            downloader.Download(_sourceDir, _targetDir, new DummyWatermark(), false, new Settings<DownloaderValidSettings>());

            //The lzmaFile should still be there
            Assert.IsTrue(File.Exists(Path.Combine(_targetDir, "lzmaExample.7z")));

            //Files should not exist
            Assert.IsFalse(File.Exists(Path.Combine(_targetDir, "lzmareadme.txt")));
            Assert.IsFalse(File.Exists(Path.Combine(_targetDir, "lzmasubfolder", "lzmareadme.txt")));
        }


        /// <summary>
        /// Obsolete Test
        /// </summary>

        [Ignore]
        [TestMethod]
        public void Download_SevenZippedDependencies_WithDllsInSameFolder_Test()
        {
            //Arrange - Delete the dlls references
            ApplicationSettings.Instance.DeleteAllValues();

            File.Copy(Path.Combine(Environment.CurrentDirectory, _resourcestDir, "lzmaExample.7z"), Path.Combine(_sourceDir, "lzmaExample.7z"));

            IDependencyDownloader downloader = new DownloaderFileShareCopy();
            var originalDownloader = downloader;
            downloader = new ZippedDependencyDownloader(originalDownloader, false);

            downloader.Download(_sourceDir, _targetDir, new DummyWatermark(), false, new Settings<DownloaderValidSettings>());

            Assert.IsTrue(File.Exists(Path.Combine(_targetDir, "lzmareadme.txt")));
            Assert.IsTrue(File.Exists(Path.Combine(_targetDir, "lzmasubfolder", "lzmareadme.txt")));

            //The lzmaFile should still be there
            Assert.IsTrue(File.Exists(Path.Combine(_targetDir, "lzmaExample.7z")));


        }


        [TestMethod]
        public void Download_ZippedDependencies_WithDllsInSameFolder_Test()
        {
            //Arrange - Delete the dlls references
            ApplicationSettings.Instance.DeleteAllValues();

            File.Copy(Path.Combine(Environment.CurrentDirectory, _resourcestDir, "example.zip"), Path.Combine(_sourceDir, "example.zip"));

            IDependencyDownloader downloader = new DownloaderFileShareCopy();
            var originalDownloader = downloader;
            downloader = new ZippedDependencyDownloader(originalDownloader, false);

            downloader.Download(_sourceDir, _targetDir, new DummyWatermark(), false, new Settings<DownloaderValidSettings>());

            //The archive should still be there
            Assert.IsTrue(File.Exists(Path.Combine(_targetDir, "example.zip")));

            Assert.IsTrue(File.Exists(Path.Combine(_targetDir, "readme.txt")));
            Assert.IsTrue(File.Exists(Path.Combine(_targetDir, "subfolder", "readme.txt")));
        }
    }
}
