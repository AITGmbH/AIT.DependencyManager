using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Serialization;
using AIT.DMF.Contracts.Provider;

namespace AIT.DMF.DependencyService.Commands
{
    internal class DownloadWatermarkService
    {
        #region Private Members

        private string _workingFolder;

        private readonly string _rootId;

        #endregion

        #region Constructor

        internal DownloadWatermarkService(string rootComponentsTargets)
        {
            if (string.IsNullOrEmpty(rootComponentsTargets))
                throw new ArgumentNullException("rootComponentsTargets");

            _rootId = GetMd5Sum(rootComponentsTargets);
            EnsureWorkingFolder();
        }

        #endregion

        #region Internal Methods

        internal void Save(IDependencyDownloaderWatermark wm, string name, string version)
        {
            if (null == wm)
                throw new ArgumentNullException("wm");

            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException("name");

            if (string.IsNullOrEmpty(version))
                throw new ArgumentNullException("version");

            // Store name and version as Tags
            wm.UpdateTag("name", name);
            wm.UpdateTag("version", version);
            Store(wm, GetCacheFilePath(name, version));
        }

        internal IDependencyDownloaderWatermark Load(string name, string version)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException("name");

            if (string.IsNullOrEmpty(version))
                throw new ArgumentNullException("version");

            var path = GetCacheFilePath(name, version);
            return File.Exists(path) ? Load(path) : null;
        }

        /// <summary>
        /// Gets a watermark list consisting of tuples with name, version of the component
        /// </summary>
        /// <returns></returns>
        internal IEnumerable<Tuple<string,string>> GetStoredDependencyWatermarks()
        {
            var files = Directory.GetFiles(_workingFolder, "*.xml");
            foreach(var file in files)
            {
                var filename = Path.GetFileNameWithoutExtension(file);
                if(string.IsNullOrWhiteSpace(filename))
                    continue;

                var items = filename.Split(new[] {'@'}, StringSplitOptions.RemoveEmptyEntries);
                if(2 != items.Length)
                    continue;

                yield return new Tuple<string, string>(items[0], items[1]);
            }

            yield break;
        }

        /// <summary>
        /// Checks if component is in the list. This check takes into consideration that the component name and version were hashed when saved.
        /// </summary>
        /// <param name="list">The list.</param>
        /// <param name="component">The component.</param>
        /// <returns></returns>
        internal bool IsComponentInList(IEnumerable<Tuple<string, string>> list, Tuple<string, string> component)
        {
            return (from listElement in list
                    let hashValues = listElement.Item1.Contains(Path.AltDirectorySeparatorChar.ToString()) || listElement.Item1.Contains(Path.DirectorySeparatorChar.ToString())
                    where component.Item1.Equals((hashValues) ? GetMd5Sum(listElement.Item1) : listElement.Item1) && component.Item2.Equals((hashValues) ? GetMd5Sum(listElement.Item2) : listElement.Item2)
                    select listElement).Any();
        }

        /// <summary>
        /// Deletes the specified watermark file.
        /// </summary>
        /// <param name="wm">The wm.</param>
        /// <exception cref="System.ArgumentNullException">wm</exception>
        internal void Delete(IDependencyDownloaderWatermark wm)
        {
            if (null == wm)
            {
                throw new ArgumentNullException("wm");
            }

            Delete(GetCacheFilePath(wm.Tags["name"], wm.Tags["version"]));
        }

        #endregion

        #region Helpers

        private static string GetMd5Sum(string str)
        {
            var unicodeText = new byte[str.Length * 2];

            var enc = Encoding.Unicode.GetEncoder();
            enc.GetBytes(str.ToCharArray(), 0, str.Length, unicodeText, 0, true);

            var md5 = new MD5CryptoServiceProvider();
            var result = md5.ComputeHash(unicodeText);

            var sb = new StringBuilder();
            for (var i = 0; i < result.Length; i++)
                sb.Append(result[i].ToString("X2"));

            return sb.ToString();
        }

        private static void Store(IDependencyDownloaderWatermark watermark, string path)
        {
            if (null == watermark)
                throw new ArgumentNullException("watermark");

            if (null == path)
                throw new ArgumentNullException("path");

            if (0 == watermark.ArtifactsToClean.Count && 0 == watermark.Watermarks.Count)
            {
                File.Delete(path);
                return;
            }

            using (var stream = File.Open(path, FileMode.Create))
            {
                var serializer = new XmlSerializer(typeof(DownloaderWatermark));
                serializer.Serialize(stream, watermark);
            }
        }

        private static IDependencyDownloaderWatermark Load(string path)
        {
            try
            {
                var serializer = new XmlSerializer(typeof(DownloaderWatermark));
                using (var stream = File.OpenRead(path))
                {
                    return serializer.Deserialize(stream) as DownloaderWatermark;
                }
            }
            catch (Exception)
            {
                //TODO Add loggging here or more logic to prevent this error
                return null;
            }
        }

        /// <summary>
        /// Deletes the watermark file at the specified path.
        /// </summary>
        /// <param name="path">The path.</param>
        private static void Delete(string path)
        {
            try
            {
                File.Delete(path);
            }
            catch (Exception)
            {
                //TODO Add loggging here or more logic to prevent this error
            }
        }

        private void EnsureWorkingFolder()
        {
            if(null == _workingFolder)
            {
                var appdata = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                _workingFolder = Path.Combine(appdata, "AIT", "DMF", _rootId);

                if (!Directory.Exists(_workingFolder))
                    Directory.CreateDirectory(_workingFolder);
            }
        }

        /// <summary>
        /// File name consists of name@version.xml
        /// </summary>
        /// <param name="name"></param>
        /// <param name="version"></param>
        /// <returns></returns>
        private string GetCacheFilePath(string name, string version)
        {
            string fileName;
            if (name.Contains(Path.AltDirectorySeparatorChar.ToString()) || name.Contains(Path.DirectorySeparatorChar.ToString()))
            {
                // Deal with server path
                fileName = string.Format("{0}@{1}.xml", GetMd5Sum(name.Trim()), GetMd5Sum(version.Trim()));
            }
            else
            {
                fileName = string.Format("{0}@{1}.xml", name.Trim(), version.Trim());
            }

            return Path.Combine(_workingFolder, fileName);
        }

        #endregion
    }
}
