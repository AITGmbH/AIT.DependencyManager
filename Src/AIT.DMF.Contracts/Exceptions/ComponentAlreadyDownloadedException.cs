using System;
using System.Runtime.Serialization;

namespace AIT.DMF.Contracts.Exceptions
{
    /// <summary>
    /// This exception is thrown in case a component was previously downloaded by a Downloader.
    /// </summary>
    public class ComponentAlreadyDownloadedException: Exception
    {
        /// <summary>
        /// Call base class constructor.
        /// </summary>
        public ComponentAlreadyDownloadedException()
            : base()
        { }

        /// <summary>
        /// Call base class constructor with message.
        /// </summary>
        /// <param name="message">Exception message</param>
        public ComponentAlreadyDownloadedException(string message)
            : base(message)
        {}

        /// <summary>
        /// Call base class constructor with serialization info and context.
        /// </summary>
        /// <param name="info">Serialization information</param>
        /// <param name="context">Streaming context</param>
        protected ComponentAlreadyDownloadedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {}
    }
}
