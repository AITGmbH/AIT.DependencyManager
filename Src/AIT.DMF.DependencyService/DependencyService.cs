using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using AIT.DMF.Common;
using AIT.DMF.Contracts.Parser;
using AIT.DMF.Contracts.Provider;
using AIT.DMF.DependencyService.Commands;
using AIT.DMF.Contracts.Common;
using AIT.DMF.Contracts.Exceptions;
using AIT.DMF.Contracts.Gui;
using AIT.DMF.Contracts.Services;
using AIT.DMF.Contracts.Graph;
using AIT.DMF.Logging;
using System.Collections.Generic;

namespace AIT.DMF.DependencyService
{
    public class DependencyService : IDependencyService
    {
        #region Private members

        private readonly ISettings<ServiceValidSettings> _settings;

        #endregion

        #region Constructor
        /// <summary>
        /// Checks if all settings needed are provided and initialize Visual Editor helper objects.
        /// </summary>
        /// <param name="settings"></param>
        public DependencyService(ISettings<ServiceValidSettings> settings)
        {
            if (!Logger.Instance().AttachedLoggers.OfType<TraceLogger>().Any())
            {
                Logger.Instance().RegisterLogSink(new TraceLogger());
            }
            Logger.Instance().Log(TraceLevel.Info, "Starting DependencyService ...");

            if (settings == null)
            {
                Logger.Instance().Log(TraceLevel.Info, "Service settings are invalid");
                throw new DependencyServiceException("Service settings are invalid");
            }
            _settings = settings;


            if (string.IsNullOrEmpty(_settings.GetSetting(ServiceValidSettings.DefaultTeamProjectCollection)))
            {
                Logger.Instance().Log(TraceLevel.Info, "Invalid service setting was found for Dependency Service (Team Project Collection)");
                throw new DependencyServiceException("Invalid service setting was found for Dependency Service (Team Project Collection)");
            }
            if (string.IsNullOrEmpty(_settings.GetSetting(ServiceValidSettings.DefaultWorkspaceName)))
            {
                Logger.Instance().Log(TraceLevel.Info, "Invalid service setting was found for Dependency Service (Workspace name)");
                throw new DependencyServiceException("Invalid service setting was found for Dependency Service (Workspace name)");
            }
            if (string.IsNullOrEmpty(_settings.GetSetting(ServiceValidSettings.DefaultWorkspaceOwner)))
            {
                Logger.Instance().Log(TraceLevel.Info, "Invalid service setting was found for Dependency Service (Workspace owner)");
                throw new DependencyServiceException("Invalid service setting was found for Dependency Service (Workspace owner)");
            }
            if (string.IsNullOrEmpty(_settings.GetSetting(ServiceValidSettings.DefaultOutputBaseFolder)))
            {
                Logger.Instance().Log(TraceLevel.Info, "Invalid service setting was found for Dependency Service (Default Output Base Folder)");
                throw new DependencyServiceException("Invalid service setting was found for Dependency Service (Default Output Base Folder)");
            }
            if (string.IsNullOrEmpty(_settings.GetSetting(ServiceValidSettings.DefaultDependencyDefinitionFilename)))
            {
                Logger.Instance().Log(TraceLevel.Info, "Invalid service setting was found for Dependency Service (Dependency Definition filename)");
                throw new DependencyServiceException("Invalid service setting was found for Dependency Service (Dependency Definition filename)");
            }
            if (_settings.GetSetting(ServiceValidSettings.BinaryTeamProjectCollectionUrl) == null)
            {
                Logger.Instance().Log(TraceLevel.Info, "Invalid service setting was found for Dependency Service (Default Team Project Collection for Binary Repository provider)");
                throw new DependencyServiceException("Invalid service setting was found for Dependency Service (Default Team Project Collection for Binary Repository provider)");
            }
            if (_settings.GetSetting(ServiceValidSettings.BinaryRepositoryTeamProject) == null)
            {
                Logger.Instance().Log(TraceLevel.Info, "Invalid service setting was found for Dependency Service (Default Team Project for Binary Reporsitory provider)");
                throw new DependencyServiceException("Invalid service setting was found for Dependency Service (Default Team Project for Binary Reporsitory provider)");
            }
            if (_settings.GetSetting(ServiceValidSettings.DefaultRelativeOutputPath) == null)
            {
                Logger.Instance().Log(TraceLevel.Info, "Invalid service setting was found for Dependency Service (Default Relative Output Path)");
                throw new DependencyServiceException("Invalid service setting was found for Dependency Service (Default Relative Output Path)");
            }

            Logger.Instance().Log(TraceLevel.Info, "DependencyService initialization finished successfully");
        }

        #endregion

        #region Properties

