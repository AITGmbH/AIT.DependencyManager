using System;
using System.Collections.Generic;
using System.IO;
using AIT.DMF.Contracts.Gui;
using AIT.DMF.Contracts.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AIT.DMF.DependencyManager.Controls;
using AIT.DMF.DependencyService;

namespace AIT.DMF.DependencyManager.Controls.Test
{
    
    /// <summary>
    ///This is a test class for BootstrapperTest and is intended
    ///to contain all BootstrapperTest Unit Tests
    ///</summary>
    [TestClass()]
    public class BootstrapperTest
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
        ///A test for Initialize with valid Initialization
        ///</summary>
        [TestMethod()]
        public void Bootstrapper_CompleteInitialization()
        {
            var depSettings = new Settings<ServiceValidSettings>();
            depSettings.AddSetting(new KeyValuePair<ServiceValidSettings, string>(ServiceValidSettings.DefaultDependencyDefinitionFilename, "component.targets"));
            depSettings.AddSetting(new KeyValuePair<ServiceValidSettings, string>(ServiceValidSettings.DefaultTeamProjectCollection, "http://localhost:8080/DefaultCollection"));
            depSettings.AddSetting(new KeyValuePair<ServiceValidSettings, string>(ServiceValidSettings.DefaultWorkspaceName, "dummyWorkspaceName"));
            depSettings.AddSetting(new KeyValuePair<ServiceValidSettings, string>(ServiceValidSettings.DefaultWorkspaceOwner, "dummyWorkspaceOwner"));
            depSettings.AddSetting(new KeyValuePair<ServiceValidSettings, string>(ServiceValidSettings.DefaultOutputBaseFolder, @"C:\Temp"));
            depSettings.AddSetting(new KeyValuePair<ServiceValidSettings, string>(ServiceValidSettings.DefaultRelativeOutputPath, ""));
            depSettings.AddSetting(new KeyValuePair<ServiceValidSettings,string>(ServiceValidSettings.CreateDirectoriesForComponentAndVersion, false.ToString()));
            depSettings.AddSetting(new KeyValuePair<ServiceValidSettings, string>(ServiceValidSettings.BinaryTeamProjectCollectionUrl, "http://localhost:8080/DefaultCollection"));
            depSettings.AddSetting(new KeyValuePair<ServiceValidSettings,string>(ServiceValidSettings.BinaryRepositoryTeamProject, "BinaryRepository"));
            
            var expectedDepDefPath = @"C:\Temp\component.targets";
            var expectedTeamProjectCollectionUrl = "http://localhost:8080/DefaultCollection";

            FileInfo fi = new FileInfo(expectedDepDefPath);
            StreamWriter sw = fi.CreateText();
            sw.Write("<?xml version=\"1.0\"?>\n<Component xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns=\"http://schemas.aitgmbh.de/DependencyManager/2011/11\">\n\t<Dependencies />\n</Component>");
            sw.Close();

            var expectedDependencyService = new AIT.DMF.DependencyService.DependencyService(depSettings);
            var expectedLogger = new DebugLogger();

            var bootstrapper = new Bootstrapper();
            bootstrapper.DependencyService = expectedDependencyService;
            bootstrapper.Logger = expectedLogger;
            bootstrapper.LocalPath = expectedDepDefPath;
            bootstrapper.TeamProjectCollectionUrl = expectedTeamProjectCollectionUrl;
            bootstrapper.Initialize();

            Assert.AreEqual(expectedDependencyService, bootstrapper.DependencyService);
            Assert.AreEqual(expectedLogger, bootstrapper.Logger);
            Assert.AreEqual(expectedDepDefPath, bootstrapper.LocalPath);
            Assert.AreEqual(expectedTeamProjectCollectionUrl, bootstrapper.TeamProjectCollectionUrl);
            Assert.IsNotNull(bootstrapper.MainWindow);
        }


        /// <summary>
        ///A test for Initialize with no dependency service initialized
        ///</summary>
        [TestMethod()]
        [ExpectedException(typeof(InvalidOperationException), "No dependency service initialized.")]
        public void Bootstrapper_InitializeWithMissingDependencyService()
        {
            var bootstrapper = new Bootstrapper();
            bootstrapper.DependencyService = null;
            bootstrapper.Logger = null;
            bootstrapper.LocalPath = null;
            bootstrapper.TeamProjectCollectionUrl = null;
            bootstrapper.Initialize();
        }

