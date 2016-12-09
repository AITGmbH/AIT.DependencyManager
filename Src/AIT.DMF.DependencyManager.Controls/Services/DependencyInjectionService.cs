using System;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Reflection;
using AIT.DMF.Contracts.Services;

namespace AIT.DMF.DependencyManager.Controls.Services
{
    public class DependencyInjectionService : IDependencyInjectionService
    {
        public static DependencyInjectionService Instance = new DependencyInjectionService();

        private CompositionContainer _container;

        public ICompositionService CompositionService
        {
            get { return _container; }
        }

        private DependencyInjectionService()
        {
        }

        public void Initialize()
        {
            var catalog = new AggregateCatalog();
            var assemblyCatalog = new AssemblyCatalog(Assembly.GetExecutingAssembly());
            catalog.Catalogs.Add(assemblyCatalog);
            _container = new CompositionContainer(catalog);
        }

        public void Initialize(CompositionContainer compositionContainer)
        {
            if (compositionContainer == null)
            {
                throw new ArgumentNullException("compositionContainer");
            }

            _container = compositionContainer;
        }

        public T GetDependency<T>() where T : class
        {
            if (_container == null)
            {
                return default(T);
            }

            return _container.GetExportedValue<T>();
        }

        public T GetDependency<T>(string contractName) where T : class
        {
            if (_container == null)
            {
                return default(T);
            }

            return _container.GetExportedValue<T>(contractName);
        }
    }
}
