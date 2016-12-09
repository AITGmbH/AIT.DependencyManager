using System;
using AIT.DMF.Common;
using AIT.DMF.Contracts.Provider;
using AIT.DMF.Contracts.Gui;
using AIT.DMF.Contracts.Graph;
using AIT.DMF.PluginFactory;

namespace AIT.DMF.DependencyService.Commands
{
    using System.Diagnostics;

    internal class Cleaner
    {
        #region Private Members

        private readonly ILogger _logger;
        private readonly bool _silentMode;

        #endregion

        #region Public Properties

        public string CommandType { get { return "Cleaner"; } }

        #endregion

        #region Constructor

        /// <summary>
        /// Initialize the DependencyGraphCleaner with a standard cleanupFolder, a logger and the logging mode.
        /// </summary>
        /// <param name="logger">Logger object</param>
        /// <param name="silent">Log with logger or log to debugging</param>
        /// <exception cref="ArgumentNullException">In case a logger is not provided.</exception>
        public Cleaner(ILogger logger, bool silent)
        {
            if (null == logger)
                throw new ArgumentNullException("logger");

            _logger = logger;
            _silentMode = silent;
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Reverts an individual component
        /// </summary>
        /// <param name="dwSvc">The watermark service to use</param>
        /// <param name="name">The name of the component</param>
        /// <param name="version">The version of the component</param>
        internal void RevertComponent(DownloadWatermarkService dwSvc, string name, string version)
        {
            var dFac = new DownloaderFactory();
            IDependencyDownloaderWatermark wm = null;
            var wmName = name;
            var wmVersion = version;

            try
            {
                wm = dwSvc.Load(name, version);

                if (null == wm)
                {
                    // TODO: might consider logging this to a trace file
                    Debug.Fail(
                        string.Format(
                            "Unable to load a watermark for component {0}@{1}. This situation should never occur due to the fact that the same method is reading and writing the watermarks",
                            name,
                            version));
                }

                // Read name and version information from watermark (saving can convert name and version back to hashes).
                if(wm.Tags.ContainsKey("name"))
                    wmName = wm.Tags["name"];
                if (wm.Tags.ContainsKey("version"))
                    wmVersion = wm.Tags["version"];

                var cleaner = dFac.GetDownloader(wm.DownloadType);
                Logger.Instance().Log(TraceLevel.Info, "{0}: Cleaning component {1}#{2} ...", CommandType, wmName, wmVersion);
                cleaner.RevertDownload(wm);

                dwSvc.Delete(wm);
            }
            catch
            {
                if (null != wm) dwSvc.Save(wm, name, version);
            }

            if(!_silentMode)
            {
                _logger.LogMsg(string.Format("  * Component {0} (Version:{1}) cleaned.", wmName, wmVersion));
            }
            Logger.Instance().Log(TraceLevel.Info, "{0}: Component {1}#{2} successfully cleaned", CommandType, wmName, wmVersion);
        }


        /// <summary>
        /// Cleans up the successors in the dependency graph.
        /// </summary>
        /// <param name="graph">Node to start with</param>
        internal void Clean(IGraph graph)
        {
            if (!_silentMode)
            {
                _logger.LogMsg("Cleaning all components...");
            }
            Logger.Instance().Log(TraceLevel.Info, "{0}: Cleaning components ...", CommandType);

            var dwSvc = new DownloadWatermarkService(graph.RootComponentTargetPath);

            //TODO: Use parallel foreach to gain cleanup speed. might infere with logging and therefore we are currently using single threaded only
            var cachedComponents = dwSvc.GetStoredDependencyWatermarks();
            foreach (var cacheComponent in cachedComponents)
            {
                // Properties of cachedComponent: Item1 = name, cachedComponent Item2 = version
                RevertComponent(dwSvc, cacheComponent.Item1, cacheComponent.Item2);
            }

            // Finish documents
            if (!_silentMode)
            {
                _logger.LogMsg("Cleaned components successfully!\n");
            }

            Logger.Instance().Log(TraceLevel.Info, "{0}: Cleaning components finished successfully", CommandType);
        }

        #endregion
    }
}
