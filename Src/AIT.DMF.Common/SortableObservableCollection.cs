using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace AIT.DMF.Common
{
    /// <summary>
    /// This class provides a sort method for ObservableCollections.
    /// Based on blogpost: http://elegantcode.com/2009/05/14/write-a-sortable-observablecollection-for-wpf/
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SortableObservableCollection<T> : ObservableCollection<T>
    {
        /// <summary>
        /// Creates an instance based on a List.
        /// </summary>
        /// <param name="list"></param>
        public SortableObservableCollection(List<T> list) : base(list)
        {
        }

        /// <summary>
        /// Creates an instance based on a IEnumerable.
        /// </summary>
        /// <param name="collection"></param>
        public SortableObservableCollection(IEnumerable<T> collection) : base(collection)
        {
        }

        /// <summary>
        /// Sorts the collection based on the direction (ascending or descending).
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="keySelector"></param>
        /// <param name="direction"></param>
        public void Sort<TKey>(Func<T, TKey> keySelector, System.ComponentModel.ListSortDirection direction)
        {
            switch (direction)
            {
                case System.ComponentModel.ListSortDirection.Ascending:
                    {
                        ApplySort(Items.OrderBy(keySelector));
                        break;
                    }
                case System.ComponentModel.ListSortDirection.Descending:
                    {
                        ApplySort(Items.OrderByDescending(keySelector));
                        break;
                    }
            }
        }

        /// <summary>
        /// Sorts the collection based on a individual comparer.
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="keySelector"></param>
        /// <param name="comparer"></param>
        public void Sort<TKey>(Func<T, TKey> keySelector, IComparer<TKey> comparer)
        {
            ApplySort(Items.OrderBy(keySelector, comparer));
        }

        /// <summary>
        /// Sorts the collection by using an SortedList.
        /// </summary>
        /// <param name="sortedItems"></param>
        private void ApplySort(IEnumerable<T> sortedItems)
        {
            var sortedItemsList = sortedItems.ToList();

            foreach (var item in sortedItemsList)
            {
                Move(IndexOf(item), sortedItemsList.IndexOf(item));
            }
        }
    }
}
