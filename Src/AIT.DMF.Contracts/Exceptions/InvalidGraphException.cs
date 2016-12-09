using System;
using System.Runtime.Serialization;

namespace AIT.DMF.Contracts.Exceptions
{
    /// <summary>
    /// This exception is thrown in case an graph contains invalid components or dependencies.
    /// </summary>
    public class InvalidGraphException: Exception
    {
        /// <summary>
        /// Call base class constructor.
        /// </summary>
        public InvalidGraphException()
        { }

        /// <summary>
        /// Call base class constructor with message.
        /// </summary>
        /// <param name="message">Exception message</param>
        public InvalidGraphException(string message)
            : base(message)
        {}

        /// <summary>
        /// Call base class constructor with serialization info and context.
        /// </summary>
        /// <param name="info">Serialization information</param>
        /// <param name="context">Streaming context</param>
        protected InvalidGraphException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {}
    }
}
