// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TfsAccessService.cs" company="AIT GmbH & Co. KG">
//   All rights reserved by AIT GmbH & Co. KG
// </copyright>
// <summary>
//   Defines the TfsAccessService type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace AIT.DMF.Plugins.Resolver.BinaryRepository
{
    using System;
    using System.Collections.Generic;
    using Microsoft.TeamFoundation.Client;
    using Microsoft.TeamFoundation.Server;

    /// <summary>
    /// Implements the TFS access service for BinaryRepository definition editor.
    /// </summary>
    internal class TfsAccessService : ITfsAccessService
    {
        #region Private Members

        /// <summary>
        /// The team project collection.
        /// </summary>
        private TfsTeamProjectCollection _tpc;

        /// <summary>
        /// The team projects.
        /// </summary>
        private IEnumerable<ProjectInfo> _projects;

        #endregion

        #region Public Methods

        /// <summary>
        /// Establish a connection to the TFS.
        /// </summary>
        /// <param name="uri">The team project collection uri.</param>
        /// <exception cref="System.ArgumentNullException">If no team project collection uri is provided a ArgumentNullException is thrown</exception>
        public void Connect(Uri uri)
        {
            if (null == uri)
            {
                throw new ArgumentNullException("uri");
            }

            _tpc = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(uri);
            _tpc.EnsureAuthenticated();
            _projects = null;
        }

        /// <summary>
        /// Closes the already established connection.
        /// </summary>
        public void Disconnect()
        {
            _tpc = null;
            _projects = null;
        }

        /// <summary>
        /// Returns all available team projects.
        /// </summary>
        /// <returns>List of TeamProject objects</returns>
        public IEnumerable<ProjectInfo> GetTeamProjects()
        {
            if (null == _tpc)
            {
                throw new InvalidOperationException("The connection is currently closed");
            }

            if (null == _projects)
            {
                var css = _tpc.GetService<ICommonStructureService>();
                _projects = css.ListAllProjects();
            }

            return _projects;
        }

        #endregion
    }
}
