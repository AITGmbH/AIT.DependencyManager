// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BuildOutputLogger.cs" company="AIT GmbH & Co. KG.">
//   All rights reserved by AIT GmbH & Co. KG.
// </copyright>
// <summary>
//   Defines the BuildOutputLogger type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace AIT.DMF.MSBuild
{
    using Contracts.Gui;
    using Microsoft.Build.Utilities;

    /// <summary>
    /// The logger for the MSBuild tasks.
    /// </summary>
    public class BuildOutputLogger : ILogger
    {
        /// <summary>
        /// The logger of type TaskLoggingHelper.
        /// </summary>
        private readonly TaskLoggingHelper _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="BuildOutputLogger"/> class with a new TaskLoggingHelper object.
        /// </summary>
        /// <param name="log">
        /// TaskLoggingHelper to use
        /// </param>
        public BuildOutputLogger(TaskLoggingHelper log)
        {
            _logger = log;
        }

        /// <summary>
        /// Writes the message to the build log output
        /// </summary>
        /// <param name="msg">Message to log</param>
        public void LogMsg(string msg)
        {
            if (!string.IsNullOrEmpty(msg))
            {
                _logger.LogMessage(msg);
            }
        }

        /// <summary>
        /// Writes an error message to the build log output
        /// </summary>
        /// <param name="msg"></param>
        public void LogError(string msg)
        {
            if (!string.IsNullOrEmpty(msg))
            {
                _logger.LogError(msg);
            }
        }

        /// <summary>
        /// Build output is wrote to a file and cannot be shown
        /// </summary>
        public void ShowMessages()
        {
        }
    }
}
