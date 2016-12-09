using System;
using System.Runtime.Serialization;

namespace AIT.DMF.Contracts.Exceptions
{
    /// <summary>
    /// This exception is thrown in case an invalid component was detected.
    /// </summary>
    public class InvalidComponentException : Exception
    {
        /// <summary>
        /// Call base class constructor.
        /// </summary>
        public InvalidComponentException()
        { }

        /// <summary>
        /// Call base class constructor with message.
        /// </summary>
        /// <param name="message">Exception message</param>
        public InvalidComponentException(string message)
            : base(message)
        { }

        /// <summary>
        /// Call base class constructor with serialization info and context.
        /// </summary>
        /// <param name="info">Serialization information</param>
        /// <param name="context">Streaming context</param>
        protected InvalidComponentException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }
    }
}
