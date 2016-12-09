using AIT.DMF.Common;
using AIT.DMF.Contracts.Common;
using AIT.DMF.Contracts.Exceptions;
using AIT.DMF.Contracts.Provider;
using AIT.DMF.Contracts.Services;
using AIT.DMF.DependencyService;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Xml;
using System.Xml.Linq;

namespace AIT.DMF.Plugins.Resolver.Subversion.Test
{
    /// <summary>
    /// This class test the Subversion Resolver against a file bases repository. When using SharpSVN it is irrelevant, if a file or server
    /// repository is used.
    /// </summary>
    [TestClass]
    [DeploymentItem("Resources", "Resources")]
    public class ResolverSubversionTest
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
        private static string _componentA = "ComponentA";
        private static string _componentEmpty = "ComponentEmpty";
        private static string _componentNoTargetsFile = "ComponentNoTargetsFile";

        [ClassInitialize()]
        public static void ClassInit(TestContext context)
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
        /// Tests resolver with invalid File Url. This should generate a InvalidComponentException exception.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(InvalidProviderConfigurationException))]
        public void Resolver_Subversion_ValidateSubversionUrl_UrlIsInvalid()
        {
            var subversionUrl = @"htp/scm.mycompany.de/svn/Dev/trunk";

            ISettings<ResolverValidSettings> settings = new Settings<ResolverValidSettings>();
            settings.AddSetting(new KeyValuePair<ResolverValidSettings, string>(ResolverValidSettings.SubversionUrl, subversionUrl));
            settings.AddSetting(new KeyValuePair<ResolverValidSettings, string>(ResolverValidSettings.DependencyDefinitionFileNameList, "component.targets"));

            IDependencyResolver rs = new ResolverSubversion(settings);
        }

        /// <summary>
        /// Tests resolver with correct File Url.
        /// </summary>
        [TestMethod]
        public void Resolver_Subversion_ValidateSubversionUrl_UrlIsValid()
        {
            ISettings<ResolverValidSettings> settings = new Settings<ResolverValidSettings>();
            settings.AddSetting(new KeyValuePair<ResolverValidSettings, string>(ResolverValidSettings.SubversionUrl, _svnSrcPath));
            settings.AddSetting(new KeyValuePair<ResolverValidSettings, string>(ResolverValidSettings.DependencyDefinitionFileNameList, "component.targets"));

            IDependencyResolver rs = new ResolverSubversion(settings);

            Assert.IsNotNull(rs);
        }

        /// <summary>
        /// A test for LoadComponentTarget method via loading an existing component.targets file.
        ///</summary>
        [TestMethod()]
        public void Resolver_Subversion_LoadComponentTarget_WithHeadRevision_Test()
        {
            var svnPath = string.Format("{0}/{1}", _svnSrcPath, _componentA);

            ISettings<ResolverValidSettings> set = new Settings<ResolverValidSettings>();
            set.AddSetting(new KeyValuePair<ResolverValidSettings, string>(ResolverValidSettings.SubversionUrl, svnPath));
            set.AddSetting(new KeyValuePair<ResolverValidSettings, string>(ResolverValidSettings.DependencyDefinitionFileNameList, "component.targets"));

            IDependencyResolver rs = new ResolverSubversion(set);
            XDocument xdoc = rs.LoadComponentTarget(new ComponentName(svnPath), new ComponentVersion("H"));
            
            Assert.IsNotNull(xdoc);
        }

        /// <summary>
        /// A test for LoadComponentTarget method via loading an existing component.targets file.
        ///</summary>
        [TestMethod()]
        public void Resolver_Subversion_LoadComponentTarget_WithSpecificRevision_Test()
        {
            var svnPath = string.Format("{0}/{1}", _svnSrcPath, _componentA);

            ISettings<ResolverValidSettings> set = new Settings<ResolverValidSettings>();
            set.AddSetting(new KeyValuePair<ResolverValidSettings, string>(ResolverValidSettings.SubversionUrl, svnPath));
            set.AddSetting(new KeyValuePair<ResolverValidSettings, string>(ResolverValidSettings.DependencyDefinitionFileNameList, "component.targets"));

            IDependencyResolver rs = new ResolverSubversion(set);
            XDocument xdoc = rs.LoadComponentTarget(new ComponentName(svnPath), new ComponentVersion("R18"));

            Assert.IsNotNull(xdoc);
        }

