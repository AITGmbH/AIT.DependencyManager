using System.Collections.Generic;

namespace AIT.DMF.Contracts.Graph
{
    public interface IGraph
    {
        /// <summary>
        /// Represents the root component of graph
        /// </summary>
        IComponent RootComponent { get; }

        /// <summary>
        /// List of side by side dependencies of components
        /// </summary>
        IEnumerable<IValidationError> SideBySideDependencies { get; }

        /// <summary>
        /// List of circular dependencies of components
        /// </summary>
        IEnumerable<IValidationError> CircularDependencies { get; }

        /// <summary>
        /// Returns the full graph flattened where every node exists only one time. This method also resolves any circular dependencies that may exists
        /// </summary>
        /// <param name="includeRoot">Determines whether the root node of the graph is part of the flattened collection</param>
        /// <param name="recursiveMode">Determines whether all graph nodes or only direct successors of the root node are included.</param>
        /// <returns>A list af all nodes in the graph</returns>
        IEnumerable<IComponent> GetFlattenedGraph(bool includeRoot = true, bool recursiveMode = true);

        /// <summary>
        /// The local path to the root component targets file stored on the local file system -> TODO Move this to the components node
        /// </summary>
        string RootComponentTargetPath { get; }
    }
}
