
using System;
using AIT.DMF.Contracts.Graph;

namespace AIT.DMF.DependencyManager.Controls.Services
{
    public interface IDependencyGraphService
    {
        IGraph GetDependencyGraph();

        /// <summary>
        /// Gets dependency graph asynchronously by using the dependency service.
        /// </summary>
        /// <param name="userCallback">The user callback.</param>
        /// <param name="userState">State of the user.</param>
        /// <returns></returns>
        IAsyncResult BeginGetDependencyGraph(AsyncCallback userCallback, object userState);

        /// <summary>
        /// Returns the graph created by the dependency service.
        /// </summary>
        /// <param name="asyncResult">The async result.</param>
        /// <returns>The dependency graph.</returns>
        IGraph EndGetDependencyGraph(IAsyncResult asyncResult);
    }
}
