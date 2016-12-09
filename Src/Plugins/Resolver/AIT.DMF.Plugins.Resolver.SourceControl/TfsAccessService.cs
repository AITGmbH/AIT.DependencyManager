// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TfsAccessService.cs" company="AIT GmbH & Co. KG">
//   All rights reserved by AIT GmbH & Co. KG
// </copyright>
// <summary>
//   Defines the TfsAccessService type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace AIT.DMF.Plugins.Resolver.SourceControl
{
    using System;
    using System.Linq;

    using Microsoft.TeamFoundation.Client;
    using Microsoft.TeamFoundation.VersionControl.Client;
    using Microsoft.TeamFoundation.VersionControl.Common;

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
        }

        /// <summary>
        /// Closes the already established connection.
        /// </summary>
        public void Disconnect()
        {
            _tpc = null;
        }

        /// <summary>
        /// Checks if a source control folder exists in this path.
        /// </summary>
        /// <param name="sourceControlPath">The source control path to check</param>
        /// <returns>True if it exists. False otherwise.</returns>
        public bool IsServerPathValid(string sourceControlPath)
        {
            if (null == _tpc)
            {
                throw new InvalidOperationException("The connection is currently closed");
            }

            var vss = _tpc.GetService<VersionControlServer>();
            if (null == vss)
            {
                throw new InvalidOperationException("The connection to the version control server could not be established");
            }

            return vss.ServerItemExists(sourceControlPath, ItemType.Folder);
        }

        /// <summary>
        /// Checks if source control folder contains a dependency definition file.
        /// </summary>
        /// <param name="sourceControlPath">The source control path to check.</param>
        /// <param name="dependencyDefinitonFilenameList">The list of valid dependency definition file names.</param>
        /// <returns>True if it contains a dependency definition file. False otherwise.</returns>
        public bool IsDependencyDefinitionFilePresentInFolder(string sourceControlPath, string dependencyDefinitonFilenameList)
        {
            if (null == _tpc)
            {
                throw new InvalidOperationException("The connection is currently closed");
            }

            var vss = _tpc.GetService<VersionControlServer>();
            if (null == vss)
            {
                throw new InvalidOperationException("The connection to the version control server could not be established");
            }

            var items = vss.GetItems(VersionControlPath.Combine(sourceControlPath, "*"), VersionSpec.Latest, RecursionType.OneLevel);
            var dependencyDefinitionFilenames = dependencyDefinitonFilenameList.Split(';');

            foreach (var item in items.Items)
            {
                if (dependencyDefinitionFilenames.Any(x => x.Equals(VersionControlPath.GetFileName(item.ServerItem), StringComparison.OrdinalIgnoreCase)))
                {
                    return true;
                }
            }

            return false;
        }

        #endregion
    }
}
