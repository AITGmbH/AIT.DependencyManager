// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BuildTaskHelper.cs" company="AIT GmbH & Co. KG.">
//   All rights reserved by AIT GmbH & Co. KG.
// </copyright>
// <summary>
//   Defines the BuildTaskHelper type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace AIT.DMF.MSBuild
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Common;
    using Common.Trash;
    using Contracts.Common;
    using Contracts.Services;
    using Microsoft.TeamFoundation.VersionControl.Client;

    /// <summary>
    /// The build task helper which provides common operations for the MSBuild tasks.
    /// </summary>
    public class BuildTaskHelper
    {
        #region Private members

        /// <summary>
        /// The dependency service settings to use.
        /// </summary>
        private readonly ISettings<ServiceValidSettings> _settings;

        /// <summary>
        /// The dependency service.
        /// </summary>
        private readonly IDependencyService _service;

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="BuildTaskHelper"/> class.
        /// </summary>
        /// <param name="dependencyDefinitionFilename">The dependency definition filename.</param>
        /// <param name="projectFile">The project file.</param>
        public BuildTaskHelper(string dependencyDefinitionFilename, string projectFile)
        {
            var workspaceInfo = GetWorkspace(projectFile);

            DependencyManagerSettings.Instance.Load(workspaceInfo.ServerUri.AbsoluteUri);

            var relativeOutputPath = DependencyManagerSettings.Instance.RelativeOutputPath;
            var binaryTeamProject = DependencyManagerSettings.Instance.BinaryRepositoryTeamProject;
            var binaryTpc = DependencyManagerSettings.Instance.BinaryRepositoryTeamProjectCollectionUrl.Equals(string.Empty) ? workspaceInfo.ServerUri.AbsoluteUri : DependencyManagerSettings.Instance.BinaryRepositoryTeamProjectCollectionUrl;
            var dependencyDefinitionFilePath = GetDependencyDefinitionFilePath(dependencyDefinitionFilename, DependencyManagerSettings.Instance.ValidDependencyDefinitionFileExtension);
            var outputBaseFolder = GetOutputBaseFolder(dependencyDefinitionFilePath);

            var dependencyDefinitionFileNameList = string.Join(";", DependencyManagerSettings.Instance.ValidDependencyDefinitionFileExtension.Select(x => string.Concat("component", x)));
            // ReSharper disable AssignNullToNotNullAttribute
            if (!dependencyDefinitionFileNameList.Contains(Path.GetFileName(dependencyDefinitionFilePath)))
            // ReSharper restore AssignNullToNotNullAttribute
            {
                dependencyDefinitionFileNameList = string.Concat(
                    Path.GetFileName(dependencyDefinitionFilePath), ";", dependencyDefinitionFileNameList);
            }

            _settings = new Settings<ServiceValidSettings>();
            _settings.AddSetting(new KeyValuePair<ServiceValidSettings, string>(ServiceValidSettings.DefaultDependencyDefinitionFilename, dependencyDefinitionFileNameList));
            _settings.AddSetting(new KeyValuePair<ServiceValidSettings, string>(ServiceValidSettings.DefaultTeamProjectCollection, workspaceInfo.ServerUri.AbsoluteUri));
            _settings.AddSetting(new KeyValuePair<ServiceValidSettings, string>(ServiceValidSettings.DefaultWorkspaceName, workspaceInfo.Name));
            _settings.AddSetting(new KeyValuePair<ServiceValidSettings, string>(ServiceValidSettings.DefaultWorkspaceOwner, workspaceInfo.OwnerName));
            _settings.AddSetting(new KeyValuePair<ServiceValidSettings, string>(ServiceValidSettings.DefaultOutputBaseFolder, outputBaseFolder));
            _settings.AddSetting(new KeyValuePair<ServiceValidSettings, string>(ServiceValidSettings.DefaultRelativeOutputPath, relativeOutputPath));
            _settings.AddSetting(new KeyValuePair<ServiceValidSettings, string>(ServiceValidSettings.BinaryTeamProjectCollectionUrl, binaryTpc));
            _settings.AddSetting(new KeyValuePair<ServiceValidSettings, string>(ServiceValidSettings.BinaryRepositoryTeamProject, binaryTeamProject));

            _service = new DependencyService.DependencyService(_settings);
        }

        #region Public properties

        /// <summary>
        /// Gets the dependency service.
        /// </summary>
        public IDependencyService DependencyService
        {
            get { return _service; }
        }

        /// <summary>
        /// Gets the dependency service settings.
        /// </summary>
        public ISettings<ServiceValidSettings> DependencyServiceSettings
        {
            get { return _settings; }
        }

        #endregion

        /// <summary>
        /// Gets the workspace base on the MSBuild targets file.
        /// </summary>
        /// <param name="targetsFile">The MSBuild targets file.</param>
        /// <returns>Information about workspace.</returns>
        private WorkspaceInfo GetWorkspace(string targetsFile)
        {
            var localProjectFile = targetsFile;
            if (string.IsNullOrEmpty(localProjectFile))
            {
                throw new ArgumentException(string.Format("Error while determining workspace information (Could not determine local folder for MSBuild targets file)"));
            }

            var workstation = Workstation.Current;
            if (workstation == null)
            {
                throw new ArgumentException(string.Format("Error while determining workspace information (Could not determine local workstation object)"));
            }

            var workstationInfo = workstation.GetLocalWorkspaceInfo(localProjectFile);
            if (workstationInfo == null)
            {
                throw new ArgumentException(string.Format("Error while determining workspace information (Could not determine workspace info object)"));
            }

            return workstationInfo;
        }

        /// <summary>
        /// Gets the dependency definition file path.
        /// </summary>
        /// <param name="dependencyDefinitionFileName">The name of the dependency definition file.</param>
        /// <param name="allowedFileExtensions">A list of allowed file extension for dependency definition files</param>
        /// <returns>The full path of dependency definition file.</returns>
        private string GetDependencyDefinitionFilePath(string dependencyDefinitionFileName, string[] allowedFileExtensions)
        {
            var dependencyDefinitionFilePath = Path.GetFullPath(dependencyDefinitionFileName);

            // Howto check if this is a valid filename? (In VSIX we have a settings for this)
            if (!File.Exists(dependencyDefinitionFilePath) || !allowedFileExtensions.Any(x => string.Equals(x, Path.GetExtension(dependencyDefinitionFilePath), StringComparison.OrdinalIgnoreCase)))
            {
                throw new ArgumentException(string.Format("Please check parameter DependencyDefinitionPath:\n\"{0}\" does not seem like a file valid path!\nExample: ..\\component{1}.", dependencyDefinitionFilePath, allowedFileExtensions.First()));
            }

            return dependencyDefinitionFilePath;
        }

        /// <summary>
        /// Gets the output base folder.
        /// </summary>
        /// <param name="dependencyDefinitionFileName">The dependency definition file.</param>
        /// <returns>The output base folder.</returns>
        private string GetOutputBaseFolder(string dependencyDefinitionFileName)
        {
            var outputBaseFolder = Path.GetDirectoryName(dependencyDefinitionFileName);
            if (string.IsNullOrEmpty(outputBaseFolder))
            {
                throw new ArgumentException(string.Format("Error while determining output base directory (Could not determine local folder based on {0})", outputBaseFolder));
            }

            return outputBaseFolder;
        }
    }
}
