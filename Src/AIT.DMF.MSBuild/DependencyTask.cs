// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DependencyTask.cs" company="AIT GmbH & Co. KG.">
//   All rights reserved by AIT GmbH & Co. KG.
// </copyright>
// <summary>
//   MSBuild task for getting dependencies during MSBuild execution
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace AIT.DMF.MSBuild
{
    using System;
    using System.IO;
    using Contracts.Exceptions;
    using Contracts.Graph;
    using DependencyService;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;

    /// <summary>
    /// MSBuild task for getting dependencies during MSBuild execution
    /// </summary>
    public abstract class DependencyTask : Task
    {
        #region TaskParameters

        /// <summary>
        /// Gets or sets the dependency definition file path with filename (Can be source control path or relative path) (Specified by user)
        /// </summary>
        [Required]
        public string DependencyDefinitionPath { get; set; }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the graph.
        /// </summary>
        protected IGraph Graph { get; private set; }

        /// <summary>
        /// Gets the logger.
        /// </summary>
        protected BuildOutputLogger Logger { get; private set; }

        /// <summary>
        /// Gets the build task helper.
        /// </summary>
        protected BuildTaskHelper BuildTaskHelper { get; private set; }

        #endregion

        /// <summary>
        /// Called by MSBuild runtime when the GetDependencies task is executed
        /// </summary>
        /// <returns>
        /// True is succeeded; false in case of any errors.
        /// </returns>
        public override bool Execute()
        {
            Platform.Initialize();

            // Log parameter values
            Logger = new BuildOutputLogger(Log);

            // Wait for debugger to attach
            // System.Diagnostics.Debugger.Launch();

            // Initialize BuildTaskHelper and validate settings
            try
            {
                BuildTaskHelper = new BuildTaskHelper(DependencyDefinitionPath, BuildEngine.ProjectFileOfTaskNode);
                foreach (var pair in BuildTaskHelper.DependencyServiceSettings.SettingsDictionary)
                {
                    Logger.LogMsg(string.Format("{0}: [{1}]", pair.Key, pair.Value));
                }
            }
            catch (ArgumentException ae)
            {
                Logger.LogError(ae.Message);
                return false;
            }

            try
            {
                try
                {
                    Logger.LogMsg(string.Format("\nProcessing dependencies (Dependency definition file {0}):", Path.GetFullPath(DependencyDefinitionPath)));

                    // Generate graph
                    Graph = BuildTaskHelper.DependencyService.GetDependencyGraph(Path.GetFullPath(DependencyDefinitionPath), Logger);
                }
                catch (DependencyServiceException dse)
                {
                    Logger.LogError(string.Format("! Error while creating dependency graph [{0}]!", dse.Message));
                    return false;
                }

                try
                {
                    InternalExecute();
                }
                catch (DependencyServiceException dse)
                {
                    Logger.LogError(string.Format("{0}", dse.Message));
                    return false;
                }

                // Everything has been executed successfully
                return true;
            }
            catch (Exception e)
            {
                Logger.LogError("Fatal error occured while executing GetDependencies MSBuild task. Aborting ...");
                Logger.LogError(string.Format("Exception message:{0}", e.Message));
                Logger.LogError(string.Format("Stacktrace:\n{0}", e.StackTrace));
                return false;
            }
        }

        /// <summary>
        /// Internal abstract execute method. Used to call dependency service only.
        /// </summary>
        protected abstract void InternalExecute();
    }
}
