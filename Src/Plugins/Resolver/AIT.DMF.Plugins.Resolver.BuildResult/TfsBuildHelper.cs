// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TfsBuildHelper.cs" company="AIT GmbH & Co. KG">
//   All rights reserved by AIT GmbH & Co. KG
// </copyright>
// <summary>
//   Defines the BinaryRepositoryDefinitionEditorViewModel type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace AIT.DMF.Plugins.Resolver.BuildResult
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.TeamFoundation.Build.Client;
    using Microsoft.TeamFoundation.Client;
    using Microsoft.TeamFoundation.VersionControl.Client;

    /// <summary>
    /// A TFS build helper class.
    /// </summary>
    internal class TfsBuildHelper
    {
        #region Private Members

        /// <summary>
        /// The team project collection url.
        /// </summary>
        private readonly Uri _tpcUrl;

        /// <summary>
        /// The team project collection.
        /// </summary>
        private readonly TfsTeamProjectCollection _tpc;

        /// <summary>
        /// The version control server.
        /// </summary>
        private readonly VersionControlServer _versionControl;

        /// <summary>
        /// The build server service.
        /// </summary>
        private readonly IBuildServer _buildServer;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="TfsBuildHelper"/> class with a connection to the TFS server, version control and to the build server.
        /// </summary>
        /// <param name="tpcUrl">The team project collection url.</param>
        public TfsBuildHelper(Uri tpcUrl)
        {
            _buildServer = null;
            _tpcUrl = tpcUrl;
            if (null == _tpcUrl)
            {
                throw new ArgumentNullException("tpcUrl");
            }

            // Connect to tfs server
            _tpc = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(_tpcUrl);
            _tpc.EnsureAuthenticated();

            // Connect to version control service & build server
            _versionControl = _tpc.GetService<VersionControlServer>();
            _buildServer = _tpc.GetService<IBuildServer>();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Returns the team projects.
        /// </summary>
        /// <returns>List of TeamProject objects</returns>
        public IEnumerable<TeamProject> GetTeamProjects()
        {
            return new List<TeamProject>(_versionControl.GetAllTeamProjects(true));
        }

        /// <summary>
        /// Returns the build definitions for a specific team project.
        /// </summary>
        /// <param name="teamProject">Team project object</param>
        /// <returns>List of IBuildDefinition objects</returns>
        public IEnumerable<IBuildDefinition> GetBuildDefinitionsFromTeamProject(TeamProject teamProject)
        {
            return GetBuildDefinitionsFromTeamProject(teamProject.Name);
        }

        /// <summary>
        /// Returns the build definitions for a specific team project name.
        /// </summary>
        /// <param name="teamProject">Team project name</param>
        /// <returns>List of IBuildDefinition objects</returns>
        public IEnumerable<IBuildDefinition> GetBuildDefinitionsFromTeamProject(string teamProject)
        {
            if (null == teamProject)
            {
                throw new ArgumentNullException("teamProject");
            }

            return new List<IBuildDefinition>(_buildServer.QueryBuildDefinitions(teamProject));
        }

        /// <summary>
        /// Returns valid build status values.
        /// </summary>
        /// <returns>List of strings</returns>
        public IEnumerable<BuildStatus> GetBuildStatus()
        {
            return Enum.GetValues(typeof(BuildStatus)).OfType<BuildStatus>();
        }

        /// <summary>
        /// Returns available build qualities for a given team project.
        /// </summary>
        /// <param name="teamProject">Team project object</param>
        /// <returns>List of strings</returns>
        public IEnumerable<string> GetBuildQuality(TeamProject teamProject)
        {
            return GetBuildQuality(teamProject.Name);
        }

        /// <summary>
        /// Returns available build qualities for a given team project name.
        /// </summary>
        /// <param name="teamProject">Team project name</param>
        /// <returns>List of strings</returns>
        public IEnumerable<string> GetBuildQuality(string teamProject)
        {
            if (null == teamProject || string.IsNullOrEmpty(teamProject))
            {
                throw new ArgumentNullException("teamProject");
            }

            return new List<string>(_buildServer.GetBuildQualities(teamProject));
        }

        /// <summary>
        /// Returns available builds in the form of build numbers for a given team project and build definition.
        /// </summary>
        /// <param name="teamProject">TeamProject object</param>
        /// <param name="buildDefinition">IBuildDefinition object</param>
        /// <returns>List of strings</returns>
        public IEnumerable<string> GetAvailableBuildNumbers(TeamProject teamProject, IBuildDefinition buildDefinition)
        {
            return GetAvailableBuildNumbers(teamProject.Name, buildDefinition.Name);
        }

        /// <summary>
        /// Returns available builds in the form of build numbers for a given team project and build definition.
        /// </summary>
        /// <param name="teamProject">Team project name</param>
        /// <param name="buildDefinition">Build definition name</param>
        /// <returns>List of strings</returns>
        public IEnumerable<string> GetAvailableBuildNumbers(string teamProject, string buildDefinition)
        {
            if (null == teamProject)
            {
                throw new ArgumentNullException("teamProject");
            }

            if (null == buildDefinition)
            {
                throw new ArgumentNullException("buildDefinition");
            }

            var spec = _buildServer.CreateBuildDetailSpec(teamProject, buildDefinition);
            spec.InformationTypes = new string[] { };

            return new List<string>(_buildServer.QueryBuilds(spec).Builds.Select(x => x.BuildNumber));
        }
        #endregion
    }
}
