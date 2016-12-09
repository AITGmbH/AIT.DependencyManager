namespace AIT.DMF.Plugins.Resolver.VNextBuildResult
{
    using System.ComponentModel;

    /// <summary>
    /// Represents the combo box item.
    /// </summary>
    /// <typeparam name="T">The combo box name type.</typeparam>
    internal class ComboBoxItem<T> : INotifyPropertyChanged
    {
        #region Private Members

        /// <summary>
        /// The state of the checkbox.
        /// </summary>
        private bool _isChecked;

        /// <summary>
        /// The content.
        /// </summary>
        private T _content;

        #endregion Private Members

        #region INotifyPropertyChanged Event

        /// <summary>
        /// The handler for property changed events.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        #endregion INotifyPropertyChanged Event

        #region Properties

        /// <summary>
        /// Gets or sets a value indicating whether item is selected.
        /// </summary>
        /// <value>True if selected. False otherwise.</value>
        public bool IsChecked
        {
            get
            {
                return _isChecked;
            }

            set
            {
                _isChecked = value;
                OnPropertyChanged("IsChecked");
            }
        }

        /// <summary>
        /// Gets or sets the content of the item.
        /// </summary>
        /// <value>The name.</value>
        public T Content
        {
            get
            {
                return _content;
            }

            set
            {
                _content = value;
                OnPropertyChanged("Content");
            }
        }

        #endregion Properties

        #region INotifyPropertyChanged

        /// <summary>
        /// Event handler for events for changed properties.
        /// </summary>
        /// <param name="propertyName">
        /// The property name.
        /// </param>
        protected virtual void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;

            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion INotifyPropertyChanged
    }
}