        /// <summary>
        /// Returns the settings for this DependencyService
        /// </summary>
        public ISettings<ServiceValidSettings> ServiceSettings
        {
            get
            {
                return _settings;
            }
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Downloads the components referenced by the dependency graph.
        /// A DependencyServiceException exception is thrown in case no graph is provided.
        /// </summary>
        /// <param name="graph">Dependency graph</param>
        /// <param name="log">Logger to log messages to</param>
        /// <param name="recursive">Indicates that we want to fetch the dependencies recursively</param>
        /// <param name="force">Indicates that we want to force a get operation and all files have to be overwritten</param>
        public void DownloadGraph(IGraph graph, ILogger log, bool recursive = true, bool force = false)
        {
            if (graph == null)
                throw new ArgumentNullException("graph");

            if (null == log)
                throw new ArgumentNullException("log");

            var baseFolderPath = _settings.GetSetting(ServiceValidSettings.DefaultOutputBaseFolder);
            var relativeOutputPath = _settings.GetSetting(ServiceValidSettings.DefaultRelativeOutputPath);

            var downloader = new Downloader(baseFolderPath, relativeOutputPath, log, false);
            downloader.Download(graph, recursive, force);
        }

        /// <summary>
        /// Downloads the download the components in the graph asynchronously.
        /// </summary>
        /// <param name="graph">The dependency graph.</param>
        /// <param name="log">The logger to log messages with.</param>
        /// <param name="userCallback">The user callback.</param>
        /// <param name="userState">State of the user.</param>
        /// <param name="force">Set to true to force downloading. Else incremental get.</param>
        /// <returns>AsyncResult object</returns>
        public IAsyncResult BeginDownloadGraph(IGraph graph, ILogger log, AsyncCallback userCallback, object userState, bool recursive = true, bool force = false)
        {
            Logger.Instance().Log(TraceLevel.Info, "Starting asynchronous graph download ...");
            var asyncResult = new AsyncResult<bool>(userCallback, userState);

            ThreadPool.QueueUserWorkItem(o =>
                                             {
                                                 try
                                                 {
                                                     DownloadGraph(graph, log, recursive, force);
                                                     asyncResult.SetAsCompleted(true, false);
                                                     Logger.Instance().Log(TraceLevel.Info, "Asynchronous graph download finished successfully");
                                                 }
                                                 catch (Exception ex)
                                                 {
                                                     var error =
                                                         new DependencyServiceException(
                                                             "Error while downloading the dependency graph: " +
                                                             ex.Message, ex);
                                                     asyncResult.SetAsCompleted(error, false);
                                                     Logger.Instance().Log(TraceLevel.Error, "Asynchronous graph download failed: {0}", ex.Message);
                                                 }
                                             });
            return asyncResult;
        }

        /// <summary>
        /// Ends the async download.
        /// </summary>
        /// <param name="asyncResult">The async result.</param>
        public void EndDownloadGraph(IAsyncResult asyncResult)
        {
            var ar = asyncResult as AsyncResult<bool>;
            if (ar == null)
            {
                throw new InvalidOperationException("Invalid IAsyncResult instance provided.");
            }

            ar.EndInvoke();
        }

        /// <summary>
        /// Cleanup the downloaded components referenced by the dependency graph.
        /// A DependencyServiceException exception is thrown in case no graph is provided.
        /// </summary>
        /// <param name="graph">Dependency graph</param>
        /// <param name="log">Logger to log messages to</param>
        public void CleanupGraph(IGraph graph, ILogger log)
        {
            if (graph == null)
                throw new ArgumentNullException("graph");

            if (null == log)
                throw new ArgumentNullException("log");

            //TODO: The graph is not needed for the download operation. We just need the path to the local dependency definition file
            var cleaner = new Cleaner(log, false);
            cleaner.Clean(graph);
        }

        /// <summary>
        /// Cleans up the downloaded components asynchronously.
        /// </summary>
        /// <param name="graph">The dependency graph.</param>
        /// <param name="log">The logger to log messages with.</param>
        /// <param name="userCallback">The user callback.</param>
        /// <param name="userState">State of the user.</param>
        /// <returns>IAsyncResult object</returns>
        public IAsyncResult BeginCleanupGraph(IGraph graph, ILogger log, AsyncCallback userCallback, object userState)
        {
            Logger.Instance().Log(TraceLevel.Info, "Starting asynchronous graph cleanup ...");
            var asyncResult = new AsyncResult<bool>(userCallback, userState);

            ThreadPool.QueueUserWorkItem(o =>
                                             {
                                                 try
                                                 {
                                                     CleanupGraph(graph, log);
                                                     asyncResult.SetAsCompleted(true, false);
                                                     Logger.Instance().Log(TraceLevel.Info, "Asyncronous graph cleanup finished successfully");
                                                 }
                                                 catch (Exception ex)
                                                 {
                                                     var error =
                                                         new DependencyServiceException(
                                                             "Error while cleaning up the dependency graph: " +
                                                             ex.Message, ex);
                                                     asyncResult.SetAsCompleted(error, false);
                                                     Logger.Instance().Log(TraceLevel.Error, "Asynchronous graph cleanup failed: {0}", ex.Message);
                                                 }
                                             });

            return asyncResult;
        }

        /// <summary>
        /// Ends the async cleanup.
        /// </summary>
        /// <param name="asyncResult">The async result.</param>
        public void EndCleanupGraph(IAsyncResult asyncResult)
        {
            var ar = asyncResult as AsyncResult<bool>;
            if (ar == null)
            {
                throw new InvalidOperationException("Invalid IAsyncResult instance provided.");
            }

            ar.EndInvoke();
        }

        /// <summary>
        /// Gets all dependency resolver types which are registered in the <see cref="IDependencyService"/>
        /// </summary>
        /// <returns>A collection of all available <see cref="IDependencyResolverType"/></returns>
        public IEnumerable<IDependencyResolverType> GetDependencyResolvers()
        {
            return DependencyResolverFactory.GetAllResolverTypes();
        }

        /// <summary>
        /// Registers a new <see cref="IDependencyResolverType"/> in the <see cref="IDependencyService"/>
        /// </summary>
        /// <param name="resolverType">The dependency resolver type that shall be registered</param>
        public void RegisterResolverType(IDependencyResolverType resolverType)
        {
            if (null == resolverType)
                throw new ArgumentNullException("resolverType");

            DependencyResolverFactory.RegisterResolverType(resolverType);
        }

        /// <summary>
        /// Creates a dependency graph.
        /// </summary>
        /// <param name="path">Local Path to component.targets file with filename</param>
        /// <param name="log">Logger to log messages to</param>
        public IGraph GetDependencyGraph(string path, ILogger log)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("Local file could not be found.", path);
            }

