using System;
using System.Collections.Generic;
using AIT.DMF.Contracts.Graph;
using AIT.DMF.Contracts.Exceptions;
using AIT.DMF.PluginFactory;

namespace AIT.DMF.DependencyService
{
    /// <summary>
    /// This class represents the dependency graph.
    /// </summary>
    public class Graph: IGraph
    {
        #region Private Members

        private IEnumerable<IValidationError> _sideBySideDependencies;
        private IEnumerable<IValidationError> _circularDependencies;
        private IEnumerable<IComponent> _flattenedGraph;

        #endregion

        #region Constructor
        /// <summary>
        /// Creates a graph based on a root node and a list of side by side and circular dependencies.
        /// </summary>
        /// <param name="rootNode"></param>
        public Graph(IComponent rootNode, string rootComponentTargetsPath)
        {
            if(rootNode == null)
                throw new InvalidGraphException("Dependency graph was initialized with an invalid root component (rootNode was null)");

            if (null == rootComponentTargetsPath)
                throw new ArgumentNullException("rootComponentTargetsPath");

            RootComponent = rootNode;
            RootComponentTargetPath = rootComponentTargetsPath;
        }
        #endregion

        #region IGraph Implementation

        /// <summary>
        /// Returns the root node.
        /// </summary>
        public IComponent RootComponent { get; private set; }

        /// <summary>
        /// Returns the list of side by side dependencies.
        /// </summary>
        public IEnumerable<IValidationError> SideBySideDependencies
        {
            get
            {
                if(null == _sideBySideDependencies)
                {
                    var validator = ValidatorFactory.GetValidator("SideBySideValidator");
                    _sideBySideDependencies = validator.Validate(this);
                }

                return _sideBySideDependencies;
            }
        }

        /// <summary>
        /// Returns all dependencies to predecessor objects.
        /// </summary>
        public IEnumerable<IValidationError> CircularDependencies
        {
            get
            {
                if(null == _circularDependencies)
                {
                    var validator = ValidatorFactory.GetValidator("CyclicComponentDepedencyValidator");
                    _circularDependencies = validator.Validate(this);
                }

                return _circularDependencies;
            }
        }

        public IEnumerable<IComponent> GetFlattenedGraph(bool includeRoot = true, bool recursiveMode = true)
        {
            //TODO this caching might be dangerous when the graph changes. We should have a hook to detect graph changes to recalculate this
            if (null == _flattenedGraph)
            {
                var result = new HashSet<IComponent>();
                if (recursiveMode)
                {
                    GetAllComponentNodesInGraph(RootComponent, result);
                }
                else
                {
                    GetAllDirectComponentNodesSucessorsInGraph(RootComponent, result);
                }

                if (!includeRoot)
                {
                    result.Remove(RootComponent);
                }

                _flattenedGraph = result;
            }

            return _flattenedGraph;
        }

        public string RootComponentTargetPath { get; private set; }

        #endregion

        #region Helpers

        /// <summary>
        /// Gets all direct component nodes successors in graph.
        /// </summary>
        /// <param name="current">The current node.</param>
        /// <param name="components">The component list.</param>
        private void GetAllDirectComponentNodesSucessorsInGraph(IComponent current, HashSet<IComponent> components)
        {
            if (components.Contains(current))
            {
                return;
            }

            components.Add(current);
            foreach (var successor in current.Successors)
            {
                if (!components.Contains(successor.Target))
                {
                    components.Add(successor.Target);
                }
            }
        }

        private void GetAllComponentNodesInGraph(IComponent current, HashSet<IComponent> components)
        {
            if (components.Contains(current))
                return;

            components.Add(current);
            foreach (var successor in current.Successors)
                GetAllComponentNodesInGraph(successor.Target, components);
        }

        #endregion
    }
}
