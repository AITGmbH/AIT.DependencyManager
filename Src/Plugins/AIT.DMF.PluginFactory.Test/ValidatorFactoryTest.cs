// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ValidatorFactoryTest.cs" company="AIT GmbH & Co. KG.">
//   All rights reserved by AIT GmbH & Co. KG.
// </copyright>
// <summary>
//   This is a test class for ValidatorFactory and is intended
//   to contain all ValidatorFactory Unit Tests
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace AIT.DMF.PluginFactory.Test
{
    using System.Collections.Generic;
    using System.Linq;

    using AIT.DMF.Contracts.Exceptions;
    using AIT.DMF.Contracts.Graph;
    using AIT.DMF.PluginFactory;
    using AIT.DMF.Plugins.Validators.Common;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// This is a test class for ValidatorFactoryTest and is intended
    /// to contain all ValidatorFactoryTest Unit Tests
    /// </summary>
    [TestClass]
    public class ValidatorFactoryTest
    {
        /// <summary>
        /// Gets or sets the test context which provides
        /// information about and functionality for the current test run.
        /// </summary>
        public TestContext TestContext { get; set; }

        /// <summary>
        /// A test for RegisterValidator
        /// </summary>
        [TestMethod]
        public void RegisterValidatorTest()
        {
            IValidator validator = new SideBySideValidator();
            ValidatorFactory.RegisterValidator(validator);

            Assert.AreEqual(ValidatorFactory.GetValidator(validator.Name).Name, validator.Name);
        }

        /// <summary>
        /// A test with a valid Validator name for GetValidator method.
        /// </summary>
        [TestMethod]
        public void GetValidatorTest()
        {
            var expected = new SideBySideValidator();

            var actual = ValidatorFactory.GetValidator(expected.Name);
            Assert.AreEqual(expected.Name, actual.Name);
        }

        /// <summary>
        /// A test with a invalid Validator name for GetValidator method.
        /// </summary>
        [ExpectedException(typeof(DependencyManagementFoundationPluginNotFoundException))]
        [TestMethod]
        public void GetValidatorExceptionTest()
        {
            ValidatorFactory.GetValidator("Test");
        }

        /// <summary>
        /// A test for GetAllValidators method.
        /// </summary>
        [TestMethod]
        public void GetAllValidatorsTest()
        {
            var actual = ValidatorFactory.GetAllValidators();
            var validators = actual as List<IValidator> ?? actual.ToList();

            Assert.IsTrue(validators.Any(x => new SideBySideValidator().Name.Equals(x.Name)));
            Assert.IsTrue(validators.Any(x => new CyclicDependencyValidator().Name.Equals(x.Name)));
        }
    }
}
