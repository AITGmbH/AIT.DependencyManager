using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using System.Linq;
using AIT.DMF.Common;
using AIT.DMF.Contracts.Common;
using AIT.DMF.Contracts.Provider;
using AIT.DMF.Contracts.Exceptions;
using AIT.DMF.Contracts.Services;
using AIT.DMF.DependencyService;

namespace AIT.DMF.Plugins.Resolver.FileShare.Test
{


    /// <summary>
    ///This is a test class for ResolverFileShareTest and is intended
    ///to contain all ResolverFileShareTest Unit Tests
    ///</summary>
    [TestClass()]
    public class ResolverFileShareTest
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
        /// Test ResolverFileShare with invalid url.
        /// </summary>
        [TestMethod()]
        [ExpectedException(typeof(InvalidProviderConfigurationException))]
        public void Resolver_FileShare_InvalidUrl_Test()
        {
            ISettings<ResolverValidSettings> sett = new Settings<ResolverValidSettings>();
            sett.AddSetting(new KeyValuePair<ResolverValidSettings, string>(ResolverValidSettings.FileShareUrl, @"\\\server\folder"));
            sett.AddSetting(new KeyValuePair<ResolverValidSettings, string>(ResolverValidSettings.DependencyDefinitionFileNameList, "component.targets"));

            var rfs = new ResolverFileShare(sett);
        }

        /// <summary>
        ///Tests the GetAvailableComponentNames method with populated fileshare.
        ///</summary>
        [TestMethod()]
        public void GetAvailableComponentNames_ExistingComponents_Test()
        {
            // Prepare environment
            string tempPath = Path.GetTempPath();
            string guid = System.Guid.NewGuid().ToString();
            string repositoryRootDir = tempPath + Path.DirectorySeparatorChar + guid;
            Directory.CreateDirectory(repositoryRootDir);
            string comp1 = "testComp1";
            string comp2 = "testComp2";

            Directory.CreateDirectory(repositoryRootDir + Path.DirectorySeparatorChar + comp1);
            Directory.CreateDirectory(repositoryRootDir + Path.DirectorySeparatorChar + comp2);

            ISettings<ResolverValidSettings> sett = new Settings<ResolverValidSettings>();
            sett.AddSetting(new KeyValuePair<ResolverValidSettings, string>(ResolverValidSettings.FileShareUrl, repositoryRootDir));
            sett.AddSetting(new KeyValuePair<ResolverValidSettings, string>(ResolverValidSettings.DependencyDefinitionFileNameList,  "component.targets"));

            IDependencyResolver rfs = new ResolverFileShare(sett);

            List<IComponentName> expectedList = new List<IComponentName>();
            expectedList.Add(new ComponentName(comp1));
            expectedList.Add(new ComponentName(comp2));
            IEnumerable<IComponentName> actual;
            actual = rfs.GetAvailableComponentNames();

            Assert.AreEqual(actual.Count(), expectedList.Count());
            IEnumerator<IComponentName> eA = actual.GetEnumerator();
            IEnumerator<IComponentName> eE = expectedList.GetEnumerator();
            while (eA.MoveNext() && eE.MoveNext())
            {
                Assert.AreEqual(eA.Current.Path, eE.Current.Path);
            }
        }

        /// <summary>
        ///Tests the GetAvailableComponentNames method with empty fileshare.
        /// </summary>
        [TestMethod()]
        public void GetAvailableComponentNames_EmptyFileShareTest_Test()
        {
            // Prepare environment
            string tempPath = Path.GetTempPath();
            string guid = System.Guid.NewGuid().ToString();
            string emptyRootDir = tempPath + Path.DirectorySeparatorChar + guid;
            Directory.CreateDirectory(emptyRootDir);

            ISettings<ResolverValidSettings> sett = new Settings<ResolverValidSettings>();
            sett.AddSetting(new KeyValuePair<ResolverValidSettings, string>(ResolverValidSettings.FileShareUrl, emptyRootDir));
            sett.AddSetting(new KeyValuePair<ResolverValidSettings, string>(ResolverValidSettings.DependencyDefinitionFileNameList, "component.targets"));

            var rfs = new ResolverFileShare(sett);

            List<IComponentName> expectedList = new List<IComponentName>();
            IEnumerable<IComponentName> actual;
            actual = rfs.GetAvailableComponentNames();

            Assert.AreEqual(actual.Count(), expectedList.Count());
            IEnumerator<IComponentName> eA = actual.GetEnumerator();
            IEnumerator<IComponentName> eE = expectedList.GetEnumerator();
            while (eA.MoveNext() && eA.MoveNext())
            {
                Assert.AreEqual(eA.Current.Path, eE.Current.Path);
            }
        }

