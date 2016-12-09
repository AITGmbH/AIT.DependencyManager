using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using AIT.DMF.Contracts.Enums;
using AIT.DMF.Contracts.Parser;
using AIT.DMF.Contracts.Provider;
using AIT.DMF.Contracts.Services;
using AIT.DMF.DependencyManager.Controls.Messaging;
using AIT.DMF.DependencyManager.Controls.Messaging.Events;
using AIT.DMF.DependencyManager.Controls.Model;
using AIT.DMF.DependencyManager.Controls.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace AIT.DMF.DependencyManager.Controls.Test.ViewModels
{
    /// <summary>
    ///This is a test class for ProviderSettingsEditorTest and is intended
    ///to contain all ProviderSettingsEditorTest Unit Tests
    ///</summary>
    [TestClass()]
    public class ProviderSettingsEditorTest : TestBase
    {
        private const string FakeFileName = "component.targets";
        private const string FakePath = "C:\\fake\\path\\to\\" + FakeFileName;
        private const string FakeTPCUrl = "http://fake.to:8080/FakeCollection";

        private TestContext testContextInstance;

        // flags for extension methods
        private bool _hasCalledIObservableSubscribe;

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
        public void Instance_NoSelectedEventPublished_XmlDependencyIsNull()
        {
            _InitializeViewModelDependencies();

            // create the view model
            var target = new ProviderSettingsEditorViewModel();

            Assert.IsNull(target.XmlDependency);
        }

        [TestMethod]
        [HostType("Moles")]
        public void Instance_NoSelectedEventPublished_ProviderSettingsEditorIsNull()
        {
            _InitializeViewModelDependencies();

            // create the view model
            var target = new ProviderSettingsEditorViewModel();

            Assert.IsNull(target.ProviderSettingsEditor);
        }

        [TestMethod]
        [HostType("Moles")]
        public void OnImportsSatisfied_EventPublisherIsSet_SelectedXmlDependencyChangedEventIsRetrieved()
        {
            var dependencies = _InitializeViewModelDependencies();
            var eventPublisherMock = dependencies.GetEntry<Mock<IEventPublisher>>();

            // create the view model
            var target = new ProviderSettingsEditorViewModel();

            // make sure the event was requested at least once
            eventPublisherMock.Verify(o => o.GetEvent<SelectedXmlDependencyChangedEvent>(), Times.Once());
        }

        [TestMethod]
        [HostType("Moles")]
        public void OnImportsSatisfied_EventPublisherIsSet_SelectedXmlDependencyChangedEventIsSubscribedTo()
        {
            _InitializeViewModelDependencies();

            // just to make sure we're initialized correctly
            Assert.IsFalse(_hasCalledIObservableSubscribe);

            // create the view model
            var target = new ProviderSettingsEditorViewModel();

            // make sure the view model subscribed to the event
            Assert.IsTrue(_hasCalledIObservableSubscribe);
        }

        [TestMethod]
        [HostType("Moles")]
        public void Instance_SelectedEventPublished_XmlDependencyIsSet()
        {
            var dependencies = _InitializeViewModelDependencies();
            var xmlDependencyViewModel = dependencies.GetEntry<XmlDependencyViewModel>();
            var eventPublisher = dependencies.GetEntry<Mock<IEventPublisher>>().Object;
            var theEvent = dependencies.GetEntry<SelectedXmlDependencyChangedEvent>();

            // create the view model
            var target = new ProviderSettingsEditorViewModel();

            // publish event
            eventPublisher.Publish(theEvent);

            Assert.AreEqual(xmlDependencyViewModel, target.XmlDependency);
        }

        [TestMethod]
        [HostType("Moles")]
        public void Instance_SelectedEventPublished_GetDependencyResolversIsInvoked()
        {
            var dependencies = _InitializeViewModelDependencies();
            var dependencyServiceMock = dependencies.GetEntry<Mock<IDependencyService>>();
            var eventPublisherStub = dependencies.GetEntry<Mock<IEventPublisher>>().Object;
            var theEvent = dependencies.GetEntry<SelectedXmlDependencyChangedEvent>();

            // create the view model
            var target = new ProviderSettingsEditorViewModel();

            // publish event
            eventPublisherStub.Publish(theEvent);

            dependencyServiceMock.Verify(o => o.GetDependencyResolvers());
        }

        [TestMethod]
        [HostType("Moles")]
        public void Instance_SelectedEventPublished_DefinitionEditorIsRetrieved()
        {
            var dependencies = _InitializeViewModelDependencies();
            var dependencyResolverTypeMock = dependencies.GetEntry<Mock<IDependencyResolverType>>();
            var dependencyViewModel = dependencies.GetEntry<XmlDependencyViewModel>();
            var eventPublisherStub = dependencies.GetEntry<Mock<IEventPublisher>>().Object;
            var theEvent = dependencies.GetEntry<SelectedXmlDependencyChangedEvent>();

            // create the view model
            var target = new ProviderSettingsEditorViewModel();

            // publish event
            eventPublisherStub.Publish(theEvent);

            dependencyResolverTypeMock.Verify(o => o.GetDefinitionEditor(It.IsAny<IDependencyInjectionService>(), dependencyViewModel, FakeFileName, FakeTPCUrl));
        }

        [TestMethod]
        [HostType("Moles")]
        public void Instance_SelectedEventPublished_ProviderSettingsEditorIsSet()
        {
            var dependencies = _InitializeViewModelDependencies();
            var definitionEditor = dependencies.GetEntry<FrameworkElement>();
            var eventPublisherStub = dependencies.GetEntry<Mock<IEventPublisher>>().Object;
            var theEvent = dependencies.GetEntry<SelectedXmlDependencyChangedEvent>();

            // create the view model
            var target = new ProviderSettingsEditorViewModel();

            // publish event
            eventPublisherStub.Publish(theEvent);

            Assert.AreEqual(definitionEditor, target.ProviderSettingsEditor);
        }

        private List<object> _InitializeViewModelDependencies()
        {
            const DependencyType usedDependencyType = DependencyType.BinaryDependency;
            var resolverTypeLookUp = "Resolver_" + usedDependencyType;

            var result = new List<object>();

            // the fake editor
            var fakeDefinitionEditor = new TextBox();
            result.Add(fakeDefinitionEditor as FrameworkElement);

            // create xml dependency fakes
            var dependencyProviderConfigFake = new Mock<IDependencyProviderConfig>();
            dependencyProviderConfigFake.Setup(o => o.Type)
                .Returns(usedDependencyType.ToString());
            result.Add(dependencyProviderConfigFake);
            var xmlDependencyFake = new Mock<IXmlDependency>();
            xmlDependencyFake.Setup(o => o.Type)
                .Returns(usedDependencyType);
            xmlDependencyFake.Setup(o => o.ProviderConfiguration)
                .Returns(dependencyProviderConfigFake.Object);
            result.Add(xmlDependencyFake);
            var xmlDependencyViewModel = new XmlDependencyViewModel(xmlDependencyFake.Object, true);
            result.Add(xmlDependencyViewModel);

            // create the fake resolver
            var dependencyResolverTypeFake = new Mock<IDependencyResolverType>();
            dependencyResolverTypeFake.Setup(o => o.ReferenceName)
                .Returns(resolverTypeLookUp);
            dependencyResolverTypeFake.Setup(o => o.GetDefinitionEditor(It.IsAny<IDependencyInjectionService>(), xmlDependencyViewModel, "component.targets", FakeTPCUrl))
                .Returns(fakeDefinitionEditor);
            result.Add(dependencyResolverTypeFake);

            // create the IDependencyService fake
            var dependencyServiceFake = new Mock<IDependencyService>();
            dependencyServiceFake.Setup(o => o.GetDependencyResolvers())
                .Returns(new List<IDependencyResolverType>(new[] { dependencyResolverTypeFake.Object }));
            result.Add(dependencyServiceFake);

            // create event fakes
            var selectedXmlDependencyChangedEvent = new SelectedXmlDependencyChangedEvent(null, xmlDependencyViewModel);
            result.Add(selectedXmlDependencyChangedEvent);

            // create an event observable fake
            Action<SelectedXmlDependencyChangedEvent> eventHandler = null;
            var selectedXmlDependencyChangedEventObservableFake = new Mock<IObservable<SelectedXmlDependencyChangedEvent>>();
            result.Add(selectedXmlDependencyChangedEventObservableFake);

            // use moles to stub the static extension method             
            _hasCalledIObservableSubscribe = false;
            System.Moles.MObservableExtensions.SubscribeIObservableOfTSourceActionOfTSource<SelectedXmlDependencyChangedEvent>((func, action) =>
                                                                                                                                   {
                                                                                                                                       _hasCalledIObservableSubscribe = true;
                                                                                                                                       eventHandler = action;
                                                                                                                                       return null;
                                                                                                                                   });

            // create an IEventPublisher fake
            var eventPublisherFake = new Mock<IEventPublisher>();
            eventPublisherFake.Setup(o => o.GetEvent<SelectedXmlDependencyChangedEvent>())
                .Returns(selectedXmlDependencyChangedEventObservableFake.Object);
            eventPublisherFake.Setup(o => o.Publish(selectedXmlDependencyChangedEvent))
                .Callback<SelectedXmlDependencyChangedEvent>(o => eventHandler(o));
            result.Add(eventPublisherFake);

            // create targets file data
            var targetsFileData = new TargetsFileData();
            targetsFileData.LocalPath = FakePath;
            result.Add(targetsFileData);

            // create team project collection data
            var tpcUrlData = new TeamProjectCollectionData();
            tpcUrlData.TPCUri = FakeTPCUrl;
            result.Add(tpcUrlData);

            // initialize injection service
            _InitializeDependenyInjectionService<ProviderSettingsEditorViewModel>(vm =>
            {
                vm.DependencyService = dependencyServiceFake.Object;
                vm.EventPublisher = eventPublisherFake.Object;
                vm.TargetsFileData = targetsFileData;
                vm.TeamProjectCollectionData = tpcUrlData;
            });

            return result;
        }
    }
}
