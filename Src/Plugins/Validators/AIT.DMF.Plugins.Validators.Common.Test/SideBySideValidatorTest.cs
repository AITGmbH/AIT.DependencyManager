using System.Linq;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using AIT.DMF.Contracts.Graph;
using AIT.DMF.Contracts.Parser;
using AIT.DMF.Common;
using AIT.DMF.DependencyService;

namespace AIT.DMF.Plugins.Validators.Common.Test
{
    /// <summary>
    ///This is a test class for SideBySideValidatorTest and is intended
    ///to contain all SideBySideValidatorTest Unit Tests
    ///</summary>
    [TestClass()]
    public class SideBySideValidatorTest
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
        ///A test for Validate
        ///</summary>
        [TestMethod()]
        public void SideBySideValidator_Validate_TwoConflictingComponents()
        {
            IComponent rootComp = new Component(GetConfig("root", "1.0", "FileShare"));
            IComponent sideComp1 = new Component(GetConfig("sideComp1", "1.0", "FileShare"));
            IComponent sideComp2 = new Component(GetConfig("sideComp1", "1.1", "FileShare"));
            IDependency depRootSideComp1 = new Dependency(rootComp, sideComp1, new ComponentVersion("1.0"));
            IDependency depRootSideComp2 = new Dependency(rootComp, sideComp2, new ComponentVersion("1.*"));
            rootComp.AddSuccessor(depRootSideComp1);
            rootComp.AddSuccessor(depRootSideComp2);
            sideComp1.AddPredecessor(depRootSideComp1);
            sideComp2.AddPredecessor(depRootSideComp2);

            var validator = new SideBySideValidator();
            IGraph graph = new Graph(rootComp, "C:\\");

            var actual = validator.Validate(graph);

            Assert.AreEqual(1, actual.Count(), "Expected exactly one conflict for sideComp1");
            Assert.AreEqual(2, actual.First().Components.Count(), "Expected exactly two conflicting components");
        }

        private DependencyProviderConfig GetConfig(string name, string version, string type)
        {
            var nameSetting = new DependencyProviderSetting
                { Name = DependencyProviderValidSettingName.ComponentName, Value = name };
            var versionSetting = new DependencyProviderSetting
                { Name = DependencyProviderValidSettingName.VersionNumber, Value = version };

            var config = new DependencyProviderConfig
                {
                    Settings =
                        new DependencyProviderSettings
                            {
                                SettingsList =
                                    new List<IDependencyProviderSetting> { nameSetting, versionSetting }
                            }
                };
            config.Type = type;

            return config;
        }

        /// <summary>
        ///A test for Validate
        ///</summary>
        [TestMethod()]
        public void SideBySideValidator_Validate_TwoConflictingComponentsAndTwoNonConflictingComponents()
        {
            IComponent rootComp = new Component(GetConfig("root", "1.0", "FileShare"));
            IComponent sideComp1 = new Component(GetConfig("sideComp1", "1.0", "FileShare"));
            IComponent sideComp2 = new Component(GetConfig("sideComp1", "1.1", "FileShare")); 
            IComponent sideComp3 = new Component(GetConfig("sideComp3", "1.0", "FileShare"));
            IComponent sideComp4 = new Component(GetConfig("sideComp4", "1.1", "FileShare"));
            
            IDependency depRootSideComp1 = new Dependency(rootComp, sideComp1, new ComponentVersion("1.0"));
            IDependency depRootSideComp2 = new Dependency(rootComp, sideComp2, new ComponentVersion("1.*"));
            IDependency depRootSideComp3 = new Dependency(rootComp, sideComp3, new ComponentVersion("1.*"));
            IDependency depRootSideComp4 = new Dependency(rootComp, sideComp4, new ComponentVersion("1.*"));

            rootComp.AddSuccessor(depRootSideComp1);
            rootComp.AddSuccessor(depRootSideComp2);
            rootComp.AddSuccessor(depRootSideComp3);
            rootComp.AddSuccessor(depRootSideComp4);
            
            sideComp1.AddPredecessor(depRootSideComp1);
            sideComp2.AddPredecessor(depRootSideComp2);
            sideComp3.AddPredecessor(depRootSideComp3);
            sideComp4.AddPredecessor(depRootSideComp4);

            var validator = new SideBySideValidator();
            IGraph graph = new Graph(rootComp, "C:\\");

            var actual = validator.Validate(graph);

            Assert.AreEqual(1, actual.Count(), "Expected exactly one conflict for sideComp1");
            Assert.AreEqual(2, actual.First().Components.Count(), "Expected exactly two conflicting components");
        }

