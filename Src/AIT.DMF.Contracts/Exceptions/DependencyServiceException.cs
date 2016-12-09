using System;
using System.Runtime.Serialization;

namespace AIT.DMF.Contracts.Exceptions
{
    /// <summary>
    /// This exception is thrown in case of any error in dependency service or a sub component.
    /// Exceptions from components inside the dependency service should be rethrown as a DependencyServiceException.
    /// </summary>
    public class DependencyServiceException : Exception
    {
       /// <summary>
       /// Call base class constructor.
       /// </summary>
        public DependencyServiceException()
        { }

        /// <summary>
        /// Call base class constructor with message.
        /// </summary>
        /// <param name="message">Exception message</param>
        public DependencyServiceException(string message)
            : base(message)
        { }

        /// <summary>
        /// Call base class constructor with message.
        /// </summary>
        /// <param name="message">Exception message</param>
        /// <param name="innerException">The original exception.</param>
        public DependencyServiceException(string message, Exception innerException)
            : base(message, innerException)
        { }

        /// <summary>
        /// Call base class constructor with serialization info and context.
        /// </summary>
        /// <param name="info">Serialization information</param>
        /// <param name="context">Streaming context</param>
        protected DependencyServiceException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }
    }
}
