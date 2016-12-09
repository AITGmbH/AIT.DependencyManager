// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ViewModelBase.cs" company="AIT GmbH & Co. KG">
//   All rights reserved by AIT GmbH & Co. KG
// </copyright>
// <summary>
//   This class implements the ViewModelBase
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace AIT.DMF.DependencyManager.Controls.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq.Expressions;
    using Contracts.GUI;

    /// <summary>
    /// A base class for view models with <see cref="INotifyPropertyChanged"/> support.
    /// </summary>
    public abstract class ViewModelBase : IViewModel
    {
        #region Implementation of INotifyPropertyChanged

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        [Obsolete("Use overload of this method that supports refactoring and prevents typos")]
        protected virtual void RaiseNotifyPropertyChanged(string propertyName)
        {
            var handlers = PropertyChanged;
            if (handlers != null)
            {
                handlers(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        /// <summary>
        /// Raises a NotifyPropertyChanged event. This is the type safe version that
        /// supports refactoring and prevents typos
        /// </summary>
        /// <param name="selectorExpression">
        /// Must be a member expression. For example "() => PropertyName" 
        /// </param>
        /// <typeparam name="T">Type of the property
        /// </typeparam>
        /// <exception cref="ArgumentNullException">If selector expression is null
        /// </exception>
        /// <exception cref="ArgumentException">If selectorExpression is not a member expression
        /// </exception>
        protected void RaiseNotifyPropertyChanged<T>(Expression<Func<T>> selectorExpression)
        {
            if (selectorExpression == null)
            {
                throw new ArgumentNullException("selectorExpression");
            }

            var body = selectorExpression.Body as MemberExpression;
            if (body == null)
            {
                throw new ArgumentException("The body must be a member expression");
            }

            RaiseNotifyPropertyChanged(body.Member.Name);
        }

        /// <summary>
        /// Sets a backing field value of a property and raises a NotifyPropertyChanged-Event in case the value has changed
        /// </summary>
        /// <param name="field">The field to change</param>
        /// <param name="value">The new value of the field</param>
        /// <param name="selectorExpression">Expression referencing the Property whose backing field is changed. This is used to infer the name of the changed property in a way that supports refactoring.</param>
        /// <typeparam name="T">Type of the property</typeparam>
        /// <returns>
        /// Whether the event was risen or not.
        /// </returns>
        protected bool SetField<T>(ref T field, T value, Expression<Func<T>> selectorExpression)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return false;
            }

            field = value;
            this.RaiseNotifyPropertyChanged(selectorExpression);
            return true;
        }
    }
}
