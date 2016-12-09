using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using AIT.DMF.Contracts.GUI;

namespace AIT.DMF.DependencyManager.Controls.Services
{
    [Export(typeof(IReferencedComponentsTrackingService))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class ReferencedComponentsTrackingService : IReferencedComponentsTrackingService
    {
        private readonly List<IXmlDependencyViewModel> _dependencies = new List<IXmlDependencyViewModel>();

        public void AddReferencedComponent(IXmlDependencyViewModel dependency)
        {
            if (!_dependencies.Contains(dependency))
            {
                _dependencies.Add(dependency);
            }
        }

        public void RemoveReferencedComponent(IXmlDependencyViewModel dependency)
        {
            if (_dependencies.Contains(dependency))
            {
                _dependencies.Remove(dependency);
            }
        }

        public bool HasDependency(string componentName, string componentType)
        {
            return
                _dependencies.Any(
                    o => !string.IsNullOrEmpty(o.ReferencedComponentName) && !string.IsNullOrEmpty(o.Type) &&
                    o.ReferencedComponentName.Equals(componentName, StringComparison.CurrentCultureIgnoreCase) &&
                    o.Type.Equals(componentType, StringComparison.CurrentCultureIgnoreCase));
        }

        public bool HasDependency(Func<IXmlDependencyViewModel, bool> externalFilter)
        {
            return _dependencies.Any(externalFilter);
        }
    }
}
