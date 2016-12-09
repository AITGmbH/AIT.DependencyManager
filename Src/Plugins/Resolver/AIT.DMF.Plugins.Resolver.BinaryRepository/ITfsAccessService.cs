// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ITfsAccessService.cs" company="AIT GmbH & Co. KG">
//   All rights reserved by AIT GmbH & Co. KG
// </copyright>
// <summary>
//   Describes the TfsAccessService interface.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace AIT.DMF.Plugins.Resolver.BinaryRepository
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using Microsoft.TeamFoundation.Server;

    /// <summary>
    /// Describes the TfsAccessService interface.
    /// </summary>
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "Reviewed. Suppression is OK here.")]
    internal interface ITfsAccessService
    {
        /// <summary>
        /// Establish a connection to the TFS
        /// </summary>
        /// <param name="uri">The team project collection uri.</param>
        void Connect(Uri uri);

        /// <summary>
        /// Closes the already established connection
        /// </summary>
        void Disconnect();

        /// <summary>
        /// Returns all available team projects.
        /// </summary>
        /// <returns>List of TeamProject objects</returns>
        IEnumerable<ProjectInfo> GetTeamProjects();
    }
}