using AIT.DMF.Contracts.Graph;

namespace AIT.DMF.Contracts.Provider
{
    public interface IDependencyDownloaderFactory
    {
        /// <summary>
        /// Gets a downloader with the specific type.
        /// </summary>
        /// <param name="component">component to get a downloader for</param>
        /// <returns>Downloader worker</returns>
        IDependencyDownloader GetDownloader(IComponent component);

        /// <summary>
        /// Gets a downloader with the specific type.
        /// </summary>
        /// <param name="downloaderType">type to get a downloader for</param>
        /// <returns>Downloader worker</returns>
        IDependencyDownloader GetDownloader(string downloaderType);
    }
}
