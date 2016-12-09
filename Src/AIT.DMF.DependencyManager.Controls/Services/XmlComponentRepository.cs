using System.ComponentModel.Composition;
using AIT.DMF.Contracts.Gui;
using AIT.DMF.Contracts.Parser;
using AIT.DMF.Contracts.Services;
using AIT.DMF.DependencyManager.Controls.Model;

namespace AIT.DMF.DependencyManager.Controls.Services
{
    [Export(typeof(IXmlComponentRepository))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class XmlComponentRepository : IXmlComponentRepository, IPartImportsSatisfiedNotification
    {
        [Import]
        public IDependencyService DependencyService
        {
            get;
            set;
        }

        [Import]
        public ILogger Logger
        {
            get;
            set;
        }

        #region Implementation of IXmlComponentRepository

        public IXmlComponent GetXmlComponent(TargetsFileData targetsFileData)
        {
            if (DependencyService == null)
            {
                return null;
            }

            if (Logger == null)
            {
                return null;
            }

            return DependencyService.LoadXmlComponent(targetsFileData.LocalPath, Logger);
        }

        public void SaveXmlComponent(IXmlComponent component, TargetsFileData targetsFileData)
        {
            if (DependencyService == null)
                return;
            if (Logger == null)
                return;

            DependencyService.StoreXmlComponent(component, targetsFileData.LocalPath, Logger);
        }

        #endregion

        #region Implementation of IPartImportsSatisfiedNotification

        public void OnImportsSatisfied()
        {
            // place additional required logic after all imports have been satisfied here
        }

        #endregion
    }
}
