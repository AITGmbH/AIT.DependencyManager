using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Reflection;
using System.Windows;
using AIT.DMF.Contracts.Gui;
using AIT.DMF.Contracts.Services;
using AIT.DMF.DependencyManager.Controls.Messaging;
using AIT.DMF.DependencyManager.Controls.Messaging.Events;
using AIT.DMF.DependencyManager.Controls.Model;
using AIT.DMF.DependencyManager.Controls.Services;
using AIT.DMF.DependencyManager.Controls.Views;

namespace AIT.DMF.DependencyManager.Controls
{
    /// <summary>
    /// The main controller class that is able to build the whole UI for the dependency editor.
    /// </summary>
    public class Bootstrapper
    {
        private CompositionContainer _compositionContainer;
        private List<Exception> _initializationExceptionList;

        // TODO: if MEF will be used througout the whole project, then as a last consequence these properties should be injected too
        // TODO: we need to dispose MEF properly

        /// <summary>
        /// Gets or sets the dependency service.
        /// </summary>
        public IDependencyService DependencyService
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the logger.
        /// </summary>
        public ILogger Logger
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the server item.
        /// </summary>
        public string LocalPath
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the main window of the editor.
        /// </summary>
        public Window MainWindow
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets the Team ProjectCollection currently bound to.
        /// </summary>
        public string TeamProjectCollectionUrl { get; set; }

        /// <summary>
        /// Initializes the main controller, all required views, view models, and creates the main window.
        /// </summary>
        public void Initialize()
        {
            if (DependencyService == null)
            {
                throw new InvalidOperationException("No dependency service initialized.");
            }

            if (Logger == null)
            {
                throw new InvalidOperationException("No logger initialized.");
            }

            if (string.IsNullOrEmpty(LocalPath))
            {
                throw new InvalidOperationException("No server item initialized.");
            }

            if (string.IsNullOrEmpty(TeamProjectCollectionUrl))
            {
                throw new InvalidOperationException("No team project collection url initialized.");
            }

            
            InitializeContainer();
            InitializeResources();

            // Subscribe to InitializationErrorEvent to be notified if initialization errors caused by exceptions happen in any of the ViewModels
            _initializationExceptionList = new List<Exception>();
            var publisher = _compositionContainer.GetExportedValue<IEventPublisher>();
            var observable = publisher.GetEvent<InitializationErrorEvent>();
            observable.Subscribe(InitializationErrorHappened);

            // create the main view (which in turn creates the main view model and child views/view models)
            MainView mainView;
            try
            {
                mainView = new MainView();
            }
            catch (Exception e)
            {
                throw _initializationExceptionList.Count != 0 ? _initializationExceptionList[0] : e;
            }

            // part of the workaround for the size issues of docked windows in Visual Studio:
            mainView.HorizontalAlignment = HorizontalAlignment.Left;
            mainView.VerticalAlignment = VerticalAlignment.Top;

            // create shell
            var shell = new Shell();
            shell.Content = mainView;
            MainWindow = shell;
        }

        private void InitializeResources()
        {
            // make sure we have an assembly for resource loading
            var entryAssembly = Assembly.GetEntryAssembly();
            if (entryAssembly == null)
            {
                // this is the case if we have an unmanaged host
                Application.ResourceAssembly = Assembly.GetExecutingAssembly();
            }

            // explicitly add local resources
            var resourcesUri = new Uri("AIT.DMF.DependencyManager.Controls;component/Resources/Styles.xaml", UriKind.Relative);
            var resources = Application.LoadComponent(resourcesUri) as ResourceDictionary;

            // a bit of a code smell: the integration unit tests fail because Application.Current is null
            // => in that case create an instance manually
            if (Application.Current == null)
            {
                new Application();
            }

            // add resources to application
            if (resources != null && Application.Current != null)
            {
                Application.Current.Resources.MergedDictionaries.Add(resources);
            }
        }

        private void InitializeContainer()
        {
            // create the MEF catalog and container
            var catalog = new AggregateCatalog();
            var assemblyCatalog = new AssemblyCatalog(Assembly.GetExecutingAssembly());
            catalog.Catalogs.Add(assemblyCatalog);
            _compositionContainer = new CompositionContainer(catalog);

            // add externally provided values
            _compositionContainer.ComposeExportedValue(DependencyService);
            _compositionContainer.ComposeExportedValue(Logger);

            var targetsFileData = new TargetsFileData();
            targetsFileData.LocalPath = LocalPath;
            _compositionContainer.ComposeExportedValue(targetsFileData);

            var tpcData = new TeamProjectCollectionData();
            tpcData.TPCUri = TeamProjectCollectionUrl;
            _compositionContainer.ComposeExportedValue(tpcData);

            // initialize the service
            DependencyInjectionService.Instance.Initialize(_compositionContainer);
        }

        // Call only after initialize is finished
        public T GetExportedValue<T>()
        {
            return _compositionContainer.GetExportedValue<T>();
        }

        // Rethrow exception from ViewModels to handle in VisualEditorPane
        public void InitializationErrorHappened(InitializationErrorEvent iee)
        {
            _initializationExceptionList.Add(iee.ExceptionCausingError);
        }
    }
}