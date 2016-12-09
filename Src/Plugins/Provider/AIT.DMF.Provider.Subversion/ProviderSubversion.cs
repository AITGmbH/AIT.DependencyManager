using System;
using System.Collections.Generic;
using SharpSvn;
using System.Collections.ObjectModel;
using System.IO;
using System.Xml;
using System.Xml.Linq;

namespace AIT.DMF.Provider.Subversion
{
    public class ProviderSubversion
    {
        #region Private Member

        private static ProviderSubversion _instance;

        private ProviderSubversion() { }

        private readonly SvnClient _svn = new SvnClient();

        #endregion

        #region Public Member

        public enum ItemType { File, Directory };

        #endregion

        /// <summary>
        /// The singleton class
        /// </summary>
        public static ProviderSubversion Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ProviderSubversion();
                }

                return _instance;
            }
        }

        public void GetFile(string svnFileUrl, string destinationFolder, bool force, string versionSpec)
        {
            SvnExportArgs args = new SvnExportArgs();
            args.Overwrite = force;
            args.Revision = ConvertToRevsion(versionSpec);

            try
            {
                if (!Directory.Exists(destinationFolder))
                {
                    Directory.CreateDirectory(destinationFolder);
                }

                _svn.Export(new Uri(svnFileUrl), destinationFolder, args);
            }
            catch (SvnIllegalTargetException)
            {
                //if force = false, the exisiting file should not be overwritten and no error should be shown
                if (force)
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// Checks whether a directory or file exists
        /// </summary>
        /// <param name="svnUrl">URL of the SVN Repository</param>
        /// <returns>True, if folder or file exisits, otherwise false</returns>
        public bool ItemExists(string svnUrl)
        {
            if (!IsUrlValid(svnUrl)) return false;

            try
            {
                Collection<SvnListEventArgs> contents;

                _svn.GetList(new Uri(svnUrl), out contents);

                return true;
            }
            catch (SvnAuthenticationException)
            {
                throw new SvnAuthenticationException();
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Checks, if the item at <paramref name="svnUrl"/> in specific revision exists
        /// </summary>
        /// <param name="svnUrl">URL of item inside the SVN Repository</param>
        /// <param name="versionSpec">Version specification ("H" for latest or "Rxxx" for specific revision)</param>
        /// <returns>true, item exists in expected revision, otherwise false.</returns>
        public bool ItemExists (string svnUrl, string versionSpec)
        {
            if (!IsUrlValid(svnUrl)) return false;

            SvnInfoArgs args = new SvnInfoArgs();
            args.Revision = ConvertToRevsion(versionSpec);

            try
            {
                Collection<SvnInfoEventArgs> contents;

                _svn.GetInfo(new Uri(svnUrl), args, out contents);

                return true;
            }
            catch (SvnAuthenticationException)
            {
                throw new SvnAuthenticationException();
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Validates a URL.
        /// </summary>
        /// <param name="svnUrl">URL of the SVN Repository</param>
        /// <returns>True, if the url is valid, otherwise false</returns>
        private bool IsUrlValid(string svnUrl)
        {
            Uri uri;

            if (!Uri.TryCreate(svnUrl, UriKind.Absolute, out uri) || null == uri)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Determines all directories OR files at <url>
        /// </summary>
        /// <param name="url">URL of the SVN Repository</param>
        /// <returns>List of directories or files. Null if url is invalid.</returns>
        ///

        //TODO: Liste<string, string> mit lokalem pfad und tatsächlichem pfad (svn:external)
        //-> artifact to clean = lokales verzeichnis
        //-> watermark = tatsächliches verzeichnis



        public Dictionary<string, string> GetItems(string svnUrl, ItemType itemType, bool recursive, string versionSpec)
        {
            SvnNodeKind searchedItemType = SvnNodeKind.Unknown;

            if (itemType == ItemType.Directory)
            {
                searchedItemType = SvnNodeKind.Directory;
            }
            else if (itemType == ItemType.File)
            {
                searchedItemType = SvnNodeKind.File;
            }

            SvnListArgs args = new SvnListArgs();
            args.Revision = ConvertToRevsion(versionSpec);
            args.IncludeExternals = true;

            if (recursive)
            {
                args.Depth = SvnDepth.Infinity;
            }
            else
            {
                args.Depth = SvnDepth.Children;
            }

            var svnRootPath = _svn.GetRepositoryRoot(new Uri(svnUrl));

            Collection<SvnListEventArgs> contents;
            Dictionary<string, string> ret = new Dictionary<string, string>();

            try
            {
                if (_svn.GetList(new Uri(svnUrl), args, out contents))
                {
                    foreach (SvnListEventArgs item in contents)
                    {
                        //first entry is always empty
                        if (!string.IsNullOrEmpty(item.Path) && item.Entry.NodeKind == searchedItemType)
                        {
                            if (string.IsNullOrEmpty(item.ExternalTarget))
                            {
                                ret.Add(string.Format("{0}/{1}", svnUrl, item.Path), string.Format("{0}/{1}", svnUrl, item.Path));

                            }
                            else
                            {
                                //Substring cuts the obosolte / at beginning
                                //ret.Add(string.Format("{0}{1}/{2}", svnRootPath, item.BasePath.Substring(1), item.Path));
                                ret.Add(string.Format("{0}{1}/{2}", svnRootPath, item.BasePath.Substring(1), item.Path), string.Format("{0}/{1}/{2}", svnUrl, item.ExternalTarget, item.Path));
                            }
                        }
                    }
                }

                return ret;
            }
            catch (SvnFileSystemException)
            {
                return ret;
            }
        }

        /// <summary>
        /// Retrieves revision from a directory or file
        /// </summary>
        /// <param name="svnUrl">URL of the SVN Repository</param>
        /// <returns>Revision number or 0</returns>
        public long GetRevision(string svnUrl)
        {
            try
            {
                SvnInfoEventArgs info;
                _svn.GetInfo(new Uri(svnUrl), out info);

                return info.Revision;
            }
            catch (SvnFileSystemException)
            {
                return 0;
            }
        }

        /// <summary>
        /// Retrieves revision from a directory or file
        /// </summary>
        /// <param name="svnUrl">URL of the SVN Repository</param>
        /// <returns>Revision number or 0</returns>
        public long GetHeadRevision(string svnUrl)
        {
            try
            {
                var svnRootPath = _svn.GetRepositoryRoot(new Uri(svnUrl));

                SvnInfoEventArgs info;

                _svn.GetInfo(svnRootPath, out info);

                return info.Revision;
            }
            catch (SvnFileSystemException)
            {
                return 0;
            }
        }

        /// <summary>
        /// Reads content of a component.target file
        /// </summary>
        /// <param name="svnUrl">URL of the SVN Repository</param>
        /// <returns>Whole content as XDocument</returns>
        public XDocument GetComponentTargetsContent(string svnUrl, string versionSpec)
        {
            var tempPath = Path.GetTempPath();
            var guid = Guid.NewGuid().ToString();
            var tempFolder = Path.Combine(tempPath, guid);

            Directory.CreateDirectory(tempFolder);

            GetFile(svnUrl, tempFolder, true, versionSpec);

            var file = Path.Combine(tempFolder, "component.targets");

            try
            {
                return XDocument.Load(file);
            }
            catch (XmlException)
            {
                throw new XmlException();
            }
            finally
            {
                Directory.Delete(tempFolder, true);
            }

        }

        #region Helpers

        /// <summary>
        /// Converts the version specification to a SvnRevision object
        /// </summary>
        /// <param name="versionSpec">Version specification ("H" for latest or "Rxxx" for specific revision)</param>
        /// <returns>SvnRevision object</returns>
        private SvnRevision ConvertToRevsion(string versionSpec)
        {
            if (versionSpec.StartsWith("H"))
            {
                return SvnRevision.Head;
            }

            if (versionSpec.StartsWith("R"))
            {
                var revision = Convert.ToInt32(versionSpec.Substring(1));
                return new SvnRevision(revision);
            }

            return null;
        }

        #endregion
    }
}
