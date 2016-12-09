using System;
using System.Collections.Concurrent;
using System.ComponentModel.Composition;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace AIT.DMF.DependencyManager.Controls.Messaging
{
    /// <summary>
    /// An implementation for the <see cref="IEventPublisher"/> interface.
    /// </summary>
    [Export(typeof(IEventPublisher))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class EventPublisher : IEventPublisher
    {
        private readonly ConcurrentDictionary<Type, object> _subjects = new ConcurrentDictionary<Type, object>();

        #region Implementation of IEventPublisher

        /// <summary>
        /// Publishes the specified event.
        /// </summary>
        /// <typeparam name="TEvent">The type of the event.</typeparam>
        /// <param name="theEvent">The event to publish.</param>
        public void Publish<TEvent>(TEvent theEvent)
        {
            object subject;
            if (_subjects.TryGetValue(typeof(TEvent), out subject))
            {
                ((ISubject<TEvent>)subject).OnNext(theEvent);
            }
        }

        /// <summary>
        /// Gets the event as <see cref="IObservable&lt;TEvent&gt;"/>.
        /// </summary>
        /// <typeparam name="TEvent">The type of the event.</typeparam>
        /// <returns>
        /// An <see cref="IObservable&lt;TEvent&gt;"/>.
        /// </returns>
        public IObservable<TEvent> GetEvent<TEvent>()
        {
            var subject = (ISubject<TEvent>)_subjects.GetOrAdd(typeof(TEvent), t => new Subject<TEvent>());
            return subject.AsObservable();
        }

        #endregion
    }
}
