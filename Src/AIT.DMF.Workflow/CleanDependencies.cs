// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CleanDependencies.cs" company="AIT GmbH & Co. KG">
//   All rights reserved by AIT GmbH & Co. KG
// </copyright>
// <summary>
//   Implementation of a workflow activity that cleans dependencies defined in dependency definition files
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace AIT.DMF.Workflow
{
    using System;
    using Contracts.Common;
    using Contracts.Services;
    using Microsoft.TeamFoundation.Build.Client;

    /// <summary>
    /// Implements the logic for cleaning dependencies.
    /// </summary>
    [BuildActivity(HostEnvironmentOption.Agent)]
    public class CleanDependencies : DependencyActivity
    {
        /// <summary>
        /// Implements the clean dependencies logic.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="settings">The settings.</param>
        protected override void InternalExecute(BuildLogLogger logger, ISettings<ServiceValidSettings> settings)
        {
            ActivityName = "CleanDependencies";

            var dependencyDefinitionFilePath = Context.GetValue(DependencyDefinitionPath);

            logger.LogMsg(string.Format("Clean dependencies (Based on {0}):", dependencyDefinitionFilePath));
            var ds = new DependencyService.DependencyService(settings);

            // Generate graph
            logger.LogMsg("Creating dependency graph...");
            var localDependencyDefinitionFilePath = Context.GetValue(Workspace).GetLocalItemForServerItem(dependencyDefinitionFilePath);
            var graph = ds.GetDependencyGraph(localDependencyDefinitionFilePath, logger);

            // Download components in graph
            logger.LogMsg("Cleaning dependency graph...");
            ds.CleanupGraph(graph, logger);
        }

        /// <summary>
        /// Validates the arguments of the activity.
        /// </summary>
        protected override void ValidateArguments()
        {
            if (string.IsNullOrEmpty(Context.GetValue(DependencyDefinitionPath)))
            {
                // ReSharper disable NotResolvedInText
                throw new ArgumentNullException("DependencyDefinitionPath", "Please supply a valid dependency definition file source control path");
                // ReSharper restore NotResolvedInText
            }
            if (Context.GetValue(Workspace) == null)
            {
                // ReSharper disable NotResolvedInText
                throw new ArgumentNullException("Workspace", "Please supply a valid workspace");
                // ReSharper restore NotResolvedInText
            }
        }
    }
}
