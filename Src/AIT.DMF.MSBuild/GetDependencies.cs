// --------------------------------------------------------------------------------------------------------------------
// <copyright file="GetDependencies.cs" company="AIT GmbH & Co. KG.">
//   All rights reserved by AIT GmbH & Co. KG.
// </copyright>
// <summary>
//   MSBuild task for getting dependencies during MSBuild execution
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace AIT.DMF.MSBuild
{
    /// <summary>
    /// MSBuild task for getting dependencies during MSBuild execution
    /// </summary>
    public class GetDependencies : DependencyTask
    {
        #region Additional Defaults

        /// <summary>
        /// The default setting for force mode.
        /// </summary>
        private const bool DefaultForce = true;

        /// <summary>
        /// The default setting for recursive mode.
        /// </summary>
        private const bool DefaultRecursive = true;

        #endregion

        #region Additional Private parameters

        /// <summary>
        /// Determines if the get operation should be forced.
        /// </summary>
        private bool _force = DefaultForce;

        /// <summary>
        /// Determines if the get operation should fetch dependencies recursively.
        /// </summary>
        private bool _recursive = DefaultRecursive;

        #endregion

        #region Additional TaskParameters

        /// <summary>
        /// Gets or sets a value indicating whether the get command should be forced (true) or incremental (false)
        /// Use force when the build workspace is cleaned before downloading dependencies
        /// True is the default value
        /// </summary>
        public bool Force
        {
            get
            {
                return _force;
            }

            set
            {
                _force = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the <see cref="GetDependencies" /> task should fetch dependencies recursive (true) or not (false).
        /// True is the default value.
        /// </summary>
        public bool Recursive
        {
            get
            {
                return _recursive;
            }

            set
            {
                _recursive = value;
            }
        }

        #endregion

        /// <summary>
        /// Overrides the logic in DependencyTask with logic for (force) recursive/non-recursive get dependencies.
        /// </summary>
        protected override void InternalExecute()
        {
            // Download components in graph
            BuildTaskHelper.DependencyService.DownloadGraph(Graph, Logger, Recursive, Force);
        }
    }
}