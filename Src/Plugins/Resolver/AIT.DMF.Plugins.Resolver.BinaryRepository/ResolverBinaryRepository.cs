// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ResolverBinaryRepository.cs" company="AIT GmbH & Co. KG">
//   All rights reserved by AIT GmbH & Co. KG
// </copyright>
// <summary>
//   Defines the ResolverBinaryRepository type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace AIT.DMF.Plugins.Resolver.BinaryRepository
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Xml.Linq;
    using Common;
    using Contracts.Common;
    using Contracts.Exceptions;
    using Contracts.Provider;
    using Contracts.Services;
    using Microsoft.TeamFoundation.Client;
    using Microsoft.TeamFoundation.VersionControl.Client;
    using Microsoft.TeamFoundation.VersionControl.Common;

    /// <summary>
    /// The resolver for BinaryRepository dependencies.
    /// </summary>
    public class ResolverBinaryRepository : IDependencyResolver
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ResolverBinaryRepository"/> class with resolver settings.
        /// </summary>
        /// <param name="settings">The resolver settings.</param>
        public ResolverBinaryRepository(ISettings<ResolverValidSettings> settings)
        {
            ResolverType = "Resolver_BinaryRepository";
            Logger.Instance().Log(TraceLevel.Info, "Initializing resolver {0} ...", ResolverType);

            if (settings == null)
            {
                throw new InvalidProviderConfigurationException();
            }

            if (string.IsNullOrEmpty(settings.GetSetting(ResolverValidSettings.BinaryTeamProjectCollectionUrl)))
            {
                throw new InvalidProviderConfigurationException(string.Format("Team project collection was not specified"));
            }

            if (string.IsNullOrEmpty(settings.GetSetting(ResolverValidSettings.BinaryRepositoryTeamProject)))
            {
                throw new InvalidProviderConfigurationException(string.Format("No repository team project was specified"));
            }

            if (string.IsNullOrEmpty(settings.GetSetting(ResolverValidSettings.DependencyDefinitionFileNameList)))
            {
                throw new InvalidProviderConfigurationException(string.Format("No dependency definition file list was specified"));
            }

            ResolverSettings = settings;
            ValidDependencyDefinitionFileNames = settings.GetSetting(ResolverValidSettings.DependencyDefinitionFileNameList).Split(new[] { ';' }).ToList();
            ComponentTargetsName = ValidDependencyDefinitionFileNames.First();
            PathPrefix = VersionControlPath.RootFolder + settings.GetSetting(ResolverValidSettings.BinaryRepositoryTeamProject);

            var tpc = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(new Uri(ResolverSettings.GetSetting(ResolverValidSettings.BinaryTeamProjectCollectionUrl)));
            tpc.EnsureAuthenticated();
            VersionControlServer = tpc.GetService<VersionControlServer>();
            if (VersionControlServer == null)
            {
                Logger.Instance().Log(TraceLevel.Error, "{0}: Could not get VersionControlServer service for {1}", ResolverType, ResolverSettings.GetSetting(ResolverValidSettings.BinaryTeamProjectCollectionUrl));
                throw new InvalidProviderConfigurationException(string.Format("Could not get VersionControlServer service for {0} in {1}", ResolverSettings.GetSetting(ResolverValidSettings.BinaryTeamProjectCollectionUrl), ResolverType));
            }

            if (!VersionControlServer.ServerItemExists(PathPrefix, VersionSpec.Latest, DeletedState.NonDeleted, ItemType.Folder))
            {
                Logger.Instance().Log(TraceLevel.Error, "{0}: Source control folder for binary repository {1} does not exist", ResolverType, ResolverSettings.GetSetting(ResolverValidSettings.BinaryRepositoryTeamProject));
                throw new InvalidProviderConfigurationException(string.Format("Could not find binary repository {0} in version control", ResolverSettings.GetSetting(ResolverValidSettings.BinaryRepositoryTeamProject)));
            }

            Logger.Instance().Log(TraceLevel.Info, "Resolver {0} successfully initialized", ResolverType);
        }

        #region Public Properties

        /// <summary>
        /// Gets the type of the binary repository provider.
        /// </summary>
        public string ResolverType { get; private set; }

        /// <summary>
        /// Gets the binary repository resolver settings.
        /// </summary>
        public ISettings<ResolverValidSettings> ResolverSettings { get; private set; }

        /// <summary>
        /// Gets the component targets name.
        /// </summary>
        public string ComponentTargetsName { get; private set; }

        #endregion

        #region Private Properties

        /// <summary>
        /// Gets or sets the valid dependency definition file names.
        /// </summary>
        /// <value>
        /// The valid dependency definition file names.
        /// </value>
        private List<string> ValidDependencyDefinitionFileNames { get; set; }

        /// <summary>
        /// Gets or sets the version control server.
        /// </summary>
        /// <value>
        /// The version control server.
        /// </value>
        private VersionControlServer VersionControlServer { get; set; }

        /// <summary>
        /// Gets or sets the path prefix.
        /// </summary>
        /// <value>
        /// The path prefix.
        /// </value>
        private string PathPrefix { get; set; }

        #endregion

        /// <summary>
        /// Returns all components found in the repository.
        /// </summary>
        /// <returns>List with components</returns>
        public IEnumerable<IComponentName> GetAvailableComponentNames()
        {
            var availableComponents = new List<IComponentName>();

            Logger.Instance().Log(TraceLevel.Info, "{0}: Querying available components...", ResolverType);

            var items = VersionControlServer.GetItems(VersionControlPath.Combine(PathPrefix, "*"));

            foreach (var item in items.Items)
            {
                if (item.ItemType.Equals(ItemType.Folder))
                {
                    var componentName = VersionControlPath.GetFileName(item.ServerItem);

                    if (!string.Equals(componentName, "BuildProcessTemplates", StringComparison.OrdinalIgnoreCase))
                    {
                        availableComponents.Add(new ComponentName(componentName));
                        Logger.Instance().Log(TraceLevel.Info, "{0}: Found component {1}", ResolverType, componentName);
                    }
                }
            }

            Logger.Instance().Log(TraceLevel.Info, "{0}: Querying components finished successfully", ResolverType);

            return availableComponents;
        }

        /// <summary>
        /// Returns all versions for a component found in the repository.
        /// </summary>
        /// <param name="name">The component name.</param>
        /// <returns>The list of versions</returns>
        public IEnumerable<IComponentVersion> GetAvailableVersions(IComponentName name)
        {
            ValidateComponentName(name);

            Logger.Instance().Log(TraceLevel.Info, "{0}: Querying available component versions...", ResolverType);

            var availableVersions = new List<IComponentVersion>();

            if (VersionControlServer.ServerItemExists(VersionControlPath.Combine(PathPrefix, name.ToString()), VersionSpec.Latest, DeletedState.NonDeleted, ItemType.Folder))
            {
                var versionItems = VersionControlServer.GetItems(VersionControlPath.Combine(VersionControlPath.Combine(PathPrefix, name.ToString()), "*"));

                foreach (var version in versionItems.Items)
                {
                    if (version.ItemType.Equals(ItemType.Folder))
                    {
                        // Check if component.targets exists inside folder
                        var folderItems = VersionControlServer.GetItems(VersionControlPath.Combine(VersionControlPath.Combine(VersionControlPath.Combine(PathPrefix, name.ToString()), VersionControlPath.GetFileName(version.ServerItem)), "*"));
                        var dependencyDefinitionFileFound = false;

                        foreach (var item in folderItems.Items)
                        {
                            var itemName = VersionControlPath.GetFileName(item.ServerItem);

                            foreach (var dependencyDefinitionFileName in ValidDependencyDefinitionFileNames)
                            {
                                if (string.Equals(itemName, dependencyDefinitionFileName, StringComparison.OrdinalIgnoreCase))
                                {
                                    Logger.Instance().Log(TraceLevel.Info, "{0}: Found version {1}", ResolverType, VersionControlPath.GetFileName(version.ServerItem));
                                    availableVersions.Add(new ComponentVersion(VersionControlPath.GetFileName(version.ServerItem)));
                                    dependencyDefinitionFileFound = true;
                                }
                            }
                        }

                        if (!dependencyDefinitionFileFound)
                        {
                            Logger.Instance().Log(TraceLevel.Warning, "{0}: Skipping version {1} (Dependency definition file is not present)", ResolverType, VersionControlPath.GetFileName(version.ServerItem));
                        }
                    }
                }
            }
            else
            {
                Logger.Instance().Log(TraceLevel.Error, "{0}: Directory {1} for component {2} does not exist", ResolverType, VersionControlPath.Combine(PathPrefix, name.ToString()), name);
                throw new InvalidComponentException(string.Format("Could not find component {0} in binary repository", name));
            }

            Logger.Instance().Log(TraceLevel.Info, "{0}: Querying component versions finished successfully", ResolverType);

            return availableVersions;
        }

        /// <summary>
        /// Loads a specific dependency definition file.
        /// </summary>
        /// <param name="name">The name of the binary repository component.</param>
        /// <param name="version">The component version.</param>
        /// <returns>The loaded dependency definition xml file</returns>
        public XDocument LoadComponentTarget(IComponentName name, IComponentVersion version)
        {
            ValidateComponentName(name);
            ValidateComponentVersion(version);

            foreach (var dependencyDefinitionFileName in ValidDependencyDefinitionFileNames)
            {
                var dependencyDefinitionFileLocation = VersionControlPath.Combine(VersionControlPath.Combine(VersionControlPath.Combine(PathPrefix, name.ToString()), version.ToString()), dependencyDefinitionFileName);

                if (!VersionControlServer.ServerItemExists(dependencyDefinitionFileLocation, ItemType.File))
                {
                    Logger.Instance().Log(TraceLevel.Verbose, "{0}: Dependency definition file {1} for component {2}#{3} was not found", ResolverType, dependencyDefinitionFileLocation, name, version);
                    continue;
                }

                var dependencyDefinitionFileStream = VersionControlServer.GetItem(dependencyDefinitionFileLocation, VersionSpec.Latest).DownloadFile();
                var xdoc = XDocument.Load(dependencyDefinitionFileStream);

                // Close the previously opened filestream to ensure a cleanup
                dependencyDefinitionFileStream.Close();

                Logger.Instance().Log(TraceLevel.Info, "{0}: Loading dependency definition file {1} for component {2}#{3} finished successfully", ResolverType, dependencyDefinitionFileLocation, name, version);
                return xdoc;
            }

            return null;
        }

        /// <summary>
        /// Determines whether a component folder exists
        /// </summary>
        /// <param name="name">The name of the component</param>
        /// <returns>True if the branch folder exists; false if not</returns>
        public bool ComponentExists(IComponentName name)
        {
            ValidateComponentName(name);

            // Check if folder exists in source control
            if (VersionControlServer.ServerItemExists(VersionControlPath.Combine(PathPrefix, name.Path), VersionSpec.Latest, DeletedState.NonDeleted, ItemType.Folder))
            {
                Logger.Instance().Log(TraceLevel.Info, "{0}: Component folder for {1} was found in binary repository", ResolverType, name);
                return true;
            }

            Logger.Instance().Log(TraceLevel.Warning, "{0}: Component folder for {1} was not found in binary repository", ResolverType, name);
            return false;
        }

        /// <summary>
        /// Determines whether a version control folder exists having a specific version
        /// </summary>
        /// <param name="name">The name of the component</param>
        /// <param name="version">The version for the component</param>
        /// <returns>True if a version folder exists at the version; false otherwise</returns>
        public bool ComponentExists(IComponentName name, IComponentVersion version)
        {
            ValidateComponentName(name);
            ValidateComponentVersion(version);

            if (VersionControlServer.ServerItemExists(VersionControlPath.Combine(PathPrefix, name.ToString()), VersionSpec.Latest, DeletedState.NonDeleted, ItemType.Folder))
            {
                var path = VersionControlPath.Combine(VersionControlPath.Combine(PathPrefix, name.ToString()), version.ToString());

                if (VersionControlServer.ServerItemExists(path, VersionSpec.Latest, DeletedState.NonDeleted, ItemType.Folder))
                {
                    // Check if component.targets exists inside folder
                    var folderItems = VersionControlServer.GetItems(VersionControlPath.Combine(path, "*"));
                    foreach (var item in folderItems.Items)
                    {
                        var itemName = VersionControlPath.GetFileName(item.ServerItem);

                        foreach (var dependencyDefinitionFileName in ValidDependencyDefinitionFileNames)
                        {
                            if (string.Equals(itemName, dependencyDefinitionFileName, StringComparison.OrdinalIgnoreCase))
                            {
                                Logger.Instance().Log(TraceLevel.Info, "{0}: Component {1}#{2} was found in binary repository", ResolverType, name, version);
                                return true;
                            }

                            Logger.Instance().Log(TraceLevel.Verbose, "{0}: Dependency definition file {1} for component {2}#{3} was not found in binary repository", ResolverType, dependencyDefinitionFileName, name, version);
                        }
                    }
                }
            }

            Logger.Instance().Log(TraceLevel.Warning, "{0}: Component {1}#{2} was not found in binary repository", ResolverType, name, version);
            return false;
        }

        #region Helpers

        /// <summary>
        /// Validates the component name.
        /// </summary>
        /// <param name="name">The component name.</param>
        private static void ValidateComponentName(IComponentName name)
        {
            if (name == null)
            {
                Logger.Instance().Log(TraceLevel.Error, "Source control component was null");
                throw new ArgumentNullException("name", "Source control component was null");
            }

            if (string.IsNullOrEmpty(name.ToString()))
            {
                Logger.Instance().Log(TraceLevel.Error, "Source control path for binary repository component {0} was empty", name);
                throw new ArgumentException(string.Format("Source control path for binary repository component {0} was empty", name), "name");
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
                Logger.Instance().Log(TraceLevel.Error, "Version for binary repository component was null");
                throw new ArgumentNullException("version", "Version for binary repository component was null");
            }

            if (string.IsNullOrEmpty(version.ToString()))
            {
                Logger.Instance().Log(TraceLevel.Error, "Version number for binary repository component was invalid");
                throw new ArgumentException("Version number for binary repository component was invalid", "version");
            }
        }

        #endregion
    }
}
