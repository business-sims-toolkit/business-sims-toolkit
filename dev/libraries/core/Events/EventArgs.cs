using System;

namespace Events
{
    public class EventArgs<T> : EventArgs
    {
        public T Parameter { get; set; }

        public EventArgs (T parameter)
        {
            Parameter = parameter;
        }
    }

    public class ReadonlyEventArgs<T> : EventArgs
    {
        public T Parameter { get; }

        public ReadonlyEventArgs(T parameter)
        {
            Parameter = parameter;
        }
    }
}
