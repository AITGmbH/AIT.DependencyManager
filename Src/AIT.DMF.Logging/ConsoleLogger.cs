// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ConsoleLogger.cs" company="AIT GmbH & Co. KG">
//   All rights reserved by AIT GmbH & Co. KG.
// </copyright>
// <summary>
//   A simple logger which dumps the log messages to the console.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace AIT.DMF.Logging
{
    using System.Diagnostics;
    using AIT.DMF.Contracts.Logging;

    /// <summary>
    /// A simple logger which dumps the log messages to the console.
    /// </summary>
    public class ConsoleLogger : ILogSink
    {
        /// <summary>
        /// Logs a new message with a specific trace level.
        /// </summary>
        /// <param name="level">The trace level of this message.</param>
        /// <param name="message">The message to log.</param>
        public void Log(TraceLevel level, string message)
        {
            if (TraceLevel.Error == level)
            {
                System.Console.Error.WriteLine(message);
            }
            else
            {
                System.Console.WriteLine(message);
            }
        }
    }
}
