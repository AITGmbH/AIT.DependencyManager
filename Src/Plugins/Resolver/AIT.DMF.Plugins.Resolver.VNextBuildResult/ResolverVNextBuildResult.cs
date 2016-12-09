using AIT.DMF.Common;
using AIT.DMF.Contracts.Common;
using AIT.DMF.Contracts.Exceptions;
using AIT.DMF.Contracts.Provider;
using AIT.DMF.Contracts.Services;
using Microsoft.TeamFoundation.Build.Client;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.VisualStudio.Services.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;
using TFSWebApi = Microsoft.TeamFoundation.Build.WebApi;

namespace AIT.DMF.Plugins.Resolver.VNextBuildResult
{
    public class ResolverVNextBuildResult : IDependencyResolver
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
        ///  Type needed to exposes the members.
        /// </summary>
        ///
        private readonly VssConnection _connection;

        /// <summary>
        /// The http build client.
        /// </summary>
        private readonly TFSWebApi.BuildHttpClient _client;

        #endregion Private Members

        public ResolverVNextBuildResult(ISettings<ResolverValidSettings> settings)
        {
            ResolverType = "Resolver_BuildResultJSON";
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

            var tpcUrl = new Uri(settings.GetSetting(ResolverValidSettings.TeamProjectCollectionUrl));
            _connection = new VssConnection(tpcUrl, new VssClientCredentials(true));
            _client = _connection.GetClient<TFSWebApi.BuildHttpClient>();

            // Connect to tfs server
            var tpc = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(tpcUrl);
            tpc.EnsureAuthenticated();

            // Connect to version control service & build server
            _versionControlServer = tpc.GetService<VersionControlServer>();
            if (_versionControlServer == null)
            {
                Logger.Instance().Log(TraceLevel.Error, "{0}: Could not get VersionControlServer service for {1}", ResolverType, ResolverSettings.GetSetting(ResolverValidSettings.TeamProjectCollectionUrl));
                throw new InvalidProviderConfigurationException(string.Format("Could not get VersionControlServer service for {0} in {1}", ResolverSettings.GetSetting(ResolverValidSettings.TeamProjectCollectionUrl), ResolverType));
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

        #endregion Public Properties

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
                var buildDef = _client.GetDefinitionsAsync(project: tp.Name, type: TFSWebApi.DefinitionType.Build).Result;
                foreach (var bd in buildDef)
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

            var buildDef = (_client.GetDefinitionsAsync(project: teamProject.Name, name: name.BuildDefinition)).Result;

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
            var build = _client.GetBuildsAsync(teamProject.Name, definitions: new List<int> { buildDef.First().Id }, type: TFSWebApi.DefinitionType.Build).Result;

            foreach (var buildResult in build)
            {
                List<string> availableBuildTags = null;
                if (buildResult.Tags != null && buildResult.Tags.Count() > 0)
                {
                    availableBuildTags = buildResult.Tags;
                }
                var availableBuildStatus = new List<string> { buildResult.Status.ToString() };
                var vers = new ComponentVersion(buildResult.BuildNumber, availableBuildStatus, acceptedBuildQuality: null, acceptedBuildTags: availableBuildTags);
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

            var build = _client.GetBuildsAsync(project: name.TeamProject, buildNumber: version.BuildNumber, type: TFSWebApi.DefinitionType.Build).Result.First();
            var artifacts = _client.GetArtifactsAsync(name.TeamProject, build.Id).Result;

            foreach (var dependencyDefinitionFile in _dependencyDefinitionFileNameList)
            {
                if (!artifacts.Any())
                {
                    break;
                }
                else
                {
                    foreach (var artifact in artifacts)
                    {
                        if (artifact.Resource.Type == "FilePath")
                        {
                            var path = $"{artifact.Resource.Data}/{artifact.Name}/{dependencyDefinitionFile}";
                            if (File.Exists(path))
                            {
                                var xdoc = XDocument.Load(path);
                                Logger.Instance().Log(TraceLevel.Info, "{0}: Loading dependency definition file {1} for component {2}#{3} finished successfully", ResolverType, path, name, version);
                                return xdoc;
                            }
                            Logger.Instance().Log(TraceLevel.Verbose, "{0}: Dependency definition file {1} for component {2}#{3} was not found", ResolverType, path, name, version);
                        }
                        else
                        {
                            var content = _client.GetArtifactContentZipAsync(name.TeamProject, build.Id, artifact.Name);
                            using (ZipArchive archive = new ZipArchive(content.Result))
                            {

                                foreach (ZipArchiveEntry entry in archive.Entries)
                                {
                                    if (entry.Name.Equals(dependencyDefinitionFile, StringComparison.OrdinalIgnoreCase))
                                    {
                                        var xdoc = XDocument.Load(entry.Open());
                                        Logger.Instance().Log(TraceLevel.Info, "{0}: Loading dependency definition file {1} for component {2}#{3} finished successfully", ResolverType, entry.FullName, name, version);

                                        return xdoc;
                                    }
                                }
                            }
                        }
                    }
                }
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
                var buildDef = _client.GetDefinitionsAsync(project: name.TeamProject, type: TFSWebApi.DefinitionType.Build).Result;
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
        /// The build number or build build status determines the version.
        /// </summary>
        /// <param name="name">The component name (Team project with build definition)</param>
        /// <param name="version">The component version (Build number or build build status)</param>
        /// <returns>True if a build result exists with this the version; false otherwise</returns>
        public bool ComponentExists(IComponentName name, IComponentVersion version)
        {
            ValidateComponentName(name);
            ValidateComponentVersion(version);

            try
            {
                var buildDef = _client.GetDefinitionsAsync(project: name.TeamProject, type: TFSWebApi.DefinitionType.Build).Result;

                if (buildDef != null && !string.IsNullOrEmpty(version.BuildNumber))
                {
                    var builds = _client.GetBuildsAsync(project: name.TeamProject, buildNumber: version.BuildNumber, type: TFSWebApi.DefinitionType.Build).Result;

                    if (builds.Any())
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

            if (string.IsNullOrEmpty(version.BuildNumber) && (version.BuildStatus == null || !version.BuildStatus.Any()))
            {
                Logger.Instance().Log(TraceLevel.Error, "Version number for build result component was invalid");
                throw new ArgumentException("Version number for build result component was invalid", "version");
            }
        }

        #endregion Helpers
    }
}