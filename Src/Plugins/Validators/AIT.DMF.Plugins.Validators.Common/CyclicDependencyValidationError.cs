using System;
using System.Collections.Generic;
using AIT.DMF.Contracts.Graph;

namespace AIT.DMF.Plugins.Validators.Common
{
    internal class CyclicDependencyValidationError : IValidationError
    {
        #region Constructor

        internal CyclicDependencyValidationError(IValidator validator, IList<IComponent> components)
        {
            if (null == validator)
                throw new ArgumentNullException("validator");

            if (null == components)
                throw new ArgumentNullException("components");

            if (0 == components.Count)
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
            Message = string.Format("Circular dependency was found for the following components: {0}", string.Join(" -> ", Components));
        }

        #endregion
    }
}
