using System;
using System.Xml.Serialization;
using AIT.DMF.Contracts.Parser;

namespace AIT.DMF.DependencyService
{
    [Serializable]
    public class DependencyProviderSetting : IDependencyProviderSetting
    {
        [XmlAttribute]
        public DependencyProviderValidSettingName Name { get; set; }

        [XmlAttribute]
        public string Value { get; set; }
    }
}