        /// <summary>
        ///A test for Validate
        ///</summary>
        [TestMethod()]
        public void SideBySideValidator_Validate_FourConflictingComponentsInTwoDifferentComponentGroups()
        {
            IComponent rootComp = new Component(GetConfig("root", "1.0", "FileShare"));
            IComponent sideComp1 = new Component(GetConfig("sideComp1", "1.0", "FileShare"));
            IComponent sideComp2 = new Component(GetConfig("sideComp1", "1.1", "FileShare"));
            IComponent sideComp3 = new Component(GetConfig("sideComp3", "1.0", "FileShare"));
            IComponent sideComp4 = new Component(GetConfig("sideComp3", "1.1", "FileShare"));

            IDependency depRootSideComp1 = new Dependency(rootComp, sideComp1, new ComponentVersion("1.0"));
            IDependency depRootSideComp2 = new Dependency(rootComp, sideComp2, new ComponentVersion("1.*"));
            IDependency depRootSideComp3 = new Dependency(rootComp, sideComp3, new ComponentVersion("1.*"));
            IDependency depRootSideComp4 = new Dependency(rootComp, sideComp4, new ComponentVersion("1.*"));

            rootComp.AddSuccessor(depRootSideComp1);
            rootComp.AddSuccessor(depRootSideComp2);
            rootComp.AddSuccessor(depRootSideComp3);
            rootComp.AddSuccessor(depRootSideComp4);

            sideComp1.AddPredecessor(depRootSideComp1);
            sideComp2.AddPredecessor(depRootSideComp2);
            sideComp3.AddPredecessor(depRootSideComp3);
            sideComp4.AddPredecessor(depRootSideComp4);

            var validator = new SideBySideValidator();
            IGraph graph = new Graph(rootComp, "C:\\");

            var actual = validator.Validate(graph);

            Assert.AreEqual(2, actual.Count(), "Expected two conflicts. One for sideComp1 and one for sideComp3");
            Assert.AreEqual(2, actual.Where(x => x.Components.First().Name.GetName() == "sideComp1").FirstOrDefault().Components.Count(), "Expected exactly two conflicting components for sideComp1");
            Assert.AreEqual(2, actual.Where(x => x.Components.First().Name.GetName() == "sideComp3").FirstOrDefault().Components.Count(), "Expected exactly two conflicting components for sideComp3");
        }

        /// <summary>
        ///A test for Validate
        ///</summary>
        [TestMethod()]
        public void SideBySideValidator_Validate_FourConflictingComponentsMoreComplexGraph()
        {
            IComponent rootComp = new Component(GetConfig("root", "1.0", "FileShare"));
            IComponent sideComp1 = new Component(GetConfig("sideComp1", "1.0", "FileShare"));
            IComponent sideComp2 = new Component(GetConfig("sideComp1", "1.1", "FileShare"));
            IComponent sideComp3 = new Component(GetConfig("sideComp3", "1.0", "FileShare"));
            IComponent sideComp4 = new Component(GetConfig("sideComp3", "1.1", "FileShare"));

            IDependency depRootSideComp1 = new Dependency(rootComp, sideComp1, new ComponentVersion("1.0"));
            IDependency depRootSideComp2 = new Dependency(rootComp, sideComp2, new ComponentVersion("1.*"));
            IDependency depRootSideComp3 = new Dependency(rootComp, sideComp3, new ComponentVersion("1.*"));
            IDependency depRootSideComp4 = new Dependency(sideComp2, sideComp4, new ComponentVersion("1.*"));

            rootComp.AddSuccessor(depRootSideComp1);
            rootComp.AddSuccessor(depRootSideComp2);
            rootComp.AddSuccessor(depRootSideComp3);
            sideComp2.AddSuccessor(depRootSideComp4);

            sideComp1.AddPredecessor(depRootSideComp1);
            sideComp2.AddPredecessor(depRootSideComp2);
            sideComp3.AddPredecessor(depRootSideComp3);
            sideComp4.AddPredecessor(depRootSideComp4);

            var validator = new SideBySideValidator();
            IGraph graph = new Graph(rootComp, "C:\\");

            var actual = validator.Validate(graph);

            Assert.AreEqual(2, actual.Count(), "Expected two conflicts. One for sideComp1 and one for sideComp3");
            Assert.AreEqual(2, actual.Where(x => x.Components.First().Name.GetName() == "sideComp1").FirstOrDefault().Components.Count(), "Expected exactly two conflicting components for sideComp1");
            Assert.AreEqual(2, actual.Where(x => x.Components.First().Name.GetName() == "sideComp3").FirstOrDefault().Components.Count(), "Expected exactly two conflicting components for sideComp3");
        }
    }
}
