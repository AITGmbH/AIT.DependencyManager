using System.ComponentModel.Composition;

namespace AIT.DMF.Contracts.Services
{
    public interface IDependencyInjectionService
    {
        /// <summary>
        /// Gets the composition service.
        /// </summary>
        ICompositionService CompositionService { get; }

        /// <summary>
        /// Gets an well known dependency.
        /// </summary>
        /// <typeparam name="T">The type.</typeparam>
        /// <returns></returns>
        T GetDependency<T>() where T : class;

        /// <summary>
        /// Gets a well known dependency for a specific contract name.
        /// </summary>
        /// <typeparam name="T">The type.</typeparam>
        /// <param name="contractName">The name of the contract.</param>
        /// <returns></returns>
        T GetDependency<T>(string contractName) where T : class;
    }
}