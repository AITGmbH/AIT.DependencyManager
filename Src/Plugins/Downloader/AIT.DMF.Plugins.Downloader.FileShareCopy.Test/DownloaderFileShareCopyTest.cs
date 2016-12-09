using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using AIT.DMF.Contracts.Provider;
using AIT.DMF.PluginFactory;
using System.Collections.Generic;
using AIT.DMF.Contracts.Services;
using AIT.DMF.Contracts.Common;
using AIT.DMF.DependencyService;

namespace AIT.DMF.Plugins.Downloader.FileShareCopy.Test
{
    /// <summary>
    ///This is a test class for DownloaderFileShareTest and is intended
    ///to contain all DownloaderFileShareTest Unit Tests
    ///</summary>
    [TestClass()]
    public class DownloaderFileShareTest
    {


        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        //
        //You can use the following additional attributes as you write your tests:
        //
        //Use ClassInitialize to run code before running the first test in the class
        //[ClassInitialize()]
        //public static void MyClassInitialize(TestContext testContext)
        //{
        //}
        //
        //Use ClassCleanup to run code after all tests in a class have run
        //[ClassCleanup()]
        //public static void MyClassCleanup()
        //{
        //}
        //
        //Use TestInitialize to run code before running each test
        //[TestInitialize()]
        //public void MyTestInitialize()
        //{
        //}
        //
        //Use TestCleanup to run code after each test has run
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{
        //}
        //
        #endregion

        /// <summary>
        /// Download an existing component (with files and subfolders).
        /// </summary>
        [TestMethod()]
        public void Download_FileShare_ExistingComponent_Test()
        {
            // Prepare environment
            string tempPath = Path.GetTempPath();
            string guid = System.Guid.NewGuid().ToString();
            string compRootDir = tempPath + Path.DirectorySeparatorChar + guid;
            Directory.CreateDirectory(compRootDir);
            string sourceDir = compRootDir + Path.DirectorySeparatorChar + "SourceDir";
            string destDir = compRootDir + Path.DirectorySeparatorChar + "DestDir";
            Directory.CreateDirectory(sourceDir);
            Directory.CreateDirectory(destDir);
            string testDir = "testDir";
            string testFile1 = "testFile1.txt";
            string testFile2 = "testFile2.txt";
            Directory.CreateDirectory(sourceDir + Path.DirectorySeparatorChar + testDir);
            StreamWriter tf1 = File.CreateText(sourceDir + Path.DirectorySeparatorChar + testDir + Path.DirectorySeparatorChar + testFile1);
            tf1.WriteLine(testFile1);
            tf1.Close();
            StreamWriter tf2 = File.CreateText(sourceDir + Path.DirectorySeparatorChar + testFile2);
            tf2.WriteLine(testFile2);
            tf2.Close();

            IDependencyDownloaderFactory df = new DownloaderFactory();
            IDependencyDownloader downloader = df.GetDownloader("Downloader_FileShare");
            downloader.Download(sourceDir, destDir, new DummyWatermark(), false, new Settings<DownloaderValidSettings>());

            Assert.IsTrue(Directory.Exists(destDir + Path.DirectorySeparatorChar + testDir));
            Assert.IsTrue(File.Exists(destDir + Path.DirectorySeparatorChar + testDir + Path.DirectorySeparatorChar + testFile1));
            Assert.IsTrue(File.Exists(destDir+Path.DirectorySeparatorChar+ testFile2));
        }

        /// <summary>
        /// Download an existing component (No filter specified).
        /// </summary>
        [TestMethod()]
        public void Download_FileShare_NoFilter_Test()
        {
            // Prepare environment
            string tempPath = Path.GetTempPath();
            string guid = System.Guid.NewGuid().ToString();
            string compRootDir = tempPath + Path.DirectorySeparatorChar + guid;
            Directory.CreateDirectory(compRootDir);
            string sourceDir = compRootDir + Path.DirectorySeparatorChar + "SourceDir";
            string destDir = compRootDir + Path.DirectorySeparatorChar + "DestDir";
            Directory.CreateDirectory(sourceDir);
            Directory.CreateDirectory(destDir);
            string testDir = "testDir";
            string testFile1 = "testFile1.txt";
            string testFile2 = "testFile2.dll";
            Directory.CreateDirectory(sourceDir + Path.DirectorySeparatorChar + testDir);
            StreamWriter tf1 = File.CreateText(sourceDir + Path.DirectorySeparatorChar + testDir + Path.DirectorySeparatorChar + testFile1);
            tf1.WriteLine(testFile1);
            tf1.Close();
            StreamWriter tf2 = File.CreateText(sourceDir + Path.DirectorySeparatorChar + testFile2);
            tf2.WriteLine(testFile2);
            tf2.Close();

            IDependencyDownloaderFactory df = new DownloaderFactory();
            IDependencyDownloader downloader = df.GetDownloader("Downloader_FileShare");
            downloader.Download(sourceDir, destDir, new DummyWatermark(), false, new Settings<DownloaderValidSettings>());

            Assert.IsTrue(Directory.Exists(destDir + Path.DirectorySeparatorChar + testDir));
            Assert.IsTrue(File.Exists(destDir + Path.DirectorySeparatorChar + testDir + Path.DirectorySeparatorChar + testFile1));
            Assert.IsTrue(File.Exists(destDir + Path.DirectorySeparatorChar + testFile2));
        }

        /// <summary>
        /// Download an existing component (Fetch all filter specified).
        /// </summary>
        [TestMethod()]
        public void Download_FileShare_AsteriskFilter_Test()
        {
            // Prepare environment
            string tempPath = Path.GetTempPath();
            string guid = System.Guid.NewGuid().ToString();
            string compRootDir = tempPath + Path.DirectorySeparatorChar + guid;
            Directory.CreateDirectory(compRootDir);
            string sourceDir = compRootDir + Path.DirectorySeparatorChar + "SourceDir";
            string destDir = compRootDir + Path.DirectorySeparatorChar + "DestDir";
            Directory.CreateDirectory(sourceDir);
            Directory.CreateDirectory(destDir);
            string testDir = "testDir";
            string testFile1 = "testFile1.txt";
            string testFile2 = "testFile2.dll";
            Directory.CreateDirectory(sourceDir + Path.DirectorySeparatorChar + testDir);
            StreamWriter tf1 = File.CreateText(sourceDir + Path.DirectorySeparatorChar + testDir + Path.DirectorySeparatorChar + testFile1);
            tf1.WriteLine(testFile1);
            tf1.Close();
            StreamWriter tf2 = File.CreateText(sourceDir + Path.DirectorySeparatorChar + testFile2);
            tf2.WriteLine(testFile2);
            tf2.Close();

            IDependencyDownloaderFactory df = new DownloaderFactory();
            IDependencyDownloader downloader = df.GetDownloader("Downloader_FileShare");
            ISettings<DownloaderValidSettings> settings = new Settings<DownloaderValidSettings>();
            settings.AddSetting(new KeyValuePair<DownloaderValidSettings, string>(DownloaderValidSettings.VersionString, "*"));
            downloader.Download(sourceDir, destDir, new DummyWatermark(), false, settings);

            Assert.IsTrue(Directory.Exists(destDir + Path.DirectorySeparatorChar + testDir));
            Assert.IsTrue(File.Exists(destDir + Path.DirectorySeparatorChar + testDir + Path.DirectorySeparatorChar + testFile1));
            Assert.IsTrue(File.Exists(destDir + Path.DirectorySeparatorChar + testFile2));
        }

        /// <summary>
        /// Download an existing component (Fetch all filter matching the specified filetype and filename with filetype filters).
        /// </summary>
        [TestMethod()]
        public void Download_FileShare_FileTypeFilter_Test()
        {
            // Prepare environment
            string tempPath = Path.GetTempPath();
            string guid = System.Guid.NewGuid().ToString();
            string compRootDir = tempPath + Path.DirectorySeparatorChar + guid;
            Directory.CreateDirectory(compRootDir);
            string sourceDir = compRootDir + Path.DirectorySeparatorChar + "SourceDir";
            string destDir = compRootDir + Path.DirectorySeparatorChar + "DestDir";
            Directory.CreateDirectory(sourceDir);
            Directory.CreateDirectory(destDir);
            string testDir = "testDir";
            string testFile1 = "TestFile1.txt";
            string testFile2 = "testFile2.dll";
            string testFile3 = "testXml1.xml";
            string testFile4 = "Abcd.txt";
            Directory.CreateDirectory(sourceDir + Path.DirectorySeparatorChar + testDir);
            StreamWriter tf1 = File.CreateText(sourceDir + Path.DirectorySeparatorChar + testDir + Path.DirectorySeparatorChar + testFile1);
            tf1.WriteLine(testFile1);
            tf1.Close();
            StreamWriter tf2 = File.CreateText(sourceDir + Path.DirectorySeparatorChar + testFile2);
            tf2.WriteLine(testFile2);
            tf2.Close();
            StreamWriter tf3 = File.CreateText(sourceDir + Path.DirectorySeparatorChar + testDir + Path.DirectorySeparatorChar + testFile3);
            tf3.WriteLine(testFile3);
            tf3.Close();
            StreamWriter tf4 = File.CreateText(sourceDir + Path.DirectorySeparatorChar + testFile4);
            tf4.WriteLine(testFile4);
            tf4.Close();

            IDependencyDownloaderFactory df = new DownloaderFactory();
            IDependencyDownloader downloader = df.GetDownloader("Downloader_FileShare");
            ISettings<DownloaderValidSettings> settings = new Settings<DownloaderValidSettings>();
            settings.AddSetting(new KeyValuePair<DownloaderValidSettings, string>(DownloaderValidSettings.IncludedFilesFilter, "Test*.txt;*.dll"));
            downloader.Download(sourceDir, destDir, new DummyWatermark(), false, settings);

            Assert.IsTrue(Directory.Exists(destDir + Path.DirectorySeparatorChar + testDir));
            Assert.IsTrue(File.Exists(destDir + Path.DirectorySeparatorChar + testDir + Path.DirectorySeparatorChar + testFile1));
            Assert.IsTrue(File.Exists(destDir + Path.DirectorySeparatorChar + testFile2));
            Assert.IsFalse(File.Exists(destDir + Path.DirectorySeparatorChar + testDir + Path.DirectorySeparatorChar + testFile3));
            Assert.IsFalse(File.Exists(destDir + Path.DirectorySeparatorChar + testFile4));
        }

        /// <summary>
        /// Download an existing component by using an exclude filter on a specific pattern.
        /// </summary>
        [TestMethod()]
        public void Download_FileShare_ExcludeFilterSameLevel_Test()
        {
            // Prepare environment
            string tempPath = Path.GetTempPath();
            string guid = System.Guid.NewGuid().ToString();
            string compRootDir = tempPath + guid;
            Directory.CreateDirectory(compRootDir);
            string sourceDir = compRootDir + Path.DirectorySeparatorChar + "SourceDir";
            string destDir = compRootDir + Path.DirectorySeparatorChar + "DestDir";
            Directory.CreateDirectory(sourceDir);
            Directory.CreateDirectory(destDir);

            string testFile1 = "TestFile1.txt";
            string testFile2 = "TestFile2.txt";
            string testFile3 = "TestFile3.txt";
            string testFile4 = "TestXml1.xml";
            string testFile5 = "TestXml2.xml";
            string testFile6 = "TestXml3.xml";

            File.Create(Path.Combine(sourceDir, testFile1)).Close();
            File.Create(Path.Combine(sourceDir, testFile2)).Close();
            File.Create(Path.Combine(sourceDir, testFile3)).Close();
            File.Create(Path.Combine(sourceDir, testFile4)).Close();
            File.Create(Path.Combine(sourceDir, testFile5)).Close();
            File.Create(Path.Combine(sourceDir, testFile6)).Close();

            IDependencyDownloaderFactory df = new DownloaderFactory();
            IDependencyDownloader downloader = df.GetDownloader("Downloader_FileShare");
            ISettings<DownloaderValidSettings> settings = new Settings<DownloaderValidSettings>();
            settings.AddSetting(new KeyValuePair<DownloaderValidSettings, string>(DownloaderValidSettings.ExcludedFilesFilter, "Test*.xml"));
            downloader.Download(sourceDir, destDir, new DummyWatermark(), false, settings);

            Assert.IsTrue(File.Exists(Path.Combine(destDir, testFile1)));
            Assert.IsTrue(File.Exists(Path.Combine(destDir, testFile2)));
            Assert.IsTrue(File.Exists(Path.Combine(destDir, testFile3)));
            Assert.IsFalse(File.Exists(Path.Combine(destDir, testFile4)));
            Assert.IsFalse(File.Exists(Path.Combine(destDir, testFile5)));
            Assert.IsFalse(File.Exists(Path.Combine(destDir, testFile6)));
        }

        /// <summary>
        /// Download an existing component with subfolders by using an exclude filter for one of the subfolders.
        /// </summary>
        [TestMethod()]
        public void Download_FileShare_ExcludeFilterChildLevel_Test()
        {
            // Prepare environment
            string tempPath = Path.GetTempPath();
            string guid = System.Guid.NewGuid().ToString();
            string compRootDir = tempPath + guid;
            Directory.CreateDirectory(compRootDir);
            string sourceDir = compRootDir + Path.DirectorySeparatorChar + "SourceDir";
            string destDir = compRootDir + Path.DirectorySeparatorChar + "DestDir";
            Directory.CreateDirectory(sourceDir);
            Directory.CreateDirectory(destDir);
            string subDir1 = "subDir1";
            string subDir2 = "subDir2";
            string testFile1 = "TestFile1.txt";
            string testFile2 = "TestFile2.txt";
            string testFile3 = "TestFile3.txt";
            string testFile4 = "TestXml1.xml";
            string testFile5 = "TestXml2.xml";
            string testFile6 = "TestXml3.xml";
            string testFile7 = "TestCs1.cs";
            string testFile8 = "TestCs2.cs";
            string testFile9 = "TestCs3.cs";

            Directory.CreateDirectory(Path.Combine(sourceDir, subDir1));
            Directory.CreateDirectory(Path.Combine(sourceDir, subDir2));
            File.Create(Path.Combine(sourceDir, subDir1, testFile1)).Close();
            File.Create(Path.Combine(sourceDir, subDir1, testFile2)).Close();
            File.Create(Path.Combine(sourceDir, subDir1, testFile3)).Close();
            File.Create(Path.Combine(sourceDir, testFile4)).Close();
            File.Create(Path.Combine(sourceDir, testFile5)).Close();
            File.Create(Path.Combine(sourceDir, testFile6)).Close();
            File.Create(Path.Combine(sourceDir, subDir2, testFile7)).Close();
            File.Create(Path.Combine(sourceDir, subDir2, testFile8)).Close();
            File.Create(Path.Combine(sourceDir, subDir2, testFile9)).Close();

            IDependencyDownloaderFactory df = new DownloaderFactory();
            IDependencyDownloader downloader = df.GetDownloader("Downloader_FileShare");
            ISettings<DownloaderValidSettings> settings = new Settings<DownloaderValidSettings>();
            settings.AddSetting(new KeyValuePair<DownloaderValidSettings, string>(DownloaderValidSettings.ExcludedFilesFilter, "subdir1\\"));
            downloader.Download(sourceDir, destDir, new DummyWatermark(), false, settings);

            Assert.IsFalse(File.Exists(Path.Combine(destDir, subDir1, testFile1)));
            Assert.IsFalse(File.Exists(Path.Combine(destDir, subDir1, testFile2)));
            Assert.IsFalse(File.Exists(Path.Combine(destDir, subDir1, testFile3)));
            Assert.IsTrue(File.Exists(Path.Combine(destDir, testFile4)));
            Assert.IsTrue(File.Exists(Path.Combine(destDir, testFile5)));
            Assert.IsTrue(File.Exists(Path.Combine(destDir, testFile6)));
            Assert.IsFalse(Directory.Exists(Path.Combine(destDir, subDir1)));
            Assert.IsTrue(File.Exists(Path.Combine(destDir, subDir2, testFile7)));
            Assert.IsTrue(File.Exists(Path.Combine(destDir, subDir2, testFile8)));
            Assert.IsTrue(File.Exists(Path.Combine(destDir, subDir2, testFile9)));
        }

        /// <summary>
        /// Try to download a non existing component.
        ///</summary>
        [TestMethod()]
        [ExpectedException(typeof(DirectoryNotFoundException))]
        public void Download_NonExistingSourceDir_Test()
        {
            // Prepare environment
            string tempPath = Path.GetTempPath();
            string guid = System.Guid.NewGuid().ToString();
            string compRootDir = tempPath+Path.DirectorySeparatorChar+guid;
            Directory.CreateDirectory(compRootDir);

            IDependencyDownloaderFactory df = new DownloaderFactory();
            IDependencyDownloader downloader = df.GetDownloader("Downloader_FileShare");
            downloader.Download(compRootDir + Path.DirectorySeparatorChar + "testComp", tempPath, new DummyWatermark(), false, new Settings<DownloaderValidSettings>());
        }
    }
}
