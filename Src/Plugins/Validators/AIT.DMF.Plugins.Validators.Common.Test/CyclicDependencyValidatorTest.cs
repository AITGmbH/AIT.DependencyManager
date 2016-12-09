using System.Collections.Generic;
using System.Linq;
using AIT.DMF.Common;
using AIT.DMF.DependencyService;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AIT.DMF.Contracts.Graph;
using AIT.DMF.Contracts.Parser;

namespace AIT.DMF.Plugins.Validators.Common.Test
{
    /// <summary>
    ///This is a test class for CyclicDependencyValidatorTest and is intended
    ///to contain all CyclicDependencyValidatorTest Unit Tests
    ///</summary>
    [TestClass()]
    public class CyclicDependencyValidatorTest
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
        public void CyclicValidatorTest_LinearDependencyChain_NoCyclicDependencies()
        {
            //root -> sideComp1 -> sideComp2 -> sideComp3

            IComponent rootComp = new Component(GetConfig("root", "1.0", "FileShare"));
            IComponent sideComp1 = new Component(GetConfig("sideComp1", "1.0", "FileShare"));
            IComponent sideComp2 = new Component(GetConfig("sideComp2", "1.0", "FileShare"));
            IComponent sideComp3 = new Component(GetConfig("sideComp3", "1.0", "FileShare"));

            IDependency depRootSideComp1 = new Dependency(rootComp, sideComp1, new ComponentVersion("1.*"));
            IDependency depRootSideComp2 = new Dependency(sideComp1, sideComp2, new ComponentVersion("1.*"));
            IDependency depRootSideComp3 = new Dependency(sideComp2, sideComp3, new ComponentVersion("1.*"));

            rootComp.AddSuccessor(depRootSideComp1);
            sideComp1.AddSuccessor(depRootSideComp2);
            sideComp2.AddSuccessor(depRootSideComp3);

            var validator = new CyclicDependencyValidator();
            IGraph graph = new Graph(rootComp, "C:\\");

            var actual = validator.Validate(graph);
            Assert.AreEqual(0, actual.Count(), "Expected no cyclic dependency for sideComp1");
        }

        /// <summary>
        ///A test for Validate
        ///</summary>
        [TestMethod()]
        public void CyclicValidatorTest_LinearDependencyChain_MultipleEdgeToComponent3_NoCycles()
        {
            //root -> comp1 -> comp2 -> comp 3
            //           |----------------|

            IComponent rootComp = new Component(GetConfig("root", "1.0", "FileShare"));
            IComponent sideComp1 = new Component(GetConfig("sideComp1", "1.0", "FileShare"));
            IComponent sideComp2 = new Component(GetConfig("sideComp2", "1.0", "FileShare"));
            IComponent sideComp3 = new Component(GetConfig("sideComp3", "1.0", "FileShare"));

            IDependency depRootSideComp1 = new Dependency(rootComp, sideComp1, new ComponentVersion("1.*"));
            IDependency depRootSideComp2 = new Dependency(sideComp1, sideComp2, new ComponentVersion("1.*"));
            IDependency depRootSideComp3 = new Dependency(sideComp2, sideComp3, new ComponentVersion("1.*"));
            IDependency depRootSideComp4 = new Dependency(sideComp1, sideComp3, new ComponentVersion("1.*"));

            rootComp.AddSuccessor(depRootSideComp1);
            sideComp1.AddSuccessor(depRootSideComp2);
            sideComp1.AddSuccessor(depRootSideComp4);
            sideComp2.AddSuccessor(depRootSideComp3);

            var validator = new CyclicDependencyValidator();
            IGraph graph = new Graph(rootComp, "C:\\");

            var actual = validator.Validate(graph);
            Assert.AreEqual(0, actual.Count());
        }

