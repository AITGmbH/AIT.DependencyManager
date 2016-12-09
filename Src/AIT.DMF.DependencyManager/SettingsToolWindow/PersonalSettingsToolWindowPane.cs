// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PersonalSettingsToolWindowPane.cs" company="AIT GmbH & Co. KG">
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

namespace AIT.AIT_DMF_DependencyManager.PersonalSettingsToolWindow
{
    using System.Runtime.InteropServices;
    using DMF.DependencyManager.Controls.ViewModels;
    using DMF.DependencyManager.Controls.Views;
    using Microsoft.VisualStudio.Shell;

    /// <summary>
    /// This class implements the tool window exposed by this package and hosts a user control.
    /// In Visual Studio tool windows are composed of a frame (implemented by the shell) and a pane,
    /// usually implemented by the package implementer.
    /// This class derives from the ToolWindowPane class provided from the MPF in order to use its
    /// implementation of the IVsUIElementPane interface.
    /// </summary>
    [Guid("5792D761-48E2-4E73-BA01-D014A8715315")]
    public class PersonalSettingsToolWindowPane : ToolWindowPane
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PersonalSettingsToolWindowPane"/> class.
        /// </summary>
        public PersonalSettingsToolWindowPane() :
            base(null)
        {
            // Set the window title reading it from the resources.
            Caption = "Dependency Manager Personal Settings";

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
            var viewModel = new PersonalSettingsEditorViewModel(((AIT_DMF_DependencyManagerPackage)Package).TeamFoundationServerExt);

            Content = new PersonalSettingsEditorView { DataContext = viewModel };
        }
    }
}
