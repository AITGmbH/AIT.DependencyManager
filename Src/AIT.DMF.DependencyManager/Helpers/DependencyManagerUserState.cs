// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DependencyManagerUserState.cs" company="AIT GmbH & Co. KG">
//   All rights reserved by AIT GmbH & Co. KG
// </copyright>
// <summary>
//   Encapsulates the user state for async operations used in DependencyManager VSIX.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace AIT.AIT_DMF_DependencyManager.Helpers
{
    using System;
    using DMF.Contracts.Gui;
    using DMF.Contracts.Services;

    /// <summary>
    /// Encapsulates the user state for async operations used in DependencyManager VSIX.
    /// </summary>
    public class DependencyManagerUserState
    {
        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="DependencyManagerUserState"/> class.
        /// </summary>
        /// <param name="depService">The dependency service.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="callbackMethod">The callback method action.</param>
        /// <param name="force">The flag if operation should be forced.</param>
        /// <param name="recursive">The mode if all or only direct dependencies should be fetched.</param>
        public DependencyManagerUserState(IDependencyService depService, ILogger logger, Action callbackMethod, bool force = false, bool recursive = true)
        {
            if (depService == null)
            {
                // ReSharper disable LocalizableElement
                throw new ArgumentNullException("depService", "DependencyService cannot be null!");
                // ReSharper restore LocalizableElement
            }

            if (logger == null)
            {
                // ReSharper disable LocalizableElement
                throw new ArgumentNullException("logger", "Logger cannot be null!");
                // ReSharper restore LocalizableElement
            }

            DependencyService = depService;
            Logger = logger;
            ForceOperation = force;
            Callback = callbackMethod;
            Recursive = recursive;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the dependency service.
        /// </summary>
        public IDependencyService DependencyService { get; private set; }

        /// <summary>
        /// Gets the logger.
        /// </summary>
        public ILogger Logger { get; private set; }

        /// <summary>
        /// Gets a value indicating whether to force the operation.
        /// </summary>
        public bool ForceOperation { get; private set; }

        /// <summary>
        /// Gets the callback.
        /// </summary>
        public Action Callback { get; private set; }

        /// <summary>
        /// Gets a value indicating whether all dependencies or only direct dependencies are fetched.
        /// </summary>
        public bool Recursive { get; private set; }

        #endregion
    }
}
