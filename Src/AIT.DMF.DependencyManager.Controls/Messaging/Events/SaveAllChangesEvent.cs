
namespace AIT.DMF.DependencyManager.Controls.Messaging.Events
{
    public class SaveAllChangesEvent
    {
        public string FileName
        {
            get;
            private set;
        }

        public SaveAllChangesEvent()
        {
        }

        public SaveAllChangesEvent(string fileName)
        {
            FileName = fileName;
        }
    }
}
