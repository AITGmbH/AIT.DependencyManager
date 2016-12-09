// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DependencyActivity.cs" company="AIT GmbH & Co. KG">
//   All rights reserved by AIT GmbH & Co. KG
// </copyright>
// <summary>
//   Base activity for dependency management workflow activities.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace AIT.DMF.Workflow
{
    using System;
    using System.Activities;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using Common;
    using Contracts.Common;
    using Contracts.Exceptions;
    using Contracts.Services;
    using DependencyService;
    using Microsoft.TeamFoundation.Build.Client;
    using Microsoft.TeamFoundation.VersionControl.Client;
    using Microsoft.TeamFoundation.VersionControl.Common;

    /// <summary>
    /// CustomActivity for getting dependencies during workflow execution.
    /// </summary>
    [BuildActivity(HostEnvironmentOption.All)]
    public abstract class DependencyActivity : CodeActivity
    {
        #region Public InArguments

        /// <summary>
        /// Gets or sets the dependency definition file (Set by user)
        /// </summary>
        [RequiredArgument]
        [Browsable(true)]
        [Description("The path to dependency definition file.")]
        public InArgument<string> DependencyDefinitionPath { get; set; }

        /// <summary>
        /// Gets or sets the workspace (From workspace used by build agent)
        /// </summary>
        [RequiredArgument]
        [Browsable(true)]
        [Description("The workspace.")]
        public InArgument<Workspace> Workspace { get; set; }

        #endregion

        #region Protected Properties

        /// <summary>
        /// Gets or sets the name of the activity.
        /// </summary>
        /// <value>The name of the activity.</value>
        protected string ActivityName { get; set; }

        /// <summary>
        /// Gets or sets the code activity context.
        /// </summary>
        /// <value>The context.</value>
        protected CodeActivityContext Context { get; set; }
        #endregion

        #region Call Dependency Service

        /// <summary>
        /// Called by workflow RunOnAgent sequence.
        /// </summary>
        /// <param name="context">The code activity context.</param>
        protected override void Execute(CodeActivityContext context)
        {
            var logger = new BuildLogLogger(context);
            Context = context;

            try
            {
                Platform.Initialize();
                var settings = LoadSettingsFromTfsRegistry(context);

                InternalExecute(logger, settings);
            }
            catch (DependencyServiceException e)
            {
                logger.LogError(string.Format("An dependency service error occured while executing {0} activity. Aborting ...", ActivityName));
                logger.LogMsg(string.Format("Exception message:{0}", e.Message));
                logger.LogMsg(string.Format("Stacktrace:\n{0}", e.StackTrace));
                throw;
            }
            catch (Exception e)
            {
                logger.LogError(string.Format("Fatal error occured while executing {0} activity. Aborting ...", ActivityName));
                logger.LogMsg(string.Format("Exception message:{0}", e.Message));
                logger.LogMsg(string.Format("Stacktrace:\n{0}", e.StackTrace));
                throw;
            }
        }

        /// <summary>
        /// Abstract method for internal logic of the activities.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="settings">The settings.</param>
        protected abstract void InternalExecute(BuildLogLogger logger, ISettings<ServiceValidSettings> settings);

        /// <summary>
        /// Validates the arguments of the activity.
        /// </summary>
        protected abstract void ValidateArguments();

        #endregion

        #region Loading settings

        /// <summary>
        /// Loads BinaryRepositoryTeamProject, BinaryRepositoryTeamProjectCollectionUrl and RelativeOutputPath from TFS registry.
        /// Loads WorkSpace and DependencyDefinitionPath from Activity Context
        /// </summary>
        /// <param name="context">The activity context.</param>
        /// <returns>Settings collection</returns>
        /// <exception cref="DependencyServiceException">If no output path could be determined or if the dependency definition path is invalid</exception>
        private ISettings<ServiceValidSettings> LoadSettingsFromTfsRegistry(CodeActivityContext context)
        {
            var workspaceValue = context.GetValue(Workspace);
            var workspaceNameValue = workspaceValue.Name;
            var workspaceOwnerValue = workspaceValue.OwnerName;
            var tfsUri = workspaceValue.VersionControlServer.TeamProjectCollection.Uri;
            var dependencyDefinitionPathValue = context.GetValue(DependencyDefinitionPath);

            DependencyManagerSettings.Instance.Load(tfsUri.AbsoluteUri);

            // Validate activity parameters
            if (!dependencyDefinitionPathValue.StartsWith(VersionControlPath.RootFolder) || !dependencyDefinitionPathValue.Contains(VersionControlPath.Separator.ToString(CultureInfo.InvariantCulture)))
            {
                throw new DependencyServiceException(string.Format("DependencyDefinitionPath:\n\"{0}\" does not seem like a valid path!\nExample: $\\Path_To_Dependency_Definition_File\\component.targets or C:\\Path_To_Dependency_Definition_File\\component.targets", dependencyDefinitionPathValue));
            }

            string outputFolder;
            if (VersionControlPath.IsValidPath(dependencyDefinitionPathValue))
            {
                outputFolder = workspaceValue.TryGetLocalItemForServerItem(dependencyDefinitionPathValue);
            }
            else
            {
                outputFolder = Path.GetDirectoryName(dependencyDefinitionPathValue);
            }

            if (string.IsNullOrEmpty(outputFolder))
            {
                throw new DependencyServiceException(string.Format("Output base folder could not be determined based on dependency definition path {0}", dependencyDefinitionPathValue));
            }

            // Lousy way to create a semicolon separated list of possible and allowed filenames: component.targets
            // I don't know why the filename of the initial definition is included but better keep it that way...
            var dependencyDefinitionFileNameList = string.Join(";", DependencyManagerSettings.Instance.ValidDependencyDefinitionFileExtension.Select(x => string.Concat("component", x)));
            var defaultDefinitionFilename = Path.GetFileName(dependencyDefinitionPathValue);
            if (defaultDefinitionFilename != null && !dependencyDefinitionFileNameList.Contains(defaultDefinitionFilename))
            {
                dependencyDefinitionFileNameList = string.Concat(
                    Path.GetFileName(dependencyDefinitionPathValue), ";", dependencyDefinitionFileNameList);
            }

            // create Settings
            ISettings<ServiceValidSettings> settings = new Settings<ServiceValidSettings>();
            settings.AddSetting(new KeyValuePair<ServiceValidSettings, string>(ServiceValidSettings.DefaultTeamProjectCollection, tfsUri.AbsoluteUri));
            settings.AddSetting(new KeyValuePair<ServiceValidSettings, string>(ServiceValidSettings.DefaultWorkspaceName, workspaceNameValue));
            settings.AddSetting(new KeyValuePair<ServiceValidSettings, string>(ServiceValidSettings.DefaultWorkspaceOwner, workspaceOwnerValue));
            settings.AddSetting(new KeyValuePair<ServiceValidSettings, string>(ServiceValidSettings.DefaultOutputBaseFolder, outputFolder));
            settings.AddSetting(new KeyValuePair<ServiceValidSettings, string>(ServiceValidSettings.DefaultRelativeOutputPath, DependencyManagerSettings.Instance.RelativeOutputPath));
            settings.AddSetting(new KeyValuePair<ServiceValidSettings, string>(ServiceValidSettings.DefaultDependencyDefinitionFilename, dependencyDefinitionFileNameList));
            settings.AddSetting(new KeyValuePair<ServiceValidSettings, string>(ServiceValidSettings.BinaryTeamProjectCollectionUrl, DependencyManagerSettings.Instance.BinaryRepositoryTeamProjectCollectionUrl));
            settings.AddSetting(new KeyValuePair<ServiceValidSettings, string>(ServiceValidSettings.BinaryRepositoryTeamProject, DependencyManagerSettings.Instance.BinaryRepositoryTeamProject));

            return settings;
        }

        #endregion
    }
}