        /// <summary>
        ///Tests the GetAvailableComponentNames method with non-existing fileshare.
        ///This should generate a InvalidProviderConfigurationExpection exception.
        /// </summary>
        [TestMethod()]
        [ExpectedException(typeof(InvalidProviderConfigurationException))]
        public void GetAvailableComponentNames_NonExistingFileShareTest_Test()
        {
            // Prepare environment
            string tempPath = Path.GetTempPath();
            string guid = System.Guid.NewGuid().ToString();
            string nonexistingRootDir = tempPath + Path.DirectorySeparatorChar + guid;

            ISettings<ResolverValidSettings> sett = new Settings<ResolverValidSettings>();
            sett.AddSetting(new KeyValuePair<ResolverValidSettings, string>(ResolverValidSettings.FileShareUrl, nonexistingRootDir));
            sett.AddSetting(new KeyValuePair<ResolverValidSettings, string>(ResolverValidSettings.DependencyDefinitionFileNameList, "component.targets"));

            IDependencyResolver rfs = new ResolverFileShare(sett);

            List<IComponentName> expectedList = new List<IComponentName>();
            IEnumerable<IComponentName> actual;
            actual = rfs.GetAvailableComponentNames();
        }

        /// <summary>
        ///Tests the GetAvailableVersions for an existing component with version subdirectories.
        ///</summary>
        [TestMethod()]
        public void GetAvailableVersions_ExistingComponent_Test()
        {
            // Prepare environment
            string tempPath = Path.GetTempPath();
            string guid = System.Guid.NewGuid().ToString();
            string repositoryRootDir = Path.Combine(tempPath, guid);
            Directory.CreateDirectory(repositoryRootDir);
            string compName = "comp";
            string compDir = Path.Combine(repositoryRootDir, compName);
            Directory.CreateDirectory(compDir);
            string vers1 = "1.0";
            string vers2 = "1.39";
            string vers3 = "1.0.0.35";
            string invalidVersion = "testabcd";
            Directory.CreateDirectory(Path.Combine(compDir, vers1));
            Directory.CreateDirectory(Path.Combine(compDir, vers2));
            Directory.CreateDirectory(Path.Combine(compDir, vers3));
            Directory.CreateDirectory(Path.Combine(compDir, invalidVersion));
            File.Create(Path.Combine(compDir, vers1, "component.targets"));
            File.Create(Path.Combine(compDir, vers2, "component.targets"));
            File.Create(Path.Combine(compDir, vers3, "component.targets"));

            ISettings<ResolverValidSettings> sett = new Settings<ResolverValidSettings>();
            sett.AddSetting(new KeyValuePair<ResolverValidSettings, string>(ResolverValidSettings.FileShareUrl, repositoryRootDir));
            sett.AddSetting(new KeyValuePair<ResolverValidSettings, string>(ResolverValidSettings.DependencyDefinitionFileNameList, "component.targets"));

            IDependencyResolver rfs = new ResolverFileShare(sett);

            List<IComponentVersion> expectedVersionList = new List<IComponentVersion>();
            expectedVersionList.Add(new ComponentVersion(vers1));
            expectedVersionList.Add(new ComponentVersion(vers3));
            expectedVersionList.Add(new ComponentVersion(vers2));

            IEnumerable<IComponentVersion> actual = rfs.GetAvailableVersions(new ComponentName(compName));

            Assert.AreEqual(actual.Count(), expectedVersionList.Count());
            IEnumerator<IComponentVersion> eA = actual.GetEnumerator();
            IEnumerator<IComponentVersion> eE = expectedVersionList.GetEnumerator();
            while (eA.MoveNext() && eE.MoveNext())
            {
                Assert.AreEqual(eA.Current.Version, eE.Current.Version);
            }
        }

