using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.TeamFoundation.VersionControl.Client;
using AIT.DMF.Contracts.Common;
using AIT.DMF.Contracts.Graph;
using AIT.DMF.Contracts.Exceptions;
using AIT.DMF.Contracts.Parser;
using AIT.DMF.Common;

namespace AIT.DMF.DependencyService
{
    public class Component : IComponent
    {
        #region Private Members

        private readonly List<IDependency> _predecessors = new List<IDependency>();
        private readonly List<IDependency> _successors = new List<IDependency>();

        private IDictionary<string, string> _fields;

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor which creates a new Component object with a name, version type and a list with predecessor and successor components.
        /// </summary>
        /// <param name="config">The deserialized config</param>
        public Component(IDependencyProviderConfig config)
        {
            if (config == null)
                throw new InvalidComponentException("Component for dependency graph was initialized with an invalid component description (Config was null)");

            InitializeFieldsFromConfig(config);
        }

        /// <summary>
        /// Constructor which creates a new Component object with a name, version type and a list with predecessor and successor components.
        /// </summary>
        /// <param name="config">The deserialized config</param>
        /// <param name="predecessors">List of predecessor components</param>
        /// <param name="successors">List of successor components</param>
        public Component(IDependencyProviderConfig config, List<IDependency> predecessors, List<IDependency> successors)
            : this(config)
        {
            _predecessors = predecessors;
            _successors = successors;
        }

        /// <summary>
        /// Constructor which creates a new Component object with a name, version type and a list with predecessor and successor components.
        /// </summary>
        /// <param name="effectiveVersion">Effective version to override</param>
        /// <param name="config">The deserialized config</param>
        /// <param name="predecessors">List of predecessor components</param>
        /// <param name="successors">List of successor components</param>
        public Component(IComponentVersion effectiveVersion, IDependencyProviderConfig config, List<IDependency> predecessors, List<IDependency> successors)
            : this(config, predecessors, successors)
        {
            // Override version here
            Version = effectiveVersion;
        }

        private void InitializeFieldsFromConfig(IDependencyProviderConfig config)
        {
            _fields = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

            foreach (var setting in config.Settings.SettingsList)
            {
                _fields.Add(setting.Name.ToString(), setting.Value);
            }

            Name = GetName(config);
            Version = GetVersion(config);
            Type = GetType(config);

        }

        public ComponentType GetType(IDependencyProviderConfig config)
        {
            switch (config.Type)
            {
                case "Local":
                    return ComponentType.Local;
                case "BuildResultJSON":
                    return ComponentType.VNextBuildResult;
                case "BuildResult":
                    return ComponentType.BuildResult;
                case "FileShare":
                    return ComponentType.FileShare;
                case "SourceControl":
                    return ComponentType.SourceControl;
                case "SourceControlCopy":
                    return ComponentType.SourceControlCopy;
                case "BinaryRepository":
                    return ComponentType.BinaryRepository;
                case "Subversion":
                    return ComponentType.Subversion;
                default:
                    throw new DependencyServiceException(string.Format("Unsupported dependency type \"{0}\" found. Please check component {1}!", config.Type, Name));
            }
        }

        public IComponentName GetName(IDependencyProviderConfig config)
        {
            switch (config.Type)
            {
                case "Local":
                    return new ComponentName(GetFieldValue(DependencyProviderValidSettingName.ComponentName));
                case "BuildResultJSON":
                case "BuildResult":
                    return new ComponentName(
                        GetFieldValue(DependencyProviderValidSettingName.TeamProjectName),
                        GetFieldValue(DependencyProviderValidSettingName.BuildDefinition));
                case "FileShare":
                    return new ComponentName(GetFieldValue(DependencyProviderValidSettingName.ComponentName));
                case "SourceControl":
                case "SourceControlCopy":
                    return
                        new ComponentName(GetFieldValue(DependencyProviderValidSettingName.ServerRootPath));
                case "BinaryRepository":
                    return new ComponentName(GetFieldValue(DependencyProviderValidSettingName.ComponentName));
                case "Subversion":
                    return new ComponentName(GetFieldValue(DependencyProviderValidSettingName.SubversionRootPath));
                default:
                    throw new DependencyServiceException(
                        string.Format(
                            "Unsupported dependency type \"{0}\" found. Please check component {1}!", config.Type, Name));
            }
        }

