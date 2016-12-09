using System;
using System.Collections.Generic;
using AIT.DMF.Contracts.Provider;

namespace AIT.DMF.Plugins.Downloader.ZipDownloader.Test
{
    public class DummyWatermark : IDependencyDownloaderWatermark
    {
        public string DownloadType
        {
            get { throw new NotImplementedException(); }
        }

        public HashSet<string> ArtifactsToClean
        {
            get { return new HashSet<string>(); }
            set {  }
        }

        public Dictionary<string, object> Watermarks
        {
            get { return new Dictionary<string, object>(); }
            set { throw new NotImplementedException();}
        }

        public void UpdateTag(string key, string value)
        {
            
        }

        public void UpdateWatermark(string key, object value)
        {
            
        }

        public T GetWatermark<T>(string key)
        {
            return default(T);
        }


        public Dictionary<string, string> Tags
        {
            get { throw new NotImplementedException(); }
        }
    }
}
