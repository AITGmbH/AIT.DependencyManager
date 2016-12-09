using System;

namespace AIT.DMF.DependencyManager.Controls.Messaging
{
    /// <summary>
    /// A simple messaging interface using reactive extensions.
    /// Original idea by José F. Romaniello:
    /// http://joseoncode.com/2010/04/29/event-aggregator-with-reactive-extensions/
    /// </summary>
    public interface IEventPublisher
    {
        /// <summary>
        /// Publishes the specified event.
        /// </summary>
        /// <typeparam name="TEvent">The type of the event.</typeparam>
        /// <param name="theEvent">The event to publish.</param>
        void Publish<TEvent>(TEvent theEvent);

        /// <summary>
        /// Gets the event as <see cref="IObservable&lt;TEvent&gt;"/>.
        /// </summary>
        /// <typeparam name="TEvent">The type of the event.</typeparam>
        /// <returns>An <see cref="IObservable&lt;TEvent&gt;"/>.</returns>
        IObservable<TEvent> GetEvent<TEvent>();
    }
}
