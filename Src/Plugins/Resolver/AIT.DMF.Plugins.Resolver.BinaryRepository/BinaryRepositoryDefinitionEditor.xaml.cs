using System;
using System.Windows;
using System.Windows.Controls;
using AIT.DMF.Common;

namespace AIT.DMF.Plugins.Resolver.BinaryRepository
{
    /// <summary>
    /// Interaction logic for BinaryRepositoryDefinitionEditor.xaml
    /// </summary>
    public partial class BinaryRepositoryDefinitionEditor : UserControl
    {
        public BinaryRepositoryDefinitionEditor()
        {
            InitializeComponent();
        }

        private void EditExcludeFilters_Click(object sender, RoutedEventArgs e)
        {
            var editor = new StringListEditor { Owner = Application.Current.MainWindow };
            var model = (BinaryRepositoryDefinitionEditorViewModel)DataContext;

            var result = editor.ShowDialog(model.ExcludeFilter.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
            model.ExcludeFilter = string.Join(";", result);
        }

        private void EditIncludeFilters_Click(object sender, RoutedEventArgs e)
        {
            var editor = new StringListEditor { Owner = Application.Current.MainWindow };
            var model = (BinaryRepositoryDefinitionEditorViewModel)DataContext;

            var result = editor.ShowDialog(model.IncludeFilter.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
            model.IncludeFilter = string.Join(";", result);
        }
    }
}
