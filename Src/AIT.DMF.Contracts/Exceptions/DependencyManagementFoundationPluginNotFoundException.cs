using System;
using System.Runtime.Serialization;

namespace AIT.DMF.Contracts.Exceptions
{
    /// <summary>
    /// This exception is thrown in case an plugin could not be found by one of the plugin factories.
    /// </summary>
    public class DependencyManagementFoundationPluginNotFoundException : Exception
    {
        /// <summary>
        /// Call base class constructor.
        /// </summary>
        public DependencyManagementFoundationPluginNotFoundException()
        { }

        /// <summary>
        /// Call base class constructor with message.
        /// </summary>
        /// <param name="message">Exception message</param>
        public DependencyManagementFoundationPluginNotFoundException(string message)
            : base(message)
        { }

        /// <summary>
        /// Call base class constructor with serialization info and context.
        /// </summary>
        /// <param name="info">Serialization information</param>
        /// <param name="context">Streaming context</param>
        protected DependencyManagementFoundationPluginNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }
    }
}
