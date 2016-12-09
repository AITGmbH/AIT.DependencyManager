using System.Windows;

namespace AIT.DMF.DependencyManager.Controls.Services
{
    public static class UserMessageService
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        public static void ShowError(string message)
        {
            MessageBox.Show(message, "AIT Dependency Manager", MessageBoxButton.OK, MessageBoxImage.Exclamation);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        public static void ShowWarning(string message)
        {
            MessageBox.Show(message, "AIT Dependency Manager", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
