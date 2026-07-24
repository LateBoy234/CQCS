using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreeNH.ColorAlgorithm.IData
{
    public interface IDensitySetting
    {
        string Channel { get; }
        DensityStatus DensityStatus { get; }
    }
}
