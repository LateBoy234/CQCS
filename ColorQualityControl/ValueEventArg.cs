using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ThreeNH.ColorQualityControl
{
    public class ValueEventArg<T> : EventArgs
    {
        public T Value { get; set; }
    }
}
