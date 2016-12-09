using System;
using System.Runtime.Serialization;

namespace AIT.DMF.Contracts.Exceptions
{
    /// <summary>
    /// This exception is thrown in case an operation counld not finish because of invalid access rights of the current user.
    /// </summary>
    public class InvalidAccessRightsException : Exception
    {
        /// <summary>
        /// Call base class constructor.
        /// </summary>
        public InvalidAccessRightsException()
        { }

        /// <summary>
        /// Call base class constructor with message.
        /// </summary>
        /// <param name="message">Exception message</param>
        public InvalidAccessRightsException(string message)
            : base(message)
        { }

        /// <summary>
        /// Call base class constructor with serialization info and context.
        /// </summary>
        /// <param name="info">Serialization information</param>
        /// <param name="context">Streaming context</param>
        protected InvalidAccessRightsException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }
    }
}
