using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreeNH.ColorAlgorithm.IData
{
    public interface IColorFormulaFactors
    {
    }

    public interface ICIEDE1994Factors : IColorFormulaFactors
    {
        double L { get; }
        double C { get; }
        double H { get; }
    }

    public interface ICIEDE2000Factors : IColorFormulaFactors
    {
        double L { get; }
        double C { get; }
        double H { get; }
    }

    public interface ICMCFactors : IColorFormulaFactors
    {
        double L { get; }
        double C { get; }
    }
}
