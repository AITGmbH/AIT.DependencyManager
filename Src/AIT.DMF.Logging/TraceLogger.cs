// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TraceLogger.cs" company="AIT GmbH & Co. KG">
//   All rights reserved by AIT GmbH & Co. KG.
// </copyright>
// <summary>
//   A simple logger which writes to the Visual Studio Trace log.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace AIT.DMF.Logging
{
    using System.Diagnostics;
    using AIT.DMF.Contracts.Logging;

    /// <summary>
    /// A simple logger which writes to the Visual Studio Trace log.
    /// </summary>
    public class TraceLogger : ILogSink
    {
        /// <summary>
        /// Logs a new message with the specified trace level.
        /// </summary>
        /// <param name="level">The trace level of this message.</param>
        /// <param name="message">The message to log.</param>
        public void Log(TraceLevel level, string message)
        {
            Trace.WriteLine(message);
        }
    }
}

