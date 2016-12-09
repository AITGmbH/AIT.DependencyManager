using System.Windows;

namespace AIT.DMF.DependencyManager.Controls
{
    /// <summary>
    /// Interaction logic for Shell.xaml
    /// </summary>
    public partial class Shell : Window
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Shell"/> class.
        /// </summary>
        public Shell()
        {
            InitializeComponent();

            LayoutUpdated += Shell_LayoutUpdated;
        }

        private void Shell_LayoutUpdated(object sender, System.EventArgs e)
        {
            var frameworkElement = Content as FrameworkElement;
            if (frameworkElement != null)
            {
                frameworkElement.Width = Width;
                frameworkElement.Height = Height;
            }
        }
    }
}
