using System;

namespace ThreeNH.Controls
{
    public class ValueChangedEventArgs<T> : EventArgs
    {
        public ValueChangedEventArgs() { }
        public ValueChangedEventArgs(T newValue, T oldValue)
        {
            this.NewValue = newValue;
            this.OldValue = oldValue;
        }

        public T NewValue { get; set; }
        public T OldValue { get; set; }
    }
}