        /// <summary>
        ///A test for Validate
        ///</summary>
        [TestMethod()]
        public void CyclicValidatorTest_OneIndirectCyclicDependency()
        {
            //root -> sideComp1 -> sideComp2 -> sideComp3 -> sideComp1 ....

            IComponent rootComp = new Component(GetConfig("root", "1.0", "FileShare"));
            IComponent sideComp1 = new Component(GetConfig("sideComp1", "1.0", "FileShare"));
            IComponent sideComp2 = new Component(GetConfig("sideComp2", "1.0", "FileShare"));
            IComponent sideComp3 = new Component(GetConfig("sideComp3", "1.0", "FileShare"));

            IDependency depRootSideComp1 = new Dependency(rootComp, sideComp1, new ComponentVersion("1.*"));
            IDependency depRootSideComp2 = new Dependency(sideComp1, sideComp2, new ComponentVersion("1.*"));
            IDependency depRootSideComp3 = new Dependency(sideComp2, sideComp3, new ComponentVersion("1.*"));
            IDependency depRootSideComp4 = new Dependency(sideComp3, sideComp1, new ComponentVersion("1.*"));

            rootComp.AddSuccessor(depRootSideComp1);
            sideComp1.AddSuccessor(depRootSideComp2);
            sideComp2.AddSuccessor(depRootSideComp3);
            sideComp3.AddSuccessor(depRootSideComp4);

            var validator = new CyclicDependencyValidator();
            IGraph graph = new Graph(rootComp, "C:\\");

            var actual = validator.Validate(graph);
            Assert.AreEqual(1, actual.Count(), "Expected exactly one cyclic dependency for sideComp1");
        }

        /// <summary>
        ///A test for Validate
        ///</summary>
        [TestMethod()]
        public void CyclicValidatorTest_Validate_SelfDepedency_Cycle()
        {
            //sideComp1 -> sideComp1 ...

            IComponent sideComp1 = new Component(GetConfig("sideComp1", "1.0", "FileShare"));
            IDependency depRootSideComp1 = new Dependency(sideComp1, sideComp1, new ComponentVersion("1.*"));
            sideComp1.AddSuccessor(depRootSideComp1);

            var validator = new CyclicDependencyValidator();
            IGraph graph = new Graph(sideComp1, "C:\\");

            var actual = validator.Validate(graph);
            Assert.AreEqual(1, actual.Count(), "Expected exactly one conflict for sideComp1");
        }

        /// <summary>
        ///A test for Validate
        ///</summary>
        [TestMethod()]
        public void CyclicValidatorTest_Validate_CyclicDependencyHavingMultiplePaths()
        {
            IComponent rootComp = new Component(GetConfig("root", "1.0", "FileShare"));
            IComponent sideComp1 = new Component(GetConfig("sideComp1", "1.0", "FileShare"));
            IComponent sideComp2 = new Component(GetConfig("sideComp2", "1.0", "FileShare"));
            IComponent sideComp3 = new Component(GetConfig("sideComp3", "1.0", "FileShare"));

            IDependency depRootSideComp1 = new Dependency(rootComp, sideComp1, new ComponentVersion("1.*"));
            IDependency depRootSideComp2 = new Dependency(sideComp1, sideComp2, new ComponentVersion("1.*"));
            IDependency depRootSideComp3 = new Dependency(sideComp1, sideComp3, new ComponentVersion("1.*"));
            IDependency depRootSideComp4 = new Dependency(sideComp2, sideComp1, new ComponentVersion("1.*"));
            IDependency depRootSideComp5 = new Dependency(sideComp3, sideComp1, new ComponentVersion("1.*"));

            rootComp.AddSuccessor(depRootSideComp1);
            sideComp1.AddSuccessor(depRootSideComp2);
            sideComp1.AddSuccessor(depRootSideComp3);
            sideComp2.AddSuccessor(depRootSideComp4);
            sideComp3.AddSuccessor(depRootSideComp5);

            var validator = new CyclicDependencyValidator();
            IGraph graph = new Graph(rootComp, "C:\\");

            var actual = validator.Validate(graph);

            Assert.AreEqual(2, actual.Count(), "Expected exactly one cyclic dependency for sideComp1");
        }

        private DependencyProviderConfig GetConfig(string name, string version, string type)
        {
            var nameSetting = new DependencyProviderSetting { Name = DependencyProviderValidSettingName.ComponentName, Value = name };
            var versionSetting = new DependencyProviderSetting { Name = DependencyProviderValidSettingName.VersionNumber, Value = version };

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
    }
}
