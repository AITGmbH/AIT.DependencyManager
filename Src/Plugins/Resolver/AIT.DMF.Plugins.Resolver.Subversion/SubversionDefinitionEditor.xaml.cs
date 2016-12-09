using AIT.DMF.Common;
using System;
using System.Windows;

namespace AIT.DMF.Plugins.Resolver.Subversion
{
    /// <summary>
    /// Interaction logic for SubversionDefinitionEditor.xaml
    /// </summary>
    public partial class SubversionDefinitionEditor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SubversionDefinitionEditor"/> class.
        /// </summary>
        public SubversionDefinitionEditor()
        {
            InitializeComponent();
        }

        private void EditExcludeFilters_Click(object sender, RoutedEventArgs e)
        {
            var editor = new StringListEditor { Owner = Application.Current.MainWindow };
            var model = (SubversionDefinitionEditorViewModel)DataContext;

            var result = editor.ShowDialog(model.ExcludeFilter.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
            model.ExcludeFilter = string.Join(";", result);
        }

        private void EditIncludeFilters_Click(object sender, RoutedEventArgs e)
        {
            var editor = new StringListEditor { Owner = Application.Current.MainWindow };
            var model = (SubversionDefinitionEditorViewModel)DataContext;

            var result = editor.ShowDialog(model.IncludeFilter.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
            model.IncludeFilter = string.Join(";", result);
        }
    }
}
