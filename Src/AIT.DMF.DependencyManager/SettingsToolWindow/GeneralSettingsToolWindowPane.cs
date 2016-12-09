// --------------------------------------------------------------------------------------------------------------------
// <copyright file="GeneralSettingsToolWindowPane.cs" company="AIT GmbH & Co. KG">
//   All rights reserved by AIT GmbH & Co. KG
// </copyright>
// <summary>
//   This class implements the tool window exposed by this package and hosts a user control.
//   In Visual Studio tool windows are composed of a frame (implemented by the shell) and a pane,
//   usually implemented by the package implementer.
//   This class derives from the ToolWindowPane class provided from the MPF in order to use its
//   implementation of the IVsUIElementPane interface.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace AIT.AIT_DMF_DependencyManager.GeneralSettingsToolWindow
{
    using System.Collections.Generic;
    using System.Runtime.InteropServices;

    using DMF.Contracts.Services;
    using DMF.DependencyManager.Controls.ViewModels;
    using DMF.DependencyManager.Controls.Views;
    using DMF.DependencyService;
    using Microsoft.VisualStudio.Shell;

    /// <summary>
    /// This class implements the tool window exposed by this package and hosts a user control.
    /// In Visual Studio tool windows are composed of a frame (implemented by the shell) and a pane,
    /// usually implemented by the package implementer.
    /// This class derives from the ToolWindowPane class provided from the MPF in order to use its
    /// implementation of the IVsUIElementPane interface.
    /// </summary>
    [Guid("091d7e83-a4f0-40ec-b97a-8f5bf2eaef97")]
    public class GeneralSettingsToolWindowPane : ToolWindowPane
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GeneralSettingsToolWindowPane"/> class.
        /// </summary>
        public GeneralSettingsToolWindowPane() :
            base(null)
        {
            // Set the window title reading it from the resources.
            Caption = "Dependency Manager General Settings";

            // Set the image that will appear on the tab of the window frame
            // when docked with an other window
            // The resource ID correspond to the one defined in the resx file
            // while the Index is the offset in the bitmap strip. Each image in
            // the strip being 16x16.
            BitmapResourceID = 301;
            BitmapIndex = 1;
        }

        /// <summary>
        /// Wait until the package is available so we can get the team foundation project context from it
        /// </summary>
        protected override void OnCreate()
        {
            // we need an instance of the dependecy resolver to query registered resolver types.
            // The settings view dynamically generates control for each resolver
            var settingsToUse = new Settings<ServiceValidSettings>();
            settingsToUse.AddSetting(new KeyValuePair<ServiceValidSettings, string>(ServiceValidSettings.DefaultDependencyDefinitionFilename, "dummy"));
            settingsToUse.AddSetting(new KeyValuePair<ServiceValidSettings, string>(ServiceValidSettings.DefaultTeamProjectCollection, "dummy"));
            settingsToUse.AddSetting(new KeyValuePair<ServiceValidSettings, string>(ServiceValidSettings.DefaultWorkspaceName, "dummy"));
            settingsToUse.AddSetting(new KeyValuePair<ServiceValidSettings, string>(ServiceValidSettings.DefaultWorkspaceOwner, "dummy"));
            settingsToUse.AddSetting(new KeyValuePair<ServiceValidSettings, string>(ServiceValidSettings.DefaultOutputBaseFolder, "dummy"));
            settingsToUse.AddSetting(new KeyValuePair<ServiceValidSettings, string>(ServiceValidSettings.DefaultRelativeOutputPath, "dummy"));
            settingsToUse.AddSetting(new KeyValuePair<ServiceValidSettings, string>(ServiceValidSettings.BinaryTeamProjectCollectionUrl, "dummy"));
            settingsToUse.AddSetting(new KeyValuePair<ServiceValidSettings, string>(ServiceValidSettings.BinaryRepositoryTeamProject, "dummy"));
            settingsToUse.AddSetting(new KeyValuePair<ServiceValidSettings, string>(ServiceValidSettings.PathToSevenZipExe, "dummy"));

            var dependencyService = new DependencyService(settingsToUse);

            var viewModel = new GeneralSettingsEditorViewModel(
                ((AIT_DMF_DependencyManagerPackage)Package).TeamFoundationServerExt,
                dependencyService.GetDependencyResolvers());

            Content = new GeneralSettingsEditorView
                          {
                              DataContext = viewModel
                          };
        }
    }
}
