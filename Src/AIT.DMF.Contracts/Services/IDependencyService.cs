using System;
using System.Collections.Generic;
using AIT.DMF.Contracts.Parser;
using AIT.DMF.Contracts.Provider;
using AIT.DMF.Contracts.Common;
using AIT.DMF.Contracts.Graph;
using AIT.DMF.Contracts.Gui;

namespace AIT.DMF.Contracts.Services
{
    public interface IDependencyService
    {
        /// <summary>
        /// Represents the service settings used in this provider
        /// </summary>
        ISettings<ServiceValidSettings> ServiceSettings { get; }

        /// <summary>
        /// Create a full dependency graph using a single starting point
        /// </summary>
        /// <param name="path">Path to branch folder</param>
        /// <param name="log">Logger to log messages with</param>
        /// <returns></returns>
        IGraph GetDependencyGraph(string path, ILogger log);

        /// <summary>
        /// Creates the dependency graph asynchronously.
        /// </summary>
        /// <param name="path">The dependency definition file.</param>
        /// <param name="log">The logger to log messages with.</param>
        /// <param name="userCallback">The user callback.</param>
        /// <param name="userState">The state of the user.</param>
        /// <returns>IAsyncResult object</returns>
        IAsyncResult BeginGetDependencyGraph(string path, ILogger log, AsyncCallback userCallback, object userState);

        /// <summary>
        /// Returns the dependency graph that was created.
        /// </summary>
        /// <param name="asyncResult">The async result.</param>
        /// <returns>The dependency graph.</returns>
        IGraph EndGetDependencyGraph(IAsyncResult asyncResult);

        /// <summary>
        /// Downloads the components in the graph to a local directory on the client.
        /// </summary>
        /// <param name="graph">Component graph</param>
        /// <param name="log">Logger to log messages with</param>
        /// <param name="recursive">Indicates if dependencies should be recursive or not.</param>
        /// <param name="force">Indicates that we want to force a get operation and all files have to be overwritten</param>
        void DownloadGraph(IGraph graph, ILogger log, bool recursive = true, bool force = false);

        /// <summary>
        /// Downloads the components in the graph asynchronously.
        /// </summary>
        /// <param name="graph">The dependency graph.</param>
        /// <param name="log">The logger to log messages with.</param>
        /// <param name="userCallback">The user callback.</param>
        /// <param name="userState">State of the user.</param>
        /// <param name="recursive">Set to true to download all dependencies. Else direct dependencies are fetched.</param>
        /// <param name="force">Set to true to force downloading. Else incremental get.</param>
        /// <returns>IAsyncResult object</returns>
        IAsyncResult BeginDownloadGraph(IGraph graph, ILogger log, AsyncCallback userCallback, object userState, bool recursive = true, bool force = false);

        /// <summary>
        /// Ends the async download.
        /// </summary>
        /// <param name="asyncResult">The async result.</param>
        void EndDownloadGraph(IAsyncResult asyncResult);

        /// <summary>
        /// Cleanup the downloaded components on the client based on the graph.
        /// </summary>
        /// <param name="graph">Component graph</param>
        /// <param name="log">Logger to log messages with</param>
        void CleanupGraph(IGraph graph, ILogger log);

        /// <summary>
        /// Cleans up the downloaded components asynchronously.
        /// </summary>
        /// <param name="graph">The dependency graph.</param>
        /// <param name="log">The logger to log messages with.</param>
        /// <param name="userCallback">The user callback.</param>
        /// <param name="userState">State of the user.</param>
        /// <returns>IAsyncResult object</returns>
        IAsyncResult BeginCleanupGraph(IGraph graph, ILogger log, AsyncCallback userCallback, object userState);

        /// <summary>
        /// Ends the async cleanup.
        /// </summary>
        /// <param name="asyncResult">The async result.</param>
        void EndCleanupGraph(IAsyncResult asyncResult);

        /// <summary>
        /// Gets all dependency resolver types which are registered in the <see cref="IDependencyService"/>
        /// </summary>
        /// <returns>A collection of all available <see cref="IDependencyResolverType"/></returns>
        IEnumerable<IDependencyResolverType> GetDependencyResolvers();

        /// <summary>
        /// Registers a new <see cref="IDependencyResolverType"/> in the <see cref="IDependencyService"/>
        /// </summary>
        /// <param name="resolverType">The dependency resolver type that shall be registered</param>
        void RegisterResolverType(IDependencyResolverType resolverType);

        /// <summary>
        /// Loads an XML component from the local disk //TODO: Refactor this so that the graph can be used for loading and storing
        /// </summary>
        /// <param name="path">Path to dependency definition file</param>
        /// <param name="log">Logger to log messages with</param>
        /// <returns></returns>
        IXmlComponent LoadXmlComponent(string path, ILogger log);

        /// <summary>
        /// Stores an XML component to the local disk //TODO: Refactor this so that the graph can be used for loading and storing
        /// </summary>
        /// <param name="component">The component that shall be stored</param>
        /// <param name="path">Path to dependency definition file</param>
        /// <param name="log">Logger to log messages with</param>
        /// <returns></returns>
        void StoreXmlComponent(IXmlComponent component, string path, ILogger log);

        /// <summary>
        /// Returns an initialized IXmlDependency object based on the resolverType //TODO: Initialize in a general way without type knowledge needed
        /// </summary>
        /// <param name="resolverType">Reference Type for initialization</param>
        /// <returns>IXmlDependency</returns>
        IXmlDependency CreateEmptyIXmlDependency(IDependencyResolverType resolverType);
    }
}
