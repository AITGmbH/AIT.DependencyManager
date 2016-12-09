using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Xml.Linq;
using System.Linq;
using System.IO;
using Microsoft.TeamFoundation;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Framework.Client;
using Microsoft.TeamFoundation.Framework.Common;
using Microsoft.TeamFoundation.Server;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.TeamFoundation.VersionControl.Common;
using AIT.DMF.Plugins.Resolver.SourceControl;
using AIT.DMF.Contracts.Provider;
using AIT.DMF.Contracts.Exceptions;
using AIT.DMF.Plugins.PluginFactory;
using AIT.DMF.Common;
using AIT.DMF.Contracts.Common;
using AIT.DMF.Contracts.Services;
using System.Collections.Generic;
using AIT.DMF.DependencyService;


namespace AIT.DMF.Plugins.Resolver.SourceControl.Test
{
    
    
    /// <summary>
    ///This is a test class for ResolverSourceControlTest and is intended
    ///to contain all ResolverSourceControlTest Unit Tests
    ///</summary>
    [TestClass()]
    public class ResolverSourceControlTest
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
        ///A test for LoadComponentTarget
        ///</summary>
        [TestMethod()]
        public void LoadComponentTargetTest()
        {
            WorkspaceInfo currentWorkspace;

            // Fetch workspace info for this Workstation
            // See AIT.Dpm.MSBuild.TeamFoundationService
            var allLocalWorkspaceInfo = Workstation.Current.GetAllLocalWorkspaceInfo();
            if (allLocalWorkspaceInfo.Length == 1)
            {
                currentWorkspace = allLocalWorkspaceInfo[0];
            }
            else
            {
                var currentDirectory = Environment.CurrentDirectory;
                if (currentDirectory.IndexOf('~') >= 0)
                {
                    currentDirectory = Path.GetFullPath(currentDirectory);
                }

                var localWorkspaceInfo = Workstation.Current.GetLocalWorkspaceInfo(currentDirectory);
                if (localWorkspaceInfo != null)
                {
                    currentWorkspace = localWorkspaceInfo;
                }
                else
                {
                    var localWorkspaceInfoRecursively = Workstation.Current.GetLocalWorkspaceInfoRecursively(currentDirectory);
                    if (localWorkspaceInfoRecursively.Length != 1)
                    {
                        throw new ApplicationException("Unable to determine workspace!");
                    }
                    currentWorkspace = localWorkspaceInfoRecursively[0];
                }
            }
            // Determine server url for connected tfs server
            var _tpcurl = currentWorkspace.ServerUri;
            if (string.IsNullOrEmpty(_tpcurl.AbsoluteUri))
            {
                throw new InvalidProviderConfigurationException("Could not determine project collection url");
            }

            string projectPath = "$/AIT.DependencyManagement";
            string relativeBranchPath = "Development/DEV-MRI";

            ISettings<ResolverValidSettings> sett = new Settings<ResolverValidSettings>();
            sett.AddSetting(new KeyValuePair<ResolverValidSettings, string>(ResolverValidSettings.TeamProjectCollectionUrl, _tpcurl.AbsoluteUri));

            IDependencyResolver rfs = new ResolverSourceControl(sett , "component.targets");

            XDocument compXdoc = rfs.LoadComponentTarget(new ComponentName(projectPath + VersionControlPath.Separator + relativeBranchPath+VersionControlPath.Separator +"Src"), new ComponentVersion(LatestVersionSpec.Instance));

            Assert.AreEqual(1, compXdoc.Descendants("Component").Count());
            Assert.AreEqual(1, compXdoc.Descendants("Dependencies").Count());
        }
    }
}
