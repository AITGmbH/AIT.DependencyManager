using System.Collections.Generic;

namespace AIT.DMF.Contracts.Graph
{
    public interface IValidator
    {
        /// <summary>
        /// Gets the name of the validator
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the display name of the validator
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// Performs a validation on the graph
        /// </summary>
        /// <param name="graph">The graph which shall be validated</param>
        /// <returns>All validation errors</returns>
        IEnumerable<IValidationError> Validate(IGraph graph);
    }
}
