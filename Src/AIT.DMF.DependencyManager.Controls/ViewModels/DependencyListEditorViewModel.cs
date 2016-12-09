using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Xml;
using AIT.DMF.Contracts.Exceptions;
using AIT.DMF.Contracts.Graph;
using AIT.DMF.Contracts.Gui;
using AIT.DMF.Contracts.GUI;
using AIT.DMF.Contracts.Parser;
using AIT.DMF.Contracts.Services;
using AIT.DMF.DependencyManager.Controls.Commands;
using AIT.DMF.DependencyManager.Controls.Messaging;
using AIT.DMF.DependencyManager.Controls.Messaging.Events;
using AIT.DMF.DependencyManager.Controls.Model;
using AIT.DMF.DependencyManager.Controls.Services;

namespace AIT.DMF.DependencyManager.Controls.ViewModels
{
    public class DependencyListEditorViewModel : ComposableViewModelBase
    {
        private IXmlComponent _xmlComponent;
        private ObservableCollection<IXmlDependencyViewModel> _xmlDependencyViewModels;
        private IXmlDependencyViewModel _selectedXmlDependencyViewModel;
        private XmlComponentViewModel _xmlComponentViewModel;

        public ObservableCollection<IXmlDependencyViewModel> XmlDependencies
        {
            get
            {
                if (_xmlDependencyViewModels == null)
                {
                    _xmlDependencyViewModels = new ObservableCollection<IXmlDependencyViewModel>();
                }

                return _xmlDependencyViewModels;
            }
        }

        public IXmlDependencyViewModel SelectedXmlDependency
        {
            get
            {
                return _selectedXmlDependencyViewModel;
            }
            set
            {
                if (_selectedXmlDependencyViewModel != value)
                {
                    // may evaluate to null for CreateXmlDependencyViewModel types
                    var oldValue = _selectedXmlDependencyViewModel as XmlDependencyViewModel;

                    // set new value
                    _selectedXmlDependencyViewModel = value;
                    RaiseNotifyPropertyChanged("SelectedXmlDependency");

                    PublishSelectedXmlDependencyChangedEvent(oldValue, value);
                }
            }
        }

        public DelegateCommand SaveCommand
        {
            get;
            private set;
        }

        public DelegateCommand GetCommand
        {
            get;
            private set;
        }

        public DelegateCommand RemoveCommand
        {
            get;
            private set;
        }

        public DelegateCommand RevertCommand
        {
            get;
            private set;
        }

        #region Imported Properties

        [Import]
        public IEventPublisher EventPublisher
        {
            get;
            set;
        }

        [Import]
        public TargetsFileData TargetsFileData
        {
            get;
            set;
        }

        [Import]
        public IXmlComponentRepository XmlComponentRepository
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
        public IDependencyService DependencyService
        {
            get;
            set;
        }

        [Import]
        public ILogger Logger { get; set; }

        #endregion

        public DependencyListEditorViewModel()
        {
            SaveCommand = new DelegateCommand(Save, CanSave);
            GetCommand = new DelegateCommand(Get, CanGet);
            RemoveCommand = new DelegateCommand(Remove, CanRemove);
            RevertCommand = new DelegateCommand(Revert, CanRevert);
        }

        #region Overrides

        public override void OnImportsSatisfied()
        {
            // create the component view model
            try
            {
                _xmlComponent = XmlComponentRepository.GetXmlComponent(TargetsFileData);

                if (_xmlComponent == null)
                {
                    var initErrorEvent = new InitializationErrorEvent(new Exception(String.Format("Error while loading local file {0}!", TargetsFileData.LocalPath)));
                    EventPublisher.Publish(initErrorEvent);
                }
            }
            catch (XmlException xe)
            {
                var initErrorEvent = new InitializationErrorEvent(new Exception(String.Format("Error while loading local file {0} (Error while parsing XML: {1})!", TargetsFileData.LocalPath, xe.Message)));
                EventPublisher.Publish(initErrorEvent);
            }
            catch (DependencyServiceException dse)
            {
                var initErrorEvent = new InitializationErrorEvent(new Exception(String.Format("Error while loading local file {0} ({1})!", TargetsFileData.LocalPath, dse.Message)));
                EventPublisher.Publish(initErrorEvent);
            }
            
            _xmlComponentViewModel = new XmlComponentViewModel(_xmlComponent);

            // initialize the list of available Xml dependencies in the current targets file
            var dependencies = _xmlComponentViewModel.GetDependencies();
            foreach (var xmlDependencyViewModel in dependencies)
            {
                XmlDependencies.Add(xmlDependencyViewModel);
            }

            // add special view model for creation
            var createXmlDependencyViewModel = new CreateXmlDependencyViewModel();
            createXmlDependencyViewModel.XmlDependencyCreationRequest += CreateXmlDependencyViewModel_XmlDependencyCreationRequest;
            XmlDependencies.Add(createXmlDependencyViewModel);

            // hook events
            ChangeTrackingService.HasChangesChanged += ChangeTrackingService_HasChangesChanged;
            var saveAllChangesEvent = EventPublisher.GetEvent<SaveAllChangesEvent>();
            saveAllChangesEvent.Subscribe(o => Save(o.FileName));
        }

