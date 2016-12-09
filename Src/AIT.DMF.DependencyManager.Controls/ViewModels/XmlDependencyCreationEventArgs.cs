using System;
using AIT.DMF.Contracts.Provider;

namespace AIT.DMF.DependencyManager.Controls.ViewModels
{
    public class XmlDependencyCreationEventArgs : EventArgs
    {
        public IDependencyResolverType ResolverType
        {
            get;
            private set;
        }

        public XmlDependencyCreationEventArgs(IDependencyResolverType resolverType)
        {
            this.ResolverType = resolverType;
        }
    }
}