            //var creator = new DependencyGraphCreator(Path.GetFileName(path), log, true, new TfsWorkspaceInfo());
            var creator = new DependencyGraphCreator(Path.GetFileName(path), log, true);
            return creator.GetDependencyGraph(_settings, path);
        }

        /// <summary>
        /// Creates the dependency graph asynchronously.
        /// </summary>
        /// <param name="path">The dependency definition file.</param>
        /// <param name="log">The logger to log messages with.</param>
        /// <param name="userCallback">The user callback.</param>
        /// <param name="userState">The state of the user.</param>
        /// <returns>AsyncResult object</returns>
        public IAsyncResult BeginGetDependencyGraph(string path, ILogger log, AsyncCallback userCallback, object userState)
        {
            Logger.Instance().Log(TraceLevel.Info, "Starting asynchronous graph generation ...");
            var asyncResult = new AsyncResult<IGraph>(userCallback, userState);

            ThreadPool.QueueUserWorkItem(o =>
                                             {
                                                 try
                                                 {
                                                     var graph = GetDependencyGraph(path, log);
                                                     asyncResult.SetAsCompleted(graph, false);
                                                     Logger.Instance().Log(TraceLevel.Info, "Asynchronous graph generation finished successfully");
                                                 }
                                                 catch (Exception ex)
                                                 {
                                                     var error =
                                                         new DependencyServiceException(
                                                             "Error while retrieving the dependency graph: " +
                                                             ex.Message, ex);
                                                     asyncResult.SetAsCompleted(error, false);
                                                     Logger.Instance().Log(TraceLevel.Error, "Asynchronous graph download failed: {0}", ex.Message);
                                                 }
                                             });

            return asyncResult;
        }

        /// <summary>
        /// Returns the dependency graph that was created.
        /// </summary>
        /// <param name="asyncResult">The async result.</param>
        /// <returns>The dependency graph.</returns>
        public IGraph EndGetDependencyGraph(IAsyncResult asyncResult)
        {
            var ar = asyncResult as AsyncResult<IGraph>;
            if (ar == null)
            {
                throw new InvalidOperationException("Invalid IAsyncResult instance provided.");
            }

            return ar.EndInvoke();
        }

        /// <summary>
        /// Returns the list of IXMLDependency objects found in the dependency definition file.
        /// </summary>
        /// <param name="path">Path to dependency definition file</param>
        /// <param name="log">Logger</param>
        /// <returns></returns>
        public IXmlComponent LoadXmlComponent(string path, ILogger log)
        {
            if (String.IsNullOrEmpty(path))
                throw new ArgumentNullException("path");

            var parser = new ParserXml();
            var xmlComponent = parser.ReadDependencyFile(path);

            return xmlComponent;
        }

        public void StoreXmlComponent(IXmlComponent component, string path, ILogger log)
        {
            if (String.IsNullOrEmpty(path))
                throw new ArgumentNullException("path");

            var parser = new ParserXml();
            parser.StoreDependencyFile(component, path);
        }

        public IXmlDependency CreateEmptyIXmlDependency(IDependencyResolverType resolverType)
        {
            if (resolverType == null)
                throw new ArgumentNullException("resolverType");

            var parser = new ParserXml();
            var iXmlDep = parser.CreateEmptyIXmlDependency(resolverType.DependencyType);

            return iXmlDep;
        }

        #endregion
    }
}
