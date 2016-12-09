using System;
using System.ComponentModel.Composition;
using AIT.DMF.Contracts.Graph;
using AIT.DMF.Contracts.Gui;
using AIT.DMF.Contracts.Services;
using AIT.DMF.DependencyManager.Controls.Model;

namespace AIT.DMF.DependencyManager.Controls.Services
{
    [Export(typeof(IDependencyGraphService))]
    public class DependencyGraphService : IDependencyGraphService
    {
        [Import]
        public IDependencyService DependencyService
        {
            get;
            set;
        }

        [Import]
        public ILogger Logger
        {
            get;
            set;
        }

        [Import]
        public TargetsFileData TargetsFileData
        {
            get;
            set;
        }

        #region Implementation of IDependencyGraphService

        public IGraph GetDependencyGraph()
        {
            return DependencyService.GetDependencyGraph(TargetsFileData.LocalPath, Logger);
        }

        /// <summary>
        /// Gets dependency graph asynchronously by using the dependency service.
        /// </summary>
        /// <param name="userCallback">The user callback.</param>
        /// <param name="userState">State of the user.</param>
        /// <returns></returns>
        public IAsyncResult BeginGetDependencyGraph(AsyncCallback userCallback, object userState)
        {
            return DependencyService.BeginGetDependencyGraph(TargetsFileData.LocalPath, Logger, userCallback, userState);
        }

        /// <summary>
        /// Returns the graph created by the dependency service.
        /// </summary>
        /// <param name="asyncResult">The async result.</param>
        /// <returns>
        /// The dependency graph.
        /// </returns>
        public IGraph EndGetDependencyGraph(IAsyncResult asyncResult)
        {
            return DependencyService.EndGetDependencyGraph(asyncResult);
        }

        #endregion
    }
}
