using System;
using System.Collections.Generic;
using Microsoft.TeamFoundation.VersionControl.Client;
using AIT.DMF.Contracts.Common;
using AIT.DMF.Contracts.Exceptions;
using System.Linq;

namespace AIT.DMF.Common
{
    public class ComponentVersion : IComponentVersion
    {
        #region Constructors

        /// <summary>
        /// Initializes Version object with a file share version string.
        /// </summary>
        /// <param name="fileShareVersion"></param>
        public ComponentVersion(string fileShareVersion)
        {
            BuildStatus = null;
            BuildQuality = null;
            BuildTags = null;
            BuildNumber = null;
            TfsVersionSpec = null;
            Version = null;

            if (String.IsNullOrEmpty(fileShareVersion))
            {
                throw new InvalidComponentException("Version for dependency graph was initialized with an invalid version description (FileShareVersion was null)");
            }
            Version = fileShareVersion;
        }

        /// <summary>
        /// Initializes Version object with VersionSpec version.
        /// </summary>
        /// <param name="versionSpecVersion">VersionSpec object</param>
        public ComponentVersion(VersionSpec versionSpecVersion)
        {
            BuildStatus = null;
            BuildQuality = null;
            BuildTags = null;
            BuildNumber = null;
            TfsVersionSpec = null;
            Version = null;

            if (versionSpecVersion == null)
            {
                throw new InvalidComponentException("Version for dependency graph was initialized with an invalid version description (VersionSpec was null)");
            }
            TfsVersionSpec = versionSpecVersion;
        }

        /// <summary>
        /// Initializes a component version for a build result resolver.
        /// </summary>
        /// <param name="buildNumber">Number of build.</param>
        /// <param name="acceptedBuildStatus">Build status.</param>
        /// <param name="acceptedBuildQuality">Build quality.</param>
        /// <param name="acceptedBuildTags">Build tags.</param>
        public ComponentVersion(string buildNumber, IEnumerable<string> acceptedBuildStatus, IEnumerable<string> acceptedBuildQuality, IEnumerable<string> acceptedBuildTags)
        {
            BuildStatus = null;
            BuildQuality = null;
            BuildTags = null;
            BuildNumber = null;
            TfsVersionSpec = null;
            Version = null;

            var buildNumberSet = !string.IsNullOrEmpty(buildNumber);
            var acceptedBuildStatusSet = acceptedBuildStatus != null && acceptedBuildStatus.Count() > 0;
            var acceptedBuildQualitySet = acceptedBuildQuality != null && acceptedBuildQuality.Count() > 0;
            var acceptedBuildTagsSet = acceptedBuildTags != null && acceptedBuildTags.Count() > 0;

            // Check if all are empty
            if (!buildNumberSet && !acceptedBuildQualitySet && !acceptedBuildStatusSet && !acceptedBuildTagsSet)
            {
                // Create a valid component version in case of limitations from user side when querying build results.
                return;
            }

            BuildNumber = buildNumber;
            BuildQuality = acceptedBuildQuality;
            BuildStatus = acceptedBuildStatus;
            BuildTags = acceptedBuildTags;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Returns the FileShare version string (if set). Otherwise null is returned.
        /// </summary>
        public string Version { get; private set; }

        /// <summary>
        /// Returns the TFS VersionSpec object (if set). Otherwise null is returned.
        /// </summary>
        public VersionSpec TfsVersionSpec { get; private set; }

        /// <summary>
        /// Returns the build number as a string (if set). Otherwise null is returned.
        /// </summary>
        public string BuildNumber { get; private set; }

        /// <summary>
        /// Returns a list of valid build qualities.
        /// </summary>
        public IEnumerable<string> BuildQuality { get; private set; }

        /// <summary>
        /// Returns a valid build status.
        /// </summary>
        public IEnumerable<string> BuildStatus { get; private set; }

        /// <summary>
        /// Returns a valid build tags.
        /// </summary>
        public IEnumerable<string> BuildTags { get; private set; }

        #endregion

        #region Public methods

        /// <summary>
        /// Checks if the version represented by this version object equals the version represented by otherVersion object.
        /// </summary>
        /// <param name="otherVersion">Version representing IVersion object</param>
        /// <returns>True if equal. False otherwise</returns>
        public bool Equals(IComponentVersion otherVersion)
        {
            if (!string.IsNullOrEmpty(Version) && !string.IsNullOrEmpty(otherVersion.Version) && Version.Equals(otherVersion.Version)) return true;

            if (TfsVersionSpec != null && otherVersion.TfsVersionSpec != null && TfsVersionSpec.Equals(otherVersion.TfsVersionSpec)) return true;

            if (!string.IsNullOrEmpty(BuildNumber) && !string.IsNullOrEmpty(otherVersion.BuildNumber) && BuildNumber.Equals(otherVersion.BuildNumber)) return true;

            if (ContainSameValues(BuildStatus, otherVersion.BuildStatus) && ContainSameValues(BuildQuality, otherVersion.BuildQuality)) return true;

            if (ContainSameValues(BuildStatus, otherVersion.BuildStatus) && ContainSameValues(BuildTags, otherVersion.BuildTags)) return true;

            return false;
        }

        private static bool ContainSameValues(IEnumerable<string> list1, IEnumerable<string> list2)
        {
            if ((list1 != null) && (list2 != null) && (list1.Count() == list2.Count()))
            {
                return list1.All(item => list2.Contains(item));
            }

            return false;
        }

        /// <summary>
        /// Returns the fileshare/version spec/build number/build quality + build status as version.
        /// </summary>
        /// <returns>Component version</returns>
        public string GetVersion()
        {
            if (!String.IsNullOrEmpty(Version))
            {
                return Version;
            }

            if (TfsVersionSpec != null)
            {
                return TfsVersionSpec.DisplayString;
            }

            if (!String.IsNullOrEmpty(BuildNumber))
            {
                return BuildNumber;
            }
            string versionString = "";

            if (BuildQuality != null)
            {
                versionString = String.Format("Quality: {0} ", String.Join(",", BuildQuality));
            }
            if (BuildTags != null)
            {
                versionString = String.Format("Tags: {0} ", String.Join(",", BuildTags));
            }
            if (BuildStatus != null)
            {
                if (versionString.Length != 0)
                {
                    versionString += ";";
                }
                versionString += String.Format("Status: {0}", String.Join(",", BuildStatus));
            }

            return versionString;
        }

        public override string ToString()
        {
            return GetVersion();
        }

        #endregion
    }
}