        /// <summary>
        ///Tests the GetAvailableVersions for an existing component with no versions existing.
        /// </summary>
        [TestMethod()]
        public void GetAvailableVersions_ExistingComponentNoVersions_Test()
        {
            // Prepare environment
            string tempPath = Path.GetTempPath();
            string guid = System.Guid.NewGuid().ToString();
            string repositoryRootDir = Path.Combine(tempPath, guid);
            Directory.CreateDirectory(repositoryRootDir);
            string compName = "comp";
            string compDir = Path.Combine(repositoryRootDir, compName);
            Directory.CreateDirectory(compDir);

            ISettings<ResolverValidSettings> sett = new Settings<ResolverValidSettings>();
            sett.AddSetting(new KeyValuePair<ResolverValidSettings, string>(ResolverValidSettings.FileShareUrl, repositoryRootDir));
            sett.AddSetting(new KeyValuePair<ResolverValidSettings, string>(ResolverValidSettings.DependencyDefinitionFileNameList, "component.targets"));

            IDependencyResolver rfs = new ResolverFileShare(sett);

            List<IComponentVersion> noVersionList = new List<IComponentVersion>();

            IEnumerable<IComponentVersion> actual = rfs.GetAvailableVersions(new ComponentName(compName));

            Assert.AreEqual(actual.Count(), noVersionList.Count());
            IEnumerator<IComponentVersion> eA = actual.GetEnumerator();
            IEnumerator<IComponentVersion> eN = noVersionList.GetEnumerator();
            while (eA.MoveNext() && eN.MoveNext())
            {
                Assert.AreEqual(eA.Current.Version, eN.Current.Version);
            }
        }

        /// <summary>
        ///Tests the GetAvailableVersions if component does not exist.
        ///This should generate a InvalidComponentException exception.
        ///</summary>
        [TestMethod()]
        [ExpectedException(typeof(InvalidComponentException))]
        public void GetAvailableVersions_NonExistingComponent_Test()
        {
            // Prepare environment
            string tempPath = Path.GetTempPath();
            string guid = System.Guid.NewGuid().ToString();
            string repositoryRootDir = tempPath + Path.DirectorySeparatorChar + guid;
            Directory.CreateDirectory(repositoryRootDir);
            string nonexistingComp = "nonExistingComp";

            ISettings<ResolverValidSettings> sett = new Settings<ResolverValidSettings>();
            sett.AddSetting(new KeyValuePair<ResolverValidSettings, string>(ResolverValidSettings.FileShareUrl, repositoryRootDir));
            sett.AddSetting(new KeyValuePair<ResolverValidSettings, string>(ResolverValidSettings.DependencyDefinitionFileNameList, "component.targets"));

            IDependencyResolver rfs = new ResolverFileShare(sett);

            rfs.GetAvailableVersions(new ComponentName(nonexistingComp));
        }

