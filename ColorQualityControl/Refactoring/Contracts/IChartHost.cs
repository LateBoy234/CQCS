using System.Collections.Generic;
using System.Windows.Controls;
using ThreeNH.ColorAlgorithm;
using ThreeNH.ColorQualityControl.Adapter;
using ThreeNH.ColorQualityControl.ViewModels;
using ThreeNH.ColorSpecimen;
using ThreeNH.Controls;
using ThreeNH.DataModel;
using ThreeNH.Optical;

namespace ThreeNH.ColorQualityControl.Refactoring.Contracts
{
    /// <summary>
    /// ChartCoordinator 访问色品图/容差图/光谱图控件与显色数据（阶段2-T06）。
    /// </summary>
    internal interface IChartHost
    {
        ContentControl ToleranceChartView { get; }

        ContentControl TriaxialChartView { get; }

        SpectrumPlotControl SpectrumPlot { get; }

        AbsorbancePlotControl AbsorbancePlot { get; }

        SpecimenDatabase Database { get; }

        StandardObserver Observer { get; }

        StandardIlluminant Illuminant { get; }

        SpecularComponent SpecularComponent { get; }

        StandardSpecimen SingleStandardInShowing { get; }

        ChromaticitySetting ChromaticitySetting { get; }

        IColorData StandardColorData { get; }

        IList<IColorData> SampleColorDatas { get; }

        IEnumerable<SpecimenWrap> TestsToShow { get; }

        IEnumerable<SpecimenWrap> StandardsToShow { get; }
    }
}
