using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreeNH.ColorAlgorithm.IData
{
    public interface IColorSetting
    {
        string Channel { get; }

        StandardIlluminant Illuminant { get; }

        StandardObserver Observer { get; }
    }
}
