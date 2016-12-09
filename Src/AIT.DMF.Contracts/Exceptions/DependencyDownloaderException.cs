using System;
using System.Runtime.Serialization;

namespace AIT.DMF.Contracts.Exceptions
{
    /// <summary>
    /// This exception is thrown in case a downloader is facing an error during download operation.
    /// </summary>
    public class DependencyDownloaderException : Exception
    {
        /// <summary>
        /// Call base class constructor.
        /// </summary>
        public DependencyDownloaderException()
        { }

        /// <summary>
        /// Call base class constructor with message.
        /// </summary>
        /// <param name="message">Exception message</param>
        public DependencyDownloaderException(string message)
            : base(message)
        { }

        /// <summary>
        /// Call base class constructor with serialization info and context.
        /// </summary>
        /// <param name="info">Serialization information</param>
        /// <param name="context">Streaming context</param>
        protected DependencyDownloaderException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }
    }
}
