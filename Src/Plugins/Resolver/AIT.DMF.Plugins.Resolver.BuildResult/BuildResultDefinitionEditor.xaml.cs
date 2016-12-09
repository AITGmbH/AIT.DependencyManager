using System;
using AIT.DMF.Common;
using System.Windows;
using System.Windows.Controls;

namespace AIT.DMF.Plugins.Resolver.BuildResult
{
    /// <summary>
    /// Interaction logic for BuildResultDefinitionEditor.xaml
    /// </summary>
    public partial class BuildResultDefinitionEditor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BuildResultDefinitionEditor"/> class.
        /// </summary>
        public BuildResultDefinitionEditor()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Occurs when a ComboBoxItem is selected.
        /// </summary>
        private void ComboBoxItemSelected(object sender, RoutedEventArgs e)
        {
            var comboBoxItem = sender as ComboBoxItem;
            if (comboBoxItem == null) return;
            var checkBox = WpfVisualHelper.FindVisualChild<CheckBox>(comboBoxItem);
            if (checkBox == null) return;
            checkBox.Focus();
        }

        private void EditExcludeFilters_Click(object sender, RoutedEventArgs e)
        {
            var editor = new StringListEditor {Owner = Application.Current.MainWindow};
            var model = (BuildResultDefinitionEditorViewModel)DataContext;

            var result = editor.ShowDialog(model.ExcludeFilter.Split(new [] {';'}, StringSplitOptions.RemoveEmptyEntries));
            model.ExcludeFilter = string.Join(";", result);
        }

        private void EditIncludeFilters_Click(object sender, RoutedEventArgs e)
        {
            var editor = new StringListEditor {Owner = Application.Current.MainWindow};
            var model = (BuildResultDefinitionEditorViewModel)DataContext;

            var result = editor.ShowDialog(model.IncludeFilter.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
            model.IncludeFilter = string.Join(";", result);
        }
    }
}
