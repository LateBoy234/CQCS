using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ThreeNH.Controls
{
    public class BackgroundWorkerEventArgs:EventArgs
    {
        public object Result { get; set; }

        public bool IsCancelled { get; set; }

    }
}
