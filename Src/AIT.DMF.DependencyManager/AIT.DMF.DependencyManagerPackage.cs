using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.ComponentModel.Design;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using AIT.AIT_DMF_DependencyManager.GeneralSettingsToolWindow;
using AIT.DMF.Common;
using AIT.DMF.DependencyManager.Controls.ViewModels;
using AIT.DMF.DependencyManager.Controls.Views;
using EnvDTE;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.TeamFoundation.VersionControl.Common;
using Microsoft.VisualStudio.TeamFoundation;
using Microsoft.VisualStudio.TeamFoundation.VersionControl;
using EnvDTE80;
using AIT.DMF.Contracts.Services;
using AIT.DMF.DependencyService;
using AIT.DMF.Contracts.Exceptions;
using AIT.DMF.Contracts.Common;
using AIT.AIT_DMF_DependencyManager.Helpers;
using AIT.AIT_DMF_DependencyManager.PersonalSettingsToolWindow;

namespace AIT.AIT_DMF_DependencyManager
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    ///
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell.
    /// </summary>
    // This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is
    // a package.
    [PackageRegistration(UseManagedResourcesOnly = true)]
    // This attribute is used to register the informations needed to show the this package
    // in the Help/About dialog of Visual Studio.
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    // This attribute is needed to let the shell know that this package exposes some menus.
    [ProvideMenuResource("Menus.ctmenu", 1)]
    // MRI: Force autoload
    // UICONTEXT_SolutionExists = {f1536ef8-92ec-443c-9ed7-fdadf150da82}
    // UICONTEXT_NoSolution = ADFC4E64-0397-11D1-9F4E-00A0C911004F
    [ProvideAutoLoad("{f1536ef8-92ec-443c-9ed7-fdadf150da82}")]
    [ProvideAutoLoad("ADFC4E64-0397-11D1-9F4E-00A0C911004F")]
    // MRI: Register as editor
    // We register our AddNewItem Templates the Miscellaneous Files Project:
    [ProvideEditorExtension(typeof(VisualEditorFactory), ".targets", 64,
              ProjectGuid = "{A2FE74E1-B743-11d0-AE1A-00A0C90FFFC3}",
              TemplateDir = @"..\..\Templates",
              NameResourceID = 106)]
    // We register that our editor supports LOGVIEWID_Designer logical view
    [ProvideEditorLogicalView(typeof(VisualEditorFactory), "{7651a703-06e5-11d1-8ebd-00a0c90f26ea}")]
    [Guid(GuidList.guidAIT_DMF_DependencyManagerPkgString)]
    [ProvideBindingPath]
    // Register tool window to edit general server settings (stored in tfs registry)
    [ProvideToolWindow(typeof(GeneralSettingsToolWindowPane), Style = VsDockStyle.Tabbed, Window = "3ae79031-e1bc-11d0-8f78-00a0c9110057")]
    // Register tool window to edit personal server settings (stored in tfs registry)
    [ProvideToolWindow(typeof(PersonalSettingsToolWindowPane), Style = VsDockStyle.Tabbed, Window = "958D76F1-BD6C-4941-A08A-DBD4D7526079")]
    public sealed class AIT_DMF_DependencyManagerPackage : Package, IDisposable
    {
        // MRI: Save command ids after initialize
        private CommandID _userHelpToolsMenuID;
        private CommandID _editGeneralSettingsMenuID;
        private CommandID _editPersonalSettingsMenuID;
        private CommandID _getDependenciesRecursiveSolutionID;
        private CommandID _getDirectDependenciesSolutionID;
        private CommandID _forcedGetDependenciesRecursiveSolutionID;
        private CommandID _forcedGetDirectDependenciesSolutionID;
        private CommandID _cleanDependenciesSolutionID;
        private CommandID _getDependenciesRecursiveSourceControlID;
        private CommandID _forcedGetDependenciesRecursiveSourceControlID;
        private CommandID _getDirectDependenciesSourceControlID;
        private CommandID _forcedGetDirectDependenciesSourceControlID;
        private CommandID _cleanDependenciesSourceControlID;
        private CommandID _createCompTargetsSourceControlID;
        // MRI: Private members
        private DependencyManagerSettings _settings;
        private OutputWindowLogger _outputWindowPaneLogger;
        //
        private VisualEditorFactory editorFactory;

        // TeamFoundationServerExt, VersionControlExt and DTE are used as hooks into Source Control Explorer and Solution Explorer
        // See also http://blogs.msdn.com/b/edhintz/archive/2006/02/03/524312.aspx
        internal VersionControlExt VersionControlExt
        {
            get
            {
                return DevEnv.GetObject("Microsoft.VisualStudio.TeamFoundation.VersionControl.VersionControlExt") as VersionControlExt;
            }
        }
        internal TeamFoundationServerExt TeamFoundationServerExt
        {
            get
            {
                return DevEnv.GetObject("Microsoft.VisualStudio.TeamFoundation.TeamFoundationServerExt") as TeamFoundationServerExt;
            }
        }
        private DTE2 _dte;
        internal DTE2 DevEnv
        {
            get
            {
                if (_dte == null)
                {
                    _dte = (DTE2)GetService(typeof(DTE));
                }

                return _dte;
            }
        }
        // Reference BuildEvents to not get caught by garbage collection of CLR
        // See also http://social.msdn.microsoft.com/Forums/en/vsx/thread/ee827d4f-db11-497b-9135-bb344a144d52
        private BuildEvents _buildEvents;

        /// <summary>
        /// Default constructor of the package.
        /// Inside this method you can place any initialization code that does not require
        /// any Visual Studio service because at this point the package object is created but
        /// not sited yet inside Visual Studio environment. The place to do all the other
        /// initialization is the Initialize method.
        /// </summary>
        public AIT_DMF_DependencyManagerPackage()
        {
            Trace.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering constructor for: {0}", ToString()));
        }

        /////////////////////////////////////////////////////////////////////////////
        // Overriden Package Implementation
        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initilaization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            Trace.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering Initialize() of: {0}", ToString()));
            base.Initialize();

            Platform.Initialize();
            _outputWindowPaneLogger = new OutputWindowLogger(DevEnv);
            _settings = DependencyManagerSettings.Instance;

            TeamFoundationServerExt.ProjectContextChanged += TeamFoundationServerExtOnProjectContextChanged;

            // Add our command handlers for menu (commands must exist in the .vsct file)
            OleMenuCommandService mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (null != mcs)
            {
                // Create the command for the menu item.
                _userHelpToolsMenuID = new CommandID(GuidList.guidAIT_DMF_DependencyManagerCmdSet, (int)PkgCmdIDList.cmdidUserHelpToolsMenu);
                OleMenuCommand userHelpToolsMenuMenuItem = new OleMenuCommand(UserHelpCallback, _userHelpToolsMenuID);
                userHelpToolsMenuMenuItem.BeforeQueryStatus += userHelpToolsMenuMenuItem_BeforeQueryStatus;
                mcs.AddCommand(userHelpToolsMenuMenuItem);

                // Open tool window to edit tfs registry settings (general settings)
                _editGeneralSettingsMenuID = new CommandID(GuidList.guidAIT_DMF_DependencyManagerCmdSet, (int)PkgCmdIDList.cmdidEditGeneralSettingsToolsMenu);
                OleMenuCommand generalSettingsWindowMenuItem = new OleMenuCommand(OpenGeneralSettingsToolWindowCallback, _editGeneralSettingsMenuID);
                mcs.AddCommand(generalSettingsWindowMenuItem);

                // Open tool window to edit windows registry settings (personal settings)
                _editPersonalSettingsMenuID = new CommandID(GuidList.guidAIT_DMF_DependencyManagerCmdSet, (int)PkgCmdIDList.cmdidEditPersonalSettingsToolsMenu);
                OleMenuCommand personalSettingsWindowMenuItem = new OleMenuCommand(OpenPersonalSettingsToolWindowCallback, _editPersonalSettingsMenuID);
                mcs.AddCommand(personalSettingsWindowMenuItem);

                _getDependenciesRecursiveSolutionID = new CommandID(GuidList.guidAIT_DMF_DependencyManagerCmdSet, (int)PkgCmdIDList.cmdidGetDependenciesRecursiveSolution);
                OleMenuCommand getDependenciesRecursiveSolutionMenuItem = new OleMenuCommand(GetDependenciesRecursiveCallback, _getDependenciesRecursiveSolutionID);
                getDependenciesRecursiveSolutionMenuItem.BeforeQueryStatus += getDependenciesContextMenuItems_BeforeQueryStatus;
                _getDirectDependenciesSolutionID = new CommandID(GuidList.guidAIT_DMF_DependencyManagerCmdSet, (int)PkgCmdIDList.cmdidGetDirectDependenciesSolution);
                OleMenuCommand getDirectDependenciesSolutionMenuItem = new OleMenuCommand(GetDirectDependenciesCallback, _getDirectDependenciesSolutionID);
                getDirectDependenciesSolutionMenuItem.BeforeQueryStatus += getDependenciesContextMenuItems_BeforeQueryStatus;
                _forcedGetDependenciesRecursiveSolutionID = new CommandID(GuidList.guidAIT_DMF_DependencyManagerCmdSet, (int)PkgCmdIDList.cmdidForcedGetDependenciesRecursiveSolution);
                OleMenuCommand forcedGetDependenciesRecursiveSolutionMenuItem = new OleMenuCommand(ForcedGetDependenciesRecursiveCallback, _forcedGetDependenciesRecursiveSolutionID);
                forcedGetDependenciesRecursiveSolutionMenuItem.BeforeQueryStatus += getDependenciesContextMenuItems_BeforeQueryStatus;
                _forcedGetDirectDependenciesSolutionID = new CommandID(GuidList.guidAIT_DMF_DependencyManagerCmdSet, (int)PkgCmdIDList.cmdidForcedGetDirectDependenciesSolution);
                OleMenuCommand forcedGetDirectDependenciesSolutionMenuItem = new OleMenuCommand(ForcedGetDirectDependenciesCallback, _forcedGetDirectDependenciesSolutionID);
                forcedGetDirectDependenciesSolutionMenuItem.BeforeQueryStatus += getDependenciesContextMenuItems_BeforeQueryStatus;
                _cleanDependenciesSolutionID = new CommandID(GuidList.guidAIT_DMF_DependencyManagerCmdSet, (int)PkgCmdIDList.cmdidCleanDependenciesSolution);
                OleMenuCommand cleanDependenciesSolutionMenuItem = new OleMenuCommand(CleanDependenciesCallback, _cleanDependenciesSolutionID);
                cleanDependenciesSolutionMenuItem.BeforeQueryStatus += cleanDependenciesContextMenuItems_BeforeQueryStatus;
                mcs.AddCommand(getDependenciesRecursiveSolutionMenuItem);
                mcs.AddCommand(getDirectDependenciesSolutionMenuItem);
                mcs.AddCommand(forcedGetDependenciesRecursiveSolutionMenuItem);
                mcs.AddCommand(forcedGetDirectDependenciesSolutionMenuItem);
                mcs.AddCommand(cleanDependenciesSolutionMenuItem);

                _getDependenciesRecursiveSourceControlID = new CommandID(GuidList.guidAIT_DMF_DependencyManagerCmdSet, (int)PkgCmdIDList.cmdidGetDependenciesRecursiveSourceControl);
                OleMenuCommand getDependenciesRecursiveSourceControlMenuItem = new OleMenuCommand(GetDependenciesRecursiveCallback, _getDependenciesRecursiveSourceControlID);
                getDependenciesRecursiveSourceControlMenuItem.BeforeQueryStatus += getDependenciesContextMenuItems_BeforeQueryStatus;
                _getDirectDependenciesSourceControlID = new CommandID(GuidList.guidAIT_DMF_DependencyManagerCmdSet, (int)PkgCmdIDList.cmdidGetDirectDependenciesSourceControl);
                OleMenuCommand getDirectDependenciesSourceControlMenuItem = new OleMenuCommand(GetDirectDependenciesCallback, _getDirectDependenciesSourceControlID);
                getDirectDependenciesSourceControlMenuItem.BeforeQueryStatus += getDependenciesContextMenuItems_BeforeQueryStatus;
                _forcedGetDependenciesRecursiveSourceControlID = new CommandID(GuidList.guidAIT_DMF_DependencyManagerCmdSet, (int)PkgCmdIDList.cmdidForcedGetDependenciesRecursiveSourceControl);
                OleMenuCommand forcedGetDependenciesRecursiveSourceControlMenuItem = new OleMenuCommand(ForcedGetDependenciesRecursiveCallback, _forcedGetDependenciesRecursiveSourceControlID);
                forcedGetDependenciesRecursiveSourceControlMenuItem.BeforeQueryStatus += getDependenciesContextMenuItems_BeforeQueryStatus;
                _forcedGetDirectDependenciesSourceControlID = new CommandID(GuidList.guidAIT_DMF_DependencyManagerCmdSet, (int)PkgCmdIDList.cmdidForcedGetDirectDependenciesSourceControl);
                OleMenuCommand forcedGetDirectDependenciesSourceControlMenuItem = new OleMenuCommand(ForcedGetDirectDependenciesCallback, _forcedGetDirectDependenciesSourceControlID);
                forcedGetDirectDependenciesSourceControlMenuItem.BeforeQueryStatus += getDependenciesContextMenuItems_BeforeQueryStatus;
                _cleanDependenciesSourceControlID = new CommandID(GuidList.guidAIT_DMF_DependencyManagerCmdSet, (int)PkgCmdIDList.cmdidCleanDependenciesSourceControl);
                OleMenuCommand cleanDependenciesSourceControlMenuItem = new OleMenuCommand(CleanDependenciesCallback, _cleanDependenciesSourceControlID);
                cleanDependenciesSourceControlMenuItem.BeforeQueryStatus += cleanDependenciesContextMenuItems_BeforeQueryStatus;
                _createCompTargetsSourceControlID = new CommandID(GuidList.guidAIT_DMF_DependencyManagerCmdSet, (int)PkgCmdIDList.cmdidCreateComponentTargetsSourceControl);
                OleMenuCommand createCompTargetsSourceControlMenuItem = new OleMenuCommand(CreateComponentTargetsCallback, _createCompTargetsSourceControlID);
                createCompTargetsSourceControlMenuItem.BeforeQueryStatus += createComponentTargetsContextMenuItems_BeforeQueryStatus;
                mcs.AddCommand(getDependenciesRecursiveSourceControlMenuItem);
                mcs.AddCommand(getDirectDependenciesSourceControlMenuItem);
                mcs.AddCommand(forcedGetDependenciesRecursiveSourceControlMenuItem);
                mcs.AddCommand(forcedGetDirectDependenciesSourceControlMenuItem);
                mcs.AddCommand(cleanDependenciesSourceControlMenuItem);
                mcs.AddCommand(createCompTargetsSourceControlMenuItem);
            }

            // Register document editor
            editorFactory = new VisualEditorFactory();
            RegisterEditorFactory(editorFactory);

            // Build Event Handler
            _buildEvents = DevEnv.Events.BuildEvents;
            _buildEvents.OnBuildBegin += BuildEvents_OnBuildBegin;

            // Register to window visibility events
            // ReSharper disable UseIndexedProperty
            var windowVisibilityEvents = ((Events2) DevEnv.Events).get_WindowVisibilityEvents();
            // ReSharper restore UseIndexedProperty
            windowVisibilityEvents.WindowShowing += OnDependencyGeneralSettingsWindowShowing;
            windowVisibilityEvents.WindowShowing += OnDependencyPersonalSettingsWindowShowing;

            ////Event Handler when startup of devenv is complete.
            DevEnv.Events.DTEEvents.OnStartupComplete += DTEEvents_OnStartupComplete;
        }

        /// <summary>
        /// Handler for the startup event. The necessary Windows Registry entries for the dependecy manager will be checked here. If the entry for 7-zip
        /// path is missing, a onetime message box and additional information is showed in the output window. For Multi-Site-Featurethe default entry will
        /// be created.
        /// </summary>
        private void DTEEvents_OnStartupComplete()
        {
            // Check if registry entry is missing and show a onetime message box; Afterwards the regstry key will be created
            if (ApplicationSettings.Instance.InstallPathForSevenZip == null)
            {
                ApplicationSettings.Instance.InstallPathForSevenZip = ApplicationSettings.Instance.DetermineSevenZipFolder();

                // Message box is displayed only, if the 7-zip folder could not be determined
                if (ApplicationSettings.Instance.InstallPathForSevenZip == null)
                {
                    DMF.DependencyManager.Controls.Services.UserMessageService.ShowWarning("No path for 7z.exe is defined. The path is necessary for full 7-zip support. Please set them in the Dependecy Manager Settings.");
                }
            }
            // Check if registry entry is empty and show the information in the output window for each start of Visual Studio
            else if (ApplicationSettings.Instance.InstallPathForSevenZip == string.Empty)
            {
                _outputWindowPaneLogger.LogMsg("No path for 7z.exe is defined. The path is necessary for full 7-zip support. Please set them in the Dependecy Manager Settings.");
            }

            // Create Windows Registry entry for Multi-Site, if it not exists
            if (ApplicationSettings.Instance.SelectedMultiSiteEntry == null)
            {
                ApplicationSettings.Instance.SelectedMultiSiteEntry = ApplicationSettings.AutomaticSite;
            }
        }


        /// <summary>
        /// Force settings refresh when general settings window is showing.
        /// </summary>
        /// <param name="window">The window.</param>
        private void OnDependencyGeneralSettingsWindowShowing(Window window)
        {
            var toolWindow = FindToolWindow(typeof(GeneralSettingsToolWindowPane), 0, false);
            if (toolWindow == null || TeamFoundationServerExt == null || TeamFoundationServerExt.ActiveProjectContext == null)
            {
                return;
            }

            if (window.Caption.Equals(toolWindow.Caption))
            {
                try
                {
                    ((GeneralSettingsEditorViewModel)((GeneralSettingsEditorView)toolWindow.Content).DataContext).LoadSettings();
                }
                catch
                {
                    _outputWindowPaneLogger.LogMsg(
                        "Failed to load settings from team foundation server. Make sure you have permission to read from team foundation registry.");
                }
            }
        }

        /// <summary>
        /// Force settings refresh when personal settings window is showing.
        /// </summary>
        /// <param name="window">The window.</param>
        private void OnDependencyPersonalSettingsWindowShowing(Window window)
        {
            var toolWindow = FindToolWindow(typeof(PersonalSettingsToolWindowPane), 0, false);
            if (toolWindow == null || TeamFoundationServerExt == null || TeamFoundationServerExt.ActiveProjectContext == null)
            {
                return;
            }

            if (window.Caption.Equals(toolWindow.Caption))
            {
                try
                {
                    ((PersonalSettingsEditorViewModel)((PersonalSettingsEditorView)toolWindow.Content).DataContext).LoadSettings();
                }
                catch
                {
                    _outputWindowPaneLogger.LogMsg(
                        "Failed to load settings from team foundation server. Make sure you have permission to read from team foundation registry.");
                }
            }
        }

        /// <summary>
        /// Load settings if project context changes.
        /// </summary>
        /// <param name="sender">The sender of the event. </param>
        /// <param name="eventArgs">The event args.</param>
        private void TeamFoundationServerExtOnProjectContextChanged(object sender, EventArgs eventArgs)
        {
            try
            {
                _settings.Load(TeamFoundationServerExt.ActiveProjectContext.DomainUri);
            }
            catch
            {
                _outputWindowPaneLogger.LogMsg(
                    "Failed to load settings from team foundation server. Make sure you have permission to read from team foundation registry.");
            }
        }

        #endregion

        #region IDisposable Pattern
        /// <summary>
        /// Releases the resources used by the Package object.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        //MRI: Editor package
        /// <summary>
        /// Releases the resources used by the Package object.
        /// </summary>
        /// <param name="disposing">This parameter determines whether the method has been called directly or indirectly by a user's code.</param>
        protected override void Dispose(bool disposing)
        {
            try
            {
                Trace.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering Dispose() of: {0}", ToString()));
                if (disposing)
                {
                    if (editorFactory != null)
                    {
                        editorFactory.Dispose();
                        editorFactory = null;
                    }

                    // Unregister to window visibility events
                    // ReSharper disable UseIndexedProperty
                    var windowVisibilityEvents = ((Events2)DevEnv.Events).get_WindowVisibilityEvents();
                    // ReSharper restore UseIndexedProperty
                    windowVisibilityEvents.WindowShowing -= OnDependencyGeneralSettingsWindowShowing;
                    windowVisibilityEvents.WindowShowing -= OnDependencyPersonalSettingsWindowShowing;

                    GC.SuppressFinalize(this);
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }
        #endregion

        #region Callback Methods

        /// <summary>
        /// Query the status for the "User Help" menu item
        /// </summary>
        /// <param name="sender">OleMenuCommand</param>
        /// <param name="e">Event args</param>
        void userHelpToolsMenuMenuItem_BeforeQueryStatus(object sender, EventArgs e)
        {
            var userHelpCommand = sender as OleMenuCommand;
            if (null != userHelpCommand)
            {
                setStatus_UserHelpToolsMenuMenuItem(userHelpCommand);
            }
        }

        /// <summary>
        /// Query the status for the "Get Dependencies" and "Force Get Dependencies" in solution explorer/ source control explorer context menu
        /// </summary>
        /// <param name="sender">OleMenuCommand</param>
        /// <param name="e">Event args</param>
        void getDependenciesContextMenuItems_BeforeQueryStatus(object sender, EventArgs e)
        {
            var getDependenciesCommand = sender as OleMenuCommand;
            if (null != getDependenciesCommand)
            {
                setStatus_GetDependenciesContextMenuItems(getDependenciesCommand);
            }
        }

        /// <summary>
        /// Query status for the "Clean Dependency" in solution explorer/ source control explorer context menu
        /// </summary>
        /// <param name="sender">OleMenuCommand</param>
        /// <param name="e">Event args</param>
        void cleanDependenciesContextMenuItems_BeforeQueryStatus(object sender, EventArgs e)
        {
            var cleanDependenciesCommand = sender as OleMenuCommand;
            if (null != cleanDependenciesCommand)
            {
                setStatus_CleanDependenciesContextMenuItems(cleanDependenciesCommand);
            }
        }

        /// <summary>
        /// Query status for the "Create component.targets" in solution explorer/ source control explorer context menu
        /// </summary>
        /// <param name="sender">OleMenuCommand</param>
        /// <param name="e">Event args</param>
        void createComponentTargetsContextMenuItems_BeforeQueryStatus(object sender, EventArgs e)
        {
            var createComponentTargetsCommand = sender as OleMenuCommand;
            if (null != createComponentTargetsCommand)
            {
                setStatus_CreateComponentTargetsContextMenuItem(createComponentTargetsCommand);
            }
        }

        /// <summary>
        /// Callback for Visual Studio build events.
        /// </summary>
        /// <param name="scope">The Visual Studio build scope.</param>
        /// <param name="action">The Visual Studio build action.</param>
        private void BuildEvents_OnBuildBegin(vsBuildScope scope, vsBuildAction action)
        {
            var outputBuildPaneLogger = new OutputWindowLogger(DevEnv, "Build");

            if (scope == vsBuildScope.vsBuildScopeSolution)
            {
                // Determine Solution and component.targets file
                var solutionFilename = DevEnv.Solution.FileName;
                if (!string.IsNullOrEmpty(solutionFilename))
                {
                    outputBuildPaneLogger.LogMsg("---------- Dependency Management ----------");

                    var workspace = GetWorkspaceForLocalFile(solutionFilename);
                    if (workspace == null)
                    {
                        outputBuildPaneLogger.LogMsg(string.Format("Could not find workspace for solution {0}\n", solutionFilename));
                        outputBuildPaneLogger.LogMsg("========== Dependency Management ==========\n");
                        outputBuildPaneLogger.ShowMessages();
                        return;
                    }

                    // Refresh settings before local build is started
                    if (TeamFoundationServerExt != null && TeamFoundationServerExt.ActiveProjectContext != null)
                    {
                        _settings.Load(TeamFoundationServerExt.ActiveProjectContext.DomainUri);
                    }

                    // Return if local build is not enabled in dependency management settings
                    if (_settings.FetchDependenciesOnLocalSolutionBuild == false)
                    {
                        return;
                    }

                    // Settings to determine for DependencyService
                    if (_settings.ValidDependencyDefinitionFileExtension == null ||
                       _settings.ValidDependencyDefinitionFileExtension.Any(string.IsNullOrEmpty))
                    {
                        outputBuildPaneLogger.LogMsg("Dependency definition file extension was invalid. Please check Dependency Manager settings!");
                        outputBuildPaneLogger.LogMsg("========== Dependency Management ==========\n");
                        outputBuildPaneLogger.ShowMessages();
                        return;
                    }

                    var dependencyDefinitionFileName = string.Concat("component", _settings.ValidDependencyDefinitionFileExtension.FirstOrDefault());
                    var dependencyDefinitionFile = Path.Combine(Path.GetDirectoryName(solutionFilename), dependencyDefinitionFileName);
                    if (string.IsNullOrEmpty(dependencyDefinitionFile) || !File.Exists(dependencyDefinitionFile))
                    {
                        outputBuildPaneLogger.LogMsg(string.Format("Could not find dependency definition file {0} in solution folder {1}!", dependencyDefinitionFileName, Path.GetDirectoryName(solutionFilename)));
                        outputBuildPaneLogger.LogMsg("========== Dependency Management ==========\n");
                        outputBuildPaneLogger.ShowMessages();
                        return;
                    }

                    switch (action)
                    {
                        case vsBuildAction.vsBuildActionBuild:
                            try
                            {
                                outputBuildPaneLogger.LogMsg(string.Format("Get dependencies (Dependency definition file: {0}):", dependencyDefinitionFile));
                                outputBuildPaneLogger.ShowMessages();

                                ShowOperationProgressInStatusBar("Fetching dependencies ...");
                                TriggerGetDependencies(workspace, dependencyDefinitionFile, false, true, outputBuildPaneLogger, null, true);
                                outputBuildPaneLogger.ShowMessages();
                                ShowOperationFinishInStatusbar("Fetching dependencies succeeded");
                            }
                            catch (Exception exc)
                            {
                                outputBuildPaneLogger.LogMsg("\nFatal error occured while fetching dependencies. Aborting ...");
                                outputBuildPaneLogger.LogMsg(string.Format("Exception message: {0}", exc.Message));
                                outputBuildPaneLogger.LogMsg(string.Format("Stacktrace:\n{0}\n", exc.StackTrace));
                                outputBuildPaneLogger.ShowMessages();
                                ShowOperationFinishInStatusbar("Fetching dependencies failed!");
                            }
                            break;
                        case vsBuildAction.vsBuildActionRebuildAll:
                            try
                            {
                                outputBuildPaneLogger.LogMsg(string.Format("Rebuild dependencies (Dependency definition file: {0}):", dependencyDefinitionFile));
                                outputBuildPaneLogger.ShowMessages();

                                ShowOperationProgressInStatusBar("Rebuilding dependencies ...");
                                TriggerGetDependencies(workspace, dependencyDefinitionFile, false, true, outputBuildPaneLogger, null, true);
                                ShowOperationFinishInStatusbar("Rebuild dependencies succeeded");
                            }
                            catch (Exception exc)
                            {
                                outputBuildPaneLogger.LogMsg("\nFatal error occured while rebuilding dependencies. Aborting ...");
                                outputBuildPaneLogger.LogMsg(string.Format("Exception message: {0}", exc.Message));
                                outputBuildPaneLogger.LogMsg(string.Format("Stacktrace:\n{0}\n", exc.StackTrace));
                                outputBuildPaneLogger.ShowMessages();
                                ShowOperationFinishInStatusbar("Rebuild dependencies failed!");
                            }

                            break;
                    }
                }

                outputBuildPaneLogger.LogMsg("========== Dependency Management ==========\n");
            }
        }

        #endregion

        #region Set Status Methods

        /// <summary>
        /// Sets the status of the user help menu item.
        /// </summary>
        /// <param name="userHelpToolsMenuMenuItem">Menu item to activate</param>
        void setStatus_UserHelpToolsMenuMenuItem(OleMenuCommand userHelpToolsMenuMenuItem)
        {
            userHelpToolsMenuMenuItem.Enabled = true;
        }

        /// <summary>
        /// Displays the "Get Dependencies" and "Force Get Dependencies" if component.targets is selected (in Source Control Explorer or Solution Explorer)
        /// or if a product subfolder of a branch was selected.
        /// </summary>
        /// <param name="getDependenciesContextMenuItem">Menu item to set visiblility</param>
        void setStatus_GetDependenciesContextMenuItems(OleMenuCommand getDependenciesContextMenuItem)
        {
            // In case of Source Control Explorer
            if (getDependenciesContextMenuItem.CommandID.Equals(_getDependenciesRecursiveSourceControlID) || getDependenciesContextMenuItem.CommandID.Equals(_getDirectDependenciesSourceControlID) ||
                getDependenciesContextMenuItem.CommandID.Equals(_forcedGetDependenciesRecursiveSourceControlID) || getDependenciesContextMenuItem.CommandID.Equals(_forcedGetDirectDependenciesSourceControlID))
            {
                var items = VersionControlExt.Explorer.SelectedItems.Select(x => x.TargetServerPath).ToArray();

                try
                {
                    // Check if selected file equals with dependency definition filename
                    if (items.Any() && _settings.ValidDependencyDefinitionFileExtension.Contains(Path.GetExtension(items.ElementAt(0)), StringComparer.OrdinalIgnoreCase))
                    {
                        getDependenciesContextMenuItem.Visible = true;
                    }
                    else
                    {
                        getDependenciesContextMenuItem.Visible = false;
                    }
                }
                catch (VersionControlException)
                {
                    getDependenciesContextMenuItem.Visible = false;
                }
            }
            else if (getDependenciesContextMenuItem.CommandID.Equals(_getDependenciesRecursiveSolutionID) || getDependenciesContextMenuItem.CommandID.Equals(_getDirectDependenciesSolutionID) ||
                     getDependenciesContextMenuItem.CommandID.Equals(_forcedGetDependenciesRecursiveSolutionID) || getDependenciesContextMenuItem.CommandID.Equals(_forcedGetDirectDependenciesSolutionID))
            {
                // See if a component.targets file was selected
                var items = DevEnv.SelectedItems;
                // If it is a component.targets file in the solution
                if (items.Count != 0 && _settings.ValidDependencyDefinitionFileExtension.Contains(Path.GetExtension(items.Item(1).Name), StringComparer.OrdinalIgnoreCase))
                {
                    getDependenciesContextMenuItem.Visible = true;
                }
                // Otherwise we do not show the clean dependencies menu item
                else
                {
                    getDependenciesContextMenuItem.Visible = false;
                }
            }
        }

        /// <summary>
        /// Displays the "Clean Dependencies" if component.targets is selected (in Source Control Explorer or Solution Explorer)
        /// or if a product subfolder of a branch was selected.
        /// </summary>
        /// <param name="cleanDependenciesContextMenuItem"></param>
        void setStatus_CleanDependenciesContextMenuItems(OleMenuCommand cleanDependenciesContextMenuItem)
        {
            // In case of Source Control Explorer
            if (cleanDependenciesContextMenuItem.CommandID.Equals(_cleanDependenciesSourceControlID))
            {
                var items = VersionControlExt.Explorer.SelectedItems.Select(x => x.TargetServerPath).ToArray();

                try
                {
                    // Check if selected file equals with dependency definition filename
                    if (items.Any() && _settings.ValidDependencyDefinitionFileExtension.Contains(Path.GetExtension(items.ElementAt(0)), StringComparer.OrdinalIgnoreCase))
                    {
                        cleanDependenciesContextMenuItem.Visible = true;
                    }
                    else
                    {
                        cleanDependenciesContextMenuItem.Visible = false;
                    }
                }
                catch(VersionControlException)
                {
                    cleanDependenciesContextMenuItem.Visible = false;
                }
            }
            else if (cleanDependenciesContextMenuItem.CommandID.Equals(_cleanDependenciesSolutionID))
            {
                // See if a component.targets file was selected
                var items = DevEnv.SelectedItems;
                // If it is a component.targets file in the solution
                if (items.Count != 0 && _settings.ValidDependencyDefinitionFileExtension.Contains(Path.GetExtension(items.Item(1).Name), StringComparer.OrdinalIgnoreCase))
                {
                    cleanDependenciesContextMenuItem.Visible = true;
                }
                // Otherwise we do not show the clean dependencies menu item
                else
                {
                    cleanDependenciesContextMenuItem.Visible = false;
                }
            }
        }

        /// <summary>
        /// Display "Create component.targets" if (product) parent folder is branch subfolder and folder has no component.targets.
        /// </summary>
        /// <param name="createComponentTargetsContextMenuItem"></param>
        void setStatus_CreateComponentTargetsContextMenuItem(OleMenuCommand createComponentTargetsContextMenuItem)
        {
            var items = VersionControlExt.Explorer.SelectedItems.Select(x => x.TargetServerPath).ToArray();

            // Check if folder is one of the subfolders of a valid path to the root/source/branch folder && component.targets exists in folder
            try
            {
                // Check if selected file equals with dependency definition filename
                if (items.Any() && IsFolder(items.ElementAt(0)))
                {
                        createComponentTargetsContextMenuItem.Visible = true;
                }
                else
                {
                    createComponentTargetsContextMenuItem.Visible = false;
                }
            }
            catch (VersionControlException)
            {
                createComponentTargetsContextMenuItem.Visible = false;
            }
        }
        #endregion

        #region Callback methods

        /// <summary>
        /// Get dependencies into local workspace
        /// </summary>
        /// <param name="sender">OleMenuCommand command</param>
        /// <param name="e">Event args</param>
        private void GetDependenciesRecursiveCallback(object sender, EventArgs e)
        {
            GetDependenciesCommonCallback(sender, e, false, true);
        }

        /// <summary>
        /// Get dependencies into local workspace
        /// </summary>
        /// <param name="sender">OleMenuCommand command</param>
        /// <param name="e">Event args</param>
        private void GetDirectDependenciesCallback(object sender, EventArgs e)
        {
            GetDependenciesCommonCallback(sender, e, false, false);
        }

        /// <summary>
        /// Forced get dependencies into local workspace
        /// </summary>
        /// <param name="sender">OleMenuCommand command</param>
        /// <param name="e">Event args</param>
        private void ForcedGetDependenciesRecursiveCallback(object sender, EventArgs e)
        {
            GetDependenciesCommonCallback(sender, e, true, true);
        }

        /// <summary>
        /// Forced get dependencies into local workspace
        /// </summary>
        /// <param name="sender">OleMenuCommand command</param>
        /// <param name="e">Event args</param>
        private void ForcedGetDirectDependenciesCallback(object sender, EventArgs e)
        {
            GetDependenciesCommonCallback(sender, e, true, false);
        }

        /// <summary>
        /// Fetch dependencies into local workspace
        /// </summary>
        /// <param name="sender">OleMenuCommand command</param>
        /// <param name="e">Event args</param>
        /// <param name="forcedGet">Force dependency fetching.</param>
        /// <param name="recursive">Fetch dependencies recursively.</param>
        private void GetDependenciesCommonCallback(object sender, EventArgs e, bool forcedGet, bool recursive)
        {
            var sendingCommand = sender as OleMenuCommand;

            // Settings to determine for DependencyService
            if (_settings.ValidDependencyDefinitionFileExtension == null ||
               _settings.ValidDependencyDefinitionFileExtension.Any(string.IsNullOrEmpty))
            {
                _outputWindowPaneLogger.LogMsg("Get dependencies:\nDependency definition file extension was invalid. Please check Dependency Manager settings!\n");
                _outputWindowPaneLogger.ShowMessages();
                return;
            }

            if (string.IsNullOrEmpty(_settings.RelativeOutputPath))
            {
                _outputWindowPaneLogger.LogMsg("Get dependencies:\nPreconfigured relative output path was invalid. Please check Dependency Manager settings!");
                _outputWindowPaneLogger.ShowMessages();
                return;
            }

            string localPath;
            Workspace workspace = null;

            if (sendingCommand.CommandID.Equals(_getDependenciesRecursiveSourceControlID) || sendingCommand.CommandID.Equals(_getDirectDependenciesSourceControlID) ||
                sendingCommand.CommandID.Equals(_forcedGetDependenciesRecursiveSourceControlID) || sendingCommand.CommandID.Equals(_forcedGetDirectDependenciesSourceControlID))
            {
                try
                {
                    localPath = GetLocalPathFromSelectedItemInSCExplorer(ref workspace);
                }
                catch (ApplicationException ae)
                {
                    _outputWindowPaneLogger.LogMsg(string.Format("Get dependencies:\n{0}\n", ae.Message));
                    _outputWindowPaneLogger.ShowMessages();
                    return;
                }
            }
            else if (sendingCommand.CommandID.Equals(_getDependenciesRecursiveSolutionID) || sendingCommand.CommandID.Equals(_getDirectDependenciesSolutionID) ||
                     sendingCommand.CommandID.Equals(_forcedGetDependenciesRecursiveSolutionID) || sendingCommand.CommandID.Equals(_forcedGetDirectDependenciesSolutionID))
            {
                var filePathForCurrentlySelectedSolutionItem = GetFilePathForCurrentlySelectedSolutionItem();

                // Check if an item was selected and if this item is the component.targets file
                if (string.IsNullOrEmpty(filePathForCurrentlySelectedSolutionItem) || !_settings.ValidDependencyDefinitionFileExtension.Contains(Path.GetExtension(filePathForCurrentlySelectedSolutionItem), StringComparer.OrdinalIgnoreCase))
                {
                    _outputWindowPaneLogger.LogMsg(string.Format("Get dependencies:\nThe file selected is not a valid dependency definition file. Please select a dependency definition file (For example component{0})!\n", _settings.ValidDependencyDefinitionFileExtension));
                    _outputWindowPaneLogger.ShowMessages();
                    return;
                }

                try
                {
                    localPath = GetLocalDependencyDefinitionFile(filePathForCurrentlySelectedSolutionItem, ref workspace);
                }
                catch (ApplicationException ae)
                {
                    _outputWindowPaneLogger.LogMsg(string.Format("Get dependencies:\n{0}\n", ae.Message));
                    _outputWindowPaneLogger.ShowMessages();
                    return;
                }
            }
            else
            {
                return;
            }

            try
            {
                _outputWindowPaneLogger.LogMsg(string.Format("Get dependencies (Dependency definition file: {0}):", localPath));
                _outputWindowPaneLogger.ShowMessages();

                ShowOperationProgressInStatusBar(
                    forcedGet
                    ? string.Format("Refetching {0} dependencies ...", recursive ? "all" : "direct")
                    : string.Format("Fetching {0} dependencies ...", recursive ? "all" : "direct"));
                TriggerGetDependencies(
                    workspace,
                    localPath,
                    forcedGet,
                    recursive,
                    _outputWindowPaneLogger,
                    () =>
                    ShowOperationFinishInStatusbar(
                        forcedGet
                            ? string.Format("Refetching {0} dependencies succeeded", recursive ? "all" : "direct")
                            : string.Format("Fetching {0} dependencies succeeded", recursive ? "all" : "direct")));
            }
            catch (Exception exc)
            {
                _outputWindowPaneLogger.LogMsg("\nFatal error occured while fetching dependencies. Aborting ...");
                _outputWindowPaneLogger.LogMsg(string.Format("Exception message: {0}", exc.Message));
                _outputWindowPaneLogger.LogMsg(string.Format("Stacktrace:\n{0}\n", exc.StackTrace));
                ShowOperationFinishInStatusbar("Fetching dependencies failed!");
            }
        }

        /// <summary>
        /// Triggers the get dependencies. Get dependencies can be called asynchronously (default) or synchronously.
        /// </summary>
        /// <param name="workspace">The workspace.</param>
        /// <param name="localPath">The local path.</param>
        /// <param name="forcedGet">if set to <c>true</c> forced get for all dependencies.</param>
        /// <param name="recursive">if set to <c>true</c> dependencies are fetched recursively.</param>
        /// <param name="loggingWindow">The logging window instance.</param>
        /// <param name="callbackAfterGetDependenciesFinished">The method for callback.</param>
        /// <param name="runSynchronously">Set to <c>true</c> to run operation synchronously</param>
        private void TriggerGetDependencies(Workspace workspace, string localPath, bool forcedGet, bool recursive, OutputWindowLogger loggingWindow, Action callbackAfterGetDependenciesFinished, bool runSynchronously = false)
        {
            var componentTargetsFolder = Path.GetDirectoryName(localPath);
            var settingsToUse =
                CreateDependencyServiceSettings(workspace, TeamFoundationServerExt.ActiveProjectContext.DomainUri, componentTargetsFolder, Path.GetFileName(localPath));

            IDependencyService ds = new DependencyService(settingsToUse);

            if (runSynchronously)
            {
                var graph = ds.GetDependencyGraph(localPath, loggingWindow);
                ds.DownloadGraph(graph, loggingWindow, recursive, forcedGet);
            }
            else
            {
                var dmus = new DependencyManagerUserState(ds, loggingWindow, callbackAfterGetDependenciesFinished, forcedGet, recursive);
                ds.BeginGetDependencyGraph(localPath, loggingWindow, OnGetDependencyGraphForGetDependenciesCompleted, dmus);
            }
        }

        /// <summary>
        /// This method is called when async generation of dependency graph (for get dependencies) completed.
        /// </summary>
        /// <param name="asyncResult">The async result.</param>
        private void OnGetDependencyGraphForGetDependenciesCompleted(IAsyncResult asyncResult)
        {
            try
            {
                var dmus = asyncResult.AsyncState as DependencyManagerUserState;
                if (dmus == null)
                {
                    _outputWindowPaneLogger.LogMsg(string.Format("  ! Error while creating dependency graph [Invalid user state found]!\n"));
                    return;
                }

                var graph = dmus.DependencyService.EndGetDependencyGraph(asyncResult);
                dmus.DependencyService.BeginDownloadGraph(graph, dmus.Logger, OnDownloadGraphForGetDependenciesCompleted, dmus, dmus.Recursive, dmus.ForceOperation);
            }
            catch (DependencyServiceException dse)
            {
                _outputWindowPaneLogger.LogMsg(string.Format("  ! {0}\n", dse.Message));
            }
        }

        /// <summary>
        /// This method is called when async downloading of dependencies (for get dependencies) completed.
        /// </summary>
        /// <param name="asyncResult">The async result.</param>
        private void OnDownloadGraphForGetDependenciesCompleted(IAsyncResult asyncResult)
        {
            try
            {
                var dmus = asyncResult.AsyncState as DependencyManagerUserState;
                if (dmus == null)
                {
                    _outputWindowPaneLogger.LogMsg(string.Format("  ! Error while downloading dependencies [Invalid user state found]!\n"));
                    return;
                }

                dmus.DependencyService.EndDownloadGraph(asyncResult);

                // Call callback method
                if (dmus.Callback != null)
                {
                    dmus.Callback();
                }
            }
            catch (DependencyServiceException dse)
            {
                _outputWindowPaneLogger.LogMsg(string.Format("  ! {0}\n", dse.Message));
            }
        }

        /// <summary>
        /// Clean dependencies from local workspace
        /// </summary>
        /// <param name="sender">OleMenuCommand command</param>
        /// <param name="e">Event args</param>
        private void CleanDependenciesCallback(object sender, EventArgs e)
        {
            var sendingCommand = sender as OleMenuCommand;

            // Settings to determine for DependencyService
            if (_settings.ValidDependencyDefinitionFileExtension == null ||
                _settings.ValidDependencyDefinitionFileExtension.Any(x => string.IsNullOrEmpty(x)))
            {
                _outputWindowPaneLogger.LogMsg("Clean dependencies:\nDependency definition file extension was invalid. Please check Dependency Manager settings!\n");
                _outputWindowPaneLogger.ShowMessages();
                return;
            }
            if (String.IsNullOrEmpty(_settings.RelativeOutputPath))
            {
                _outputWindowPaneLogger.LogMsg("Clean dependencies:\nPreconfigured relative output path was invalid. Please check Dependency Manager settings!");
                _outputWindowPaneLogger.ShowMessages();
                return;
            }
            string localPath = null;
            Workspace workspace = null;

            if (sendingCommand.CommandID.Equals(_cleanDependenciesSourceControlID))
            {
                try
                {
                    localPath = GetLocalPathFromSelectedItemInSCExplorer(ref workspace);
                }
                catch (ApplicationException ae)
                {
                    _outputWindowPaneLogger.LogMsg(string.Format("Clean dependencies:\n{0}\n", ae.Message));
                    _outputWindowPaneLogger.ShowMessages();
                    return;
                }
            }
            else if (sendingCommand.CommandID.Equals(_cleanDependenciesSolutionID))
            {
                var filePathForCurrentlySelectedSolutionItem = GetFilePathForCurrentlySelectedSolutionItem();

                // Check if an item was selected and if this item is the component.targets file
                if (String.IsNullOrEmpty(filePathForCurrentlySelectedSolutionItem) || !_settings.ValidDependencyDefinitionFileExtension.Contains(Path.GetExtension(filePathForCurrentlySelectedSolutionItem), StringComparer.OrdinalIgnoreCase))
                {
                    _outputWindowPaneLogger.LogMsg(String.Format("Clean dependencies:\nThe file selected is not a valid dependency definition file. Please select a dependency definition file (For example component{0})!\n", _settings.ValidDependencyDefinitionFileExtension));
                    _outputWindowPaneLogger.ShowMessages();
                    return;
                }

                try
                {
                    localPath = GetLocalDependencyDefinitionFile(filePathForCurrentlySelectedSolutionItem, ref workspace);
                }
                catch (ApplicationException ae)
                {
                    _outputWindowPaneLogger.LogMsg(string.Format("Clean dependencies:\n{0}\n", ae.Message));
                    _outputWindowPaneLogger.ShowMessages();
                    return;
                }
            }
            else
            {
                return;
            }

            try
            {
                _outputWindowPaneLogger.LogMsg(string.Format("Clean dependencies (Dependency definition file: {0}):", localPath));
                _outputWindowPaneLogger.ShowMessages();

                ShowOperationProgressInStatusBar("Cleaning dependencies started ...");
                TriggerCleanDependencies(workspace, localPath, _outputWindowPaneLogger, () => ShowOperationFinishInStatusbar("Cleaning dependencies succeeded"));
            }
            catch (Exception exc)
            {
                _outputWindowPaneLogger.LogMsg("\nFatal error occured while cleaning up dependencies. Aborting ...");
                _outputWindowPaneLogger.LogMsg(string.Format("Exception message: {0}", exc.Message));
                _outputWindowPaneLogger.LogMsg(string.Format("Stacktrace:\n{0}\n", exc.StackTrace));
                ShowOperationFinishInStatusbar("Cleaning dependencies failed!");
                return;
            }
        }

        /// <summary>
        /// Triggers the clean dependencies.
        /// </summary>
        /// <param name="workspace">The workspace to use.</param>
        /// <param name="localPath">The dependency definition file local path.</param>
        /// <param name="loggingWindow">The logging window.</param>
        /// <param name="callbackAfterCleanDependenciesFinished">Callback method which is calling after clean dependencies finishes.</param>
        /// <param name="runSynchronously">Set to <c>true</c> to run operation synchronously</param>
        private void TriggerCleanDependencies(Workspace workspace, string localPath, OutputWindowLogger loggingWindow, Action callbackAfterCleanDependenciesFinished, bool runSynchronously = false)
        {
            var componentTargetsFolder = Path.GetDirectoryName(localPath);
            var settingsToUse =
                CreateDependencyServiceSettings(workspace, TeamFoundationServerExt.ActiveProjectContext.DomainUri, componentTargetsFolder, Path.GetFileName(localPath));

            IDependencyService ds = new DependencyService(settingsToUse);

            if (runSynchronously)
            {
                var graph = ds.GetDependencyGraph(localPath, loggingWindow);
                ds.CleanupGraph(graph, loggingWindow);
            }
            else
            {
                var dmus = new DependencyManagerUserState(ds, loggingWindow, callbackAfterCleanDependenciesFinished);
                ds.BeginGetDependencyGraph(localPath, loggingWindow, OnGetDependencyGraphForCleanDependenciesCompleted, dmus);
            }
        }

        /// <summary>
        /// This method is called when async generation of dependency graph (for cleanup dependencies) completed.
        /// </summary>
        /// <param name="asyncResult">The async result.</param>
        private void OnGetDependencyGraphForCleanDependenciesCompleted(IAsyncResult asyncResult)
        {
            try
            {
                var dmus = asyncResult.AsyncState as DependencyManagerUserState;
                if (dmus == null)
                {
                    _outputWindowPaneLogger.LogMsg(string.Format("  ! Error while creating dependency graph [Invalid user state found]!\n"));
                    return;
                }

                var graph = dmus.DependencyService.EndGetDependencyGraph(asyncResult);
                dmus.DependencyService.BeginCleanupGraph(graph, dmus.Logger, OnCleanupGraphForCleanDependenciesCompleted, dmus);
            }
            catch (DependencyServiceException dse)
            {
                _outputWindowPaneLogger.LogMsg(string.Format("  ! {0}\n", dse.Message));
            }
        }

        /// <summary>
        /// This method is called when async cleaning up of dependencies (for cleanup dependencies) completed.
        /// </summary>
        /// <param name="asyncResult">The async result.</param>
        private void OnCleanupGraphForCleanDependenciesCompleted(IAsyncResult asyncResult)
        {
            try
            {
                var dmus = asyncResult.AsyncState as DependencyManagerUserState;
                if (dmus == null)
                {
                    _outputWindowPaneLogger.LogMsg(string.Format("  ! Error while cleaning up dependencies [Invalid user state found]!\n"));
                    return;
                }

                dmus.DependencyService.EndCleanupGraph(asyncResult);

                // Call callback method
                if(dmus.Callback != null)
                    dmus.Callback();
            }
            catch (DependencyServiceException dse)
            {
                _outputWindowPaneLogger.LogMsg(string.Format("  ! {0}\n", dse.Message));
            }
        }

        /// <summary>
        /// Shows a list of dependencies found in this solution.
        /// </summary>
        /// <param name="sender">OleMenuCommand command</param>
        /// <param name="e">Event args</param>
        private void ViewDependenciesCallback(object sender, EventArgs e)
        {
            _outputWindowPaneLogger.LogMsg("View dependencies list ...");
            // FIXME: Implement view dependency list functionality in later release
            _outputWindowPaneLogger.LogMsg("View depedencies list is not supported in this release");
            _outputWindowPaneLogger.ShowMessages();
        }

        /// <summary>
        /// Opens the user help pdf in a new browser instance.
        /// </summary>
        /// <param name="sender">OleMenuCommand command</param>
        /// <param name="e">Event args</param>
        private void UserHelpCallback(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("http://www.aitgmbh.de/fileadmin/user_upload/downloads/AIT_DependencyManager_User_Guide_v14.pdf");
        }

        /// <summary>
        /// Shows the general settings editor as a pane in the visual studio tool window
        /// </summary>
        /// <param name="sender">OleMenuCommand command</param>
        /// <param name="e">Event args</param>
        private void OpenGeneralSettingsToolWindowCallback(object sender, EventArgs e)
        {
            // Get the instance number 0 of this tool window. This window is single instance so this instance
            // is actually the only one.
            // The last flag is set to true so that if the tool window does not exists it will be created.
            var window = FindToolWindow(typeof(GeneralSettingsToolWindowPane), 0, true);

            if ((null == window) || (null == window.Frame))
            {
                _outputWindowPaneLogger.LogMsg("Failed to create tool window.");
            }
            else
            {
                var windowFrame = (IVsWindowFrame)window.Frame;
                // Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
                windowFrame.Show();
            }
        }

        /// <summary>
        /// Shows the personal settings editor as a pane in the visual studio tool window
        /// </summary>
        /// <param name="sender">OleMenuCommand command</param>
        /// <param name="e">Event args</param>
        private void OpenPersonalSettingsToolWindowCallback(object sender, EventArgs e)
        {
            // Get the instance number 0 of this tool window. This window is single instance so this instance
            // is actually the only one.
            // The last flag is set to true so that if the tool window does not exists it will be created.
            var window = FindToolWindow(typeof(PersonalSettingsToolWindowPane), 0, true);

            if ((null == window) || (null == window.Frame))
            {
                _outputWindowPaneLogger.LogMsg("Failed to create tool window.");
            }
            else
            {
                var windowFrame = (IVsWindowFrame)window.Frame;
                // Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
                windowFrame.Show();
            }
        }

        /// <summary>
        /// Create component.targets in the current selected directory and add to source control.
        /// </summary>
        /// <param name="sender">OleMenuCommand command</param>
        /// <param name="e">Event arguments</param>
        private void CreateComponentTargetsCallback(object sender, EventArgs e)
        {
            try
            {
                var items = VersionControlExt.Explorer.SelectedItems.Select(x => x.TargetServerPath).ToArray();

                if (items.Length == 0)
                {
                    _outputWindowPaneLogger.LogMsg(string.Format("Create dependency definition file: \nPlease select a folder in Source Control Explorer!\n"));
                    _outputWindowPaneLogger.ShowMessages();
                    return;
                }

                if(ComponentTargetsIsPresentInFolder(items.First()))
                {
                    _outputWindowPaneLogger.LogMsg(string.Format("Create dependency definition file: \nDependency definition file is already present in folder {0}!\n", items.First()));
                    _outputWindowPaneLogger.ShowMessages();
                    return;
                }

                if(ComponentTargetsAlreadyPresentInPendingChanges(items.First()))
                {
                    _outputWindowPaneLogger.LogMsg(string.Format("Create dependency definition file: \nA dependency definition file exists already in pending changes for folder {0}!\n", items.First()));
                    _outputWindowPaneLogger.ShowMessages();
                    return;
                }
            }
            catch (VersionControlException)
            {
                _outputWindowPaneLogger.LogMsg(string.Format("Create dependency definition file: \nAn error occured while creating a new dependency definition file!\n"));
                _outputWindowPaneLogger.ShowMessages();
                return;
            }

            var workspace = VersionControlExt.Explorer.Workspace;
            if (workspace == null)
            {
                return;
            }

            var explorerItem = VersionControlExt.Explorer.SelectedItems[0];
            var localpath = workspace.TryGetLocalItemForServerItem(explorerItem.SourceServerPath);

            if (string.IsNullOrEmpty(localpath))
            {
                _outputWindowPaneLogger.LogMsg(string.Format("Create dependency definition file: \nNo workspace mapping was found for server path {0}. Please add a workspace mapping!\n", explorerItem.SourceServerPath));
                _outputWindowPaneLogger.ShowMessages();
                return;
            }

            try
            {
                var newDependencyDefinitionPath = Path.Combine(localpath, string.Concat("component", _settings.ValidDependencyDefinitionFileExtension.First()));
                var fi = new FileInfo(newDependencyDefinitionPath);
                var compTargetsWriter = fi.CreateText();
                compTargetsWriter.Write(_settings.DependencyDefinitionFileTemplate);
                compTargetsWriter.Flush();
                compTargetsWriter.Close();
                workspace.PendAdd(newDependencyDefinitionPath);

                _outputWindowPaneLogger.LogMsg(string.Format("Create dependency definition file: \nDependency definition file created in directory {0}\n", localpath));
                _outputWindowPaneLogger.ShowMessages();
            }
            catch (DirectoryNotFoundException)
            {
                _outputWindowPaneLogger.LogMsg(string.Format("Create dependency definition file: \nDestination folder from workspace mapping does not exist {0}. Please check workspace!\n", localpath));
                _outputWindowPaneLogger.ShowMessages();
            }
        }

        #endregion

        #region Helper methods

        /// <summary>
        /// Creates the settings for the DependencyService.
        /// </summary>
        /// <param name="workspace">The workspace.</param>
        /// <param name="tpcUri">The team project collection uri.</param>
        /// <param name="outputFolder">The output folder.</param>
        /// <returns></returns>
        private ISettings<ServiceValidSettings> CreateDependencyServiceSettings(Workspace workspace, string tpcUri, string outputFolder, string selectedDependencyDefinitionFileName)
        {
            ISettings<ServiceValidSettings> settingsToUse = new Settings<ServiceValidSettings>();
            // Default dependency definition file name
            var dependencyDefinitionFileNameList = string.Join(
                ";", _settings.ValidDependencyDefinitionFileExtension.Select(x => string.Concat("component", x)));
            if (!dependencyDefinitionFileNameList.Contains(selectedDependencyDefinitionFileName))
            {
                dependencyDefinitionFileNameList = string.Concat(
                    selectedDependencyDefinitionFileName, ";", dependencyDefinitionFileNameList);
            }
            settingsToUse.AddSetting(new KeyValuePair<ServiceValidSettings, string>(ServiceValidSettings.DefaultDependencyDefinitionFilename, dependencyDefinitionFileNameList));
            // Default TFS settings
            settingsToUse.AddSetting(new KeyValuePair<ServiceValidSettings, string>(ServiceValidSettings.DefaultTeamProjectCollection, tpcUri));
            settingsToUse.AddSetting(new KeyValuePair<ServiceValidSettings, string>(ServiceValidSettings.DefaultWorkspaceName, workspace.Name));
            settingsToUse.AddSetting(new KeyValuePair<ServiceValidSettings, string>(ServiceValidSettings.DefaultWorkspaceOwner, workspace.OwnerName));
            // Output configuration
            settingsToUse.AddSetting(new KeyValuePair<ServiceValidSettings, string>(ServiceValidSettings.DefaultOutputBaseFolder, outputFolder));
            settingsToUse.AddSetting(new KeyValuePair<ServiceValidSettings, string>(ServiceValidSettings.DefaultRelativeOutputPath, _settings.RelativeOutputPath));
            // Binary Repository settings
            settingsToUse.AddSetting(new KeyValuePair<ServiceValidSettings, string>(ServiceValidSettings.BinaryTeamProjectCollectionUrl, _settings.BinaryRepositoryTeamProjectCollectionUrl.Equals(string.Empty) ? tpcUri : _settings.BinaryRepositoryTeamProjectCollectionUrl));
            settingsToUse.AddSetting(new KeyValuePair<ServiceValidSettings, string>(ServiceValidSettings.BinaryRepositoryTeamProject, _settings.BinaryRepositoryTeamProject));
            return settingsToUse;
        }

        /// <summary>
        /// Loads the dependency definition file
        /// </summary>
        /// <param name="path">Local or server path to dependency definition file</param>
        /// <param name="workspace">The optional workspace.</param>
        /// <returns></returns>
        private string GetLocalDependencyDefinitionFile(string path, ref Workspace workspace)
        {
            string localPathToDependencyDefinitionFile;

            if (File.Exists(path))
            {
                // Local path
                localPathToDependencyDefinitionFile = path;

                // Get workspace info
                workspace = GetWorkspaceForLocalFile(path);
            }
            else if (VersionControlPath.IsValidPath(path))
            {
                // Server path

                // Translate server path to local path
                localPathToDependencyDefinitionFile = workspace.GetLocalItemForServerItem(path);

                if (!File.Exists(localPathToDependencyDefinitionFile))
                {
                    throw new ApplicationException(
                        string.Format("Local file for given server path '{0}' does not exist. Run a Get in order to download it.", path));
                }
            }
            else
            {
                throw new ApplicationException(
                    string.Format("Path '{0}' is not a valid local or server file path", path));
            }

            return localPathToDependencyDefinitionFile;
        }

        /// <summary>
        /// Gets the workspace for local file.
        /// </summary>
        /// <param name="path">The local file path.</param>
        /// <returns></returns>
        private Workspace GetWorkspaceForLocalFile(string path)
        {
            if (!File.Exists(path))
            {
                throw new ApplicationException(
                    string.Format("Path '{0}' is not a valid local path", path));
            }

            var workstation = Workstation.Current;
            var info = workstation.GetLocalWorkspaceInfo(path);
            var collection = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(info.ServerUri);
            var workspace = info.GetWorkspace(collection);
            return workspace;
        }

        /// <summary>
        /// Determines the local file for a dependency definition file or folder with a dependency definition file.
        /// If a folder could not be determined an empty string is returned.
        /// </summary>
        /// <param name="workspace"></param>
        /// <returns>Local path or empty string</returns>
        private string GetLocalPathFromSelectedItemInSCExplorer(ref Workspace workspace)
        {
            // Check if component.targets exists in Source Control
            string selectedServerItem;

            try
            {
                var items = VersionControlExt.Explorer.SelectedItems.Select(x => x.TargetServerPath).ToArray();

                if (items.Length == 0)
                {
                    throw new ApplicationException("No file was selected in Source Control Explorer. Please select a dependency definition file first!");
                }

                if (IsFolder(items.First()) && !ComponentTargetsIsPresentInFolder(items.First()))
                {
                    throw new ApplicationException(
                        string.Format(
                            "A dependency definition file was not found in folder. Please create a dependency definition file in folder {0}!",
                            items.First()));
                }

                if (!IsFolder(items.First()) &&
                    (!_settings.ValidDependencyDefinitionFileExtension.Contains(Path.GetExtension(items.First()), StringComparer.OrdinalIgnoreCase) ||
                     !FileIsPresentOnServer(items.First())))
                {
                    throw new ApplicationException(
                        string.Format(
                            "The dependency definition file was not found in source control. Please checkin {0}!",
                            items.First()));
                }

                selectedServerItem = items.First();
            }
            catch (VersionControlException)
            {
                throw new ApplicationException("An error occured while processing dependency definition file!");
            }

            // Select selected SourceControl Explorer workspace
            workspace = VersionControlExt.Explorer.Workspace;
            if (workspace == null)
            {
                throw new ApplicationException("No workspace was found. Please create a workspace!");
            }

            string serverPath;

            if (!_settings.ValidDependencyDefinitionFileExtension.Contains(Path.GetExtension(selectedServerItem), StringComparer.OrdinalIgnoreCase))
            {
                // Folder selected
                serverPath = VersionControlPath.Combine(selectedServerItem, string.Concat("component", _settings.ValidDependencyDefinitionFileExtension.First()));
            }
            else
            {
                serverPath = selectedServerItem;
            }

            // Determine output folder for server item
            var localPath = workspace.TryGetLocalItemForServerItem(serverPath);
            if (!File.Exists(localPath))
            {
                throw new ApplicationException(
                    string.Format(
                        "Could not determine local file for server path {0}!", serverPath));
            }

            return localPath;
        }

        /// <summary>
        /// Gets the file path for currently selected solution item.
        /// </summary>
        /// <returns></returns>
        private string GetFilePathForCurrentlySelectedSolutionItem()
        {
            IntPtr hierarchyPtr, selectionContainerPtr;
            uint projectItemId;
            IVsMultiItemSelect mis;
            var selectionService = (IVsMonitorSelection)GetGlobalService(typeof(SVsShellMonitorSelection));
            selectionService.GetCurrentSelection(out hierarchyPtr, out projectItemId, out mis, out selectionContainerPtr);

            var hierarchy = (IVsHierarchy)Marshal.GetTypedObjectForIUnknown(hierarchyPtr, typeof(IVsHierarchy));
            if (hierarchy == null)
                return string.Empty;

            object selectedItem;
            hierarchy.GetProperty(projectItemId, (int)__VSHPROPID.VSHPROPID_Name, out selectedItem);
            if (selectedItem == null)
                return string.Empty;

            string filePathForCurrentlySelectedSolutionItem = null;

            // Get FilePath
            // ToDo: try to get the filepath without using the dynamic cast
            object browseObject;
            hierarchy.GetProperty(projectItemId, (int)__VSHPROPID.VSHPROPID_BrowseObject, out browseObject);
            if (browseObject == null)
                filePathForCurrentlySelectedSolutionItem = string.Empty;

            try
            {
                // clicked on a File in the Solution Explorer
                filePathForCurrentlySelectedSolutionItem = ((dynamic)browseObject).FilePath;
            }
            catch
            {
            }
            return filePathForCurrentlySelectedSolutionItem;
        }

        /// <summary>
        /// Checks if item is a folder.
        /// </summary>
        /// <param name="folderPath">Folder path which represents the item.</param>
        /// <returns>True if item type is a folder. Else return false.</returns>
        private bool IsFolder(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath))
            {
                return false;
            }

            VersionControlExplorerExt explorer = VersionControlExt.Explorer;
            if (explorer == null)
                return false;
            Workspace workspace = explorer.Workspace;
            if (workspace == null)
                return false;
            VersionControlServer vcServer = workspace.VersionControlServer;
            if (vcServer == null)
                return false;

            // Check if current item is a folder
            Item item = vcServer.GetItem(folderPath);
            if (item.ItemType.Equals(ItemType.Folder))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if component.targets for this folder is already in pending changes
        /// </summary>
        /// <param name="folderPath">Path to folder which could contain component.targets</param>
        /// <returns>True if component.targets is present; Else false</returns>
        private bool ComponentTargetsAlreadyPresentInPendingChanges(string folderPath)
        {
            VersionControlExplorerExt explorer = VersionControlExt.Explorer;
            if (explorer == null)
            {
                return false;
            }

            Workspace workspace = explorer.Workspace;
            if (workspace == null)
            {
                return false;
            }

            var pendingchanges = workspace.GetPendingChanges();

            foreach (PendingChange change in pendingchanges)
            {
                if(string.Equals(change.ServerItem,
                                 VersionControlPath.Combine(folderPath, string.Concat("component", _settings.ValidDependencyDefinitionFileExtension.First())),
                                 StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Checks if file is present on server. This could generate a VersionControlExcepetion
        /// </summary>
        /// <param name="filePath">Path of file to check</param>
        /// <returns>True if present. False else</returns>
        private bool FileIsPresentOnServer(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return false;
            }

            VersionControlExplorerExt explorer = VersionControlExt.Explorer;
            if (explorer == null)
                return false;
            Workspace workspace = explorer.Workspace;
            if (workspace == null)
                return false;
            VersionControlServer vcServer = workspace.VersionControlServer;
            if (vcServer == null)
                return false;

            // Check if current item is a folder
            Item item = vcServer.GetItem(filePath);
            if (!item.ItemType.Equals(ItemType.File))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Checks if current item is a folder and contains a component.targets file.
        /// </summary>
        /// <param name="folderPath">Folder path representing item to check</param>
        /// <returns>True if component.targets file is present. Else false.</returns>
        private bool ComponentTargetsIsPresentInFolder(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath))
            {
                return false;
            }

            VersionControlExplorerExt explorer = VersionControlExt.Explorer;
            if (explorer == null)
                return false;
            Workspace workspace = explorer.Workspace;
            if (workspace == null)
                return false;
            VersionControlServer vcServer = workspace.VersionControlServer;
            if (vcServer == null)
                return false;

            // Check if current item is a folder
            Item item = vcServer.GetItem(folderPath);
            if(!item.ItemType.Equals(ItemType.Folder))
            {
                return false;
            }

            // Check if files exist (Use path AND pattern)
            ItemSet folderItems = vcServer.GetItems(folderPath, VersionSpec.Latest, RecursionType.OneLevel, DeletedState.NonDeleted, ItemType.File);
            foreach (Item it in folderItems.Items)
            {
                if (string.Equals(it.ServerItem,
                                     VersionControlPath.Combine(folderPath, string.Concat("component", _settings.ValidDependencyDefinitionFileExtension.First())),
                                     StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        #endregion

        #region Visual Studio GUI Helpers

        /// <summary>
        /// Shows the operation progress in the status bar.
        /// </summary>
        /// <param name="statusbarText">The statusbar text.</param>
        private void ShowOperationProgressInStatusBar(string statusbarText)
        {
            DevEnv.StatusBar.Animate(true, vsStatusAnimation.vsStatusAnimationGeneral);
            DevEnv.StatusBar.Progress(true,
                                      statusbarText, 0,
                                      100);
        }

        /// <summary>
        /// Shows the operation finish in statusbar.
        /// </summary>
        /// <param name="statusbarText">The statusbar text.</param>
        private void ShowOperationFinishInStatusbar(string statusbarText)
        {
            DevEnv.StatusBar.Animate(false, vsStatusAnimation.vsStatusAnimationGeneral);
            DevEnv.StatusBar.Progress(false);
            DevEnv.StatusBar.Text = statusbarText;
        }

        #endregion
    }
}