        public IComponentVersion GetVersion(IDependencyProviderConfig config)
        {
            switch (config.Type)
            {
                case "Local":
                    return new ComponentVersion(GetFieldValue(DependencyProviderValidSettingName.VersionNumber));
                case "BuildResultJSON":
                    return new ComponentVersion(
                        buildNumber: GetFieldValue(DependencyProviderValidSettingName.BuildNumber),
                        acceptedBuildStatus: GetBuildStatusOrQuality(DependencyProviderValidSettingName.BuildStatus),
                        acceptedBuildQuality: null,
                        acceptedBuildTags: GetBuildStatusOrQuality(DependencyProviderValidSettingName.BuildTags)
                        );
                case "BuildResult":
                    return new ComponentVersion(
                        buildNumber: GetFieldValue(DependencyProviderValidSettingName.BuildNumber),
                        acceptedBuildStatus: GetBuildStatusOrQuality(DependencyProviderValidSettingName.BuildStatus),
                        acceptedBuildQuality: GetBuildStatusOrQuality(DependencyProviderValidSettingName.BuildQuality),
                        acceptedBuildTags: null);
                case "FileShare":
                    return new ComponentVersion(GetFieldValue(DependencyProviderValidSettingName.VersionNumber));
                case "SourceControl":
                case "SourceControlCopy":
                    var versionSpec = GetFieldValue(DependencyProviderValidSettingName.VersionSpec);
                    if (string.IsNullOrWhiteSpace(versionSpec))
                    {
                        // Version spec is not set probably because it is an already downloaded component
                        versionSpec = string.Format(
                            "W{0};{1}",
                            GetFieldValue(DependencyProviderValidSettingName.WorkspaceName),
                            GetFieldValue(DependencyProviderValidSettingName.WorkspaceOwner));
                    }
                    var version = VersionSpec.Parse(
                              versionSpec, GetFieldValue(DependencyProviderValidSettingName.WorkspaceOwner)).
                              First();
                    return new ComponentVersion(version);
                case "BinaryRepository":
                    return new ComponentVersion(GetFieldValue(DependencyProviderValidSettingName.VersionNumber));
                case "Subversion":
                    return new ComponentVersion(GetFieldValue(DependencyProviderValidSettingName.VersionSpec));
                default:
                    throw new DependencyServiceException(
                        string.Format(
                            "Unsupported dependency type \"{0}\" found. Please check component {1}!", config.Type, Name));
            }
        }

        private IEnumerable<string> GetBuildStatusOrQuality(DependencyProviderValidSettingName name)
        {
            var settingValue = GetFieldValue(name);
            IEnumerable<string> expectedBuildStatusOrQuality;

            if (string.IsNullOrEmpty(settingValue))
            {
                expectedBuildStatusOrQuality = null;
            }
            else
            {
                var valuesFound = settingValue.Split(',');
                expectedBuildStatusOrQuality = valuesFound.Length > 0
                                                   ? valuesFound.Select(x => x.ToLower()).ToList()
                                                   : null;
            }

            return expectedBuildStatusOrQuality;
        }

        #endregion

        #region IComponent Implementation

        /// <summary>
        /// Returns the name of the component.
        /// </summary>
        public IComponentName Name { get; private set; }

        /// <summary>
        /// Returns the exact version of the component.
        /// </summary>
        public IComponentVersion Version { get; private set; }

        /// <summary>
        /// Returns the type of the component (TFSSC; FS; Build)
        /// </summary>
        public ComponentType Type { get; private set; }

        /// <summary>
        /// Returns all dependencies to successor objects.
        /// </summary>
        public IEnumerable<IDependency> Successors
        {
            get { return _successors; }
        }

        /// <summary>
        /// Returns all dependencies to predecessor objects.
        /// </summary>
        public IEnumerable<IDependency> Predecessors
        {
            get { return _predecessors; }
        }

        /// <summary>
        /// Adds the new sucessor to the list of successor (If not already in the list).
        /// </summary>
        /// <param name="newSuccessor">Predecessor dependency to add</param>
        public void AddSuccessor(IDependency newSuccessor)
        {
            if (!_successors.Contains(newSuccessor))
            {
                // Add new successor as it is not in the list till now
                _successors.Add(newSuccessor);
            }
        }

        /// <summary>
        /// Adds the new predecessor to the list of predecessors (If not already in the list).
        /// </summary>
        /// <param name="newPredecessor">Predecessor dependency to add</param>
        public void AddPredecessor(IDependency newPredecessor)
        {
            if (!_predecessors.Contains(newPredecessor))
            {
                // Add new predecessor as it is not in the list till now
                _predecessors.Add(newPredecessor);
            }
        }

        public string GetFieldValue(DependencyProviderValidSettingName name)
        {
            if (!_fields.ContainsKey(name.ToString()))
            {
                return null;
            }

            return _fields[name.ToString()];
        }


        /// <summary>
        /// Adds a fallback value in case the setting can't be found in deserialized config
        /// </summary>
        /// <param name="name">Name of the setting</param>
        /// <param name="value">Value of the setting</param>
        public void AddFallbackFieldValue(DependencyProviderValidSettingName name, string value)
        {
            if (!_fields.ContainsKey(name.ToString()))
            {
                _fields.Add(name.ToString(), value);
            }
            else if (string.IsNullOrEmpty(GetFieldValue(name)))
            {
                _fields[name.ToString()] = value;
            }
        }


        #endregion

        #region Overrides

        public override string ToString()
        {
            return string.Format("{0}#{1}", Name.GetName(), Version.GetVersion());
        }

        public override int GetHashCode()
        {
            return ToString().ToUpperInvariant().GetHashCode();
        }

        public override bool Equals(object obj)
        {
            var component = obj as IComponent;
            if (null == component)
                return false;

            var other = string.Format("{0}#{1}", component.Name.GetName(), component.Version.GetVersion());
            return string.Equals(other, ToString(), StringComparison.OrdinalIgnoreCase) && component.Type.Equals(Type);
        }

        #endregion
    }
}
