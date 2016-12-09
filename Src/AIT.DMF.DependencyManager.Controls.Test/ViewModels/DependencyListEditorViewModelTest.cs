using System.Collections.Generic;
using System.IO;
using System.Linq;
using AIT.DMF.Contracts.Services;
using AIT.DMF.DependencyManager.Controls.Messaging;
using AIT.DMF.DependencyManager.Controls.ViewModels;
using AIT.DMF.DependencyService;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AIT.DMF.DependencyManager.Controls.Test.ViewModels
{


    /// <summary>
    ///This is a test class for DependencyListEditorViewModelTest and is intended
    ///to contain all DependencyListEditorViewModelTest Unit Tests
    ///</summary>
    [TestClass()]
    public class DependencyListEditorViewModelTest
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
        private Bootstrapper InitializeEnvironmentForTests(string componentName, string componentVersion, string providerType, string fileContext, string depDefPath)
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

            var fi = new FileInfo(depDefPath);
            var sw = fi.CreateText();
            sw.Write(fileContext);
            sw.Close();

            Platform.Initialize();
            var bootstrapper = new Bootstrapper
                                   {
                                       DependencyService = new AIT.DMF.DependencyService.DependencyService(depSettings),
                                       Logger = new DebugLogger(),
                                       LocalPath = depDefPath
                                   };
            bootstrapper.Initialize();

            return bootstrapper;
        }

        /// <summary>
        ///A test for OnImportsSatisfied
        ///</summary>
        [TestMethod()]
        public void DependencyListEditorViewModel_LoadComponent()
        {
            const string expectedComponentName = "PackageA";
            const string expectedComponentVersion = "1.0";
            const string expectedProviderType = "BinaryRepository";

            const string dummyDepDefFile = "C:\\Temp\\component.targets";
            const string dummyContentFromFile = "<?xml version=\"1.0\"?><Component xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns=\"http://schemas.aitgmbh.de/DependencyManager/2011/11\">  <Dependencies>    <Dependency Type=\"BinaryDependency\">      <Provider Type=\"" + expectedProviderType + "\">        <Settings Type=\"" + expectedProviderType + "Settings\">          <Setting Name=\"ComponentName\" Value=\"" + expectedComponentName + "\" />          <Setting Name=\"VersionNumber\" Value=\"" + expectedComponentVersion + "\" />        </Settings>      </Provider>    </Dependency>  </Dependencies></Component>";
            InitializeEnvironmentForTests(expectedComponentName, expectedComponentVersion, expectedProviderType, dummyContentFromFile, dummyDepDefFile);
            var viewModel = new DependencyListEditorViewModel
            {
                EventPublisher = new EventPublisher()
            };

            // Check generated view model
            Assert.IsNotNull(viewModel);
            Assert.AreEqual(1, viewModel.XmlDependencies.Count());
            var firstXmlDependency = viewModel.XmlDependencies.First() as XmlDependencyViewModel;
            Assert.AreEqual(expectedComponentName, firstXmlDependency.ReferencedComponentName);
            Assert.AreEqual(expectedComponentVersion, firstXmlDependency.ReferencedComponentVersion);
            Assert.AreEqual(expectedProviderType, firstXmlDependency.Type);
            Assert.AreEqual(2, firstXmlDependency.Settings.SettingsList.Count);
        }

        /// <summary>
        ///A test for OnImportsSatisfied
        ///</summary>
        [TestMethod()]
        public void DependencyListEditorViewModel_SaveComponent()
        {
            const string expectedComponentName = "PackageA";
            const string expectedComponentVersion = "1.0";
            const string expectedProviderType = "BinaryRepository";
            const string expectedContentFromFile = "<?xml version=\"1.0\"?><Component xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns=\"http://schemas.aitgmbh.de/DependencyManager/2011/11\">  <Dependencies>    <Dependency Type=\"BinaryDependency\">      <Provider Type=\"" + expectedProviderType + "\">        <Settings Type=\"" + expectedProviderType + "Settings\">          <Setting Name=\"ComponentName\" Value=\"" + expectedComponentName + "\" />          <Setting Name=\"VersionNumber\" Value=\"" + expectedComponentVersion + "\" />        </Settings>      </Provider>    </Dependency>  </Dependencies></Component>";
            string actualContentFromSave;
            string actualContentFromSavedAs;

            const string dummyDepDefFile = "C:\\Temp\\component.targets";
            const string generatedDepDefFile = "C:\\Temp\\component_saved.targets";
            InitializeEnvironmentForTests(expectedComponentName, expectedComponentVersion, expectedProviderType, expectedContentFromFile, dummyDepDefFile);
            var viewModel = new DependencyListEditorViewModel
            {
                EventPublisher = new EventPublisher()
            };

            // Save view model into original and other file
            viewModel.SaveCommand.Execute();
            using (var f = File.OpenText(dummyDepDefFile))
            {
                actualContentFromSave = f.ReadToEnd().Replace("\r", "").Replace("\n", "").Replace("\t", "");
            }
            viewModel.TargetsFileData.LocalPath = generatedDepDefFile;
            viewModel.SaveCommand.Execute();
            using (var f2 = File.OpenText(generatedDepDefFile))
            {
                actualContentFromSavedAs = f2.ReadToEnd().Replace("\r", "").Replace("\n", "").Replace("\t", "");
            }

            // Check generated dependency definition file
            Assert.AreEqual(expectedContentFromFile, actualContentFromSave);
            Assert.AreEqual(expectedContentFromFile, actualContentFromSavedAs);
        }
    }
}
