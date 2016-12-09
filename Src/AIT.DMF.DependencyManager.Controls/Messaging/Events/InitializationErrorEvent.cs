using System;

namespace AIT.DMF.DependencyManager.Controls.Messaging.Events
{
    public class InitializationErrorEvent
    {
        public Exception ExceptionCausingError { get; private set; }

        public InitializationErrorEvent()
        {
            ExceptionCausingError = null;
        }

        public InitializationErrorEvent(Exception ex)
        {
            ExceptionCausingError = ex;
        }
    }
}
