// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FileLogger.cs" company="AIT GmbH & Co. KG">
//   All rights reserved by AIT GmbH & Co. KG.
// </copyright>
// <summary>
//   A simple logger writing to a log file.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace AIT.DMF.Logging
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using Contracts.Logging;

    /// <summary>
    /// A simple logger writing to a log file.
    /// </summary>
    public class FileLogger : ILogSink
    {
        #region Private Members

        /// <summary>
        /// The path to the log file.
        /// </summary>
        private readonly string _filepath;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="FileLogger"/> class with the path for the logging file.
        /// </summary>
        /// <param name="filepath">The filepath.</param>
        public FileLogger(string filepath)
        {
            if (string.IsNullOrWhiteSpace(filepath))
            {
                throw new ArgumentNullException("filepath");
            }

            _filepath = filepath;
            if (File.Exists(filepath))
            {
                File.SetAttributes(filepath, FileAttributes.Normal);
                File.Delete(filepath);
            }
        }

        #endregion

        #region ILogSink

        /// <summary>
        /// Logs a message with the specified trace level.
        /// </summary>
        /// <param name="level">The trace level of this message.</param>
        /// <param name="message">The message to log.</param>
        public void Log(TraceLevel level, string message)
        {
            lock (this)
            {
                File.AppendAllText(_filepath, message + Environment.NewLine);
            }
        }

        #endregion
    }
}
