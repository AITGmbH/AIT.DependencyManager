using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.Linq;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.TeamFoundation.VersionControl.Common;
using AIT.DMF.Common;
using AIT.DMF.Contracts.Exceptions;
using AIT.DMF.Contracts.Provider;
using AIT.DMF.Contracts.Common;
using AIT.DMF.Contracts.Services;

namespace AIT.DMF.Plugins.Resolver.SourceControl
{
    using System.Linq;

    public class ResolverSourceControl : IDependencyResolver
    {
        #region Private Properties

        private List<string> ValidDependencyDefinitionFileNames { get; set; }
        private VersionControlServer VersionControlServer { get; set; }

        #endregion

        #region Public properties

        /// <summary>
        /// Returns the type of the source control provider.
        /// </summary>
        public string ResolverType { get; private set; }

        /// <summary>
        /// Returns the source control resolver settings.
        /// </summary>
        public ISettings<ResolverValidSettings> ResolverSettings { get; private set; }

        /// <summary>
        /// Returns the component targets name.
        /// </summary>
        public string ComponentTargetsName { get; private set; }

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="ResolverSourceControl"/> class resolver type and resolver settings.
        /// </summary>
        /// <param name="settings">
        /// The resolver settings.
        /// </param>
        /// <param name="resolverType">
        /// The specific source control resolver type.
        /// </param>
        public ResolverSourceControl(ISettings<ResolverValidSettings> settings, string resolverType)
        {
            if (string.IsNullOrEmpty(resolverType))
            {
                throw new ArgumentNullException("resolverType", "No resolver type was supplied!");
            }

            ResolverType = resolverType;
            Logger.Instance().Log(TraceLevel.Info, "Initializing resolver {0} ...", ResolverType);

            if (settings == null)
            {
                throw new ArgumentNullException("settings", "No resolver settings were supplied");
            }

            if (string.IsNullOrEmpty(settings.GetSetting(ResolverValidSettings.TeamProjectCollectionUrl)))
            {
                throw new InvalidProviderConfigurationException("No team project collection url was supplied!");
            }

            if (string.IsNullOrEmpty(settings.GetSetting(ResolverValidSettings.DependencyDefinitionFileNameList)))
            {
                throw new InvalidProviderConfigurationException(string.Format("No dependency definition file list was specified"));
            }

            ValidDependencyDefinitionFileNames = settings.GetSetting(ResolverValidSettings.DependencyDefinitionFileNameList).Split(new[] { ';' }).ToList();
            ComponentTargetsName = ValidDependencyDefinitionFileNames.First();
            ResolverSettings = settings;

            var tpc = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(new Uri(ResolverSettings.GetSetting(ResolverValidSettings.TeamProjectCollectionUrl)));
            tpc.EnsureAuthenticated();
            VersionControlServer = tpc.GetService<VersionControlServer>();
            if (VersionControlServer == null)
            {
                Logger.Instance().Log(TraceLevel.Error, "{0}: Could not get VersionControlServer service for {1}", ResolverType, ResolverSettings.GetSetting(ResolverValidSettings.TeamProjectCollectionUrl));
                throw new InvalidProviderConfigurationException(string.Format("Could not get VersionControlServer service for {0} in {1}", ResolverSettings.GetSetting(ResolverValidSettings.TeamProjectCollectionUrl), ResolverType));
            }

            Logger.Instance().Log(TraceLevel.Info, "Resolver {0} successfully initialized", ResolverType);
        }

        /// <summary>
        /// The source control resolver does not support querying component versions.
        /// </summary>
        /// <returns>An <see cref="NotImplementedException" /> exception is thrown</returns>
        public IEnumerable<IComponentName> GetAvailableComponentNames()
        {
            Logger.Instance().Log(TraceLevel.Error, "{0}: Querying available components is not supported", ResolverType);
            throw new NotImplementedException(string.Format("Querying available components is not supported by the {0}", ResolverType));
        }

        /// <summary>
        /// The source control resolver does not support querying component versions.
        /// </summary>
        /// <param name="name">The name of the component</param>
        /// <returns>An InvalidComponentException exception is thrown</returns>
        public IEnumerable<IComponentVersion> GetAvailableVersions(IComponentName name)
        {
            Logger.Instance().Log(TraceLevel.Error, "{0}: Querying available component versions is not supported", ResolverType);
            throw new NotImplementedException(string.Format("Querying available component versions is not supported by the {0}", ResolverType));
        }

