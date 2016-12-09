using AIT.DMF.Common;
using System;
using System.Windows;
using System.Windows.Controls;

namespace AIT.DMF.Plugins.Resolver.VNextBuildResult
{
    /// <summary>
    /// Interaction logic for VNextBuildResultDefinitionEditor.xaml
    /// </summary>
    public partial class VNextBuildResultDefinitionEditor : UserControl
    {
        public VNextBuildResultDefinitionEditor()
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
            var editor = new StringListEditor { Owner = Application.Current.MainWindow };
            var model = (VNextBuildResultDefinitionEditorViewModel)DataContext;

            var result = editor.ShowDialog(model.ExcludeFilter.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
            model.ExcludeFilter = string.Join(";", result);
        }

        private void EditIncludeFilters_Click(object sender, RoutedEventArgs e)
        {
            var editor = new StringListEditor { Owner = Application.Current.MainWindow };
            var model = (VNextBuildResultDefinitionEditorViewModel)DataContext;

            var result = editor.ShowDialog(model.IncludeFilter.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
            model.IncludeFilter = string.Join(";", result);
        }
    }
}