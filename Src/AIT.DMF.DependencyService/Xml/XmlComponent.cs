using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using AIT.DMF.Contracts.Parser;

namespace AIT.DMF.DependencyService
{
    [XmlRoot("Component", IsNullable = false, Namespace = "http://schemas.aitgmbh.de/DependencyManager/2011/11")]
    public class XmlComponent : IXmlComponent
    {
        [XmlAttribute]
        public string Name
        {
            get;
            set;
        }

        [XmlAttribute]
        public string Version
        {
            get;
            set;
        }

        [XmlArray("Dependencies")]
        [XmlArrayItem("Dependency", typeof(XmlDependency))]
        public List<XmlDependency> _DependencyList
        {
            get;
            set;
        }

        // TODO: this is an extremely bad and error-prone implementation
        [XmlIgnore]
        public IList<IXmlDependency> Dependencies
        {
            get
            {
                EnsureDependencyList();

                return _DependencyList.Cast<IXmlDependency>().ToList();
            }
            set
            {
                _DependencyList = value.Cast<XmlDependency>().ToList();
            }
        }

        // TODO: workaround for the bad list behavior above
        public void AddDependency(IXmlDependency dependency)
        {
            EnsureDependencyList();
            _DependencyList.Add((XmlDependency)dependency);
        }

        // TODO: workaround for the bad list behavior above
        public void RemoveDependency(IXmlDependency dependency)
        {
            EnsureDependencyList();
            _DependencyList.Remove((XmlDependency)dependency);
        }

        private void EnsureDependencyList()
        {
            if (_DependencyList == null)
            {
                _DependencyList = new List<XmlDependency>();
            }
        }
    }
}
