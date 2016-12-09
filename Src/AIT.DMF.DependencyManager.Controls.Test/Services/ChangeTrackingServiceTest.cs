using System;
using System.Collections.Generic;
using System.ComponentModel;
using AIT.DMF.DependencyManager.Controls.Messaging;
using AIT.DMF.DependencyManager.Controls.Messaging.Events;
using AIT.DMF.DependencyManager.Controls.Services;
using AIT.DMF.DependencyManager.Controls.Test.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace AIT.DMF.DependencyManager.Controls.Test.Services
{
    /// <summary>
    ///This is a test class for ChangeTrackingServiceTest and is intended
    ///to contain all ChangeTrackingServiceTest Unit Tests
    ///</summary>
    [TestClass()]
    public class ChangeTrackingServiceTest : TestBase
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

        [TestMethod]
        [HostType("Moles")]
        public void Instance_IsCreated_EventPublisherIsImported()
        {
            _InitializeDependencies();

            var target = new ChangeTrackingService();

            Assert.IsNotNull(target.EventPublisher);
        }

        [TestMethod]
        [HostType("Moles")]
        public void Save_IsInvoked_SaveAllChangesEventIsPublished()
        {
            bool saveAllChangesEventReceived = false;
            Action<SaveAllChangesEvent> eventHandler = o => saveAllChangesEventReceived = true;
            _InitializeDependencies(eventHandler);

            // act
            var target = new ChangeTrackingService();
            target.Save();

            Assert.IsTrue(saveAllChangesEventReceived);
        }

        [TestMethod]
        [HostType("Moles")]
        public void Save_IsInvokedWithFileName_SaveAllChangesEventIsPublishedWithFileName()
        {
            const string testFileName = "C:\\test.targets";

            bool saveAllChangesEventReceivedCorrectly = false;
            Action<SaveAllChangesEvent> eventHandler = o => saveAllChangesEventReceivedCorrectly = o.FileName == testFileName;
            _InitializeDependencies(eventHandler);

            // act
            var target = new ChangeTrackingService();
            target.Save(testFileName);

            Assert.IsTrue(saveAllChangesEventReceivedCorrectly);
        }

        [TestMethod]
        [HostType("Moles")]
        public void Instance_NoObjectsAdded_HasChangesIsFalse()
        {
            _InitializeDependencies();

            var target = new ChangeTrackingService();

            Assert.IsFalse(target.HasChanges);
        }

        [TestMethod]
        [HostType("Moles")]
        public void Add_ChangedObjectIsAdded_HasChangesIsTrue()
        {
            _InitializeDependencies();

            // stub change tracking object
            var changedObject = new Mock<IChangeTracking>();
            changedObject.Setup(o => o.IsChanged)
                .Returns(true);

            var target = new ChangeTrackingService();
            target.Add(changedObject.Object);

            Assert.IsTrue(target.HasChanges);
        }

        [TestMethod]
        [HostType("Moles")]
        public void Remove_SoleChangedObjectIsRemoved_HasChangesIsFalse()
        {
            _InitializeDependencies();

            // stub change tracking object
            var changedObject = new Mock<IChangeTracking>();
            changedObject.Setup(o => o.IsChanged)
                .Returns(true);

            var target = new ChangeTrackingService();
            target.Add(changedObject.Object);
            target.Remove(changedObject.Object);

            Assert.IsFalse(target.HasChanges);
        }

        [TestMethod]
        [HostType("Moles")]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void Add_UnchangedObjectIsAdded_ThrowsException()
        {
            _InitializeDependencies();

            // stub change tracking object
            var changedObject = new Mock<IChangeTracking>();
            changedObject.Setup(o => o.IsChanged)
                .Returns(false);

            var target = new ChangeTrackingService();
            target.Add(changedObject.Object);
        }

        [TestMethod]
        [HostType("Moles")]
        [ExpectedException(typeof(InvalidOperationException))]
        public void HasChanges_ChangedObjectIsResetToUnchangedButNotRemoved_ThrowsException()
        {
            _InitializeDependencies();

            // stub change tracking object
            var changedObject = new Mock<IChangeTracking>();
            changedObject.Setup(o => o.IsChanged)
                .Returns(true);

            var target = new ChangeTrackingService();
            target.Add(changedObject.Object);

            // reset change (cannot be detected by service)
            changedObject.Setup(o => o.IsChanged)
                .Returns(false);

            // successive access to "HasChanges" shall detect invalid state
            var hasChanges = target.HasChanges;
        }

        [TestMethod]
        [HostType("Moles")]
        public void Add_ChangedObjectIsAdded_RaisesHasChangesChangedEvent()
        {
            _InitializeDependencies();

            // stub change tracking object
            var changedObject = new Mock<IChangeTracking>();
            changedObject.Setup(o => o.IsChanged)
                .Returns(true);

            bool eventWasRaised = false;

            var target = new ChangeTrackingService();
            target.HasChangesChanged += (o, e) => eventWasRaised = true;
            target.Add(changedObject.Object);

            Assert.IsTrue(eventWasRaised);
        }

        [TestMethod]
        [HostType("Moles")]
        public void Remove_SoleChangedObjectIsRemoved_RaisesHasChangesChangedEvent()
        {
            _InitializeDependencies();

            // stub change tracking object
            var changedObject = new Mock<IChangeTracking>();
            changedObject.Setup(o => o.IsChanged)
                .Returns(true);

            bool eventWasRaised = false;

            var target = new ChangeTrackingService();
            target.Add(changedObject.Object);

            target.HasChangesChanged += (o, e) => eventWasRaised = true;
            target.Remove(changedObject.Object);

            Assert.IsTrue(eventWasRaised);
        }

        private List<object> _InitializeDependencies(Action<SaveAllChangesEvent> saveAllChangesEventHandler = null)
        {
            var result = new List<object>();

            // create an IEventPublisher fake
            var eventPublisherFake = new Mock<IEventPublisher>();
            eventPublisherFake.Setup(o => o.Publish(It.IsAny<SaveAllChangesEvent>()))
                .Callback<SaveAllChangesEvent>(o =>
                              {
                                  if (saveAllChangesEventHandler != null)
                                  {
                                      saveAllChangesEventHandler(o);
                                  }
                              });
            result.Add(eventPublisherFake);

            // initialize injection service
            _InitializeDependenyInjectionService<ChangeTrackingService>(service =>
            {
                service.EventPublisher = eventPublisherFake.Object;
            });

            return result;
        }
    }
}
