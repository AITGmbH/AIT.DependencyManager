using System;
using System.Windows.Forms;

namespace AIT.AIT_DMF_DependencyManager.Helpers
{
    /// <summary>
    /// A simple wrapper for the <see cref="IWin32Window"/> interface to overcome
    /// some issues when using the built-in <see cref="NativeWindow"/> type.
    /// </summary>
    public class WindowWrapper : IWin32Window
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WindowWrapper"/> class.
        /// </summary>
        /// <param name="handle">The handle to wrap.</param>
        public WindowWrapper(IntPtr handle)
        {
            _hwnd = handle;
        }

        /// <summary>
        /// Gets the handle to the window represented by the implementer.
        /// </summary>
        /// <returns>A handle to the window represented by the implementer.</returns>
        public IntPtr Handle
        {
            get
            {
                return _hwnd;
            }
        }

        private readonly IntPtr _hwnd;
    }
}
