using AIT.DMF.Common;
using System;
using System.Windows;
using System.Windows.Controls;

namespace AIT.DMF.Plugins.Resolver.FileShare
{
    /// <summary>
    /// Interaction logic for FileShareDefinitionEditor.xaml
    /// </summary>
    public partial class FileShareDefinitionEditor : UserControl
    {
        public FileShareDefinitionEditor()
        {
            InitializeComponent();
        }

        private void EditExcludeFilters_Click(object sender, RoutedEventArgs e)
        {
            var editor = new StringListEditor { Owner = Application.Current.MainWindow };
            var model = (FileShareDefinitionEditorViewModel)DataContext;

            var result = editor.ShowDialog(model.ExcludeFilter.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
            model.ExcludeFilter = string.Join(";", result);
        }

        private void EditIncludeFilters_Click(object sender, RoutedEventArgs e)
        {
            var editor = new StringListEditor { Owner = Application.Current.MainWindow };
            var model = (FileShareDefinitionEditorViewModel)DataContext;

            var result = editor.ShowDialog(model.IncludeFilter.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
            model.IncludeFilter = string.Join(";", result);
        }
    }
}
