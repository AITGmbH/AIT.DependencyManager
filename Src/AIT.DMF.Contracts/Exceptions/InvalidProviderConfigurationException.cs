using System;
using System.Runtime.Serialization;

namespace AIT.DMF.Contracts.Exceptions
{
    /// <summary>
    /// This exception is thrown in case an invalid provider configuration was found.
    /// </summary>
    public class InvalidProviderConfigurationException : Exception
    {
        /// <summary>
        /// Call base class constructor.
        /// </summary>
        public InvalidProviderConfigurationException()
        { }

        /// <summary>
        /// Call base class constructor with message.
        /// </summary>
        /// <param name="message">Exception message</param>
        public InvalidProviderConfigurationException(string message)
            : base(message)
        { }

        /// <summary>
        /// Call base class constructor with serialization info and context.
        /// </summary>
        /// <param name="info">Serialization information</param>
        /// <param name="context">Streaming context</param>
        protected InvalidProviderConfigurationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }
    }
}
