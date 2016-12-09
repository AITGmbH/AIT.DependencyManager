using System.Collections.Generic;

namespace AIT.DMF.Contracts.Graph
{
    public interface IValidationError
    {
        /// <summary>
        /// Gets the validator object that generated this validation error
        /// </summary>
        IValidator Validator { get; }

        /// <summary>
        /// Gets a textual representation of the validation error
        /// </summary>
        string Message { get; }

        /// <summary>
        /// Gets a list of all component objects that are involved in the validation error
        /// </summary>
        IEnumerable<IComponent> Components { get; } 
    }
}
