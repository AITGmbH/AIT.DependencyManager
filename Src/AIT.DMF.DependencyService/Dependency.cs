using AIT.DMF.Contracts.Common;
using AIT.DMF.Contracts.Graph;
using AIT.DMF.Contracts.Exceptions;

namespace AIT.DMF.DependencyService
{
    public class Dependency: IDependency
    {
        private readonly IComponent _source;
        private readonly IComponent _target;
        private readonly IComponentVersion _version;

        /// <summary>
        ///
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <param name="version"></param>
        public Dependency(IComponent source, IComponent target, IComponentVersion version)
        {
            _source = source;
            if (_source == null)
            {
                throw new InvalidComponentException("Dependency for dependency graph was initialized with an invalid dependency description (Source component was null)");
            }

            _target = target;
            if (_target == null)
            {
                throw new InvalidComponentException("Dependency for dependency graph was initialized with an invalid dependency description (Target component was null)");
            }

            _version = version;
            if (_version == null)
            {
                throw new InvalidComponentException("Dependency for dependency graph was initialized with an invalid dependency description (Version was null)");
            }
        }

        /// <summary>
        /// Returns the source component of this dependency.
        /// </summary>
        public IComponent Source
        {
            get
            {
                return _source;
            }
        }

        /// <summary>
        /// Returns the target component of this dependency.
        /// </summary>
        public IComponent Target
        {
            get
            {
                return _target;
            }
        }

        /// <summary>
        /// Returns the needed version of the target component.
        /// </summary>
        public IComponentVersion Version
        {
            get
            {
                return _version;
            }
        }
    }
}
