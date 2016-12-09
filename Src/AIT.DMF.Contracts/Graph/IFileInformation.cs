namespace AIT.DMF.Contracts.Graph
{
    public interface IFileInformation
    {
        /// <summary>
        /// Represents the name of the file.
        /// </summary>
        string FileName { get; }

        /// <summary>
        /// Represents the path of the file.
        /// </summary>
        string Path { get; }

        /// <summary>
        /// Saves the name of the checksum algorithm (for example SHA256, SHA1, MD5) (SHA256 should be the default!).
        /// </summary>
        string ChecksumAlgorithm { get; }

        /// <summary>
        /// Represents the checksum value of the file.
        /// (Should be serialized as string by using BitConverter.ToString()!)
        /// </summary>
        byte[] FileChecksum { get; }
    }
}
