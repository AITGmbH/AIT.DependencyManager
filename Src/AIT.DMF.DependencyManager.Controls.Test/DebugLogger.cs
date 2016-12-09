using System.Diagnostics;
using AIT.DMF.Contracts.Gui;

namespace AIT.DMF.DependencyManager.Controls.Test
{
    public class DebugLogger : ILogger
    {
        public void LogMsg(string msg)
        {
            Debug.WriteLine(msg);
        }

        public void ShowMessages()
        {
        }
    }
}