        /// <summary>
        ///A test for Initialize with no logger attached
        ///</summary>
        [TestMethod()]
        [ExpectedException(typeof(InvalidOperationException), "No logger initialized.")]
        public void Bootstrapper_InitializeWithMissingLogger()
        {
            var depSettings = new Settings<ServiceValidSettings>();
            depSettings.AddSetting(new KeyValuePair<ServiceValidSettings, string>(ServiceValidSettings.DefaultDependencyDefinitionFilename, "component.targets"));
            depSettings.AddSetting(new KeyValuePair<ServiceValidSettings, string>(ServiceValidSettings.DefaultTeamProjectCollection, "http://localhost:8080/DefaultCollection"));
            depSettings.AddSetting(new KeyValuePair<ServiceValidSettings, string>(ServiceValidSettings.DefaultWorkspaceName, "dummyWorkspaceName"));
            depSettings.AddSetting(new KeyValuePair<ServiceValidSettings, string>(ServiceValidSettings.DefaultWorkspaceOwner, "dummyWorkspaceOwner"));
            depSettings.AddSetting(new KeyValuePair<ServiceValidSettings, string>(ServiceValidSettings.DefaultOutputBaseFolder, @"C:\Temp"));
            depSettings.AddSetting(new KeyValuePair<ServiceValidSettings, string>(ServiceValidSettings.DefaultRelativeOutputPath, ""));
            depSettings.AddSetting(new KeyValuePair<ServiceValidSettings, string>(ServiceValidSettings.CreateDirectoriesForComponentAndVersion, false.ToString()));
            depSettings.AddSetting(new KeyValuePair<ServiceValidSettings, string>(ServiceValidSettings.BinaryTeamProjectCollectionUrl, "http://localhost:8080/DefaultCollection"));
            depSettings.AddSetting(new KeyValuePair<ServiceValidSettings, string>(ServiceValidSettings.BinaryRepositoryTeamProject, "BinaryRepository"));

            var bootstrapper = new Bootstrapper();
            bootstrapper.DependencyService = new AIT.DMF.DependencyService.DependencyService(depSettings);
            bootstrapper.Logger = null;
            bootstrapper.LocalPath = null;
            bootstrapper.TeamProjectCollectionUrl = null;
            bootstrapper.Initialize();
        }

        /// <summary>
        ///A test for Initialize with no dependency definition file path.
        ///</summary>
        [TestMethod()]
        [ExpectedException(typeof(InvalidOperationException), "No server item initialized.")]
        public void Bootstrapper_InitializeWithMissingDependencyDefinitionPath()
        {
            var depSettings = new Settings<ServiceValidSettings>();
            depSettings.AddSetting(new KeyValuePair<ServiceValidSettings, string>(ServiceValidSettings.DefaultDependencyDefinitionFilename, "component.targets"));
            depSettings.AddSetting(new KeyValuePair<ServiceValidSettings, string>(ServiceValidSettings.DefaultTeamProjectCollection, "http://localhost:8080/DefaultCollection"));
            depSettings.AddSetting(new KeyValuePair<ServiceValidSettings, string>(ServiceValidSettings.DefaultWorkspaceName, "dummyWorkspaceName"));
            depSettings.AddSetting(new KeyValuePair<ServiceValidSettings, string>(ServiceValidSettings.DefaultWorkspaceOwner, "dummyWorkspaceOwner"));
            depSettings.AddSetting(new KeyValuePair<ServiceValidSettings, string>(ServiceValidSettings.DefaultOutputBaseFolder, @"C:\Temp"));
            depSettings.AddSetting(new KeyValuePair<ServiceValidSettings, string>(ServiceValidSettings.DefaultRelativeOutputPath, ""));
            depSettings.AddSetting(new KeyValuePair<ServiceValidSettings, string>(ServiceValidSettings.CreateDirectoriesForComponentAndVersion, false.ToString()));
            depSettings.AddSetting(new KeyValuePair<ServiceValidSettings, string>(ServiceValidSettings.BinaryTeamProjectCollectionUrl, "http://localhost:8080/DefaultCollection"));
            depSettings.AddSetting(new KeyValuePair<ServiceValidSettings, string>(ServiceValidSettings.BinaryRepositoryTeamProject, "BinaryRepository"));

            var bootstrapper = new Bootstrapper();
            bootstrapper.DependencyService = new AIT.DMF.DependencyService.DependencyService(depSettings);
            bootstrapper.Logger = new DebugLogger();
            bootstrapper.LocalPath = null;
            bootstrapper.TeamProjectCollectionUrl = null;
            bootstrapper.Initialize();
        }


