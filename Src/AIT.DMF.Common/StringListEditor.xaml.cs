using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using AIT.DMF.Common.Annotations;

namespace AIT.DMF.Common
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class StringListEditor : INotifyPropertyChanged
    {
        /// <summary>
        /// Wrapper for strings that allows two way synchronization.
        /// </summary>
        public class StringListEntry
        {
            private string _value;

            /// <summary>
            /// Gets or sets the actual string value.
            /// </summary>
            public string Value
            {
                get { return _value; }
                set
                {
                    _value = value;
                    IsNew = false;
                    if (Owner != null)
                        Owner.Sort();
                }
            }

            /// <summary>
            /// Gets or sets whether this entry is new.
            /// </summary>
            public bool IsNew
            {
                get;
                set;
            }

            /// <summary>
            /// Gets or sets the owner of the refreshable UI (sorry for avoiding INotifyPropertyChanged)
            /// </summary>
            public StringListEditor Owner
            {
                get;
                set;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets or sets the string collection.
        /// </summary>
        public ObservableCollection<StringListEntry> StringCollection
        {
            get;
            set;
        }

        /// <summary>
        /// Gets whether the delete button is enabled.
        /// </summary>
        public bool CanDelete
        {
            get
            {
                return Entries.SelectedItem != null;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StringListEditor"/> class.
        /// </summary>
        public StringListEditor()
        {
            StringCollection = new ObservableCollection<StringListEntry>();
            DataContext = this;
            InitializeComponent();
        }

        /// <summary>
        /// Select textbox and text when item is selected
        /// </summary>
        private void EditBox_OnLoaded(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox == null) return;

            if (((StringListEntry)textBox.DataContext).IsNew)
            {
                textBox.SelectAll();
            }
            else
            {
                textBox.SelectionStart = textBox.Text.Length;
            }

            textBox.Focus();
        }

        /// <summary>
        /// Remove selected entry from list
        /// </summary>
        private void RemoveClick(object sender, RoutedEventArgs e)
        {
            StringCollection.Remove((StringListEntry)Entries.SelectedItem);
            OnPropertyChanged("CanDelete");
        }

        /// <summary>
        /// Add new entry
        /// </summary>
        private void AddClick(object sender, RoutedEventArgs e)
        {
            Entries.SelectedItem = AddItem(" New Filter", true);
        }

        /// <summary>
        /// Create wrapper for string and add to list
        /// </summary>
        private StringListEntry AddItem(string value, bool isNew)
        {
            var newItem = new StringListEntry { Value = value, Owner = this, IsNew = isNew };
            StringCollection.Add(newItem);
            OnPropertyChanged("CanDelete");
            return newItem;
        }

        /// <summary>
        /// Sort presentation. Could not make ICollectionView work
        /// </summary>
        public void Sort()
        {
            var collectionView = Entries.ItemsSource as ICollectionView;
            if (collectionView != null)
            {
                collectionView.Refresh();
            }
        }

        /// <summary>
        /// Shows a modal dialog for editing a filter list.
        /// </summary>
        /// <param name="filterList">The items to edit.</param>
        /// <returns>A new enumeration with the edited filters.</returns>
        public IEnumerable<string> ShowDialog(IEnumerable<string> filterList)
        {
            foreach (var item in filterList)
            {
                Entries.SelectedItem = AddItem(item, false);
            }

            ShowDialog();
            return Entries.Items.Cast<StringListEntry>().Select(x => x.Value);
        }

        /// <summary>
        /// Close dialog
        /// </summary>
        private void OkClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        private void Entries_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            OnPropertyChanged("CanDelete");
        }
    }
}
