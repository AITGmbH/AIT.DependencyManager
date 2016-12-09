// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ResolverFileShare.cs" company="AIT GmbH & Co. KG">
//   All rights reserved by AIT GmbH & Co. KG
// </copyright>
// <summary>
//   Defines the ResolverFileShare type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace AIT.DMF.Plugins.Resolver.FileShare
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

    /// <summary>
    /// The file share resolver.
    /// </summary>
    public class ResolverFileShare : IDependencyResolver
    {
        #region Private members

        /// <summary>
        /// Defines the valid dependency definition file names.
        /// </summary>
        private readonly List<string> _validDependencyDefinitionFileNames;

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="ResolverFileShare"/> class with resolver settings.
        /// </summary>
        /// <param name="settings">The resolver settings.</param>
        public ResolverFileShare(ISettings<ResolverValidSettings> settings)
        {
            ResolverType = "Resolver_FileShare";
            Logger.Instance().Log(TraceLevel.Info, "Initializing resolver {0} ...", ResolverType);

            if (settings == null)
            {
                throw new ArgumentNullException("settings", "No resolver settings were supplied");
            }

            if (string.IsNullOrEmpty(settings.GetSetting(ResolverValidSettings.FileShareUrl)))
            {
                throw new InvalidProviderConfigurationException(string.Format("File share url was not supplied"));
            }

            if (!Directory.Exists(settings.GetSetting(ResolverValidSettings.FileShareUrl)))
            {
                Logger.Instance().Log(TraceLevel.Error, "{0}: Could not connect to file share {1}", ResolverType, settings.GetSetting(ResolverValidSettings.FileShareUrl));
                throw new InvalidProviderConfigurationException(string.Format("Could not connect to file share {0}", settings.GetSetting(ResolverValidSettings.FileShareUrl)));
            }

            if (string.IsNullOrEmpty(settings.GetSetting(ResolverValidSettings.DependencyDefinitionFileNameList)))
            {
                throw new InvalidProviderConfigurationException(string.Format("No dependency definition file list was specified"));
            }

            ResolverSettings = settings;
            _validDependencyDefinitionFileNames = settings.GetSetting(ResolverValidSettings.DependencyDefinitionFileNameList).Split(new[] { ';' }).ToList();
            ComponentTargetsName = _validDependencyDefinitionFileNames.First();

            Logger.Instance().Log(TraceLevel.Info, "Resolver {0} successfully initialized", ResolverType);
        }

        #region Public properties

        /// <summary>
        /// Gets the type of the file share provider.
        /// </summary>
        public string ResolverType { get; private set; }

        /// <summary>
        /// Gets the resolver settings.
        /// </summary>
        public ISettings<ResolverValidSettings> ResolverSettings { get; private set; }

        /// <summary>
        /// Gets the component targets name.
        /// </summary>
        public string ComponentTargetsName { get; private set; }

        #endregion

        /// <summary>
        /// Discover all available component names in the file share.
        /// </summary>
        /// <returns>Returns a list with all component names</returns>
        public IEnumerable<IComponentName> GetAvailableComponentNames()
        {
            var componentNames = new List<IComponentName>();

            Logger.Instance().Log(TraceLevel.Info, "{0}: Querying available components...", ResolverType);

            try
            {
                foreach (var componentPath in Directory.GetDirectories(ResolverSettings.GetSetting(ResolverValidSettings.FileShareUrl)))
                {
                    var componentName = componentPath.Split(Path.DirectorySeparatorChar).Last();
                    componentNames.Add(new ComponentName(componentName));
                    Logger.Instance().Log(TraceLevel.Info, "{0}: Found component {1}", ResolverType, componentName);
                }
            }
            catch (UnauthorizedAccessException uae)
            {
                Logger.Instance().Log(
                    TraceLevel.Error,
                    "{0}: Could not access file share {1} (Unauthorized access exception {2})",
                    ResolverType,
                    ResolverSettings.GetSetting(ResolverValidSettings.FileShareUrl),
                    uae.Message);
                throw new InvalidAccessRightsException(string.Format("Could not access file share ({0})", ResolverSettings.GetSetting(ResolverValidSettings.FileShareUrl)));
            }

            Logger.Instance().Log(TraceLevel.Info, "{0}: Querying components finished successfully", ResolverType);

            return componentNames;
        }

        /// <summary>
        /// Discover all available versions for a specific component.
        /// </summary>
        /// <param name="componentName">The name of the component</param>
        /// <returns>A list with all versions</returns>
        public IEnumerable<IComponentVersion> GetAvailableVersions(IComponentName componentName)
        {
            var componentVersions = new List<IComponentVersion>();
            var componentPath = Path.Combine(ResolverSettings.GetSetting(ResolverValidSettings.FileShareUrl), componentName.Path);

            Logger.Instance().Log(TraceLevel.Info, "{0}: Querying available component versions...", ResolverType);

            try
            {
                if (Directory.Exists(componentPath))
                {
                    foreach (var versionPath in Directory.GetDirectories(componentPath))
                    {
                        var componentVersion = versionPath.Split(Path.DirectorySeparatorChar).Last();

                        foreach (var dependencyDefinitionFileName in _validDependencyDefinitionFileNames)
                        {
                            var dependencyDefinitionFile = Path.Combine(versionPath, dependencyDefinitionFileName);

                            if (File.Exists(dependencyDefinitionFile))
                            {
                                Logger.Instance().Log(TraceLevel.Info, "{0}: Found version {1}", ResolverType, componentVersion);
                                componentVersions.Add(new ComponentVersion(componentVersion));
                            }
                        }

                        Logger.Instance().Log(TraceLevel.Warning, "{0}: Skipping version {1} (Dependency definition file is not present)", ResolverType, componentVersion);
                    }
                }
                else
                {
                    Logger.Instance().Log(TraceLevel.Error, "{0}: Directory {1} for component {2} does not exist", ResolverType, componentPath, componentName);
                    throw new InvalidComponentException(string.Format("Could not find component {0} on file share", componentName));
                }
            }
            catch (UnauthorizedAccessException uae)
            {
                Logger.Instance().Log(
                    TraceLevel.Error,
                    "{0}: Could not access file share {1} (Unauthorized access exception {2})",
                    ResolverType,
                    ResolverSettings.GetSetting(ResolverValidSettings.FileShareUrl),
                    uae.Message);
                throw new InvalidAccessRightsException(string.Format("Could not access file share ({0})", ResolverSettings.GetSetting(ResolverValidSettings.FileShareUrl)));
            }

            Logger.Instance().Log(TraceLevel.Info, "{0}: Querying component versions finished successfully", ResolverType);

            return componentVersions;
        }

        /// <summary>
        /// Loads a specific dependency definition file.
        /// </summary>
        /// <param name="name">The name of the component</param>
        /// <param name="version">The exact version of the component</param>
        /// <returns>The loaded dependency definition xml file</returns>
        public XDocument LoadComponentTarget(IComponentName name, IComponentVersion version)
        {
            ValidateComponentName(name);

            foreach (var dependencyDefinitionFileName in _validDependencyDefinitionFileNames)
            {
                var dependencyDefinitionFileLocation = Path.Combine(ResolverSettings.GetSetting(ResolverValidSettings.FileShareUrl), name.ToString(), version.ToString(), dependencyDefinitionFileName);
                if (!File.Exists(dependencyDefinitionFileLocation))
                {
                    Logger.Instance().Log(TraceLevel.Verbose, "{0}: Dependency definition file {1} for component {2}#{3} was not found", ResolverType, dependencyDefinitionFileLocation, name, version);
                    continue;
                }

                var xdoc = XDocument.Load(dependencyDefinitionFileLocation);

                Logger.Instance().Log(TraceLevel.Info, "{0}: Loading dependency definition file {1} for component {2}#{3} finished successfully", ResolverType, dependencyDefinitionFileLocation, name, version);
                return xdoc;
            }

            return null;
        }

        /// <summary>
        /// Determines whether a component exists
        /// </summary>
        /// <param name="name">The name of the component</param>
        /// <returns>true if the component exists; false if not</returns>
        public bool ComponentExists(IComponentName name)
        {
            ValidateComponentName(name);

            if (Directory.Exists(Path.Combine(ResolverSettings.GetSetting(ResolverValidSettings.FileShareUrl), name.ToString())))
            {
                Logger.Instance().Log(TraceLevel.Info, "{0}: Component folder for component {1} was found on file share", ResolverType, name);
                return true;
            }

            Logger.Instance().Log(TraceLevel.Warning, "{0}: Component folder for component {1} was not found on file share", ResolverType, name);
            return false;
        }

        /// <summary>
        /// Determines whether a component exists having a specific version
        /// </summary>
        /// <param name="name">The name of the component</param>
        /// <param name="version">The specific version of the component</param>
        /// <returns>True if the component exists at the version; false otherwise</returns>
        public bool ComponentExists(IComponentName name, IComponentVersion version)
        {
            ValidateComponentName(name);
            ValidateComponentVersion(version);

            var componentVersionPath = Path.Combine(ResolverSettings.GetSetting(ResolverValidSettings.FileShareUrl), name.ToString(), version.ToString());

            if (Directory.Exists(componentVersionPath))
            {
                // ReSharper disable LoopCanBeConvertedToQuery
                foreach (var dependencyDefinitionFileName in _validDependencyDefinitionFileNames)
                // ReSharper restore LoopCanBeConvertedToQuery
                {
                    var dependencyDefinitionFile = Path.Combine(componentVersionPath, dependencyDefinitionFileName);
                    if (File.Exists(dependencyDefinitionFile))
                    {
                        Logger.Instance().Log(TraceLevel.Info, "{0}: Component {1}#{2} was found on file share", ResolverType, name, version);
                        return true;
                    }
                }
            }

            Logger.Instance().Log(TraceLevel.Warning, "{0}: Component {1}#{2} was not found on file share", ResolverType, name, version);
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
                Logger.Instance().Log(TraceLevel.Error, "File share component name was null");
                throw new ArgumentNullException("name", "File share component name was null");
            }

            if (string.IsNullOrEmpty(name.ToString()))
            {
                Logger.Instance().Log(TraceLevel.Error, "File share path for component {0} was empty", name);
                throw new ArgumentException(string.Format("File share path for component {0} was empty", name), "name");
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
                Logger.Instance().Log(TraceLevel.Error, "Version for file share component was null");
                throw new ArgumentNullException("version", "Version for file share component was null");
            }

            if (version.Version == null)
            {
                Logger.Instance().Log(TraceLevel.Error, "Version number for file share component was invalid");
                throw new ArgumentException("Version number for file share component was invalid", "version");
            }
        }

        #endregion
    }
}
