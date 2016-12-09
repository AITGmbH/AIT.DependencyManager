using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using AIT.DMF.Contracts.Exceptions;
using AIT.DMF.Contracts.Graph;
using AIT.DMF.DependencyManager.Controls.Commands;
using AIT.DMF.DependencyManager.Controls.Messaging;
using AIT.DMF.DependencyManager.Controls.Messaging.Events;
using AIT.DMF.DependencyManager.Controls.Services;

namespace AIT.DMF.DependencyManager.Controls.ViewModels
{
    /// <summary>
    /// The view model for the dependency graph view.
    /// </summary>
    public class DependencyGraphViewModel : ComposableViewModelBase
    {
        private IGraph _graph;
        private CompositeCollection _anomalies;
        private string _refreshHint;

        [Import]
        public IDependencyGraphService DependencyGraphService
        {
            get;
            set;
        }

        [Import]
        public IChangeTrackingService ChangeTrackingService
        {
            get;
            set;
        }

        [Import]
        public IEventPublisher EventPublisher
        {
            get; 
            set;
        }

        /// <summary>
        /// Gets or sets the graph of effective dependencies.
        /// </summary>
        public IGraph Graph
        {
            get
            {
                return _graph;
            }
            set
            {
                if (_graph != value)
                {
                    _graph = value;
                    RaiseNotifyPropertyChanged("Graph");
                }
            }
        }

        /// <summary>
        /// Gets or sets the combined circular reference and side-by-side anomalies.
        /// </summary>
        public CompositeCollection Anomalies
        {
            get
            {
                return _anomalies;
            }
            set
            {
                if (_anomalies != value)
                {
                    _anomalies = value;
                    RaiseNotifyPropertyChanged("Anomalies");
                }
            }
        }

        public string RefreshHint
        {
            get
            {
                return _refreshHint;
            }
            set
            {
                if (_refreshHint != value)
                {
                    _refreshHint = value;
                    RaiseNotifyPropertyChanged("RefreshHint");
                }
            }
        }

        /// <summary>
        /// Gets the refresh command.
        /// </summary>
        public DelegateCommand RefreshCommand
        {
            get;
            private set;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DependencyGraphViewModel"/> class.
        /// </summary>
        public DependencyGraphViewModel()
        {
            RefreshCommand = new DelegateCommand(Refresh, CanRefresh);
        }

        private void Refresh()
        {
            RefreshHint = string.Empty;
            Refresh(false);
        }

        /// <summary>
        /// Refreshes the specified by using the dependency graph service.
        /// </summary>
        /// <param name="onLoad">True if graph is initially created. False if graph is refreshed.</param>
        private void Refresh(bool onLoad)
        {
            DependencyGraphService.BeginGetDependencyGraph(OnGetDependencyGraphCompleted, onLoad);
        }

        /// <summary>
        /// This method is called when the dependency graph service created the dependency graph.
        /// </summary>
        /// <param name="ar">The async result.</param>
        private void OnGetDependencyGraphCompleted(IAsyncResult ar)
        {
            var dispatcher = Application.Current.Dispatcher;
            if (!dispatcher.CheckAccess())
            {
                dispatcher.BeginInvoke(new Action<IAsyncResult>(OnGetDependencyGraphCompleted), ar);
                return;
            }

            bool onLoad = (bool) ar.AsyncState;

            try
            {
                Graph = DependencyGraphService.EndGetDependencyGraph(ar);

                if (Graph == null)
                {
                    RefreshHint = onLoad ? "Effective dependencies and identified anomalies are not resolved (Graph could not be created)" : "Effective dependencies and identified anomalies are not resolved (Graph could not be refreshed)";
                    return;
                }
            }
            catch (InvalidComponentException)
            {
                RefreshHint = onLoad ? "Effective dependencies and identified anomalies are not resolved (Graph could not be created)" : "Effective dependencies and identified anomalies are not resolved (Graph could not be refreshed)";
                return;
            }
            catch (DependencyServiceException)
            {
                RefreshHint = onLoad ? "Effective dependencies and identified anomalies are not resolved (Graph could not be created)" : "Effective dependencies and identified anomalies are not resolved (Graph could not be refreshed)";
                return;
            }

            var anomalies = new CompositeCollection();
            if (Graph.CircularDependencies.Any())
            {
                var collectionContainer = new CollectionContainer();
                collectionContainer.Collection = Graph.CircularDependencies;
                anomalies.Add(collectionContainer);
            }
            if (Graph.SideBySideDependencies.Any())
            {
                var collectionContainer = new CollectionContainer();
                collectionContainer.Collection = Graph.SideBySideDependencies;
                anomalies.Add(collectionContainer);
            }
            Anomalies = anomalies;
        }

        private bool CanRefresh()
        {
            return (ChangeTrackingService != null && !ChangeTrackingService.HasChanges) || Graph == null;
        }

        private void ChangeTrackingService_HasChangesChanged(object sender, EventArgs e)
        {
            RefreshCommand.RaiseCanExecuteChanged();

            if (ChangeTrackingService.HasChanges)
            {
                RefreshHint = "Please save your changes to enable the refresh option.";
            }
            else
            {
                RefreshHint = string.Empty;
            }
        }

        #region Overrides

        /// <summary>
        /// Called when a part's imports have been satisfied and it is safe to use.
        /// </summary>
        public override void OnImportsSatisfied()
        {
            try
            {
                // Hack: Due to latency issues, the dependency graph should be built async -> until this is the case, we detach it here
                // Refresh(true);
                RefreshHint = "Click Refresh in order to list current dependency graph.";
            }
            catch(InvalidComponentException ice)
            {
                var initErrorEvent = new InitializationErrorEvent(ice);
                EventPublisher.Publish(initErrorEvent);
            }

            // hook changed event
            ChangeTrackingService.HasChangesChanged += ChangeTrackingService_HasChangesChanged;
        }

        #endregion
    }
}
