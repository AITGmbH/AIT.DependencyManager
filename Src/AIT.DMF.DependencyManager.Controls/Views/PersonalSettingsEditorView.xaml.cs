using AIT.DMF.Common;
using AIT.DMF.DependencyManager.Controls.ViewModels;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;


namespace AIT.DMF.DependencyManager.Controls.Views
{
    /// <summary>
    /// Interaction logic for GeneralSettingsEditorView.xaml
    /// </summary>
    public partial class PersonalSettingsEditorView : UserControl
    {
        public PersonalSettingsEditorView()
        {
            InitializeComponent();
        }

       private void BrowseButton_Click(object sender, RoutedEventArgs e)
       {
           TextBox targetTextBox = null;

           if(e.OriginalSource.GetType() == typeof (Button))
           {
             if(  ((Button)e.OriginalSource).Name.Equals("BrowseSevenZipExe"))
               {
                   targetTextBox = SevenZipExeTextBox;
               }

           }
            // Create OpenFileDialog
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();           
           
            // Set filter for file extension and default file extension
            dlg.DefaultExt = ".exe";
            dlg.Filter = "Exe Files (.exe)|*.exe"; 

            // Display OpenFileDialog by calling ShowDialog method
            Nullable<bool> result = dlg.ShowDialog();

            // Get the selected file name and display in a TextBox
            if (result == true)
            {
                // Open document
                string filename = dlg.FileName;
                targetTextBox.Text = filename;

               
            }
        }
        /// <summary>
        /// The event that handles the click on a link
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        private void ConfigureMultiSite_Click(object sender, RoutedEventArgs e)
        {
            var editor = new MultiSiteEditor { Owner = Application.Current.MainWindow };
            var model = (GeneralSettingsEditorViewModel)DataContext;

            // Show dialog with current data; write data back when dialog was closed
            model.Sites = editor.ShowDialog(model.Sites);
        }
    }
}
