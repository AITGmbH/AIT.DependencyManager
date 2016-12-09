using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using AIT.DMF.Common.Annotations;
using System;

namespace AIT.DMF.Common
{
    /// <summary>
    /// Interaction logic for MultiSiteEditor.xaml
    /// </summary>
    public partial class MultiSiteEditor : INotifyPropertyChanged
    {
        /// <summary>
        /// Wrapper that allows two way synchronization.
        /// </summary>
        public class MultiSiteEntry
        {
            private string _site;

            /// <summary>
            /// Gets or sets the actual site value.
            /// </summary>
            public string Site
            {
                get { return _site; }
                set
                {
                    _site = value;
                    IsNew = false;
                }
            }

            /// <summary>
            /// Gets or sets the corresponding basepath of the site
            /// </summary>
            public string Basepath { get; set; }

            /// <summary>
            /// Gets or sets the corresponding replacepath of the site
            /// </summary>
            public string Replacepath { get; set; }

            /// <summary>
            /// Gets or sets whether this entry is new.
            /// </summary>
            public bool IsNew { get; set;}
        }

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets or sets the string collection.
        /// </summary>
        public SortableObservableCollection<MultiSiteEntry> DataCollection
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
        /// Initializes a new instance of the <see cref="MultiSiteEditor"/> class.
        /// </summary>
        public MultiSiteEditor()
        {
            DataCollection = new SortableObservableCollection<MultiSiteEntry>(new List<MultiSiteEntry>());
            DataContext = this;
            InitializeComponent();
        }

        /// <summary>
        /// Remove selected entry from list
        /// </summary>
        private void RemoveClick(object sender, RoutedEventArgs e)
        {
            DataCollection.Remove((MultiSiteEntry)Entries.SelectedItem);
            OnPropertyChanged("CanDelete");
        }

        /// <summary>
        /// Add new entry to list
        /// </summary>
        private void AddClick(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(tbxSite.Text) && !string.IsNullOrEmpty(tbxBasepath.Text) && !string.IsNullOrEmpty(tbxReplacepath.Text))
            {
                if (!DataCollection.Any(item => item.Site == tbxSite.Text))
                {
                    lblError.Text = string.Empty;

                    AddItem(tbxSite.Text, tbxBasepath.Text, tbxReplacepath.Text, true);
                }
                else
                {
                    lblError.Text = string.Format("Duplicate entry. An entry with AD-Site '{0}' already exists", tbxSite.Text);
                }
            }
            else
            {
                lblError.Text = "Missing value. All textfields are mandatory.";
            }
        }

        /// <summary>
        /// Create wrapper for string and add to list
        /// </summary>
        private MultiSiteEntry AddItem(string site, string basepath, string replacepath, bool isNew)
        {
            var newItem = new MultiSiteEntry { Site = site, Basepath = basepath, Replacepath = replacepath, IsNew = isNew };
            DataCollection.Add(newItem);

            DataCollection.Sort(x => x.Site, ListSortDirection.Ascending);

            OnPropertyChanged("CanDelete");

            tbxSite.Text = string.Empty;
            tbxBasepath.Text = string.Empty;
            tbxReplacepath.Text = string.Empty;

            return newItem;
        }

        /// <summary>
        /// Shows a modal dialog for editing the multi site configuration.
        /// </summary>
        /// <param name="multiSiteList">The items to edit.</param>
        /// <returns>A new enumeration with the edited filters.</returns>
        public string ShowDialog(string multiSiteList)
        {
            if (multiSiteList != null)
            {
                var entries = multiSiteList.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var entry in entries)
                {
                    var tmp = entry.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                    DataCollection.Add(new MultiSiteEntry{Site= tmp[0], Basepath= tmp[1], Replacepath=tmp[2]});
                }
            }

            DataCollection.Sort(x => x.Site, ListSortDirection.Ascending);

            ShowDialog();

            var ret = string.Empty;

            for (int i = 0; i < DataCollection.Count; i++)
            {
                ret += string.Format("{0},{1},{2};", DataCollection[i].Site, DataCollection[i].Basepath, DataCollection[i].Replacepath);
            }

            //Remove last semicolon
            if (!string.IsNullOrEmpty(ret))
            {
                ret = ret.Substring(0, ret.Length - 1);
            }

            return ret;
        }

        /// <summary>
        /// Close dialog
        /// </summary>
        private void CloseClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
