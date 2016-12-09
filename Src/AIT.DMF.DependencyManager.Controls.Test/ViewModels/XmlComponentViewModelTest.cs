using System;
using System.Collections.Generic;
using System.Linq;
using AIT.DMF.Contracts.Parser;
using AIT.DMF.DependencyManager.Controls.Services;
using AIT.DMF.DependencyManager.Controls.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace AIT.DMF.DependencyManager.Controls.Test.ViewModels
{
    /// <summary>
    ///This is a test class for XmlComponentViewModelTest and is intended
    ///to contain all XmlComponentViewModelTest Unit Tests
    ///</summary>
    [TestClass()]
    public class XmlComponentViewModelTest : TestBase
    {
        private const string TestComponentName = "TestComponentName";
        private const string TestComponentVersion = "TestComponentVersion";

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

        [TestMethod]
        [HostType("Moles")]
        public void Instance_IsCreated_ChangeTrackingServiceIsImported()
        {
            var dependencies = _InitializeViewModelDependencies();
            var changeTrackingServiceStub = dependencies.GetEntry<Mock<IChangeTrackingService>>();

            var xmlComponentStub = new Mock<IXmlComponent>();

            var target = new XmlComponentViewModel(xmlComponentStub.Object);

            Assert.AreEqual(changeTrackingServiceStub.Object, target.ChangeTrackingService);
        }

        [TestMethod]
        [HostType("Moles")]
        public void ComponentName_XmlComponentIsSetInConstructor_ComponentNameIsReturned()
        {
            _InitializeViewModelDependencies();

            var xmlComponentStub = new Mock<IXmlComponent>();
            xmlComponentStub.Setup(o => o.Name)
                .Returns(TestComponentName);

            var target = new XmlComponentViewModel(xmlComponentStub.Object);

            Assert.AreEqual(TestComponentName, target.ComponentName);
        }

        [TestMethod]
        [HostType("Moles")]
        public void ComponentVersion_XmlComponentIsSetInConstructor_ComponentVersionIsReturned()
        {
            _InitializeViewModelDependencies();

            var xmlComponentStub = new Mock<IXmlComponent>();
            xmlComponentStub.Setup(o => o.Version)
                .Returns(TestComponentVersion);

            var target = new XmlComponentViewModel(xmlComponentStub.Object);

            Assert.AreEqual(TestComponentVersion, target.ComponentVersion);
        }

        [TestMethod]
        [HostType("Moles")]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Instance_XmlComponentIsNullInConstructor_ExceptionIsThrown()
        {
            _InitializeViewModelDependencies();

            var target = new XmlComponentViewModel(null);
        }

        [TestMethod]
        [HostType("Moles")]
        public void GetDependencies_XmlComponentHasDependency_DependencyViewModelIsReturned()
        {
            _InitializeViewModelDependencies();

            // create the component
            var xmlComponentStub = new Mock<IXmlComponent>();

            // create a dependency
            var xmlDependencyStub = new Mock<IXmlDependency>();
            var xmlDependencyList = new List<IXmlDependency>
                                        {
                                            xmlDependencyStub.Object
                                        };
            xmlComponentStub.Setup(o => o.Dependencies)
                .Returns(xmlDependencyList);

            var target = new XmlComponentViewModel(xmlComponentStub.Object);
            var xmlDependencyViewModels = target.GetDependencies();

            Assert.AreEqual(xmlDependencyViewModels.First().XmlDependency, xmlDependencyStub.Object);
        }

        [TestMethod]
        [HostType("Moles")]
        public void GetDependencies_XmlComponentHasMultipleDependencies_DependencyViewModelsAreCreatedAccordingly()
        {
            _InitializeViewModelDependencies();

            // create the component
            var xmlComponentStub = new Mock<IXmlComponent>();
            xmlComponentStub.Setup(o => o.Name)
                .Returns(TestComponentName);

            // create a dependency
            var xmlDependencyStub = new Mock<IXmlDependency>();
            var xmlDependencyStub2 = new Mock<IXmlDependency>();
            var xmlDependencyList = new List<IXmlDependency>
                                        {
                                            xmlDependencyStub.Object,
                                            xmlDependencyStub2.Object
                                        };
            xmlComponentStub.Setup(o => o.Dependencies)
                .Returns(xmlDependencyList);

            var target = new XmlComponentViewModel(xmlComponentStub.Object);
            var xmlDependencyViewModels = target.GetDependencies();

            Assert.AreEqual(xmlDependencyViewModels.Count(), xmlDependencyList.Count);
        }

        [TestMethod]
        [HostType("Moles")]
        public void IsChanged_XmlComponentWithOutDependenciesIsSet_IsChangedIsFalse()
        {
            _InitializeViewModelDependencies();

            // create the component
            var xmlComponentStub = new Mock<IXmlComponent>();

            var target = new XmlComponentViewModel(xmlComponentStub.Object);

            Assert.IsFalse(target.IsChanged);
        }

        [TestMethod]
        [HostType("Moles")]
        public void IsChanged_XmlComponentWithDependenciesIsSet_IsChangedIsFalse()
        {
            _InitializeViewModelDependencies();

            // create the component
            var xmlComponentStub = new Mock<IXmlComponent>();

            // create a dependency
            var xmlDependencyStub = new Mock<IXmlDependency>();
            var xmlDependencyStub2 = new Mock<IXmlDependency>();
            var xmlDependencyList = new List<IXmlDependency>
                                        {
                                            xmlDependencyStub.Object,
                                            xmlDependencyStub2.Object
                                        };
            xmlComponentStub.Setup(o => o.Dependencies)
                .Returns(xmlDependencyList);

            var target = new XmlComponentViewModel(xmlComponentStub.Object);

            Assert.IsFalse(target.IsChanged);
        }

        [TestMethod]
        [HostType("Moles")]
        public void IsChanged_XmlDependencyIsAdded_IsChangedIsTrue()
        {
            _InitializeViewModelDependencies();

            // create the component
            var xmlComponentStub = new Mock<IXmlComponent>();
            var dependencyList = new List<IXmlDependency>();
            xmlComponentStub.Setup(o => o.Dependencies)
                .Returns(dependencyList);

            // create a dependency
            var xmlDependencyStub = new Mock<IXmlDependency>();
            var xmlDependencyViewModelStub = new Mock<XmlDependencyViewModel>(xmlDependencyStub.Object, true);

            var target = new XmlComponentViewModel(xmlComponentStub.Object);
            target.AddDependency(xmlDependencyViewModelStub.Object);

            Assert.IsTrue(target.IsChanged);
        }

        [TestMethod]
        [HostType("Moles")]
        public void IsChanged_XmlDependencyIsRemoved_IsChangedIsTrue()
        {
            _InitializeViewModelDependencies();

            // create the component
            var xmlComponentStub = new Mock<IXmlComponent>();

            // create a dependency
            var xmlDependencyStub = new Mock<IXmlDependency>();
            var xmlDependencyStub2 = new Mock<IXmlDependency>();
            var xmlDependencyList = new List<IXmlDependency>
                                        {
                                            xmlDependencyStub.Object,
                                            xmlDependencyStub2.Object
                                        };
            xmlComponentStub.Setup(o => o.Dependencies)
                .Returns(xmlDependencyList);

            var target = new XmlComponentViewModel(xmlComponentStub.Object);
            var firstDependency = target.GetDependencies().First();
            target.RemoveDependency(firstDependency);

            Assert.IsTrue(target.IsChanged);
        }

        [TestMethod]
        [HostType("Moles")]
        public void RemoveDependency_XmlDependencyIsRemoved_DependencyIsRemovedFromChangeTrackingService()
        {
            var dependencies = _InitializeViewModelDependencies();
            var changeTrackingServiceMock = dependencies.GetEntry<Mock<IChangeTrackingService>>();

            // create the component
            var xmlComponentStub = new Mock<IXmlComponent>();

            // create a dependency
            var xmlDependencyStub = new Mock<IXmlDependency>();
            var xmlDependencyList = new List<IXmlDependency>
                                        {
                                            xmlDependencyStub.Object
                                        };
            xmlComponentStub.Setup(o => o.Dependencies)
                .Returns(xmlDependencyList);

            // act
            var target = new XmlComponentViewModel(xmlComponentStub.Object);
            var firstDependency = target.GetDependencies().First();
            // make sure the dependency has a change so it is tracked by the change tracking service
            firstDependency.SetChanged();
            // now remove the dependency
            target.RemoveDependency(firstDependency);

            // make sure the dependency was removed
            changeTrackingServiceMock.Verify(o => o.Remove(firstDependency));
        }

        [TestMethod]
        [HostType("Moles")]
        public void AcceptChanges_XmlDependencyHasChanges_DependencyIsRemovedFromChangeTrackingService()
        {
            var dependencies = _InitializeViewModelDependencies();
            var changeTrackingServiceMock = dependencies.GetEntry<Mock<IChangeTrackingService>>();

            // create the component
            var xmlComponentStub = new Mock<IXmlComponent>();

            // create a dependency
            var xmlDependencyStub = new Mock<IXmlDependency>();
            var xmlDependencyList = new List<IXmlDependency>
                                        {
                                            xmlDependencyStub.Object
                                        };
            xmlComponentStub.Setup(o => o.Dependencies)
                .Returns(xmlDependencyList);

            // act
            var target = new XmlComponentViewModel(xmlComponentStub.Object);
            var firstDependency = target.GetDependencies().First();
            // make sure the dependency has a change 
            firstDependency.SetChanged();
            // accept on component
            target.AcceptChanges();

            // make sure the dependency was removed from the tracking service too
            changeTrackingServiceMock.Verify(o => o.Remove(firstDependency));
        }

        private List<object> _InitializeViewModelDependencies()
        {
            var result = new List<object>();

            var changeTrackingServiceFake = new Mock<IChangeTrackingService>();
            result.Add(changeTrackingServiceFake);

            var referencedComponentsTrackingServiceFake = new Mock<IReferencedComponentsTrackingService>();
            result.Add(referencedComponentsTrackingServiceFake);

            // initialize injection services
            _InitializeDependenyInjectionService<ViewModelBase>(vm =>
                                                                    {
                                                                        var xmlDependencyViewModel = vm as XmlDependencyViewModel;
                                                                        if (xmlDependencyViewModel != null)
                                                                        {
                                                                            xmlDependencyViewModel.ChangeTrackingService = changeTrackingServiceFake.Object;
                                                                        }

                                                                        var xmlComponentViewModel = vm as XmlComponentViewModel;
                                                                        if (xmlComponentViewModel != null)
                                                                        {
                                                                            xmlComponentViewModel.ChangeTrackingService = changeTrackingServiceFake.Object;
                                                                            xmlComponentViewModel.ReferencedComponentsTrackingService = referencedComponentsTrackingServiceFake.Object;
                                                                        }
                                                                    });

            return result;
        }
    }
}
