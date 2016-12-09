using System;
using System.Runtime.Serialization;

namespace AIT.DMF.Contracts.Exceptions
{
    /// <summary>
    /// This exception is thrown in case a cleaner is facing an error during cleanup operation.
    /// </summary>
    public class DependencyCleanerException : Exception
    {
        /// <summary>
        /// Call base class constructor.
        /// </summary>
        public DependencyCleanerException()
        { }

        /// <summary>
        /// Call base class constructor with message.
        /// </summary>
        /// <param name="message">Exception message</param>
        public DependencyCleanerException(string message)
            : base(message)
        { }

        /// <summary>
        /// Call base class constructor with serialization info and context.
        /// </summary>
        /// <param name="info">Serialization information</param>
        /// <param name="context">Streaming context</param>
        protected DependencyCleanerException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }
    }
}
