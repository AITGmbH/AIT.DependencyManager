using AIT.DMF.Plugins.Resolver.BinaryRepository;
using AIT.DMF.Plugins.Resolver.BuildResult;
using AIT.DMF.Plugins.Resolver.FileShare;
using AIT.DMF.Plugins.Resolver.SourceControl;
using AIT.DMF.Plugins.Resolver.Subversion;
using AIT.DMF.Plugins.Resolver.VNextBuildResult;

namespace AIT.DMF.DependencyService
{
    /// <summary>
    /// This class is responsible for bootstrapping the environment which is needed to run the dependency manager and all the related services
    /// </summary>
    public static class Platform
    {
        #region Private Members

        private static bool _isInitialized;

        #endregion

        /// <summary>
        /// Bootstrapps the dependency service environment
        /// </summary>
        public static void Initialize()
        {
            //TODO this is just a hack. we should use reflection to load the plugins properly without being dependend on the references
            if(!_isInitialized)
            {
                DependencyResolverFactory.RegisterResolverType(new BinaryRepositoryResolverType());
                DependencyResolverFactory.RegisterResolverType(new BuildResultResolverType());
                DependencyResolverFactory.RegisterResolverType(new VNextBuildResultResolverType());
                DependencyResolverFactory.RegisterResolverType(new FileShareResolverType());
                DependencyResolverFactory.RegisterResolverType(new SourceControlCopyResolverType());
                DependencyResolverFactory.RegisterResolverType(new SourceControlMappingResolverType());
                DependencyResolverFactory.RegisterResolverType(new SubversionResolverType());

                _isInitialized = true;
            }
        }
    }
}
