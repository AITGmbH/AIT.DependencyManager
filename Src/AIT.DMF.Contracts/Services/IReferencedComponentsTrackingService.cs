using System;
using AIT.DMF.Contracts.GUI;

namespace AIT.DMF.DependencyManager.Controls.Services
{
    public interface IReferencedComponentsTrackingService
    {
        /// <summary>
        /// Adds the referenced component.
        /// </summary>
        /// <param name="dependency">The xml dependency view model.</param>
        void AddReferencedComponent(IXmlDependencyViewModel dependency);

        /// <summary>
        /// Removes the referenced component.
        /// </summary>
        /// <param name="dependency">The xml ependency view model.</param>
        void RemoveReferencedComponent(IXmlDependencyViewModel dependency);

        /// <summary>
        /// Determines whether a componentent is used already.
        /// </summary>
        /// <param name="componentName">The component name.</param>
        /// <param name="componentType">The type of the component.</param>
        /// <returns>
        ///   <c>true</c> if a xml dependency view model exists with this type and component name is tracked already; otherwise, <c>false</c>.
        /// </returns>
        bool HasDependency(string componentName, string componentType);

        /// <summary>
        /// Determines whether a componentent is used already by using the external filter.
        /// </summary>
        /// <param name="externalFilter">The external filter.</param>
        /// <returns>
        ///   <c>true</c> if a xml dependency view model exists with this type and component name is tracked already; otherwise, <c>false</c>.
        /// </returns>
        bool HasDependency(Func<IXmlDependencyViewModel, bool> externalFilter);
    }
}