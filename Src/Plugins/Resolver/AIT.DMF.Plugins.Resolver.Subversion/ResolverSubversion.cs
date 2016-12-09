using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.Linq;
using AIT.DMF.Common;
using AIT.DMF.Contracts.Exceptions;
using AIT.DMF.Contracts.Provider;
using AIT.DMF.Contracts.Common;
using AIT.DMF.Contracts.Services;

namespace AIT.DMF.Plugins.Resolver.Subversion
{
    using System.Linq;
    using Provider.Subversion;
    using SharpSvn;

    public class ResolverSubversion : IDependencyResolver
    {
        #region Private Properties

        private List<string> ValidDependencyDefinitionFileNames { get; set; }

        #endregion

        #region Public properties

        /// <summary>
        /// Returns the type of the subversion provider.
        /// </summary>
        public string ResolverType { get; private set; }

        /// <summary>
        /// Returns the subversion resolver settings.
        /// </summary>
        public ISettings<ResolverValidSettings> ResolverSettings { get; private set; }

        /// <summary>
        /// Returns the component targets name.
        /// </summary>
        public string ComponentTargetsName { get; private set; }

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="ResolverSubversionControl"/> class resolver type and resolver settings.
        /// </summary>
        /// <param name="settings">
        /// The resolver settings.
        /// </param>
        /// <param name="resolverType">
        /// The specific subversion resolver type.
        /// </param>
        public ResolverSubversion(ISettings<ResolverValidSettings> settings)
        {
            ResolverType = "Resolver_Subversion";
            Logger.Instance().Log(TraceLevel.Info, "Initializing resolver {0} ...", ResolverType);

            if (settings == null)
            {
                throw new ArgumentNullException("settings", "No resolver settings were supplied");
            }

            var subversionUrl = settings.GetSetting(ResolverValidSettings.SubversionUrl);

            if (string.IsNullOrEmpty(subversionUrl))
            {
                throw new InvalidProviderConfigurationException(string.Format("Subversion url was not supplied"));
            }

            try
            {
                if (!ProviderSubversion.Instance.ItemExists(subversionUrl))
                {
                    Logger.Instance().Log(TraceLevel.Error, "{0}: Folder not exists in Subversion {1}", ResolverType, settings.GetSetting(ResolverValidSettings.SubversionUrl));
                    throw new InvalidProviderConfigurationException(string.Format("Could not connect to Subversion {0}", settings.GetSetting(ResolverValidSettings.SubversionUrl)));
                }
            }
            catch (SvnAuthenticationException)
            {
                throw new SvnAuthenticationException();
            }

            if (string.IsNullOrEmpty(settings.GetSetting(ResolverValidSettings.DependencyDefinitionFileNameList)))
            {
                throw new InvalidProviderConfigurationException(string.Format("No dependency definition file list was specified"));
            }

            ResolverSettings = settings;
            ValidDependencyDefinitionFileNames = settings.GetSetting(ResolverValidSettings.DependencyDefinitionFileNameList).Split(new[] { ';' }).ToList();
            ComponentTargetsName = ValidDependencyDefinitionFileNames.First();

            Logger.Instance().Log(TraceLevel.Info, "Resolver {0} successfully initialized", ResolverType);
        }

        /// <summary>
        /// The subversion resolver does not support querying component versions.
        /// </summary>
        /// <returns>An <see cref="NotImplementedException" /> exception is thrown</returns>
        public IEnumerable<IComponentName> GetAvailableComponentNames()
        {
            Logger.Instance().Log(TraceLevel.Error, "{0}: Querying available components is not supported", ResolverType);
            throw new NotImplementedException(string.Format("Querying available components is not supported by the {0}", ResolverType));
        }

        /// <summary>
        /// The subversion resolver does not support querying component versions.
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
        /// <param name="name">The subversion folder path for the component.</param>
        /// <param name="version">The component version.</param>
        /// <returns>The loaded dependency definition xml file</returns>
        public XDocument LoadComponentTarget(IComponentName name, IComponentVersion version)
        {
            ValidateComponentName(name);
            ValidateComponentVersion(version);

            foreach (var dependencyDefinitionFileName in ValidDependencyDefinitionFileNames)
            {
                var dependencyDefinitionFileLocation = string.Format("{0}/{1}", name, dependencyDefinitionFileName);

                try
                {
                    if (!ProviderSubversion.Instance.ItemExists(dependencyDefinitionFileLocation, version.ToString()))
                    {
                        Logger.Instance().Log(TraceLevel.Verbose, "{0}: Dependency definition file {1} for component {2}#{3} was not found", ResolverType, dependencyDefinitionFileLocation, name, version);
                        continue;
                    }
                }
                catch (SvnAuthenticationException)
                {
                    Logger.Instance().Log(TraceLevel.Error, "{0}: Unable to connect to repository {1}, because authentication failed. Please login once at your your Subversion client to store the credentials locally.", ResolverType, name);
                    throw new InvalidProviderConfigurationException(string.Format("Could not connect to Subversion {0}, because Authentication failed.",name));
                }

                var xdoc = ProviderSubversion.Instance.GetComponentTargetsContent(dependencyDefinitionFileLocation, version.ToString());

                Logger.Instance().Log(TraceLevel.Info, "{0}: Loading dependency definition file {1} for component {2}#{3} finished successfully", ResolverType, dependencyDefinitionFileLocation, name, version);
                return xdoc;
            }

            return null;
        }

