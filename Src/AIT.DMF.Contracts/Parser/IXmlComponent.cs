using System.Collections.Generic;

namespace AIT.DMF.Contracts.Parser
{
    public interface IXmlComponent
    {
        /// <summary>
        /// Gets the name of the component from the xml document.
        /// If a name was not specified a empty string is returned.
        /// </summary>
        string Name
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the exact version of the component from the xml document.
        /// </summary>
        string Version
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the list of dependencies from the xml document.
        /// If no dependencies were specified an empty list is returned.
        /// </summary>
        IList<IXmlDependency> Dependencies
        {
            get;
        }

        // TODO: Remove this when the broken implementation of the Dependencies property in XmlComponent is fixed
        void AddDependency(IXmlDependency dependency);

        // TODO: Remove this when the broken implementation of the Dependencies property in XmlComponent is fixed
        void RemoveDependency(IXmlDependency dependency);
    }
}
