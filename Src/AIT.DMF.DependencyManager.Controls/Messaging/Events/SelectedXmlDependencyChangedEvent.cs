using AIT.DMF.DependencyManager.Controls.ViewModels;

namespace AIT.DMF.DependencyManager.Controls.Messaging.Events
{
    public class SelectedXmlDependencyChangedEvent
    {
        public XmlDependencyViewModel OldValue
        {
            get;
            private set;
        }

        public XmlDependencyViewModel NewValue
        {
            get;
            private set;
        }

        public SelectedXmlDependencyChangedEvent(XmlDependencyViewModel oldValue, XmlDependencyViewModel newValue)
        {
            OldValue = oldValue;
            NewValue = newValue;
        }
    }
}
