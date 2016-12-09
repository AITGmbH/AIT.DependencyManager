using System.ComponentModel.Composition;
using AIT.DMF.DependencyManager.Controls.Services;

namespace AIT.DMF.DependencyManager.Controls.ViewModels
{
    public abstract class ComposableViewModelBase : ViewModelBase, IPartImportsSatisfiedNotification
    {
        protected ComposableViewModelBase()
        {
            // check for null to support design-time
            if (DependencyInjectionService.Instance.CompositionService != null)
            {
                DependencyInjectionService.Instance.CompositionService.SatisfyImportsOnce(this);
            }
        }

        #region Implementation of IPartImportsSatisfiedNotification

        /// <summary>
        /// Called when a part's imports have been satisfied and it is safe to use.
        /// </summary>
        public virtual void OnImportsSatisfied()
        {
        }

        #endregion
    }
}
