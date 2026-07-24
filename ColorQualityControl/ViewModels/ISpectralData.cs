using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ThreeNH.ColorQualityControl.ViewModels
{
    public interface ISpectralData
    {
        double this[int wavelength] { get; }

        int Count { get; }
        int WavelengthEnd { get; }
        int WavelengthInterval { get; }
        int WavelengthBegin { get; }

        bool Contains(int wavelength);
    }
}
