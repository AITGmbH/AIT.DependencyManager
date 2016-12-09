using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AIT.DMF.Contracts.Services;
using AIT.DMF.DependencyManager.Controls.Messaging;
using AIT.DMF.DependencyManager.Controls.Messaging.Events;
using AIT.DMF.DependencyManager.Controls.ViewModels;
using AIT.DMF.DependencyService;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AIT.DMF.DependencyManager.Controls.Test.ViewModels
{


    /// <summary>
    ///This is a test class for DependencyDetailsEditorViewModelTest and is intended
    ///to contain all DependencyDetailsEditorViewModelTest Unit Tests
    ///</summary>
    [TestClass()]
    public class DependencyDetailsEditorViewModelTest
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
        /// Initializes the test environment (Initialize plattform, bootstrapper and creates a minimal dependency definition file which references binary repository component).
        /// </summary>
        private Bootstrapper InitializeEnvironmentForTests(string componentName, string componentVersion, string providerType, string depDefPath)
        {
            var depSettings = new Settings<ServiceValidSettings>();
            depSettings.AddSetting(new KeyValuePair<ServiceValidSettings, string>(ServiceValidSettings.DefaultDependencyDefinitionFilename, "component.targets"));
            depSettings.AddSetting(new KeyValuePair<ServiceValidSettings, string>(ServiceValidSettings.DefaultTeamProjectCollection, "http://localhost:8080/tfs/TestingCollection"));
            depSettings.AddSetting(new KeyValuePair<ServiceValidSettings, string>(ServiceValidSettings.DefaultWorkspaceName, "dummyWorkspaceName"));
            depSettings.AddSetting(new KeyValuePair<ServiceValidSettings, string>(ServiceValidSettings.DefaultWorkspaceOwner, "dummyWorkspaceOwner"));
            depSettings.AddSetting(new KeyValuePair<ServiceValidSettings, string>(ServiceValidSettings.DefaultOutputBaseFolder, @"C:\Temp"));
            depSettings.AddSetting(new KeyValuePair<ServiceValidSettings, string>(ServiceValidSettings.DefaultRelativeOutputPath, ""));
            depSettings.AddSetting(new KeyValuePair<ServiceValidSettings, string>(ServiceValidSettings.CreateDirectoriesForComponentAndVersion, false.ToString()));
            depSettings.AddSetting(new KeyValuePair<ServiceValidSettings, string>(ServiceValidSettings.BinaryTeamProjectCollectionUrl, "http://localhost:8080/tfs/TestingCollection"));
            depSettings.AddSetting(new KeyValuePair<ServiceValidSettings, string>(ServiceValidSettings.BinaryRepositoryTeamProject, "BinaryRepository"));

            FileInfo fi = new FileInfo(depDefPath);
            StreamWriter sw = fi.CreateText();
            sw.Write("<?xml version=\"1.0\"?>\n<Component xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns=\"http://schemas.aitgmbh.de/DependencyManager/2011/11\">\n\t<Dependencies><Dependency Type=\"BinaryDependency\"><Provider Type = \"" + providerType + "\"><Settings Type=\"" + providerType + "Settings\"><Setting Name=\"ComponentName\" Value=\"" + componentName + "\" /><Setting Name=\"VersionNumber\" Value=\"" + componentVersion + "\" /></Settings></Provider></Dependency></Dependencies>\n</Component>");
            sw.Close();

            Platform.Initialize();
            var bootstrapper = new Bootstrapper();
            bootstrapper.DependencyService = new AIT.DMF.DependencyService.DependencyService(depSettings);
            bootstrapper.Logger = new DebugLogger();
            bootstrapper.LocalPath = depDefPath;
            bootstrapper.Initialize();

            return bootstrapper;
        }

        /// <summary>
        /// Tests if Editor Framework element is correctly fetched via factory and initialized based on the dependency definition file.
        ///</summary>
        [TestMethod()]
        public void DependencyDetailsEditorViewModel_XmlDependencyViewModelAndTypeAreSet()
        {
            var componentName = "PackageA";
            var componentVersion = "1.0";
            var providerType = "BinaryRepository";
            string dummyDepDefFile = "C:\\Temp\\component.targets";
            var bootstrapper = InitializeEnvironmentForTests(componentName, componentVersion, providerType, dummyDepDefFile);
            var iXmlDef = bootstrapper.DependencyService.LoadXmlComponent(dummyDepDefFile, bootstrapper.Logger).Dependencies.First();

            XmlDependencyViewModel expected = new XmlDependencyViewModel(iXmlDef, true);
            XmlDependencyViewModel actual;
            String actualType;

            // Get the DependencyDetailsEditorViewModel
            DependencyDetailsEditorViewModel ddEditor = new DependencyDetailsEditorViewModel();
            Assert.IsNotNull(ddEditor);
            Assert.IsNull(ddEditor.XmlDependency);

            // Assert they are equal
            EventPublisher publ = new EventPublisher();
            ddEditor.EventPublisher = publ;
            publ.Publish(new SelectedXmlDependencyChangedEvent(null, expected));

            // FIXME: This fails because the event is not published into the DependencyDetailsEditorViewModel
            actual = ddEditor.XmlDependency;
            Assert.AreEqual(expected, actual);
        }
    }
}
