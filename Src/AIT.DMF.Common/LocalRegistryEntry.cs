using System;

namespace AIT.DMF.Common
{

    /// <summary>
    /// Attribute that marks a property that should be saved and loaded to and from the local windows registry
    /// </summary>
    class LocalRegistryEntry : Attribute
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="TfsRegistryEntryAttribute"/> class.
        /// </summary>
        /// <param name="defaultValue">
        /// The default value of the property if no value is set in the registry or registry is not accessible.
        /// </param>
        public LocalRegistryEntry(object defaultValue)
        {
            DefaultValue = defaultValue;
        }

        /// <summary>
        /// Gets the default value of the property if no value is set in the registry or registry is not accessible.
        /// </summary>
        public object DefaultValue { get; private set; }
    }
}
