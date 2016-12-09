using System;
using System.Collections.Generic;
using System.Diagnostics;
using AIT.DMF.Contracts.Exceptions;
using AIT.DMF.Contracts.Provider;
using AIT.DMF.Common;

namespace AIT.DMF.DependencyService
{
    /// <summary>
    /// Factory and management class for <see cref="IDependencyResolver"/>
    /// </summary>
    internal static class DependencyResolverFactory
    {
        #region Private Members

        private static readonly Dictionary<string, IDependencyResolverType> Types = new Dictionary<string, IDependencyResolverType>(StringComparer.OrdinalIgnoreCase);

        #endregion

        #region Factory Methods

        /// <summary>
        /// Registers a new <see cref="IDependencyResolverType"/> in the factory
        /// </summary>
        /// <param name="resolverType">An actual instance of the <see cref="IDependencyResolverType"/></param>
        internal static void RegisterResolverType(IDependencyResolverType resolverType)
        {
            if(null == resolverType)
                return;

            if (Types.ContainsKey(resolverType.ReferenceName))
                Types[resolverType.ReferenceName] = resolverType;
            else
            {
                Types.Add(resolverType.ReferenceName, resolverType);
                Logger.Instance().Log(TraceLevel.Info, "New resolver type {0} registered", resolverType);
            }
        }

        /// <summary>
        /// Gets a specific <see cref="IDependencyResolverType"/> based on the reference name of the resolver
        /// </summary>
        /// <param name="referenceName">The reference name of the resolver</param>
        /// <returns>The </returns>
        /// <exception cref="DependencyResolverNotFoundException">Will be thrown when an unknown resolver type has been requested</exception>
        internal static IDependencyResolverType GetResolverType(string referenceName)
        {
            if (Types.ContainsKey(referenceName))
                return Types[referenceName];

            Logger.Instance().Log(TraceLevel.Error, "The dependency resolver type {0} is not registered", referenceName);
            throw new DependencyResolverNotFoundException(string.Format("The dependency resolver type {0} is not registered", referenceName));
        }

        /// <summary>
        /// Gets all available <see cref="IDependencyResolverType"/> which are currently registered in the factory
        /// </summary>
        /// <returns>A collection with all <see cref="IDependencyResolverType"/></returns>
        internal static IEnumerable<IDependencyResolverType> GetAllResolverTypes()
        {
            return Types.Values;
        }

        #endregion
    }
}