        /// <summary>
        ///A test for LoadComponentTarget method via loading an existing component.targets file.
        ///</summary>
        [TestMethod()]
        public void LoadComponentTarget_ExistingCompTargetsFile_Test()
        {
            // Prepare environment
            string tempPath = Path.GetTempPath();
            string guid = System.Guid.NewGuid().ToString();
            string repositoryRootDir = tempPath + Path.DirectorySeparatorChar + guid;
            Directory.CreateDirectory(repositoryRootDir);
            string compName = "comp";
            string compDir = repositoryRootDir + Path.DirectorySeparatorChar + compName;
            Directory.CreateDirectory(compDir);
            string compVersion = "1.0.0.35";
            Directory.CreateDirectory(compDir + Path.DirectorySeparatorChar + compVersion);
            string compTargetsFilename = "component.targets";
            StreamWriter sw = new StreamWriter(File.Create(compDir + Path.DirectorySeparatorChar + compVersion + Path.DirectorySeparatorChar + compTargetsFilename));
            sw.WriteLine("<Component>");
            sw.WriteLine("<Dependencies>");
            sw.WriteLine("</Dependencies>");
            sw.WriteLine("</Component>");
            sw.Close();

            ISettings<ResolverValidSettings> sett = new Settings<ResolverValidSettings>();
            sett.AddSetting(new KeyValuePair<ResolverValidSettings, string>(ResolverValidSettings.FileShareUrl, repositoryRootDir));
            sett.AddSetting(new KeyValuePair<ResolverValidSettings, string>(ResolverValidSettings.DependencyDefinitionFileNameList, "component.targets"));

            IDependencyResolver rfs = new ResolverFileShare(sett);
            XDocument compXdoc = rfs.LoadComponentTarget(new ComponentName(compName), new ComponentVersion(compVersion));

            Assert.AreEqual(1, compXdoc.Descendants("Component").Count());
            Assert.AreEqual(1, compXdoc.Descendants("Dependencies").Count());

            // Cleanup the temp folder, to test the Cleanup on Build Server
            File.Delete(compDir + Path.DirectorySeparatorChar + compVersion + Path.DirectorySeparatorChar + compTargetsFilename);
            Directory.Delete(Path.Combine(compDir, compVersion));
        }

        /// <summary>
        ///Tests the LoadComponentTarget method with a component which has no component.targets file.
        /// </summary>
        [TestMethod()]
        public void LoadComponentTarget_NonExistingCompTargetsFile_Test()
        {
            // Prepare environment
            string tempPath = Path.GetTempPath();
            string guid = System.Guid.NewGuid().ToString();
            string repositoryRootDir = tempPath + Path.DirectorySeparatorChar + guid;
            Directory.CreateDirectory(repositoryRootDir);
            string compName = "existCompWithVersion";
            string compDir = repositoryRootDir + Path.DirectorySeparatorChar + compName;
            Directory.CreateDirectory(compDir);
            string compVersion = "1.0.0.35";
            Directory.CreateDirectory(compDir + Path.DirectorySeparatorChar + compVersion);

            ISettings<ResolverValidSettings> sett = new Settings<ResolverValidSettings>();
            sett.AddSetting(new KeyValuePair<ResolverValidSettings, string>(ResolverValidSettings.FileShareUrl, repositoryRootDir));
            sett.AddSetting(new KeyValuePair<ResolverValidSettings, string>(ResolverValidSettings.DependencyDefinitionFileNameList, "component.targets"));

            IDependencyResolver rfs = new ResolverFileShare(sett);
            var xdoc = rfs.LoadComponentTarget(new ComponentName(compName), new ComponentVersion(compVersion));

            Assert.IsNull(xdoc);
        }

        /// <summary>
        ///Tests the LoadComponentTarget method with a component with non existing version directory.
        /// </summary>
        [TestMethod()]
        public void LoadComponentTarget_NonExistingVersion_Test()
        {
            // Prepare environment
            string tempPath = Path.GetTempPath();
            string guid = System.Guid.NewGuid().ToString();
            string repositoryRootDir = tempPath + Path.DirectorySeparatorChar + guid;
            Directory.CreateDirectory(repositoryRootDir);
            string compName = "existComp";
            string compDir = repositoryRootDir + Path.DirectorySeparatorChar + compName;
            Directory.CreateDirectory(compDir);
            string compVersion = "1.0.0.35";

            ISettings<ResolverValidSettings> sett = new Settings<ResolverValidSettings>();
            sett.AddSetting(new KeyValuePair<ResolverValidSettings, string>(ResolverValidSettings.FileShareUrl, repositoryRootDir));
            sett.AddSetting(new KeyValuePair<ResolverValidSettings, string>(ResolverValidSettings.DependencyDefinitionFileNameList, "component.targets"));

            IDependencyResolver rfs = new ResolverFileShare(sett);
            var xdoc = rfs.LoadComponentTarget(new ComponentName(compName), new ComponentVersion(compVersion));

            Assert.IsNull(xdoc);
        }

