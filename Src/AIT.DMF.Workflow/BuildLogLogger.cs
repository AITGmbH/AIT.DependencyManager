// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BuildLogLogger.cs" company="AIT GmbH & Co. KG">
//   All rights reserved by AIT GmbH & Co. KG
// </copyright>
// <summary>
//   The logger which logs into the build log.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace AIT.DMF.Workflow
{
    using System.Activities;
    using Contracts.Gui;

    using Microsoft.TeamFoundation.Build.Client;
    using System.Activities.Tracking;
    /// <summary>
    /// The logger which logs into the build log.
    /// </summary>
    public class BuildLogLogger : ILogger
    {
        #region Private Members

        /// <summary>
        /// The code activity context.
        /// </summary>
        // ReSharper disable InconsistentNaming
        private readonly CodeActivityContext _context;
        // ReSharper restore InconsistentNaming
        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="BuildLogLogger"/> class.
        /// </summary>
        /// <param name="context">
        /// The code activity context.
        /// </param>
        public BuildLogLogger(CodeActivityContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Build log is written to the build log and is shown at the end.
        /// </summary>
        public void ShowMessages()
        {
        }

        /// <summary>
        /// Logs a message to the build summary.
        /// </summary>
        /// <param name="message">The message to the build summary.</param>
        public void LogMsg(string message)
        {

            var importance = new CustomTrackingRecord(string.Format("TrackBuildMessage {0} {1}", BuildMessageImportance.High, message));
            _context.Track(importance);
        }

        /// <summary>
        /// Logs a warning message to the build summary.
        /// </summary>
        /// <param name="message">The message to the build summary.</param>
        public void LogWarning(string message)
        {
            var importance = new CustomTrackingRecord(string.Format("TrackBuildWarning: {0}", message));
            _context.Track(importance);
        }

        /// <summary>
        /// Logs a error message to the build summary.
        /// </summary>
        /// <param name="message">The message to the build summary.</param>
        public void LogError(string message)
        {
            var importance = new CustomTrackingRecord(string.Format("TrackBuildError: {0}", message));
            _context.Track(importance);
        }
    }
}
