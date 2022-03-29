using System;

namespace Events
{
    public static class EventHandlerExtensions
    {
        public static EventArgs<T> CreateArgs<T>(this EventHandler<EventArgs<T>> _, T argument)
        {
            return new EventArgs<T>(argument);
        }

        public static ReadonlyEventArgs<T> CreateReadonlyArgs<T> (this EventHandler<ReadonlyEventArgs<T>> _, T argument)
        {
            return new ReadonlyEventArgs<T>(argument);
        }
    }
}
