using System.Windows.Controls;
using ThreeNH.ColorAlgorithm;
using ThreeNH.ColorQualityControl.UI;
using ThreeNH.Controls;
using ThreeNH.DataModel;
using ThreeNH.Optical;

namespace ThreeNH.ColorQualityControl.Refactoring.Contracts
{
    /// <summary>
    /// SpecimenViewCoordinator 访问 TreeView 与显色上下文，用于 adapter 批量刷新（阶段2-T07）。
    /// </summary>
    internal interface ISpecimenViewHost
    {
        TreeViewItem StandardGroupRoot { get; }

        TreeViewItem AllTestsRoot { get; }

        TreeViewItem OrphanTestsRoot { get; }

        SpecimenDatabase Database { get; }

        Formula Formula { get; }

        StandardObserver Observer { get; }

        StandardIlluminant Illuminant { get; }

        SpecularComponent SpecularComponent { get; }

        StandardSpecimen SingleStandardInShowing { get; }

        StandardSpecimen CurrentStandard { get; }

        TestSpecimen CurrentTest { get; }

        ChromaticitySetting ChromaticitySetting { get; }

        bool IsShowSingleStandard { get; }

        StatisticsPlotControl StatisticsPlot { get; }

        ColorDifferenceControl ColorDiffTable { get; }

        DataGrid TestDataGrid { get; }

        SpectrumPlotControl SpectrumPlot { get; }

        AbsorbancePlotControl AbsorbancePlot { get; }
    }
}
