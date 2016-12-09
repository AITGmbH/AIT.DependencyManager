// --------------------------------------------------------------------------------------------------------------------
// <copyright file="GetDependencies.cs" company="AIT GmbH & Co. KG">
//   All rights reserved by AIT GmbH & Co. KG
// </copyright>
// <summary>
//   Implementation of a workflow activity that fetches dependencies defined in dependency definition files
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace AIT.DMF.Workflow
{
    using System;

    using Contracts.Common;
    using Contracts.Services;
    using Microsoft.TeamFoundation.Build.Client;

    /// <summary>
    /// Implements the logic for fetching dependencies.
    /// </summary>
    [BuildActivity(HostEnvironmentOption.Agent)]
    public class GetDependencies : DependencyActivity
    {
        /// <summary>
        /// Implements the get dependencies logic.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="settings">The settings.</param>
        protected override void InternalExecute(BuildLogLogger logger, ISettings<ServiceValidSettings> settings)
        {
            ActivityName = "GetDependencies";

            var dependencyDefinitionFilePath = Context.GetValue(DependencyDefinitionPath);

            logger.LogMsg(string.Format("Get dependencies (Based on {0}):", dependencyDefinitionFilePath));
            var ds = new DependencyService.DependencyService(settings);

            // Generate graph
            logger.LogMsg("Creating dependency graph...");
            var localDependencyDefinitionFilePath = Context.GetValue(Workspace).GetLocalItemForServerItem(dependencyDefinitionFilePath);
            var graph = ds.GetDependencyGraph(localDependencyDefinitionFilePath, logger);

            // Download components in graph
            logger.LogMsg("Downloading dependency graph...");
            ds.DownloadGraph(graph, logger);
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