        /// <summary>
        ///A test for Initialize with an empty dependency definition file path.
        ///</summary>
        [TestMethod()]
        [ExpectedException(typeof(InvalidOperationException), "LocalPath")]
        public void Bootstrapper_InitializeWithEmptyDependencyDefinitionPath()
        {
            var depSettings = new Settings<ServiceValidSettings>();
            depSettings.AddSetting(new KeyValuePair<ServiceValidSettings, string>(ServiceValidSettings.DefaultDependencyDefinitionFilename, "component.targets"));
            depSettings.AddSetting(new KeyValuePair<ServiceValidSettings, string>(ServiceValidSettings.DefaultTeamProjectCollection, "http://localhost:8080/DefaultCollection"));
            depSettings.AddSetting(new KeyValuePair<ServiceValidSettings, string>(ServiceValidSettings.DefaultWorkspaceName, "dummyWorkspaceName"));
            depSettings.AddSetting(new KeyValuePair<ServiceValidSettings, string>(ServiceValidSettings.DefaultWorkspaceOwner, "dummyWorkspaceOwner"));
            depSettings.AddSetting(new KeyValuePair<ServiceValidSettings, string>(ServiceValidSettings.DefaultOutputBaseFolder, @"C:\Temp"));
            depSettings.AddSetting(new KeyValuePair<ServiceValidSettings, string>(ServiceValidSettings.DefaultRelativeOutputPath, ""));
            depSettings.AddSetting(new KeyValuePair<ServiceValidSettings, string>(ServiceValidSettings.CreateDirectoriesForComponentAndVersion, false.ToString()));
            depSettings.AddSetting(new KeyValuePair<ServiceValidSettings, string>(ServiceValidSettings.BinaryTeamProjectCollectionUrl, "http://localhost:8080/DefaultCollection"));
            depSettings.AddSetting(new KeyValuePair<ServiceValidSettings, string>(ServiceValidSettings.BinaryRepositoryTeamProject, "BinaryRepository"));

            var bootstrapper = new Bootstrapper();
            bootstrapper.DependencyService = new AIT.DMF.DependencyService.DependencyService(depSettings);
            bootstrapper.Logger = new DebugLogger();
            bootstrapper.LocalPath = String.Empty;
            bootstrapper.TeamProjectCollectionUrl = null;
            bootstrapper.Initialize();
        }

        /// <summary>
        ///A test for Initialize with an empty team project collection url.
        ///</summary>
        [TestMethod()]
        [ExpectedException(typeof(InvalidOperationException), "TeamProjectCollectionUrl")]
        public void Bootstrapper_InitializeWithEmptyTeamProjectCollectionUrl()
        {
            var depSettings = new Settings<ServiceValidSettings>();
            depSettings.AddSetting(new KeyValuePair<ServiceValidSettings, string>(ServiceValidSettings.DefaultDependencyDefinitionFilename, "component.targets"));
            depSettings.AddSetting(new KeyValuePair<ServiceValidSettings, string>(ServiceValidSettings.DefaultTeamProjectCollection, "http://localhost:8080/DefaultCollection"));
            depSettings.AddSetting(new KeyValuePair<ServiceValidSettings, string>(ServiceValidSettings.DefaultWorkspaceName, "dummyWorkspaceName"));
            depSettings.AddSetting(new KeyValuePair<ServiceValidSettings, string>(ServiceValidSettings.DefaultWorkspaceOwner, "dummyWorkspaceOwner"));
            depSettings.AddSetting(new KeyValuePair<ServiceValidSettings, string>(ServiceValidSettings.DefaultOutputBaseFolder, @"C:\Temp"));
            depSettings.AddSetting(new KeyValuePair<ServiceValidSettings, string>(ServiceValidSettings.DefaultRelativeOutputPath, ""));
            depSettings.AddSetting(new KeyValuePair<ServiceValidSettings, string>(ServiceValidSettings.CreateDirectoriesForComponentAndVersion, false.ToString()));
            depSettings.AddSetting(new KeyValuePair<ServiceValidSettings, string>(ServiceValidSettings.BinaryTeamProjectCollectionUrl, "http://localhost:8080/DefaultCollection"));
            depSettings.AddSetting(new KeyValuePair<ServiceValidSettings, string>(ServiceValidSettings.BinaryRepositoryTeamProject, "BinaryRepository"));

            var bootstrapper = new Bootstrapper();
            bootstrapper.DependencyService = new AIT.DMF.DependencyService.DependencyService(depSettings);
            bootstrapper.Logger = new DebugLogger();
            bootstrapper.LocalPath = @"C:\Temp\component.targets";
            bootstrapper.TeamProjectCollectionUrl = String.Empty;
            bootstrapper.Initialize();
        }
    }
}
