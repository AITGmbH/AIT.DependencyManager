// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ValidatorFactory.cs" company="AIT GmbH & Co. KG.">
//   All rights reserved by AIT GmbH & Co. KG.
// </copyright>
// <summary>
//   Defines the ValidatorFactory factory.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace AIT.DMF.PluginFactory
{
    using System;
    using System.Collections.Generic;
    using AIT.DMF.Contracts.Exceptions;
    using AIT.DMF.Contracts.Graph;
    using AIT.DMF.Plugins.Validators.Common;

    /// <summary>
    /// The validator factory.
    /// </summary>
    public static class ValidatorFactory
    {
        #region Private Members

        /// <summary>
        /// The validators dictionary.
        /// </summary>
        private static readonly Dictionary<string, IValidator> Validators;

        #endregion

        #region Static Constructor

        /// <summary>
        /// Initializes static members of the <see cref="ValidatorFactory"/> class.
        /// </summary>
        static ValidatorFactory()
        {
            Validators = new Dictionary<string, IValidator>(StringComparer.OrdinalIgnoreCase);

            var sidebyside = new SideBySideValidator();
            Validators.Add(sidebyside.Name, sidebyside);

            var cyclic = new CyclicDependencyValidator();
            Validators.Add(cyclic.Name, cyclic);
        }

        #endregion

        #region Factory Methods

        /// <summary>
        /// Gets a specific validator by its name
        /// </summary>
        /// <param name="name">The name of the validator</param>
        /// <returns>The validator object if found</returns>
        /// <exception cref="DependencyManagementFoundationPluginNotFoundException">Will be thrown if the requested validator has not been found</exception>
        public static IValidator GetValidator(string name)
        {
            if (Validators.ContainsKey(name))
            {
                return Validators[name];
            }

            throw new DependencyManagementFoundationPluginNotFoundException(string.Format("The validator object {0} has not been found", name));
        }

        /// <summary>
        /// Registers a new validator object in the factory. If the key for a value is arleady in use the key will be overwritten
        /// </summary>
        /// <param name="validator">The validator that has to be registered</param>
        public static void RegisterValidator(IValidator validator)
        {
            if (Validators.ContainsKey(validator.Name))
            {
                Validators[validator.Name] = validator;
            }
            else
            {
                Validators.Add(validator.Name, validator);
            }
        }

        /// <summary>
        /// Gets all validators which are currently registered in the factory
        /// </summary>
        /// <returns>The collection of all validators</returns>
        public static IEnumerable<IValidator> GetAllValidators()
        {
            return Validators.Values;
        }

        #endregion
    }
}
