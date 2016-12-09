using AIT.DMF.Contracts.Common;
using AIT.DMF.Contracts.Services;

namespace AIT.DMF.Contracts.Provider
{
    public interface IDependencyDownloader
    {
        /// <summary>
        /// Determines the provider type.
        /// </summary>
        string DownloadType { get; }

        /// <summary>
        /// Downloads a component to a local path.
        /// </summary>
        /// <param name="source">Path to source location</param>
        /// <param name="destination">Path to destination folder</param>
        /// <param name="watermark">The watermark can be used to perform incremental updates and cleanup operations</param>
        /// <param name="force">Indicates that we want to force a get operation and all files have to be overwritten</param>
        /// <param name="settings">Settings which contains the pattern for directories and files to include ("Debug;Bin*;*.dll")</param>
        void Download(string source, string destination, IDependencyDownloaderWatermark watermark, bool force, ISettings<DownloaderValidSettings> settings);

        /// <summary>
        /// Performs a revert operation by removing all the files which have been downloaded previously. The watermark writte during the Download will be provided
        /// </summary>
        /// <param name="watermark">The watermark that has been used for the download operation</param>
        void RevertDownload(IDependencyDownloaderWatermark watermark);
    }
}
