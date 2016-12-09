// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ResolverBuildResult.cs" company="AIT GmbH & Co. KG">
//   AIT GmbH & Co. KG
// </copyright>
// <summary>
//   Defines the ResolverBuildResult type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace AIT.DMF.Plugins.Resolver.BuildResult
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Xml.Linq;
    using Common;
    using Contracts.Common;
    using Contracts.Exceptions;
    using Contracts.Provider;
    using Contracts.Services;
    using Microsoft.TeamFoundation.Build.Client;
    using Microsoft.TeamFoundation.Client;
    using Microsoft.TeamFoundation.VersionControl.Client;

    /// <summary>
    /// The resolver class for build result dependencies.
    /// </summary>
    public class ResolverBuildResult : IDependencyResolver
    {
        #region Private Members

        /// <summary>
        /// The dependency definition file name list.
        /// </summary>
        private readonly List<string> _dependencyDefinitionFileNameList;

        /// <summary>
        /// The version control server.
        /// </summary>
        private readonly VersionControlServer _versionControlServer;

        /// <summary>
        /// The build server.
        /// </summary>
        private readonly IBuildServer _buildServer;

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="ResolverBuildResult"/> class.
        /// </summary>
        /// <param name="settings">The resolver settings.</param>
        public ResolverBuildResult(ISettings<ResolverValidSettings> settings)
        {
            ResolverType = "Resolver_BuildResult";
            Logger.Instance().Log(TraceLevel.Info, "Initializing resolver {0} ...", ResolverType);

            if (settings == null)
            {
                throw new InvalidProviderConfigurationException(string.Format("Invalid connection settings were supplied"));
            }

            if (string.IsNullOrEmpty(settings.GetSetting(ResolverValidSettings.TeamProjectCollectionUrl)))
            {
                throw new InvalidProviderConfigurationException(string.Format("No team project collection url was supplied"));
            }

            if (string.IsNullOrEmpty(settings.GetSetting(ResolverValidSettings.DependencyDefinitionFileNameList)))
            {
                throw new InvalidProviderConfigurationException(string.Format("No dependency definition file name list was supplied"));
            }

            _dependencyDefinitionFileNameList =
                settings.GetSetting(ResolverValidSettings.DependencyDefinitionFileNameList).Split(new[] { ';' }).ToList();
            ComponentTargetsName = _dependencyDefinitionFileNameList.First();
            ResolverSettings = settings;

            // Connect to tfs server
            var tpc = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(new Uri(settings.GetSetting(ResolverValidSettings.TeamProjectCollectionUrl)));
            tpc.EnsureAuthenticated();

            // Connect to version control service
            _versionControlServer = tpc.GetService<VersionControlServer>();
            if (_versionControlServer == null)
            {
                Logger.Instance().Log(TraceLevel.Error, "{0}: Could not get VersionControlServer service for {1}", ResolverType, ResolverSettings.GetSetting(ResolverValidSettings.TeamProjectCollectionUrl));
                throw new InvalidProviderConfigurationException(string.Format("Could not get VersionControlServer service for {0} in {1}", ResolverSettings.GetSetting(ResolverValidSettings.TeamProjectCollectionUrl), ResolverType));
            }

            // Connect to build server
            _buildServer = tpc.GetService<IBuildServer>();
            if (_buildServer == null)
            {
                Logger.Instance().Log(TraceLevel.Error, "{0}: Could not get BuildServer service for {1}", ResolverType, ResolverSettings.GetSetting(ResolverValidSettings.TeamProjectCollectionUrl));
                throw new InvalidProviderConfigurationException(string.Format("Could not get BuildServer service for {0} in {1}", ResolverSettings.GetSetting(ResolverValidSettings.TeamProjectCollectionUrl), ResolverType));
            }

            Logger.Instance().Log(TraceLevel.Info, "Resolver {0} successfully initialized", ResolverType);
        }

        #region Public Properties

        /// <summary>
        /// Gets the type of the build result provider.
        /// </summary>
        public string ResolverType { get; private set; }

        /// <summary>
        /// Gets the settings for the BuildResult resolver.
        /// </summary>
        public ISettings<ResolverValidSettings> ResolverSettings { get; private set; }

        /// <summary>
        /// Gets the component targets name.
        /// </summary>
        public string ComponentTargetsName { get; private set; }

        #endregion

        /// <summary>
        /// Discover all available build definitions available.
        /// </summary>
        /// <returns>Returns a list with all component names</returns>
        public IEnumerable<IComponentName> GetAvailableComponentNames()
        {
            var compList = new List<IComponentName>();
            Logger.Instance().Log(TraceLevel.Info, "{0}: Querying available components...", ResolverType);

            // Create a component for every build definition for every team project
            foreach (var tp in _versionControlServer.GetAllTeamProjects(true))
            {
                foreach (var bd in _buildServer.QueryBuildDefinitions(tp.Name))
                {
                    var componentName = new ComponentName(tp.Name, bd.Name);
                    compList.Add(componentName);
                    Logger.Instance().Log(TraceLevel.Info, "{0}: Found component {1}", componentName);
                }
            }

            Logger.Instance().Log(TraceLevel.Info, "{0}: Querying components finished successfully", ResolverType);
            return compList;
        }

        /// <summary>
        /// Discover all available versions.
        /// </summary>
        /// <param name="name">The component name</param>
        /// <returns>A list with all versions</returns>
        public IEnumerable<IComponentVersion> GetAvailableVersions(IComponentName name)
        {
            var versList = new List<IComponentVersion>();
            Logger.Instance().Log(TraceLevel.Info, "{0}: Querying available component versions...", ResolverType);

            var teamProject = _versionControlServer.TryGetTeamProject(name.TeamProject);

            if (teamProject == null)
            {
                Logger.Instance().Log(
                    TraceLevel.Info,
                    "{0}: Could not find team project {1} on version control server for team project collection {2}",
                    ResolverType,
                    name.TeamProject,
                    ResolverSettings.GetSetting(ResolverValidSettings.TeamProjectCollectionUrl));
                throw new InvalidComponentException(
                    string.Format(
                        "Could not find team project {0} on version control server for team project collection {1}",
                        name.TeamProject,
                        ResolverSettings.GetSetting(ResolverValidSettings.TeamProjectCollectionUrl)));
            }

            var buildDef = _buildServer.GetBuildDefinition(teamProject.Name, name.BuildDefinition);

            if (buildDef == null)
            {
                Logger.Instance().Log(
                    TraceLevel.Info,
                    "{0}: Could not find build definition {1} for team project {2} on version control server for tfs {3}",
                    ResolverType,
                        name.BuildDefinition,
                        name.TeamProject,
                        ResolverSettings.GetSetting(ResolverValidSettings.TeamProjectCollectionUrl));
                throw new InvalidComponentException(
                    string.Format(
                        "Could not find build definition {0} for team project {1} on version control server for tfs {2}",
                        name.BuildDefinition,
                        name.TeamProject,
                        ResolverSettings.GetSetting(ResolverValidSettings.TeamProjectCollectionUrl)));
            }

            // Query all builds with this teamProject and build definition name
            // BuildNumber xor (BuildQuality or BuildStatus) = version
            var spec = _buildServer.CreateBuildDetailSpec(buildDef);
            spec.InformationTypes = new string[] { };
            var details = _buildServer.QueryBuilds(spec);

            foreach (var buildResult in details.Builds)
            {
                List<string> availableBuildQuality = null;
                if (buildResult.Quality != null)
                {
                    availableBuildQuality = new List<string> { buildResult.Quality };
                }

                var availableBuildStatus = new List<string> { buildResult.Status.ToString() };
                var vers = new ComponentVersion(buildResult.BuildNumber, availableBuildStatus, availableBuildQuality, null);
                versList.Add(vers);
                Logger.Instance().Log(TraceLevel.Info, "{0}: Found build {1}", ResolverType, vers.ToString());
            }

            Logger.Instance().Log(TraceLevel.Info, "{0}: Querying component versions finished successfully", ResolverType);
            return versList;
        }

        /// <summary>
        /// Loads a specific dependency definition file.
        /// </summary>
        /// <param name="name">The name of the build result component.</param>
        /// <param name="version">The component version.</param>
        /// <returns>The loaded dependency definition xml file or null if dependency definition file was not found</returns>
        public XDocument LoadComponentTarget(IComponentName name, IComponentVersion version)
        {
            ValidateComponentName(name);
            ValidateComponentVersion(version);

            var buildNumberSpec = _buildServer.CreateBuildDetailSpec(name.TeamProject, name.BuildDefinition);
            buildNumberSpec.BuildNumber = version.BuildNumber;
            buildNumberSpec.InformationTypes = new string[] { };
            var result = _buildServer.QueryBuilds(buildNumberSpec);

            foreach (var dependencyDefinitionFile in _dependencyDefinitionFileNameList)
            {
                var dependencyDefinitionFileLocation = Path.Combine(result.Builds.First().DropLocation, dependencyDefinitionFile);

                // DesignDecision MRI: Dependency definition files are optional!
                if (!File.Exists(dependencyDefinitionFileLocation))
                {
                    continue;
                }

                var xdoc = XDocument.Load(dependencyDefinitionFileLocation);

                Logger.Instance().Log(TraceLevel.Info, "{0}: Loading dependency definition file {1} for component {2}#{3} finished successfully", ResolverType, dependencyDefinitionFileLocation, name, version);

                return xdoc;
            }

            Logger.Instance().Log(TraceLevel.Info, "{0}: Loading a dependency definition file was skipped for component {1}#{2}.", ResolverType, name, version);
            return null;
        }

        /// <summary>
        /// Determines whether a build definition exists with this build definition name in a specific team project.
        /// </summary>
        /// <param name="name">The component name (Build definition and team project)</param>
        /// <returns>True if a build definition exists; False if not</returns>
        public bool ComponentExists(IComponentName name)
        {
            ValidateComponentName(name);

            try
            {
                var buildDef = _buildServer.GetBuildDefinition(name.TeamProject, name.BuildDefinition);
                if (buildDef != null)
                {
                    Logger.Instance().Log(TraceLevel.Info, "{0}: Build Definition for component {1} was found on build server", ResolverType, name);
                    return true;
                }

                Logger.Instance().Log(TraceLevel.Info, "{0}: Build Definition for component {1} was not found on build server", ResolverType, name);
                return false;
            }
            catch (BuildDefinitionNotFoundException)
            {
                Logger.Instance().Log(TraceLevel.Info, "{0}: Build Definition for component {1} was not found on build server", ResolverType, name);
                return false;
            }
        }

        /// <summary>
        /// Determines whether a build result exists based on a build definition in a specific team project.
        /// The build number or build quality/build status determines the version.
        /// </summary>
        /// <param name="name">The component name (Team project with build definition)</param>
        /// <param name="version">The component version (Build number or build quality/build status)</param>
        /// <returns>True if a build result exists with this the version; false otherwise</returns>
        public bool ComponentExists(IComponentName name, IComponentVersion version)
        {
            ValidateComponentName(name);
            ValidateComponentVersion(version);

            try
            {
                var buildDef = _buildServer.GetBuildDefinition(name.TeamProject, name.BuildDefinition);

                if (buildDef != null && !string.IsNullOrEmpty(version.BuildNumber))
                {
                    var buildNumberSpec = _buildServer.CreateBuildDetailSpec(name.TeamProject, name.BuildDefinition);
                    buildNumberSpec.BuildNumber = version.BuildNumber;
                    buildNumberSpec.InformationTypes = new string[] { };
                    var result = _buildServer.QueryBuilds(buildNumberSpec);

                    if (result.Builds.Any())
                    {
                        Logger.Instance().Log(TraceLevel.Info, "{0}: Component {1}#{2} was found on build server", ResolverType, name, version);
                        return true;
                    }
                }

                Logger.Instance().Log(TraceLevel.Warning, "{0}: Component {1}#{2} was not found on build server", ResolverType, name, version);
                return false;
            }
            catch (BuildDefinitionNotFoundException)
            {
                Logger.Instance().Log(TraceLevel.Warning, "{0}: Component {1}#{2} was not found on build server", ResolverType, name, version);
                return false;
            }
        }

        #region Helpers

        /// <summary>
        /// Validates the component name and check path for wildcards.
        /// </summary>
        /// <param name="name">The component name.</param>
        private static void ValidateComponentName(IComponentName name)
        {
            if (name == null)
            {
                Logger.Instance().Log(TraceLevel.Error, "Build result component name was null");
                throw new ArgumentNullException("name", "Build result component name was null");
            }

            if (string.IsNullOrEmpty(name.ToString()))
            {
                Logger.Instance().Log(TraceLevel.Error, "Team project and/or build definition for component {0} was empty", name);
                throw new ArgumentException(string.Format("Team project and/or build definition for component {0} was empty", name), "name");
            }
        }

        /// <summary>
        /// Validates the component version and check if version number is invalid.
        /// </summary>
        /// <param name="version">The component version.</param>
        private static void ValidateComponentVersion(IComponentVersion version)
        {
            if (version == null)
            {
                Logger.Instance().Log(TraceLevel.Error, "Version for build result component was null");
                throw new ArgumentNullException("version", "Version for build result component was null");
            }

            if (string.IsNullOrEmpty(version.BuildNumber) && (version.BuildQuality == null || !version.BuildQuality.Any()) && (version.BuildStatus == null || !version.BuildStatus.Any()))
            {
                Logger.Instance().Log(TraceLevel.Error, "Version number for build result component was invalid");
                throw new ArgumentException("Version number for build result component was invalid", "version");
            }
        }

        #endregion
    }
}
