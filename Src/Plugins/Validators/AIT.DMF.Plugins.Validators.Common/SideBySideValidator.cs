using System.Linq;
using System.Collections.Generic;
using AIT.DMF.Contracts.Graph;
using AIT.DMF.Contracts.Parser;

namespace AIT.DMF.Plugins.Validators.Common
{
    public class SideBySideValidator : IValidator
    {
        #region IValidator Implementation

        public string DisplayName
        {
            get { return "Side by Side Component Validator"; }
        }

        public string Name
        {
            get { return "SideBySideValidator"; }
        }

        public IEnumerable<IValidationError> Validate(IGraph graph)
        {
            if (null == graph || null == graph.RootComponent)
                return new List<IValidationError>();

            return AnalyzeForSideBySide(graph.GetFlattenedGraph());
        }

        #endregion

        #region Helpers

        private IEnumerable<IValidationError> AnalyzeForSideBySide(IEnumerable<IComponent> components)
        {
            var res = new List<IValidationError>();

            var filteredComponent = components.Where(x => x.GetFieldValue(DependencyProviderValidSettingName.IgnoreInSideBySideAnomalyChecks) == null
                || !x.GetFieldValue(DependencyProviderValidSettingName.IgnoreInSideBySideAnomalyChecks).Equals("True"));
            var groups = filteredComponent.GroupBy(x => x.Name.GetName());
            foreach (var group in groups)
            {
                var hash = new HashSet<IComponent>(group);
                if(hash.Count > 1)
                    res.Add(new SideBySideValidationError(this, hash));
            }

            return res;
        }

        #endregion
    }
}
