using System;
using System.Xml.Serialization;
using AIT.DMF.Contracts.Parser;

namespace AIT.DMF.DependencyService
{
    [Serializable]
    public class DependencyProviderConfig : IDependencyProviderConfig
    {
        [XmlAttribute]
        public string Type { get; set; }
        [XmlElement("Settings")]
        public DependencyProviderSettings _Settings { get; set; }
        [XmlIgnore]
        public IDependencyProviderSettings Settings
        {
            get { return _Settings; }
            set { _Settings = (DependencyProviderSettings)value; }
        }
    }
}