        private void CreateXmlDependencyViewModel_XmlDependencyCreationRequest(object sender, XmlDependencyCreationEventArgs xmlDependencyCreationEventArgs)
        {
            if (xmlDependencyCreationEventArgs == null || xmlDependencyCreationEventArgs.ResolverType == null)
            {
                return;
            }

            var resolverType = xmlDependencyCreationEventArgs.ResolverType;
            IXmlDependency newXmlDependency = DependencyService.CreateEmptyIXmlDependency(resolverType);

            // create the view model and add it to our list as well as the component
            var newXmlDependencyViewModel = new XmlDependencyViewModel(newXmlDependency, true);
            XmlDependencies.Insert(XmlDependencies.Count - 1, newXmlDependencyViewModel);
            _xmlComponentViewModel.AddDependency(newXmlDependencyViewModel);

            // pre-select the new dependency
            SelectedXmlDependency = newXmlDependencyViewModel;
        }

        private void ChangeTrackingService_HasChangesChanged(object sender, EventArgs e)
        {
            // let the commands re-evaluate
            SaveCommand.RaiseCanExecuteChanged();
            GetCommand.RaiseCanExecuteChanged();
            RemoveCommand.RaiseCanExecuteChanged();
            RevertCommand.RaiseCanExecuteChanged();
        }

        #endregion

        #region Implementation of save functionality

        /// <summary>
        /// Overwrite original dependency definition file (Use for "Save").
        /// </summary>
        private void Save()
        {
            Save(TargetsFileData);
        }

        /// <summary>
        /// Save original dependency definition file into another file (Use for "Save As ...").
        /// </summary>
        /// <param name="dependencyDefinitionFilePath"></param>
        private void Save(string dependencyDefinitionFilePath)
        {
            // normal save?
            if (string.IsNullOrEmpty(dependencyDefinitionFilePath))
            {
                Save();
                return;
            }

            var targetsFileData = new TargetsFileData
            {
                LocalPath = dependencyDefinitionFilePath
            };

            Save(targetsFileData);
        }

        private void Save(TargetsFileData targetsFileData)
        {
            // this is where the actual saving is triggered,
            // including a check to see if we can save at all

            // first check if we can save
            var allValid = AreAllDependenciesValid();
            if (!allValid)
            {
                UserMessageService.ShowWarning("Please correct all errors before saving.");
                return;
            }

            try
            {
                SaveDependencies(targetsFileData);
            }
            catch (Exception ex)
            {
                // TODO: this should throw well-known exceptions only

                UserMessageService.ShowError("An error occurred while saving the dependencies file: " + ex.Message);
            }
        }

        private bool CanSave()
        {
            // do we have any changes?
            var hasChanges = ChangeTrackingService != null && ChangeTrackingService.HasChanges;
            if (!hasChanges)
            {
                return false;
            }

            return AreAllDependenciesValid();
        }

        private bool AreAllDependenciesValid()
        {
            foreach (var xmlDependency in XmlDependencies)
            {
                var xmlDependencyViewModel = xmlDependency as XmlDependencyViewModel;
                if (xmlDependencyViewModel != null && !xmlDependencyViewModel.IsValid)
                {
                    return false;
                }
            }

            return true;
        }

        private void Get()
        {
            // Check if save can be triggered
            var allValid = AreAllDependenciesValid();
            if (!allValid)
            {
                UserMessageService.ShowWarning("Please correct all errors before saving.");
                return;
            }

            try
            {
                GetDependencies(TargetsFileData);
            }
            catch (Exception ex)
            {
                // TODO: DependencyService should only throw well-known exceptions
                UserMessageService.ShowError(String.Format("An fatal error occurred while getting the dependencies. See DependencyManager output window for further information!"));

                Logger.LogMsg("\nFatal error occured while fetching dependencies. Aborting ...");
                Logger.LogMsg(String.Format("Exception message: {0}", ex.Message));
                Logger.LogMsg(String.Format("Stacktrace:\n{0}\n", ex.StackTrace));
                Logger.ShowMessages();
            }
        }

