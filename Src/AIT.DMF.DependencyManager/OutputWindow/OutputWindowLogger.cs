// --------------------------------------------------------------------------------------------------------------------
// <copyright file="OutputWindowLogger.cs" company="AIT GmbH & Co. KG">
//   All rights reserved by AIT GmbH & Co. KG
// </copyright>
// <summary>
//   Wrapper for OutputWindowPane which is used like a logger.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
// ReSharper disable CheckNamespace
namespace AIT.AIT_DMF_DependencyManager
// ReSharper restore CheckNamespace
{
    using System;
    using DMF.Contracts.Gui;
    using EnvDTE;
    using EnvDTE80;

    /// <summary>
    /// Wrapper for OutputWindowPane which is used like a logger.
    /// </summary>
    internal class OutputWindowLogger : ILogger
    {
        #region Private members

        /// <summary>
        /// The development environment reference.
        /// </summary>
        private readonly DTE2 _devEnv;

        /// <summary>
        /// The dependency download output window pane.
        /// </summary>
        private OutputWindowPane _dependencyDownloadOutputWindowPane;

        /// <summary>
        /// The dependency download output window.
        /// </summary>
        private OutputWindow _dependencyDownloadOutputWindow;

        /// <summary>
        /// The dependency download output window title.
        /// </summary>
        private readonly string _dependencyDownloadOutputWindowTitle;

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="OutputWindowLogger"/> class with a reference to the development environment.
        /// </summary>
        /// <param name="devEnv">The development environment reference.</param>
        public OutputWindowLogger(DTE2 devEnv)
        {
            _devEnv = devEnv;
            _dependencyDownloadOutputWindow = null;
            _dependencyDownloadOutputWindowPane = null;
            _dependencyDownloadOutputWindowTitle = "Dependency Manager";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OutputWindowLogger"/> class with a reference to the development environment.
        /// </summary>
        /// <param name="devEnv">The development environment reference.</param>
        /// <param name="title">The output window title.</param>
        public OutputWindowLogger(DTE2 devEnv, string title)
        {
            _devEnv = devEnv;
            _dependencyDownloadOutputWindow = null;
            _dependencyDownloadOutputWindowPane = null;
            _dependencyDownloadOutputWindowTitle = title;
        }

        /// <summary>
        /// Activates the OutputWindowPane "Dependency Manager" inside of Visual Studio
        /// </summary>
        public void ShowMessages()
        {
            if (_dependencyDownloadOutputWindow == null || _dependencyDownloadOutputWindowPane == null)
            {
                InitializeOutputWindowPane();
            }

            _dependencyDownloadOutputWindowPane.Activate();
            _dependencyDownloadOutputWindow.Parent.Activate();
        }

        /// <summary>
        /// Writes the message (if not null or empty) inside the OutputWindowPane
        /// </summary>
        /// <param name="msg">The message to log.</param>
        public void LogMsg(string msg)
        {
            if (_dependencyDownloadOutputWindow == null || _dependencyDownloadOutputWindowPane == null)
            {
                InitializeOutputWindowPane();
            }

            if (!string.IsNullOrEmpty(msg))
            {
                // ReSharper disable PossibleNullReferenceException
                _dependencyDownloadOutputWindowPane.OutputString(msg + "\n");
                // ReSharper restore PossibleNullReferenceException
            }
        }

        /// <summary>
        /// Initialize the output window pane.
        /// </summary>
        private void InitializeOutputWindowPane()
        {
            if (_dependencyDownloadOutputWindow == null || _dependencyDownloadOutputWindowPane == null)
            {
                _dependencyDownloadOutputWindow = (OutputWindow)_devEnv.Windows.Item(Constants.vsWindowKindOutput).Object;

                try
                {
                    _dependencyDownloadOutputWindowPane =
                        _dependencyDownloadOutputWindow.OutputWindowPanes.Item(_dependencyDownloadOutputWindowTitle);
                }
                catch (Exception)
                {
                    _dependencyDownloadOutputWindowPane = _dependencyDownloadOutputWindow.OutputWindowPanes.Add(_dependencyDownloadOutputWindowTitle);
                }

                if ((null == _dependencyDownloadOutputWindow) || (null == _dependencyDownloadOutputWindow.OutputWindowPanes) || (_dependencyDownloadOutputWindowPane == null))
                {
                    throw new NotSupportedException();
                }
            }
        }
    }
}
