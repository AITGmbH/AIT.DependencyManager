using AIT.DMF.Contracts.Parser;
using AIT.DMF.DependencyManager.Controls.Model;

namespace AIT.DMF.DependencyManager.Controls.Services
{
    /// <summary>
    /// An interface with common actions to perform on Xml components.
    /// </summary>
    public interface IXmlComponentRepository
    {
        IXmlComponent GetXmlComponent(TargetsFileData targetsFileData);

        void SaveXmlComponent(IXmlComponent component, TargetsFileData targetsFileData);

        // this interface will be extended later, for example by adding methods to write back/save 
        // changed xml dependencies data etc.
    }
}
