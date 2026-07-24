using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ThreeNH.ColorAlgorithm.IData
{
    public interface ISpectralData
    {
        int Count { get; }

        int WavelengthEnd { get; }

        int WavelengthInterval { get; }

        int WavelengthBegin { get; }

        float this[int wavelength] { get; }

        [return: MarshalAs(UnmanagedType.U1)]
        bool Contains(int wavelength);
    }
}
