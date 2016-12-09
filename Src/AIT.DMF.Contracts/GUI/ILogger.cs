namespace AIT.DMF.Contracts.Gui
{
    public interface ILogger
    {
        /// <summary>
        /// Logs a message inside the logger.
        /// </summary>
        /// <param name="msg">Message to log</param>
        void LogMsg(string msg);

        /// <summary>
        /// Shows the log to the user if possible.
        /// </summary>
        void ShowMessages();
    }
}
