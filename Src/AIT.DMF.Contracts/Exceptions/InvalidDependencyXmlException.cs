using System;
using System.Runtime.Serialization;

namespace AIT.DMF.Contracts.Exceptions
{
    /// <summary>
    /// This exception is thrown in case an invalid xml element was found in dependency definition file.
    /// </summary>
    public class InvalidDependencyXmlException : Exception
    {
        /// <summary>
        /// Call base class constructor.
        /// </summary>
        public InvalidDependencyXmlException()
        { }

        /// <summary>
        /// Call base class constructor with message.
        /// </summary>
        /// <param name="message">Exception message</param>
        public InvalidDependencyXmlException(string message)
            : base(message)
        { }

        /// <summary>
        /// Call base class constructor with serialization info and context.
        /// </summary>
        /// <param name="info">Serialization information</param>
        /// <param name="context">Streaming context</param>
        protected InvalidDependencyXmlException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }
    }
}
