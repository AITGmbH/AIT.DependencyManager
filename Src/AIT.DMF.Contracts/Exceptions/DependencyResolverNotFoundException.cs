using System;
using System.Runtime.Serialization;

namespace AIT.DMF.Contracts.Exceptions
{
    /// <summary>
    /// The exception that is thrown when a resolver has been requested that has not been found
    /// </summary>
    public class DependencyResolverNotFoundException : Exception
    {
        public DependencyResolverNotFoundException() { }

        public DependencyResolverNotFoundException(string message) : base(message) { }

        public DependencyResolverNotFoundException(string message, Exception inner) : base(message, inner) { }

        protected DependencyResolverNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
