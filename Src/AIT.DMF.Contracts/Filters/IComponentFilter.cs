using System.Collections.Generic;
using AIT.DMF.Contracts.Common;
using AIT.DMF.Contracts.Services;

namespace AIT.DMF.Contracts.Filters
{
    public interface IComponentFilter
    {
        /// <summary>
        /// Filter by the specified dependency type, source component name and specific resolver settings.
        /// Component names can be excluded from being filtered.
        /// </summary>
        /// <param name="dependencyType">Type of the dependency.</param>
        /// <param name="sourceComponentNames">The source component names.</param>
        /// <param name="resolverSettings">The resolver settings.</param>
        /// <param name="ignoredComponentNames">The component names to ignore.</param>
        /// <returns></returns>
        IEnumerable<string> Filter(string dependencyType, IEnumerable<string> sourceComponentNames, ISettings<ResolverValidSettings> resolverSettings, params string[] ignoredComponentNames);
    }
}
