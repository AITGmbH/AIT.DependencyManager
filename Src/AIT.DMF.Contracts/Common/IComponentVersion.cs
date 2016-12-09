using System.Collections.Generic;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace AIT.DMF.Contracts.Common
{
    public interface IComponentVersion
    {
        /// <summary>
        /// Gets the version from file share or subversion.
        /// </summary>
        string Version { get; }

        /// <summary>
        /// Gets the version as a TFS VersionSpec object
        /// </summary>
        VersionSpec TfsVersionSpec { get; }
        
        /// <summary>
        /// Gets the version as build number.
        /// </summary>
        string BuildNumber { get; }

        /// <summary>
        /// Gets a list of valid build quality values.
        /// </summary>
        IEnumerable<string> BuildQuality { get; }

        /// <summary>
        /// Gets a list of valid build status.
        /// </summary>
        IEnumerable<string> BuildStatus { get;  }

        /// <summary>
        /// Gets a list of valid build tags.
        /// </summary>
        IEnumerable<string> BuildTags { get; }        

        /// <summary>
        /// Checks if both IVersion objects are equal
        /// </summary>
        /// <returns></returns>
        bool Equals(IComponentVersion otherVersion);

        /// <summary>
        /// Gets the version string specified as version number, version spec, build number or build quality (or build tags) + build status.
        /// </summary>
        /// <returns>Component version</returns>
        string GetVersion();
    }
}