        /// <summary>
        /// Determins whether a folder exists
        /// </summary>
        /// <param name="name">The name of the component (branch folder)</param>
        /// <returns>true if the branch folder exists; false if not</returns>
        public bool ComponentExists(IComponentName name)
        {
            ValidateComponentName(name);

            try
            {
                if (ProviderSubversion.Instance.ItemExists(name.ToString()))
                {
                    Logger.Instance().Log(TraceLevel.Info, "{0}: Component folder for component {1} was found on Subversion", ResolverType, name);
                    return true;
                }
            }
            catch (SvnAuthenticationException)
            {
                Logger.Instance().Log(TraceLevel.Error, "{0}: Unable to connect to repository {1}, because authentication failed. Please login once at your your Subversion client to store the credentials locally.", ResolverType, name);
                throw new InvalidProviderConfigurationException(string.Format("Could not connect to Subversion {0}, because Authentication failed.", name));
            }

            Logger.Instance().Log(TraceLevel.Warning, "{0}: Component folder for component {1} was not found on Subversion", ResolverType, name);

            return false;
        }

        /// <summary>
        /// Determines whether a folder exists having a specific version
        /// </summary>
        /// <param name="name">The name of the component</param>
        /// <param name="version">The name of the component (branch folder)</param>
        /// <returns>true if the branch folder exists at the version; false otherwise</returns>
        public bool ComponentExists(IComponentName name, IComponentVersion version)
        {
            ValidateComponentName(name);
            ValidateComponentVersion(version);

            try
            {
                if (ProviderSubversion.Instance.ItemExists(name.ToString(), version.ToString()))
                {
                    foreach (var dependencyDefinitionFileName in ValidDependencyDefinitionFileNames)
                    {
                        var dependencyDefinitionFile = string.Format("{0}/{1}", name.ToString(), dependencyDefinitionFileName);
                        if (ProviderSubversion.Instance.ItemExists(dependencyDefinitionFile, version.ToString()))
                        {
                            Logger.Instance().Log(TraceLevel.Info, "{0}: Component {1}#{2} was found on Subversion", ResolverType, name, version);
                            return true;
                        }
                    }
                }
            }
            catch (SvnAuthenticationException)
            {
                Logger.Instance().Log(TraceLevel.Error, "{0}: Unable to connect to repository {1}, because authentication failed. Please login once at your your Subversion client to store the credentials locally.", ResolverType, name);
                throw new InvalidProviderConfigurationException(string.Format("Could not connect to Subversion {0}, because Authentication failed.", name));
            }

            Logger.Instance().Log(TraceLevel.Warning, "{0}: Component {1}#{2} was not found on Subversion", ResolverType, name, version);
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
                Logger.Instance().Log(TraceLevel.Error, "Subversion component name was null");
                throw new ArgumentNullException("name", "Subversioncomponent name was null");
            }

            if (string.IsNullOrEmpty(name.ToString()))
            {
                Logger.Instance().Log(TraceLevel.Error, "Subversion path for component {0} was empty", name);
                throw new ArgumentException(string.Format("Subversion path for component {0} was empty", name), "name");
            }
        }

        /// <summary>
        /// Validates the component version and checks if version number is invalid.
        /// </summary>
        /// <param name="version">The component version.</param>
        private static void ValidateComponentVersion(IComponentVersion version)
        {
            if (version == null)
            {
                Logger.Instance().Log(TraceLevel.Error, "Version for Subversion component was null");
                throw new ArgumentNullException("version", "Version for Subversion component was null");
            }

            if (version.Version == null)
            {
                Logger.Instance().Log(TraceLevel.Error, "Version number for Subversion component was invalid");
                throw new ArgumentException("Version number for Subversion component was invalid", "version");
            }
        }

        #endregion
    }
}