        /// <summary>
        ///Tests the LoadComponentTarget method with a non existing component.
        /// </summary>
        [TestMethod()]
        public void LoadComponentTarget_NonExistingComponentName_Test()
        {
            // Prepare environment
            string tempPath = Path.GetTempPath();
            string guid = System.Guid.NewGuid().ToString();
            string repositoryRootDir = tempPath + Path.DirectorySeparatorChar + guid;
            Directory.CreateDirectory(repositoryRootDir);
            string compName = "nonExistingComp";
            string compVersion = "1.0.0.35";

            ISettings<ResolverValidSettings> sett = new Settings<ResolverValidSettings>();
            sett.AddSetting(new KeyValuePair<ResolverValidSettings, string>(ResolverValidSettings.FileShareUrl, repositoryRootDir));
            sett.AddSetting(new KeyValuePair<ResolverValidSettings, string>(ResolverValidSettings.DependencyDefinitionFileNameList, "component.targets"));

            IDependencyResolver rfs = new ResolverFileShare(sett);
            var xdoc = rfs.LoadComponentTarget(new ComponentName(compName), new ComponentVersion(compVersion));

            Assert.IsNull(xdoc);
        }

        /// <summary>
        ///Tests the ComponentExists method with a name of one existing and one non existing component.
        ///</summary>
        [TestMethod()]
        public void ComponentExists_CompName_Test()
        {
            // Prepare environment
            string tempPath = Path.GetTempPath();
            string guid = System.Guid.NewGuid().ToString();
            string repositoryRootDir = tempPath + Path.DirectorySeparatorChar + guid;
            Directory.CreateDirectory(repositoryRootDir);
            string compName = "comp";
            string nonExistingCompName = "comp2";
            string compDir = repositoryRootDir + Path.DirectorySeparatorChar + compName;
            Directory.CreateDirectory(compDir);

            ISettings<ResolverValidSettings> sett = new Settings<ResolverValidSettings>();
            sett.AddSetting(new KeyValuePair<ResolverValidSettings, string>(ResolverValidSettings.FileShareUrl, repositoryRootDir));
            sett.AddSetting(new KeyValuePair<ResolverValidSettings, string>(ResolverValidSettings.DependencyDefinitionFileNameList, "component.targets"));
            IDependencyResolver rfs = new ResolverFileShare(sett);
            Assert.IsTrue(rfs.ComponentExists(new ComponentName(compName)));
            Assert.IsFalse(rfs.ComponentExists(new ComponentName(nonExistingCompName)));
        }

        /// <summary>
        ///Tests the ComponentExists method with one existing and one non existing version.
        ///</summary>
        [TestMethod()]
        public void ComponentExists_CompVersion_Test()
        {
            // Prepare environment
            string tempPath = Path.GetTempPath();
            string guid = System.Guid.NewGuid().ToString();
            string repositoryRootDir = tempPath + Path.DirectorySeparatorChar + guid;
            Directory.CreateDirectory(repositoryRootDir);
            string compName = "comp";
            string compDir = repositoryRootDir + Path.DirectorySeparatorChar + compName;
            Directory.CreateDirectory(compDir);
            string existingVersion = "1.0";
            string nonexistingVersion = "1.2";
            Directory.CreateDirectory(compDir+Path.DirectorySeparatorChar+existingVersion);
            File.Create(Path.Combine(compDir, existingVersion, "component.targets"));

            ISettings<ResolverValidSettings> sett = new Settings<ResolverValidSettings>();
            sett.AddSetting(new KeyValuePair<ResolverValidSettings, string>(ResolverValidSettings.FileShareUrl, repositoryRootDir));
            sett.AddSetting(new KeyValuePair<ResolverValidSettings, string>(ResolverValidSettings.DependencyDefinitionFileNameList, "component.targets"));
            IDependencyResolver rfs = new ResolverFileShare(sett);
            Assert.IsTrue(rfs.ComponentExists(new ComponentName(compName), new ComponentVersion(existingVersion)));
            Assert.IsFalse(rfs.ComponentExists(new ComponentName(compName), new ComponentVersion(nonexistingVersion)));
        }
    }
}
