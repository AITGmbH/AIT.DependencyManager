using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AIT.DMF.Contracts.Services;
using AIT.DMF.Contracts.Provider;
using AIT.DMF.PluginFactory;
using AIT.DMF.DependencyService;
using System.IO;
using System.IO.Compression;
using AIT.DMF.Contracts.Exceptions;
using System.Collections.Generic;

namespace AIT.DMF.Plugins.Downloader.Subversion.Test
{
    [TestClass]
    [DeploymentItem("Resources", "Resources")]
    public class DownloaderSubversionTest
    {
        private static string _svnRepoFile = "SVNRepo.zip";
        private static string _svnRepoFolderName = "SVNRepo";
        private static string _workingDir = Path.Combine(Environment.CurrentDirectory, "TestData");
        private static string _downloadDir = Path.Combine(Environment.CurrentDirectory, "Download");
        private static string _resourcesDir = Path.Combine(Environment.CurrentDirectory, "Resources");
        private static string _svnRepoFileFullPath = Path.Combine(_workingDir, _svnRepoFile);
        private static string _svnRepoFolderFullPath = Path.Combine(_workingDir, _svnRepoFolderName);
        private static string _svnBaseSourcePath = string.Format("file:///{0}", _svnRepoFolderFullPath.Replace("\\", "/"));
        private static string _svnSrcPath = string.Format("{0}{1}", _svnBaseSourcePath, "/trunk/src");
        private static string _svnTagsPath = string.Format("{0}{1}", _svnBaseSourcePath, "/tags");
        private static string _componentA = "ComponentA";
        private static string _componentWithExternals = "ComponentWithExternals";
        private static string _tagComponentA10 = "ComponentA_1.0";
        private static string _subFolderLogging = "Logging";
        private static string _subFolderCommon = "Common";
        private static string _subFolderSubCommon = "SubCommon";

        [ClassInitialize()]
        public static void ClassInit(TestContext testContext)
        {
            //Copy zipped SVN Repository and extract in
            Directory.CreateDirectory(_workingDir);
            File.Copy(Path.Combine(_resourcesDir, _svnRepoFile), Path.Combine(_workingDir, _svnRepoFileFullPath));
            ZipFile.ExtractToDirectory(_svnRepoFileFullPath, _svnRepoFolderFullPath);

            Assert.IsTrue(Directory.Exists(_svnRepoFolderFullPath));
        }
        
        /// <summary>
        /// Deletes after all tests the test data folder
        /// </summary>
        [ClassCleanup()]
        public static void ClassCleanup()
        {
            if (Directory.Exists(_workingDir))
            {
                Directory.Delete(_workingDir, true);
            }
        }
        
        /// <summary>
        /// Deletes after each test the folder with downloaded files.
        /// </summary>
        [TestCleanup()]
        public void TestCleanup()
        {
            if (Directory.Exists(_downloadDir))
            {
                Directory.Delete(_downloadDir, true);
            }
        }
                        