        private bool CanGet()
        {
            return !CanSave();
        }

        #endregion

        #region Implementation of remove functionality

        private void Remove()
        {
            if (SelectedXmlDependency != null)
            {
                // temporarily store reference
                var dependency = SelectedXmlDependency as XmlDependencyViewModel;
                if (dependency == null)
                {
                    // should never happen because the command is disabled if the type 
                    // of the selected dependency is not correct
                    return;
                }

                // get index
                int selectedIndex = XmlDependencies.IndexOf(dependency);

                // remove from our collection
                XmlDependencies.Remove(dependency);

                // remove from container
                _xmlComponentViewModel.RemoveDependency(dependency);

                // select next entry
                while (selectedIndex > -1 && selectedIndex >= XmlDependencies.Count)
                {
                    selectedIndex--;
                }
                SelectedXmlDependency = selectedIndex > -1 ? XmlDependencies[selectedIndex] : null;
            }
        }

        private bool CanRemove()
        {
            var xmlDependency = SelectedXmlDependency as XmlDependencyViewModel;
            return xmlDependency != null;
        }

        #endregion

        #region Implementation of revert functionality

        private void Revert()
        {
            if (SelectedXmlDependency != null)
            {
                var iEditableObject = SelectedXmlDependency as IEditableObject;
                if (iEditableObject != null)
                {
                    iEditableObject.CancelEdit();

                    // this changes the execution possibility of the revert command...
                    RevertCommand.RaiseCanExecuteChanged();

                    // ... and also _potentially_ changes the other command options
                    SaveCommand.RaiseCanExecuteChanged();
                    GetCommand.RaiseCanExecuteChanged();

                    // finally, publish the selection change event to allow dependent 
                    // places to update their values/visual representation
                    PublishSelectedXmlDependencyChangedEvent(SelectedXmlDependency, SelectedXmlDependency);
                }
            }
        }

        private bool CanRevert()
        {
            var xmlDependency = SelectedXmlDependency as XmlDependencyViewModel;
            return xmlDependency != null && xmlDependency.IsChanged && !xmlDependency.IsNew;
        }

        #endregion

        #region Helpers

        private void PublishSelectedXmlDependencyChangedEvent(IXmlDependencyViewModel oldValue, IXmlDependencyViewModel newValue)
        {
            // publish distributed event
            if (EventPublisher != null)
            {
                // may evaluate to null for CreateXmlDependencyViewModel types (desired)
                var oldXmlDependencyViewModel = oldValue as XmlDependencyViewModel;
                var newXmlDependencyViewModel = newValue as XmlDependencyViewModel;

                var theEvent = new SelectedXmlDependencyChangedEvent(oldXmlDependencyViewModel, newXmlDependencyViewModel);
                EventPublisher.Publish(theEvent);
            }
        }

        private void SaveDependencies(TargetsFileData dependencyDefinitionFilePath)
        {
            // save
            XmlComponentRepository.SaveXmlComponent(_xmlComponent, dependencyDefinitionFilePath);
            _xmlComponentViewModel.AcceptChanges();

            Logger.LogMsg(String.Format("Saved dependency definition file {0}\n", dependencyDefinitionFilePath.LocalPath));
            Logger.ShowMessages();
        }

        private void GetDependencies(TargetsFileData dependencyDefinitionFilePath)
        {
            Logger.LogMsg(String.Format("Get dependencies (Dependency definition file: {0}):", dependencyDefinitionFilePath.LocalPath));
            Logger.ShowMessages();

            DependencyService.BeginGetDependencyGraph(dependencyDefinitionFilePath.LocalPath, Logger, OnGetDependencyGraphCompleted, null);
        }

        private void OnGetDependencyGraphCompleted(IAsyncResult asyncResult)
        {
            IGraph graph;
            try
            {
                graph = DependencyService.EndGetDependencyGraph(asyncResult);
                DependencyService.BeginDownloadGraph(graph, Logger, OnDownloadDependencyGraphCompleted, null);
            }
            catch (DependencyServiceException dse)
            {
                Logger.LogMsg(String.Format("  ! {0}\n", dse.Message));
                return;
            }
        }

        private void OnDownloadDependencyGraphCompleted(IAsyncResult asyncResult)
        {
            try
            {
                DependencyService.EndDownloadGraph(asyncResult);
            }
            catch (DependencyServiceException dse)
            {
                Logger.LogMsg(String.Format("  ! {0}\n", dse.Message));
            }
        }

        #endregion
    }
}
