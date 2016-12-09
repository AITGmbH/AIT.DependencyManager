using System.Collections.Generic;

namespace AIT.DMF.Contracts.Provider
{
    /// <summary>
    /// A watermark class that can be used to persist the state of the download. This can also be used for cleanup operations
    /// </summary>
    public interface IDependencyDownloaderWatermark
    {
        /// <summary>
        /// Gets the name of the downloader that whill be used
        /// </summary>
        string DownloadType { get; }

        /// <summary>
        /// The tags dictionary which can be used to store settings which may be needed like connection settings
        /// </summary>
        Dictionary<string, string> Tags { get; }

        /// <summary>
        /// Gets all the artifacts that have to be deleted during the clean operation to undo the get operation
        /// </summary>
        HashSet<string> ArtifactsToClean { get; set; }

        /// <summary>
        /// A dictionary containing all the watermark objects that are used for incremental downloads. The object type itself must be xml serializable
        /// </summary>
        Dictionary<string, object> Watermarks { get; set; }

        /// <summary>
        /// Updates the tag in the dictionary. This method ensures that the key is either added or updated
        /// </summary>
        /// <param name="key">The key which has to be added or updated</param>
        /// <param name="value">The new tag value</param>
        void UpdateTag(string key, string value);

        /// <summary>
        /// Updates the watermark in the dictionary. This method ensures that the key is either added or updated
        /// </summary>
        /// <param name="key">The key which has to be added or updated</param>
        /// <param name="value">The new watermark value</param>
        void UpdateWatermark(string key, object value);

        /// <summary>
        /// Gets the watermark value
        /// </summary>
        /// <param name="key">The key to retrieve</param>
        /// <returns>The watermark object if the key exists; null otherwise</returns>
        T GetWatermark<T>(string key);
    }
}
