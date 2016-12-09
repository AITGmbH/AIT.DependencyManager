// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UsedComponentsFilter.cs" company="AIT GmbH & Co. KG">
//   All rights reserved by AIT GmbH & Co. KG
// </copyright>
// <summary>
//   Implementation of used components filter.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace AIT.DMF.Plugins.Resolver.BinaryRepository
{
    using System.Collections.Generic;
    using System.Linq;
    using Contracts.Common;
    using Contracts.Filters;
    using Contracts.GUI;
    using Contracts.Parser;
    using Contracts.Services;
    using DependencyManager.Controls.Services;

    /// <summary>
    /// Implementation of used components filter.
    /// </summary>
    public class UsedComponentsFilter : IComponentFilter
    {
        #region Private Members

        /// <summary>
        /// The referenced components tracking service.
        /// </summary>
        private readonly IReferencedComponentsTrackingService _referencedComponentsTrackingService;

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="UsedComponentsFilter" /> class.
        /// </summary>
        /// <param name="referencedComponentsTrackingService">The referenced components tracking service.</param>
        public UsedComponentsFilter(IReferencedComponentsTrackingService referencedComponentsTrackingService)
        {
            _referencedComponentsTrackingService = referencedComponentsTrackingService;
        }

        /// <summary>
        /// Filter by the specified dependency type and source component name.
        /// Component names can be excluded from being filtered.
        /// </summary>
        /// <param name="dependencyType">Type of the dependency.</param>
        /// <param name="sourceComponentNames">The source component names.</param>
        /// <param name="resolverSettings">The resolver settings.</param>
        /// <param name="ignoredComponentNames">The component names to ignore.</param>
        /// <returns>The filtered list.</returns>
        public IEnumerable<string> Filter(string dependencyType, IEnumerable<string> sourceComponentNames, ISettings<ResolverValidSettings> resolverSettings, params string[] ignoredComponentNames)
        {
            var teamProject = resolverSettings.GetSetting(ResolverValidSettings.BinaryRepositoryTeamProject);
            if (string.IsNullOrEmpty(teamProject))
            {
                return new List<string>(sourceComponentNames);
            }

            var result = new List<string>();
            foreach (var componentName in sourceComponentNames)
            {
                // ignore?
                if (ignoredComponentNames.Contains(componentName))
                {
                    result.Add(componentName);
                    continue;
                }

                // check if the dependency already exists
                var currentComponentName = componentName;
                if (!_referencedComponentsTrackingService.HasDependency(dependency => IsMatch(dependency, teamProject, currentComponentName, dependencyType)))
                {
                    result.Add(componentName);
                }
            }

            return result;
        }

        /// <summary>
        /// Determines whether the specified dependency matches.
        /// </summary>
        /// <param name="dependency">The dependency.</param>
        /// <param name="teamProject">The team project.</param>
        /// <param name="componentName">The name of the component.</param>
        /// <param name="dependencyType">The type of the dependency.</param>
        /// <returns>
        ///   <c>true</c> if the specified dependency is match; otherwise, <c>false</c>.
        /// </returns>
        private static bool IsMatch(IXmlDependencyViewModel dependency, string teamProject, string componentName, string dependencyType)
        {
            var otherTeamProject =
                dependency.XmlDependency.ProviderConfiguration.Settings.GetSettingValue(DependencyProviderValidSettingName.BinaryRepositoryTeamProject);

            if (!string.IsNullOrEmpty(otherTeamProject))
            {
                return dependency.ReferencedComponentName == componentName
                        && dependency.Type == dependencyType
                        && otherTeamProject == teamProject;
            }

            return false;
        }
    }
}
