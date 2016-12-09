using AIT.DMF.Contracts.Common;

namespace AIT.DMF.Contracts.Graph
{
    public interface IDependency
    {
        /// <summary>
        /// Gets the source component of this dependency.
        /// </summary>
        IComponent Source { get; }

        /// <summary>
        /// Gets the target component of this dependency.
        /// </summary>
        IComponent Target { get; }

        /// <summary>
        /// Gets the needed version of the target component.
        /// </summary>
        IComponentVersion Version { get; }
    }
}
