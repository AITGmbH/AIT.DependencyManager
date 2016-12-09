using AIT.DMF.Common;
using AIT.DMF.DependencyManager.Controls.ViewModels;
using System.Windows;
using System.Windows.Controls;


namespace AIT.DMF.DependencyManager.Controls.Views
{
    /// <summary>
    /// Interaction logic for GeneralSettingsEditorView.xaml
    /// </summary>
    public partial class GeneralSettingsEditorView : UserControl
    {
        public GeneralSettingsEditorView()
        {
            InitializeComponent();
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
