using System;
using System.Xml.Serialization;
using AIT.DMF.Contracts.Enums;
using AIT.DMF.Contracts.Parser;

namespace AIT.DMF.DependencyService
{
    [Serializable]
    public class XmlDependency : IXmlDependency
    {
        [XmlAttribute]
        public DependencyType Type
        {
            get;
            set;
        }

        [XmlElement("Provider")]
        public DependencyProviderConfig _Provider
        {
            get;
            set;
        }

        [XmlIgnore]
        public IDependencyProviderConfig ProviderConfiguration
        {
            get
            {
                return _Provider;
            }
            set
            {
                _Provider = (DependencyProviderConfig)value;
            }
        }
    }
}