        /// <summary>
        /// Loads a specific dependency definition file.
        /// </summary>
        /// <param name="name">The source control folder path for the component.</param>
        /// <param name="version">The component version.</param>
        /// <returns>The loaded dependency definition xml file</returns>
        public XDocument LoadComponentTarget(IComponentName name, IComponentVersion version)
        {
            ValidateComponentName(name);
            ValidateComponentVersion(version);

            foreach (var dependencyDefinitionFileName in ValidDependencyDefinitionFileNames)
            {
                var dependencyDefinitionFileLocation = VersionControlPath.Combine(name.ToString(), dependencyDefinitionFileName);
                if (!VersionControlServer.ServerItemExists(dependencyDefinitionFileLocation, version.TfsVersionSpec, DeletedState.Any, ItemType.File))
                {
                    Logger.Instance().Log(TraceLevel.Verbose, "{0}: Dependency definition file {1} for component {2}#{3} was not found", ResolverType, dependencyDefinitionFileLocation, name, version);
                    continue;
                }

                var item = VersionControlServer.GetItem(dependencyDefinitionFileLocation, version.TfsVersionSpec, DeletedState.Any, true);
                using (var content = item.DownloadFile())
                {
                    var xdoc = XDocument.Load(content);
                    Logger.Instance().Log(TraceLevel.Info, "{0}: Loading dependency definition file {1} for component {2}#{3} finished successfully", ResolverType, dependencyDefinitionFileLocation, name, version);
                    return xdoc;
                }
            }

            return null;
        }

        /// <summary>
        /// Determins whether a branch folder exists
        /// </summary>
        /// <param name="name">The name of the component (branch folder)</param>
        /// <returns>true if the branch folder exists; false if not</returns>
        public bool ComponentExists(IComponentName name)
        {
            ValidateComponentName(name);

            // Check if folder exists in source control
            if (!VersionControlServer.ServerItemExists(name.ToString(), VersionSpec.Latest, DeletedState.NonDeleted, ItemType.Folder))
            {
                Logger.Instance().Log(TraceLevel.Warning, "{0}: Component folder for {1} was not found in source control", ResolverType, name);
                return false;
            }

            // Check if dependency definition file exists inside folder
            var folderItems = VersionControlServer.GetItems(VersionControlPath.Combine(name.ToString(), "*"));
            foreach (var item in folderItems.Items)
            {
                var itemName = VersionControlPath.GetFileName(item.ServerItem);

                foreach (var dependencyDefinitionFileName in ValidDependencyDefinitionFileNames)
                {
                    if (string.Equals(itemName, dependencyDefinitionFileName, StringComparison.OrdinalIgnoreCase))
                    {
                        Logger.Instance().Log(TraceLevel.Info, "{0}: Component folder for component {1} was found in source control", ResolverType, name);
                        return true;
                    }
                }
            }

            Logger.Instance().Log(TraceLevel.Warning, "{0}: Component folder for component {1} was not found in source control", ResolverType, name);
            return false;
        }

        /// <summary>
        /// Determines whether a version control folder exists having a specific version
        /// </summary>
        /// <param name="name">The name of the component</param>
        /// <param name="version">The name of the component (branch folder)</param>
        /// <returns>true if the branch folder exists at the version; false otherwise</returns>
        public bool ComponentExists(IComponentName name, IComponentVersion version)
        {
            ValidateComponentName(name);
            ValidateComponentVersion(version);

            // Check if folder exists in source control
            if (!VersionControlServer.ServerItemExists(name.ToString(), version.TfsVersionSpec, DeletedState.NonDeleted, ItemType.Folder))
            {
                Logger.Instance().Log(TraceLevel.Warning, "{0}: Component {1}#{2} was not found in source control", ResolverType, name, version);
                return false;
            }

            // Check if component.targets exists inside folder
            var folderItems = VersionControlServer.GetItems(VersionControlPath.Combine(name.ToString(), "*"), version.TfsVersionSpec, RecursionType.OneLevel);
            foreach (Item item in folderItems.Items)
            {
                var itemName = VersionControlPath.GetFileName(item.ServerItem);

                foreach (var dependencyDefinitionFileName in ValidDependencyDefinitionFileNames)
                {
                    if (string.Equals(itemName, dependencyDefinitionFileName, StringComparison.OrdinalIgnoreCase))
                    {
                        Logger.Instance().Log(TraceLevel.Info, "{0}: Component {1}#{2} was found in source control", ResolverType, name, version);
                        return true;
                    }
                }
            }

            Logger.Instance().Log(TraceLevel.Warning, "{0}: Component {1}#{2} was not found in source control", ResolverType, name, version);
            return false;
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
                Logger.Instance().Log(TraceLevel.Error, "Source control component was null");
                throw new ArgumentNullException("name", "Source control component was null");
            }

            if (string.IsNullOrEmpty(name.Path) || VersionControlPath.IsWildcard(name.Path))
            {
                Logger.Instance().Log(TraceLevel.Error, "Source control path for component {0} was empty or contained wildcards", name);
                throw new ArgumentException(string.Format("Source control path for component {0} was empty or contained wildcards", name), "name");
            }
        }

        /// <summary>
        /// Validates the component version and check if version spec is invalid.
        /// </summary>
        /// <param name="version">The component version.</param>
        private static void ValidateComponentVersion(IComponentVersion version)
        {
            if (version == null)
            {
                Logger.Instance().Log(TraceLevel.Error, "Version for source control component was null");
                throw new ArgumentNullException("version", "Version for source control component was null");
            }

            if (version.TfsVersionSpec == null)
            {
                Logger.Instance().Log(TraceLevel.Error, "Version spec for source control component was invalid");
                throw new ArgumentException("Version spec for source control component was invalid", "version");
            }
        }

        #endregion
    }
}
