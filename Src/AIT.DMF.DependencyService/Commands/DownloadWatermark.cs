using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using AIT.DMF.Common;
using AIT.DMF.Contracts.Provider;

namespace AIT.DMF.DependencyService.Commands
{
    [Serializable]
    public class DownloaderWatermark : IDependencyDownloaderWatermark
    {
        #region Private Members

        private SerializableDictionary<string, object> _watermarks = new SerializableDictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        private SerializableDictionary<string, string> _tags = new SerializableDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private HashSet<string> _artifactsToClean = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        #endregion

        #region Constructor

        public DownloaderWatermark() { }

        public DownloaderWatermark(IDependencyDownloader downloader)
        {
            if (null == downloader)
                throw new ArgumentNullException("downloader");

            DownloadType = downloader.DownloadType;
        }

        #endregion

        #region IDependencyDownloaderWatermark Implementation

        public string DownloadType { get; set; }

        public HashSet<string> ArtifactsToClean
        {
            get { return _artifactsToClean; }
            set { _artifactsToClean = value; }
        }

        public SerializableDictionary<string, object> SerializableWatermarks
        {
            get { return _watermarks; }
            set { _watermarks = value; }
        }

        [XmlIgnore]
        public Dictionary<string, object> Watermarks
        {
            get { return _watermarks; }
            set
            {
                _watermarks = new SerializableDictionary<string, object>();
                {
                    foreach (KeyValuePair<string, object> keyValue in value)
                    {
                        _watermarks.Add(keyValue.Key, keyValue.Value);
                    }
                }
            }
        }

        public void UpdateTag(string key, string value)
        {
            if (_tags.ContainsKey(key))
                _tags[key] = value;
            else
                _tags.Add(key, value);
        }

        public void UpdateWatermark(string key, object value)
        {
            if (_watermarks.ContainsKey(key))
                _watermarks[key] = value;
            else
                _watermarks.Add(key, value);
        }

        public T GetWatermark<T>(string key)
        {
            return _watermarks.ContainsKey(key) ? (T)_watermarks[key] : default(T);
        }

        public SerializableDictionary<string, string> SerializableTags
        {
            get { return _tags; }
            set { _tags = value; }
        }

        [XmlIgnore]
        public Dictionary<string, string> Tags
        {
            get { return _tags; }
        }

        #endregion
    }
}