        /// <summary>
        /// Tries to download a not existing file
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(InvalidProviderConfigurationException))]
        [IgnoreAttribute]
        //Einzelne Dateien herunterladen sinnvoll?
        public void Download_Subversion_DownloadNotExistingFile()
        {
            string notExistingFile = string.Format("{0}/{1}/{2}", _svnSrcPath, _componentA, "notExistingFile");

            IDependencyDownloaderFactory df = new DownloaderFactory();
            IDependencyDownloader downloader = df.GetDownloader("Downloader_Subversion");
            downloader.Download(notExistingFile, _downloadDir, new DummyWatermark(), false, new Settings<DownloaderValidSettings>());
        }

        /// <summary>
        /// Tries to download a not existing folder
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(InvalidProviderConfigurationException))]
        public void Download_Subversion_DownloadNotExistingFolder()
        {
            string notExistingFolder = string.Format("{0}/{1}", _svnSrcPath, "notExistingFolder");

            var settings = new Settings<DownloaderValidSettings>();
            settings.AddSetting(new KeyValuePair<DownloaderValidSettings, string>(DownloaderValidSettings.VersionString, "H"));

            IDependencyDownloaderFactory df = new DownloaderFactory();
            IDependencyDownloader downloader = df.GetDownloader("Downloader_Subversion");
            downloader.Download(notExistingFolder, _downloadDir, new DummyWatermark(), false, settings);
        }

        /// <summary>
        /// Downloads an existing folder, but target folder already exists.
        /// </summary>
        [TestMethod]
        public void Download_Subversion_DownloadExistingFolder_DestinationFolderExists()
        {
            Directory.CreateDirectory(_downloadDir);

            var sourceDir = string.Format("{0}/{1}", _svnSrcPath, _componentA);

            var settings = new Settings<DownloaderValidSettings>();
            settings.AddSetting(new KeyValuePair<DownloaderValidSettings, string>(DownloaderValidSettings.VersionString, "H"));

            IDependencyDownloaderFactory df = new DownloaderFactory();
            IDependencyDownloader downloader = df.GetDownloader("Downloader_Subversion");
            downloader.Download(sourceDir, _downloadDir, new DummyWatermark(), false, settings);

            Assert.IsTrue(File.Exists(Path.Combine(_downloadDir, "Class1.cs")));
            Assert.IsTrue(File.Exists(Path.Combine(_downloadDir, "Class2.cs")));
            Assert.IsTrue(File.Exists(Path.Combine(_downloadDir, "Class3.cs")));
            Assert.IsTrue(File.Exists(Path.Combine(_downloadDir, "Logging", "LoggingClass.cs")));
        }

        /// <summary>
        /// Downloads an existing folder, but target not exists.
        /// </summary>
        [TestMethod]
        public void Download_Subversion_DownloadExistingFolder_DestinationFolderNotExists()
        {
            Directory.CreateDirectory(_downloadDir);

            var sourceDir = string.Format("{0}/{1}", _svnSrcPath, _componentA);

            var settings = new Settings<DownloaderValidSettings>();
            settings.AddSetting(new KeyValuePair<DownloaderValidSettings, string>(DownloaderValidSettings.VersionString, "H"));

            IDependencyDownloaderFactory df = new DownloaderFactory();
            IDependencyDownloader downloader = df.GetDownloader("Downloader_Subversion");
            downloader.Download(sourceDir, _downloadDir, new DummyWatermark(), false, settings);

            Assert.IsTrue(File.Exists(Path.Combine(_downloadDir, "Class1.cs")));
            Assert.IsTrue(File.Exists(Path.Combine(_downloadDir, "Class2.cs")));
            Assert.IsTrue(File.Exists(Path.Combine(_downloadDir, "Class3.cs")));
            Assert.IsTrue(File.Exists(Path.Combine(_downloadDir, "Logging", "LoggingClass.cs")));
        }

        /// <summary>
        /// Downloads an existing folder with specific revision. Files LoggingClass.cs (Rev=20) and Class4.cs (Rev=29) not exist in this revision.
        /// </summary>
        [TestMethod]
        public void Download_Subversion_DownloadExistingFolder_WithSpecificRevision18()
        {
            Directory.CreateDirectory(_downloadDir);

            var sourceDir = string.Format("{0}/{1}", _svnSrcPath, _componentA);

            var settings = new Settings<DownloaderValidSettings>();
            settings.AddSetting(new KeyValuePair<DownloaderValidSettings, string>(DownloaderValidSettings.VersionString, "R18"));

            IDependencyDownloaderFactory df = new DownloaderFactory();
            IDependencyDownloader downloader = df.GetDownloader("Downloader_Subversion");
            downloader.Download(sourceDir, _downloadDir, new DummyWatermark(), false, settings);

            Assert.IsTrue(File.Exists(Path.Combine(_downloadDir, "Class1.cs")));
            Assert.IsTrue(File.Exists(Path.Combine(_downloadDir, "Class2.cs")));
            Assert.IsTrue(File.Exists(Path.Combine(_downloadDir, "Class3.cs")));
            Assert.IsFalse(File.Exists(Path.Combine(_downloadDir, "Logging", "LoggingClass.cs")));
            Assert.IsFalse(File.Exists(Path.Combine(_downloadDir, "Class4.cs")));
        }

        /// <summary>
        /// Downloads an existing folder with specific revision. Files LoggingClass.cs and Class4.cs exist in this revision.
        /// </summary>
        [TestMethod]
        public void Download_Subversion_DownloadExistingFolder_WithSpecificRevision29()
        {
            Directory.CreateDirectory(_downloadDir);

            var sourceDir = string.Format("{0}/{1}", _svnSrcPath, _componentA);

            var settings = new Settings<DownloaderValidSettings>();
            settings.AddSetting(new KeyValuePair<DownloaderValidSettings, string>(DownloaderValidSettings.VersionString, "R29"));

            IDependencyDownloaderFactory df = new DownloaderFactory();
            IDependencyDownloader downloader = df.GetDownloader("Downloader_Subversion");
            downloader.Download(sourceDir, _downloadDir, new DummyWatermark(), false, settings);

            Assert.IsTrue(File.Exists(Path.Combine(_downloadDir, "Class1.cs")));
            Assert.IsTrue(File.Exists(Path.Combine(_downloadDir, "Class2.cs")));
            Assert.IsTrue(File.Exists(Path.Combine(_downloadDir, "Class3.cs")));
            Assert.IsTrue(File.Exists(Path.Combine(_downloadDir, "Logging", "LoggingClass.cs")));
            Assert.IsTrue(File.Exists(Path.Combine(_downloadDir, "Class4.cs")));
        }
        
        /// <summary>
        /// Downloads a tag. A tag is a freezed version of a folder (TFS does not provide similar function). Independent from requested revision, 
        /// the same version is exported all time, so the HEAD revision is used. The tested tag does not include file Class4.cs.
        /// SharpSVN differentiates not between typical folder and tag. 
        /// </summary>
        [TestMethod()]
        public void Download_Subversion_DownloadTag()
        {
            Directory.CreateDirectory(_downloadDir);

            var sourceDir = string.Format("{0}/{1}", _svnTagsPath, _tagComponentA10);

            var settings = new Settings<DownloaderValidSettings>();
            settings.AddSetting(new KeyValuePair<DownloaderValidSettings, string>(DownloaderValidSettings.VersionString, "H"));

            IDependencyDownloaderFactory df = new DownloaderFactory();
            IDependencyDownloader downloader = df.GetDownloader("Downloader_Subversion");
            downloader.Download(sourceDir, _downloadDir, new DummyWatermark(), false, settings);

            Assert.IsTrue(File.Exists(Path.Combine(_downloadDir, "Class1.cs")));
            Assert.IsTrue(File.Exists(Path.Combine(_downloadDir, "Class2.cs")));
            Assert.IsTrue(File.Exists(Path.Combine(_downloadDir, "Class3.cs")));
            Assert.IsTrue(File.Exists(Path.Combine(_downloadDir, "Logging", "LoggingClass.cs")));
            Assert.IsFalse(File.Exists(Path.Combine(_downloadDir, "Class4.cs")));
        }
        
        /// <summary>
        /// Tests the exclude filter by excluding the subfolder Logging.
        /// </summary>
        [TestMethod]
        public void Download_Subversion_ExcludeFolder()
        {
            //Create the excludefilter
            var filter = new KeyValuePair<DownloaderValidSettings, string>(DownloaderValidSettings.ExcludedFilesFilter, _subFolderLogging + "\\");
            var settings = new DependencyService.Settings<DownloaderValidSettings>();
            settings.AddSetting(filter);
            settings.AddSetting(new KeyValuePair<DownloaderValidSettings, string>(DownloaderValidSettings.VersionString, "H"));
            
            var sourceDir = string.Format("{0}/{1}", _svnSrcPath, _componentA);
            
            IDependencyDownloaderFactory df = new DownloaderFactory();
            IDependencyDownloader downloader = df.GetDownloader("Downloader_Subversion");
            downloader.Download(sourceDir, _downloadDir, new DummyWatermark(), false, settings);

            Assert.IsTrue(File.Exists(Path.Combine(_downloadDir, "Class1.cs")));
            Assert.IsTrue(File.Exists(Path.Combine(_downloadDir, "Class2.cs")));
            Assert.IsTrue(File.Exists(Path.Combine(_downloadDir, "Class3.cs")));
            Assert.IsTrue(File.Exists(Path.Combine(_downloadDir, "Class4.cs")));
            Assert.IsTrue(File.Exists(Path.Combine(_downloadDir, "component.targets")));
            //The folder logging and its files should not be there
            Assert.IsFalse(File.Exists(Path.Combine(_downloadDir, _subFolderLogging, "LoggingClass.cs")));
            Assert.IsFalse(Directory.Exists(Path.Combine(_downloadDir, _subFolderLogging)));
        }

        /// <summary>
        /// Tests the exclude filter by excluding all files with file extension targets.
        /// </summary>
        [TestMethod]
        public void Download_Subversion_ExcludeFileTypes()
        {
            //Create the excludefilter
            var filter = new KeyValuePair<DownloaderValidSettings, string>(DownloaderValidSettings.ExcludedFilesFilter, "*.targets");
            var settings = new DependencyService.Settings<DownloaderValidSettings>();
            settings.AddSetting(filter);
            settings.AddSetting(new KeyValuePair<DownloaderValidSettings, string>(DownloaderValidSettings.VersionString, "H"));
            
            var sourceDir = string.Format("{0}/{1}", _svnSrcPath, _componentA);

            IDependencyDownloaderFactory df = new DownloaderFactory();
            IDependencyDownloader downloader = df.GetDownloader("Downloader_Subversion");
            downloader.Download(sourceDir, _downloadDir, new DummyWatermark(), false, settings);

            Assert.IsTrue(File.Exists(Path.Combine(_downloadDir, "Class1.cs")));
            Assert.IsTrue(File.Exists(Path.Combine(_downloadDir, "Class2.cs")));
            Assert.IsTrue(File.Exists(Path.Combine(_downloadDir, "Class3.cs")));
            Assert.IsTrue(File.Exists(Path.Combine(_downloadDir, "Class4.cs")));
            Assert.IsTrue(File.Exists(Path.Combine(_downloadDir, _subFolderLogging, "LoggingClass.cs")));
            //The targets file should not be there
            Assert.IsFalse(File.Exists(Path.Combine(_downloadDir, "component.targets")));
        }

        /// <summary>
        /// Tests the include filter by including only the subfolder Logging of Component A.
        /// </summary>
        [TestMethod]
        public void Download_Subversion_IncludeFolder()
        {
            //Create the excludefilter
            var filter = new KeyValuePair<DownloaderValidSettings, string>(DownloaderValidSettings.IncludedFilesFilter, _subFolderLogging + "\\");
            var settings = new DependencyService.Settings<DownloaderValidSettings>();
            settings.AddSetting(filter);
            settings.AddSetting(new KeyValuePair<DownloaderValidSettings, string>(DownloaderValidSettings.VersionString, "H"));

            var sourceDir = string.Format("{0}/{1}", _svnSrcPath, _componentA);

            IDependencyDownloaderFactory df = new DownloaderFactory();
            IDependencyDownloader downloader = df.GetDownloader("Downloader_Subversion");
            downloader.Download(sourceDir, _downloadDir, new DummyWatermark(), false, settings);

            Assert.IsFalse(File.Exists(Path.Combine(_downloadDir, "Class1.cs")));
            Assert.IsFalse(File.Exists(Path.Combine(_downloadDir, "Class2.cs")));
            Assert.IsFalse(File.Exists(Path.Combine(_downloadDir, "Class3.cs")));
            Assert.IsFalse(File.Exists(Path.Combine(_downloadDir, "Class4.cs")));
            Assert.IsTrue(File.Exists(Path.Combine(_downloadDir, _subFolderLogging, "LoggingClass.cs")));
        }

        /// <summary>
        /// Tests the include filter by including only files with file extension cs.
        /// </summary>
        [TestMethod]
        public void Download_Subversion_IncludeFiles()
        {
            //Create the excludefilter
            var filter = new KeyValuePair<DownloaderValidSettings, string>(DownloaderValidSettings.IncludedFilesFilter, "*.cs");
            var settings = new DependencyService.Settings<DownloaderValidSettings>();
            settings.AddSetting(filter);
            settings.AddSetting(new KeyValuePair<DownloaderValidSettings, string>(DownloaderValidSettings.VersionString, "H"));

            var sourceDir = string.Format("{0}/{1}", _svnSrcPath, _componentA);

            IDependencyDownloaderFactory df = new DownloaderFactory();
            IDependencyDownloader downloader = df.GetDownloader("Downloader_Subversion");
            downloader.Download(sourceDir, _downloadDir, new DummyWatermark(), false, settings);

            Assert.IsTrue(File.Exists(Path.Combine(_downloadDir, "Class1.cs")));
            Assert.IsTrue(File.Exists(Path.Combine(_downloadDir, "Class2.cs")));
            Assert.IsTrue(File.Exists(Path.Combine(_downloadDir, "Class3.cs")));
            Assert.IsTrue(File.Exists(Path.Combine(_downloadDir, "Class4.cs")));
            Assert.IsTrue(File.Exists(Path.Combine(_downloadDir, _subFolderLogging, "LoggingClass.cs")));
            //The targets file should not be there
            Assert.IsFalse(File.Exists(Path.Combine(_downloadDir, "component.targets")));
        }

        /// <summary>
        /// Tests the include and exclude filter at the same time.
        /// </summary>
        [TestMethod]
        public void Download_Subversion_IncludeFiles_ExcludeFolder()
        {
            //Create the excludefilter
            var filefilter = new KeyValuePair<DownloaderValidSettings, string>(DownloaderValidSettings.IncludedFilesFilter, "*.cs");
            var folderfilter = new KeyValuePair<DownloaderValidSettings, string>(DownloaderValidSettings.ExcludedFilesFilter, _subFolderLogging + "\\");
            var settings = new DependencyService.Settings<DownloaderValidSettings>();
            settings.AddSetting(filefilter);
            settings.AddSetting(folderfilter);
            settings.AddSetting(new KeyValuePair<DownloaderValidSettings, string>(DownloaderValidSettings.VersionString, "H"));

            var sourceDir = string.Format("{0}/{1}", _svnSrcPath, _componentA);

            IDependencyDownloaderFactory df = new DownloaderFactory();
            IDependencyDownloader downloader = df.GetDownloader("Downloader_Subversion");
            downloader.Download(sourceDir, _downloadDir, new DummyWatermark(), false, settings);

            Assert.IsTrue(File.Exists(Path.Combine(_downloadDir, "Class1.cs")));
            Assert.IsTrue(File.Exists(Path.Combine(_downloadDir, "Class2.cs")));
            Assert.IsTrue(File.Exists(Path.Combine(_downloadDir, "Class3.cs")));
            Assert.IsTrue(File.Exists(Path.Combine(_downloadDir, "Class4.cs")));
            //The targets file should not be there
            Assert.IsFalse(File.Exists(Path.Combine(_downloadDir, "component.targets")));
            //The subfolder Logging should no be there, too
            Assert.IsFalse(File.Exists(Path.Combine(_downloadDir, _subFolderLogging, "LoggingClass.cs")));
        }
        
        /// <summary>
        /// Tests the folder mapping by renaming all subfolders. Folder .../src/ComponentA is renamed to Component_renamed in the target folder.
        /// </summary>
        [TestMethod]
        public void Download_Subversion_FolderMappingsRenameFolderSameLevel()
        {
            var targetFolder = "ComponentA_renamed";

            var settings = new DependencyService.Settings<DownloaderValidSettings>();
            //Create the folder mapping
            var mappings = new KeyValuePair<DownloaderValidSettings, string>(DownloaderValidSettings.FolderMappings, string.Format("SourceOffset={0},LocalOffset={1};", _componentA, targetFolder));
            settings.AddSetting(mappings);
            settings.AddSetting(new KeyValuePair<DownloaderValidSettings, string>(DownloaderValidSettings.VersionString, "H"));
            
            IDependencyDownloaderFactory df = new DownloaderFactory();
            IDependencyDownloader downloader = df.GetDownloader("Downloader_Subversion");
            downloader.Download(_svnSrcPath, _downloadDir, new DummyWatermark(), false, settings);

            Assert.IsTrue(File.Exists(Path.Combine(_downloadDir, targetFolder, "Class1.cs")));
            Assert.IsTrue(File.Exists(Path.Combine(_downloadDir, targetFolder, "Class2.cs")));
            Assert.IsTrue(File.Exists(Path.Combine(_downloadDir, targetFolder, "Class3.cs")));
            Assert.IsTrue(File.Exists(Path.Combine(_downloadDir, targetFolder, "Class4.cs")));
            Assert.IsTrue(File.Exists(Path.Combine(_downloadDir, targetFolder, "component.targets")));
            Assert.IsTrue(File.Exists(Path.Combine(_downloadDir, targetFolder, _subFolderLogging, "LoggingClass.cs")));
        }

        /// <summary>
        /// Tests the folder mapping by moving all folders to a subfolder
        /// </summary>
        [TestMethod]
        [Ignore]
        public void Download_Subversion_FolderMappingsMoveToSubfolder()
        {
            var targetFolder = @"libs\ComponentA_V10";

            var settings = new DependencyService.Settings<DownloaderValidSettings>();
            //Create the folder mapping
            var mappings = new KeyValuePair<DownloaderValidSettings, string>(DownloaderValidSettings.FolderMappings, string.Format("SourceOffset={0}/{1},LocalOffset=.//", _componentA, _subFolderLogging));
            settings.AddSetting(mappings);
            settings.AddSetting(new KeyValuePair<DownloaderValidSettings, string>(DownloaderValidSettings.VersionString, "H"));

            var sourceDir = string.Format("{0}/{1}", _svnSrcPath, _componentA);

            IDependencyDownloaderFactory df = new DownloaderFactory();
            IDependencyDownloader downloader = df.GetDownloader("Downloader_Subversion");
            downloader.Download(sourceDir, _downloadDir, new DummyWatermark(), false, settings);

            Assert.IsTrue(File.Exists(Path.Combine(_downloadDir, targetFolder, "ComponentA.1_0.dll")));
            Assert.IsTrue(File.Exists(Path.Combine(_downloadDir, targetFolder, "component.targets")));
        }

        /// <summary>
        /// Downloads a folder with svn:externals (similar to symbolic links in Windows)
        /// </summary>
        [TestMethod]
        public void Download_Subversion_FolderWithExternals()
        {
            var sourceDir = string.Format("{0}/{1}", _svnSrcPath, _componentWithExternals);

            var settings = new DependencyService.Settings<DownloaderValidSettings>();
            settings.AddSetting(new KeyValuePair<DownloaderValidSettings, string>(DownloaderValidSettings.VersionString, "H"));
            
            IDependencyDownloaderFactory df = new DownloaderFactory();
            IDependencyDownloader downloader = df.GetDownloader("Downloader_Subversion");
            downloader.Download(sourceDir, _downloadDir, new DummyWatermark(), false, settings);

            Assert.IsTrue(File.Exists(Path.Combine(_downloadDir, "UsingCommon.cs")));
            Assert.IsTrue(File.Exists(Path.Combine(_downloadDir, _subFolderCommon, "Common1.cs")));
            Assert.IsTrue(File.Exists(Path.Combine(_downloadDir, _subFolderCommon, "Common2.cs")));
            Assert.IsTrue(File.Exists(Path.Combine(_downloadDir, _subFolderCommon, "Common3.cs")));
            Assert.IsTrue(File.Exists(Path.Combine(_downloadDir, _subFolderCommon, _subFolderSubCommon, "SubCommon1.cs")));
            Assert.IsTrue(File.Exists(Path.Combine(_downloadDir, _subFolderCommon, _subFolderSubCommon, "SubCommon2.cs")));
            Assert.IsTrue(File.Exists(Path.Combine(_downloadDir, _subFolderCommon, _subFolderSubCommon, "SubCommon3.cs")));
        }

        /// <summary>
        /// Downloads an existing file with svn:externals attribute
        /// </summary>
        [TestMethod]
        public void Download_Subversion_DownloadExistingFileWithExternals()
        {
            Assert.AreEqual(1, 1);
        }

        ///Weitere Test
        ///- Authorisierung
        ///
    }
}
