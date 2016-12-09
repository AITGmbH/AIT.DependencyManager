using System.Collections.Generic;
using AIT.DMF.Contracts.GUI;
using AIT.DMF.Contracts.Parser;
using AIT.DMF.DependencyService;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace AIT.DMF.Plugins.Resolver.BinaryRepository.Test
{


    /// <summary>
    ///This is a test class for BinaryRepositoryDefinitionEditorModelTest and is intended
    ///to contain all BinaryRepositoryDefinitionEditorModelTest Unit Tests
    ///</summary>
    [TestClass()]
    public class BinaryRepositoryDefinitionEditorModelTest
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
        /// Create View Model with new XMLDependency.
        /// This test will be ignored, because an test system is missing. This test must be redesigned
        ///</summary>
        [TestMethod()]
        [Ignore]
        public void BinaryRepositoryDefinitionEditorModel_CreateNewXMLDependency_Test()
        {
            var xmlDependency = new XmlDependency();
            xmlDependency.ProviderConfiguration = new DependencyProviderConfig();
            xmlDependency.ProviderConfiguration.Settings = new DependencyProviderSettings();
            xmlDependency.ProviderConfiguration.Settings.SettingsList = new List<IDependencyProviderSetting>();
            var xmlDependencyViewModelStub = new Mock<IXmlDependencyViewModel>();
            xmlDependencyViewModelStub.Setup(o => o.XmlDependency)
                .Returns(xmlDependency);

            var model = new BinaryRepositoryDefinitionEditorViewModel(new TfsAccessService(), new BinaryRepositoryResolverType(), xmlDependencyViewModelStub.Object, "component.targets", null, "http://localhost:8080/tfs/DefaultCollection");

            const string expectedTPC = "http://localhost:8080/tfs/DefaultCollection";
            model.SelectedBinaryTeamProjectCollection = expectedTPC;
            Assert.AreEqual(xmlDependency.ProviderConfiguration.Settings.GetSettingValue(DependencyProviderValidSettingName.BinaryTeamProjectCollectionUrl), expectedTPC);
            const string expectedTeamProject = "000_Test_Repository";
            model.SelectedBinaryRepositoryTeamProject = expectedTeamProject;
            Assert.AreEqual(xmlDependency.ProviderConfiguration.Settings.GetSettingValue(DependencyProviderValidSettingName.BinaryRepositoryTeamProject), expectedTeamProject);
            const string expectedComponentName = "PaketAB";
            model.SelectedComponent = expectedComponentName;
            Assert.AreEqual(xmlDependency.ProviderConfiguration.Settings.GetSettingValue(DependencyProviderValidSettingName.ComponentName), expectedComponentName);
            const string expectedVersion = "1.*";
            model.SelectedVersion = expectedVersion;
            Assert.AreEqual(xmlDependency.ProviderConfiguration.Settings.GetSettingValue(DependencyProviderValidSettingName.VersionNumber), expectedVersion);
            const string expectedOutputPath = @"Bin\Package\TestPaket";
            model.SelectedOutputPath = expectedOutputPath;
            Assert.AreEqual(xmlDependency.ProviderConfiguration.Settings.GetSettingValue(DependencyProviderValidSettingName.RelativeOutputPath), expectedOutputPath);
        }

        /// <summary>
        /// Create View Model with new XMLDependency.
        /// This test will be ignored, because an test system is missing. This test must be redesigned
        ///</summary>
        [TestMethod()]
        [Ignore]
        public void BinaryRepositoryDefinitionEditorModel_ChangeTPC_Test()
        {
            var xmlDependency = new XmlDependency();
            xmlDependency.ProviderConfiguration = new DependencyProviderConfig();
            xmlDependency.ProviderConfiguration.Settings = new DependencyProviderSettings();
            xmlDependency.ProviderConfiguration.Settings.SettingsList = new List<IDependencyProviderSetting>();
            var xmlDependencyViewModelStub = new Mock<IXmlDependencyViewModel>();
            xmlDependencyViewModelStub.Setup(o => o.XmlDependency)
                .Returns(xmlDependency);

            var model = new BinaryRepositoryDefinitionEditorViewModel(new TfsAccessService(), new BinaryRepositoryResolverType(), xmlDependencyViewModelStub.Object, "component.targets", null, "http://localhost:8080/tfs/DefaultCollection");

            model.SelectedBinaryTeamProjectCollection = "http://localhost:8080/tfs/DefaultCollection";
            model.SelectedBinaryRepositoryTeamProject = "000_Test_Repository";
            model.SelectedComponent = "PaketAB";
            model.SelectedVersion = "1.*";
            const string expectedOutputPath = @"Bin\Package\TestPaket";
            model.SelectedOutputPath = expectedOutputPath;

            const string newTPC = "http://localhost:8080/tfs/TestingCollection";
            model.SelectedBinaryTeamProjectCollection = newTPC;

            Assert.AreEqual(null, model.SelectedBinaryRepositoryTeamProject);
            Assert.AreEqual(null, model.SelectedComponent);
            Assert.AreEqual(null, model.SelectedVersion);
            Assert.AreEqual(expectedOutputPath, model.SelectedOutputPath);
        }

        public void BinaryRepositoryDefinitionEditorModel_LoadXMLDependency_Test()
        {

        }
    }
}
