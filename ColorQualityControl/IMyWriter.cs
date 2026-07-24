using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ThreeNH.ColorQualityControl
{
    interface IMyWriter
    {
        void Create(string fileName);
        void WriteRow(string[] items);
        void Close(); 
    }
}
