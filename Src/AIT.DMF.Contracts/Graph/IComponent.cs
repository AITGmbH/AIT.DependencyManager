using System.Collections.Generic;

using AIT.DMF.Contracts.Common;
using AIT.DMF.Contracts.Parser;

namespace AIT.DMF.Contracts.Graph
{
    public interface IComponent
    {
        /// <summary>
        /// Gets the name of the component.
        /// </summary>
        IComponentName Name { get; }

        /// <summary>
        /// Gets the exact version of the component.
        /// </summary>
        IComponentVersion Version { get; }

        /// <summary>
        /// Gets the type of the component (TFSSC; FS; Build)
        /// </summary>
        ComponentType Type { get; }

        /// <summary>
        /// Gets all dependencies to successor objects.
        /// </summary>
        IEnumerable<IDependency> Successors{ get; }

        /// <summary>
        /// Get all dependencies to predecessor objects.
        /// </summary>
        IEnumerable<IDependency> Predecessors { get; }

        /// <summary>
        /// Add a new IDependency object to the list of successors.
        /// </summary>
        /// <param name="newSuccessor">New dependency edge to new successor</param>
        void AddSuccessor(IDependency newSuccessor);

        /// <summary>
        /// Add a new IDependency object to the list of predecessor.
        /// </summary>
        /// <param name="newPredecessor">New dependency edge to new predecessor</param>
        void AddPredecessor(IDependency newPredecessor);

        /// <summary>
        /// Gets the value of the setting
        /// </summary>
        /// <param name="name">Name of the setting</param>
        /// <returns></returns>
        string GetFieldValue(DependencyProviderValidSettingName name);

        /// <summary>
        /// Adds a fallback value in case the setting can't be found in deserialized config
        /// </summary>
        /// <param name="name">Name of the setting</param>
        /// <param name="value">Value of the setting</param>
        void AddFallbackFieldValue(DependencyProviderValidSettingName name, string value);
    }
}
