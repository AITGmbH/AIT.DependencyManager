// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TfsRegistryEntryAttribute.cs" company="AIT GmbH & Co. KG">
//   All rights reserved by AIT GmbH & Co. KG
// </copyright>
// <summary>
//   Defines the TfsRegistryEntryAttribute type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace AIT.DMF.Common
{
    using System;

    /// <summary>
    /// Attribute that marks a property that should be saved and loaded to and from team foundation registry
    /// </summary>
    public class TfsRegistryEntryAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TfsRegistryEntryAttribute"/> class.
        /// </summary>
        /// <param name="registryKeyOverride">
        /// The registry key in case it is different to the member name.
        /// </param>
        /// <param name="defaultValue">
        /// The default value of the property if no value is set in the registry.
        /// </param>
        public TfsRegistryEntryAttribute(string registryKeyOverride, object defaultValue)
        {
            RegistryKeyOverride = registryKeyOverride;
            DefaultValue = defaultValue;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TfsRegistryEntryAttribute"/> class.
        /// </summary>
        /// <param name="defaultValue">
        /// The default value of the property if no value is set in the registry or registry is not accessible.
        /// </param>
        public TfsRegistryEntryAttribute(object defaultValue)
        {
            DefaultValue = defaultValue;
        }

        /// <summary>
        /// Gets the default value of the property if no value is set in the registry or registry is not accessible.
        /// </summary>
        public object DefaultValue { get; private set; }

        /// <summary>
        /// Gets the registry key in case it is different to the member name.
        /// </summary>
        public string RegistryKeyOverride { get; private set; }
    }
}
