namespace AIT.DMF.Plugins.Resolver.VNextBuildResult
{
    using Microsoft.TeamFoundation.Build.Client;
    using Microsoft.TeamFoundation.Client;
    using Microsoft.VisualStudio.Services.Client;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using TFSWebApi = Microsoft.TeamFoundation.Build.WebApi;
    using VSClient = Microsoft.TeamFoundation.VersionControl.Client;

    internal class TfsBuild2Helper
    {
        #region Private Members

        /// <summary>
        /// The version control server.
        /// </summary>
        private readonly VSClient.VersionControlServer versionControl;

        /// <summary>
        ///  Type needed to exposes the members.
        /// </summary>
        private readonly VssConnection connection;

        /// <summary>
        /// The http build client.
        /// </summary>
        private readonly TFSWebApi.BuildHttpClient client;

        #endregion Private Members

        #region Constructor

        internal TfsBuild2Helper(Uri tpcUrl)
        {
            this.connection = new VssConnection(tpcUrl, new VssClientCredentials(true));
            this.client = connection.GetClient<TFSWebApi.BuildHttpClient>();

            // Connect to tfs server
            var tpc = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(tpcUrl);
            tpc.EnsureAuthenticated();

            // Connect to version control service
            this.versionControl = tpc.GetService<VSClient.VersionControlServer>();
        }

        #endregion Constructor

        #region Public Methods

        /// <summary>
        /// Returns the team projects.
        /// </summary>
        /// <returns>List of TeamProject objects</returns>
        public IEnumerable<VSClient.TeamProject> GetTeamProjects()
        {
            return new List<VSClient.TeamProject>(versionControl.GetAllTeamProjects(true));
        }

        /// <summary>
        /// Returns the build definitions for a specific team project.
        /// </summary>
        /// <param name="teamProject">Team project object</param>
        /// <returns>List of <see cref="TFSWebApi.DefinitionReference"/> objects</returns>
        public async Task<IEnumerable<TFSWebApi.DefinitionReference>> GetBuildDefinitionsFromTeamProject(VSClient.TeamProject teamProject)
        {
            return await this.GetBuildDefinitionsFromTeamProject(teamProject.Name);
        }

        /// <summary>
        /// Returns the build definitions for a specific team project.
        /// </summary>
        /// <param name="teamProject">Team project name</param>
        /// <returns>List of <see cref="TFSWebApi.DefinitionReference"/> objects</returns>
        public async Task<IEnumerable<TFSWebApi.DefinitionReference>> GetBuildDefinitionsFromTeamProject(string teamProject)
        {
            if (null == teamProject)
            {
                throw new ArgumentNullException("teamProject");
            }

            return await this.client.GetDefinitionsAsync(project: teamProject, type: TFSWebApi.DefinitionType.Build);
        }

        /// <summary>
        /// Returns valid build status values.
        /// </summary>
        /// <returns>List of build statuses</returns>
        public IEnumerable<TFSWebApi.BuildStatus> GetBuildStatus()
        {
            return Enum.GetValues(typeof(TFSWebApi.BuildStatus)).OfType<TFSWebApi.BuildStatus>();
        }

        /// <summary>
        /// Returns available builds in the form of build numbers for a given team project and build definition.
        /// </summary>
        /// <param name="teamProject">TeamProject object</param>
        /// <param name="buildDefinition">IBuildDefinition object</param>
        /// <returns>List of available build numbers</returns>
        public async Task<IEnumerable<string>> GetAvailableBuildNumbers(VSClient.TeamProject teamProject, IBuildDefinition buildDefinition)
        {
            return await this.GetAvailableBuildNumbers(teamProject.Name, buildDefinition.Name);
        }

        /// <summary>
        /// Returns available builds in the form of build numbers for a given team project and build definition.
        /// </summary>
        /// <param name="teamProject">Team project name</param>
        /// <param name="buildDefinition">Build definition name</param>
        /// <returns>List of available build numbers</returns>
        public async Task<IEnumerable<string>> GetAvailableBuildNumbers(string teamProject, string buildDefinition)
        {
            if (null == teamProject)
            {
                throw new ArgumentNullException("teamProject");
            }

            if (null == buildDefinition)
            {
                throw new ArgumentNullException("buildDefinition");
            }

            var definition = await this.client.GetDefinitionsAsync(project: teamProject, name: buildDefinition, type: TFSWebApi.DefinitionType.Build);
            var definitionId = definition.First().Id;
            var builds = await this.client.GetBuildsAsync(project: teamProject, definitions: new List<int> { definitionId }, type: TFSWebApi.DefinitionType.Build);
            return builds.Select(b => b.BuildNumber);
        }

        /// <summary>
        /// Get available build tags.
        /// </summary>
        /// <param name="teamProject">Team project name</param>
        /// <param name="buildDefinition">Build definition name</param>
        /// <returns>Get available build tags.</returns>
        public async Task<List<string>> GetAvailableBuildTags(string teamProject)
        {
            if (null == teamProject)
            {
                throw new ArgumentNullException("Team project");
            }

            var builds = await this.client.GetBuildsAsync(project: teamProject, type: TFSWebApi.DefinitionType.Build);

            var listOfTagList = builds.Select(b => b.Tags);
            return listOfTagList.SelectMany(x => x).Distinct().ToList();
        }

        #endregion Public Methods
    }
}