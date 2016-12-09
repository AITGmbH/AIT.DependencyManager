using System.Collections.Generic;

namespace AIT.DMF.Contracts.Graph
{
    public interface IComponentFileList
    {
        /// <summary>
        /// Represents all subfolders containing files (IFileInformation objects).
        /// </summary>
        List<IComponentFileList> FolderList { get; }

        /// <summary>
        /// Represents a list of files.
        /// </summary>
        List<IFileInformation> FileList { get; }
    }
}
