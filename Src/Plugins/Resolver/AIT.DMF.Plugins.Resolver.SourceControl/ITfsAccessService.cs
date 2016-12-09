// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ITfsAccessService.cs" company="AIT GmbH & Co. KG">
//   All rights reserved by AIT GmbH & Co. KG
// </copyright>
// <summary>
//   Describes the TfsAccessService interface.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace AIT.DMF.Plugins.Resolver.SourceControl
{
    using System;
    using System.Diagnostics.CodeAnalysis;

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
        /// Checks if a source control folder exists in this path.
        /// </summary>
        /// <param name="sourceControlPath">The source control path to check.</param>
        /// <returns>True if it exists. False otherwise.</returns>
        bool IsServerPathValid(string sourceControlPath);

        /// <summary>
        /// Checks if source control folder contains a dependency definition file.
        /// </summary>
        /// <param name="sourceControlPath">The source control path to check.</param>
        /// <param name="dependencyDefinitonFilenameList">The list of valid dependency definition file names.</param>
        /// <returns>True if it contains a dependency definition file. False otherwise.</returns>
        bool IsDependencyDefinitionFilePresentInFolder(string sourceControlPath, string dependencyDefinitonFilenameList);
    }
}