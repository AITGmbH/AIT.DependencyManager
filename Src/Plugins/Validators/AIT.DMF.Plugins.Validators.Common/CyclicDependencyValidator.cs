using System.Collections.Generic;
using System.Linq;
using AIT.DMF.Contracts.Graph;

namespace AIT.DMF.Plugins.Validators.Common
{
    public class CyclicDependencyValidator : IValidator
    {
        #region IValidator Implementation

        public string DisplayName
        {
            get { return "Cyclic Component Depedency Validator"; }
        }

        public string Name
        {
            get { return "CyclicComponentDepedencyValidator"; }
        }

        public IEnumerable<IValidationError> Validate(IGraph graph)
        {
            var visited = new HashSet<IComponent>();
            var backtrack = new List<IComponent>();
            var loops = new List<IValidationError>();

            if (null == graph || null == graph.RootComponent)
                return loops;

            DetectCyclicDependencies(graph.RootComponent, visited, backtrack, loops);
            return loops;
        }

        #endregion

        #region Private Helpers

        private void DetectCyclicDependencies(IComponent current, HashSet<IComponent> visited, List<IComponent> backtrack, List<IValidationError> loops)
        {
            //Loop detected. Add it to the loops collection
            var index = backtrack.IndexOf(current);
            if(index >= 0)
            {
                loops.Add(new CyclicDependencyValidationError(this, backtrack.Skip(index).ToList()));
                return;
            }

            //Test whether this item has been visited from another forest. If this is the case, we can simply quit here
            if (!visited.Contains(current))
            {
                //Recursive process the graph. Add the current value on the backtrack stack
                visited.Add(current);
                backtrack.Add(current);
                foreach (var successor in current.Successors)
                    DetectCyclicDependencies(successor.Target, visited, backtrack, loops);
                backtrack.RemoveAt(backtrack.Count - 1);
            }
        }

        #endregion
    }
}
