// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Logger.cs" company="AIT GmbH & Co. KG.">
//   All rights reserved by AIT GmbH & Co. KG.
// </copyright>
// <summary>
//   The central logging instance for tracing, log files etc.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace AIT.DMF.Common
{
    using System;
    using System.Collections.Generic;//
    using System.Diagnostics;
    using AIT.DMF.Contracts.Logging;

    /// <summary>
    /// The central logging instance for tracing, log files etc.
    /// </summary>
    public class Logger
    {
        #region Private Members

        /// <summary>
        /// The private logger instance
        /// </summary>
        private static readonly Logger LoggerInstance = new Logger();

        #endregion

        #region Constructor

        /// <summary>
        /// Prevents a default instance of the <see cref="Logger"/> class from being created.
        /// </summary>
        private Logger()
        {
            this.AttachedLoggers = new List<ILogSink>();
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the attached loggers.
        /// </summary>
        public List<ILogSink> AttachedLoggers { get; private set; }

        #endregion

        #region Factory Method

        /// <summary>
        /// Returns an instance of this logger.
        /// </summary>
        /// <returns>The logger.</returns>
        public static Logger Instance()
        {
            return LoggerInstance;
        }

        #endregion

        #region Logging Methods

        /// <summary>
        /// Logs the specified level.
        /// </summary>
        /// <param name="level">The level.</param>
        /// <param name="message">The message.</param>
        public void Log(TraceLevel level, string message)
        {
            var fmessage = string.Format("{0} - {1} {2}", DateTime.Now, string.Format("{0}:", level).PadRight(9), message);
            foreach (var sink in this.AttachedLoggers)
            {
                try
                {
                    sink.Log(level, fmessage);
                }
                // ReSharper disable EmptyGeneralCatchClause
                catch (Exception)
                // ReSharper restore EmptyGeneralCatchClause
                {
                }
            }
        }

        /// <summary>
        /// Logs a message which formats the arguments (based on the format string) with the specified trace level.
        /// </summary>
        /// <param name="level">The trace level.</param>
        /// <param name="format">The format string.</param>
        /// <param name="args">The arguments.</param>
        // ReSharper disable MethodOverloadWithOptionalParameter
        public void Log(TraceLevel level, string format, params object[] args)
        // ReSharper restore MethodOverloadWithOptionalParameter
        {
            this.Log(level, string.Format(format, args));
        }

        /// <summary>
        /// Registers the provided log sink.
        /// </summary>
        /// <param name="sink">The log sink.</param>
        public void RegisterLogSink(ILogSink sink)
        {
            if (null != sink)
            {
                this.AttachedLoggers.Add(sink);
            }
        }

        #endregion
    }
}
