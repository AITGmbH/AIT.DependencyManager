using System.Diagnostics;

namespace AIT.DMF.Contracts.Logging
{
    /// <summary>
    /// A log sink will be registered to the <see cref="ILogger"/> and will receive all log messages that shall be written
    /// </summary>
    public interface ILogSink
    {
        /// <summary>
        /// Logs a new message
        /// </summary>
        /// <param name="level">The trace level of this message</param>
        /// <param name="message">The message to log</param>
        void Log(TraceLevel level, string message);
    }
}