        /// <summary>
        /// A test for LoadComponentTarget method via loading an existing component.targets file.
        ///</summary>
        [TestMethod()]
        public void Resolver_Subversion_LoadComponentTarget_WithNonExistingRevision_Test()
        {
            var svnPath = string.Format("{0}/{1}", _svnSrcPath, _componentA);

            ISettings<ResolverValidSettings> set = new Settings<ResolverValidSettings>();
            set.AddSetting(new KeyValuePair<ResolverValidSettings, string>(ResolverValidSettings.SubversionUrl, svnPath));
            set.AddSetting(new KeyValuePair<ResolverValidSettings, string>(ResolverValidSettings.DependencyDefinitionFileNameList, "component.targets"));

            IDependencyResolver rs = new ResolverSubversion(set);
            XDocument xdoc = rs.LoadComponentTarget(new ComponentName(svnPath), new ComponentVersion("R12345"));

            Assert.IsNull(xdoc);
        }

        /// <summary>
        /// A test for LoadComponentTarget method via loading an not existing component.targets file.
        /// </summary>
        [TestMethod()]
        [ExpectedException(typeof(InvalidProviderConfigurationException))]
        public void Resolver_Subversion_LoadComponentTarget_CompTargetsFileNotExists_Test()
        {
            var svnPath = string.Format("{0}/{1}", _svnSrcPath, "notExistingPath");

            ISettings<ResolverValidSettings> set = new Settings<ResolverValidSettings>();
            set.AddSetting(new KeyValuePair<ResolverValidSettings, string>(ResolverValidSettings.SubversionUrl, svnPath));
            set.AddSetting(new KeyValuePair<ResolverValidSettings, string>(ResolverValidSettings.DependencyDefinitionFileNameList, "component.targets"));

            IDependencyResolver rs = new ResolverSubversion(set);
            XDocument xdoc = rs.LoadComponentTarget(new ComponentName(svnPath), new ComponentVersion("H"));

            Assert.IsNull(xdoc);
        }

        /// <summary>
        /// A test for LoadComponentTarget method via loading an existing - but invlaid, because it is empty - component.targets file.
        /// </summary>
        [TestMethod()]
        [ExpectedException(typeof(XmlException))]
        public void Resolver_Subversion_LoadComponentTarget_CompTargetsFileIsEmpty_Test()
        {
            var svnPath = string.Format("{0}/{1}", _svnSrcPath, _componentEmpty);

            ISettings<ResolverValidSettings> set = new Settings<ResolverValidSettings>();
            set.AddSetting(new KeyValuePair<ResolverValidSettings, string>(ResolverValidSettings.SubversionUrl, svnPath));
            set.AddSetting(new KeyValuePair<ResolverValidSettings, string>(ResolverValidSettings.DependencyDefinitionFileNameList, "component.targets"));

            IDependencyResolver rs = new ResolverSubversion(set);
            XDocument xdoc = rs.LoadComponentTarget(new ComponentName(svnPath), new ComponentVersion("H"));
        }

        /// <summary>
        /// Tests the LoadComponentTarget method with a component which has no component.targets file.
        /// </summary>
        [TestMethod()]
        public void Resolver_Subversion_LoadComponentTarget_NonExistingCompTargetsFile_Test()
        {
            var svnPath = string.Format("{0}/{1}", _svnSrcPath, _componentNoTargetsFile);

            ISettings<ResolverValidSettings> set = new Settings<ResolverValidSettings>();
            set.AddSetting(new KeyValuePair<ResolverValidSettings, string>(ResolverValidSettings.SubversionUrl, svnPath));
            set.AddSetting(new KeyValuePair<ResolverValidSettings, string>(ResolverValidSettings.DependencyDefinitionFileNameList, "component.targets"));

            IDependencyResolver rfs = new ResolverSubversion(set);
            var xdoc = rfs.LoadComponentTarget(new ComponentName(svnPath), new ComponentVersion("H"));

            Assert.IsNull(xdoc);
        }
    }
}
