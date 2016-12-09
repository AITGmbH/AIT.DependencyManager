using AIT.DMF.Contracts.Parser;

namespace AIT.DMF.Contracts.GUI
{
    /// <summary>
    /// An interface describing an XML dependency view model.
    /// </summary>
    public interface IXmlDependencyViewModel
    {
        /// <summary>
        /// Gets the underlying XML dependency.
        /// </summary>
        IXmlDependency XmlDependency { get; }

        /// <summary>
        /// Gets the name of the referenced component.
        /// </summary>
        string ReferencedComponentName { get; }

        /// <summary>
        /// Gets the referenced component version.
        /// </summary>
        string ReferencedComponentVersion { get; }

        /// <summary>
        /// Gets the type of the referenced component.
        /// </summary>
        string Type { get; }

        /// <summary>
        /// Sets the changed flag for the dependency to signal that the dependency has changed.
        /// </summary>
        void SetChanged();

        /// <summary>
        /// Sets the valid flag for the dependency to signal whether the dependency is valid.
        /// </summary>
        void SetValid(bool isValid);
    }
}