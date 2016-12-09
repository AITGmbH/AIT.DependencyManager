using System;
using System.Collections.Generic;
using System.Linq;
using AIT.DMF.Contracts.Graph;

namespace AIT.DMF.Plugins.Validators.Common
{
    internal class SideBySideValidationError : IValidationError
    {
        #region Constructor

        internal SideBySideValidationError(IValidator validator, HashSet<IComponent> components)
        {
            if (null == validator)
                throw new ArgumentNullException("validator");

            if (null == components)
                throw new ArgumentNullException("components");

            if (0 == components.Count())
                throw new ArgumentException("components.count == 0");

            Validator = validator;
            Components = components;

            GenerateMessage();
        }

        #endregion

        #region IValidationError

        public IValidator Validator { get; private set; }

        public string Message { get; private set; }

        public IEnumerable<IComponent> Components { get; private set; }

        #endregion

        #region Overrides

        public override string ToString()
        {
            return Message;
        }

        #endregion

        #region Helpers

        private void GenerateMessage()
        {
            var versions = Components.Select(x => x.Version.GetVersion());
            Message = string.Format("Component {0} was used in the following (potentially incompatible) versions: {1}", Components.First().Name.GetName(), string.Join(",", versions));
        }

        #endregion
    }
}
