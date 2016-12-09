// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CleanDependencies.cs" company="AIT GmbH & Co. KG.">
//   All rights reserved by AIT GmbH & Co. KG.
// </copyright>
// <summary>
//   MSBuild task for cleaning dependencies during MSBuild execution
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace AIT.DMF.MSBuild
{
    /// <summary>
    /// MSBuild task for cleaning dependencies during MSBuild execution
    /// </summary>
    public class CleanDependencies : DependencyTask
    {
        /// <summary>
        /// Overrides the logic in DependencyTask with logic for clean dependencies.
        /// </summary>
        protected override void InternalExecute()
        {
            // Cleanup dependencies in graph
            BuildTaskHelper.DependencyService.CleanupGraph(this.Graph, this.Logger);
        }
    }
}