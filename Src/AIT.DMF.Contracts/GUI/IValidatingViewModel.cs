using System.ComponentModel;

namespace AIT.DMF.Contracts.GUI
{
    public interface IValidatingViewModel : IViewModel, IDataErrorInfo
    {
        /// <summary>
        /// Gets a value indicating whether the data in this view model is valid.
        /// </summary>
        bool IsValid { get; }
    }
}
