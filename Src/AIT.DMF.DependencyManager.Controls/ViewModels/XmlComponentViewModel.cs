using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using AIT.DMF.Contracts.Parser;
using AIT.DMF.DependencyManager.Controls.Services;

namespace AIT.DMF.DependencyManager.Controls.ViewModels
{
    /// <summary>
    /// The view model which encapsulates a single IXmlComponent object.
    /// </summary>
    public class XmlComponentViewModel : ChangeTrackingViewModelBase
    {
        #region Private members

        private readonly IXmlComponent _xmlComponent;
        private List<XmlDependencyViewModel> _xmlDependencyViewModels;

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor checks argument and saves xml component.
        /// </summary>
        /// <param name="xmlComponent">Xml component</param>
        public XmlComponentViewModel(IXmlComponent xmlComponent)
        {
            if (xmlComponent == null)
            {
                throw new ArgumentNullException("xmlComponent");
            }

            _xmlComponent = xmlComponent;
        }

        #endregion

        #region Properties

        [Import]
        public IReferencedComponentsTrackingService ReferencedComponentsTrackingService { get; set; }

        public string ComponentName
        {
            get
            {
                return _xmlComponent.Name;
            }
        }

        public string ComponentVersion
        {
            get
            {
                return _xmlComponent.Version;
            }
        }

        #endregion

        #region Dependencies handling

        public IEnumerable<XmlDependencyViewModel> GetDependencies()
        {
            EnsureDependencyViewModels();

            return _xmlDependencyViewModels.AsEnumerable();
        }

        public void RemoveDependency(XmlDependencyViewModel dependency)
        {
            EnsureDependencyViewModels();

            if (_xmlDependencyViewModels.Contains(dependency))
            {
                _xmlDependencyViewModels.Remove(dependency);

                // also remove from underlying model
                if (_xmlComponent.Dependencies.Contains(dependency.XmlDependency))
                {
                    // this is what it should look like, but the implementation is broken:
                    //_xmlComponent.Dependencies.Remove(dependency.XmlDependency);
                    _xmlComponent.RemoveDependency(dependency.XmlDependency);

                    ReferencedComponentsTrackingService.RemoveReferencedComponent(dependency);
                }

                // set our own "dirty" flag
                SetChanged();
            }

            // call accept changes to let the dependency remove itself 
            // from the change tracking service, if necessary
            dependency.AcceptChanges();
        }

        public void AddDependency(XmlDependencyViewModel dependency)
        {
            EnsureDependencyViewModels();

            if (!_xmlDependencyViewModels.Contains(dependency))
            {
                _xmlDependencyViewModels.Add(dependency);

                // also add to the underlying model
                if (!_xmlComponent.Dependencies.Contains(dependency.XmlDependency))
                {
                    // this is what it should look like, but the implementation is broken:
                    //_xmlComponent.Dependencies.Add(dependency.XmlDependency);
                    _xmlComponent.AddDependency(dependency.XmlDependency);

                    ReferencedComponentsTrackingService.AddReferencedComponent(dependency);
                }

                // set our own "dirty" flag
                SetChanged();
            }
        }

        private void EnsureDependencyViewModels()
        {
            if (_xmlDependencyViewModels == null)
            {
                CreateXmlDependencyViewModelList();
            }
        }

        #endregion

        #region Overrides

        protected override void OnAcceptChanges()
        {
            foreach (var dependency in _xmlDependencyViewModels)
            {
                dependency.AcceptChanges();
            }
        }

        #endregion

        #region Helpers

        private void CreateXmlDependencyViewModelList()
        {
            _xmlDependencyViewModels = new List<XmlDependencyViewModel>();

            foreach (var dependency in _xmlComponent.Dependencies)
            {
                var dependencyViewModel = new XmlDependencyViewModel(dependency, false);
                _xmlDependencyViewModels.Add(dependencyViewModel);

                // update tracking service
                ReferencedComponentsTrackingService.AddReferencedComponent(dependencyViewModel);
            }

        }

        #endregion
    }
}
