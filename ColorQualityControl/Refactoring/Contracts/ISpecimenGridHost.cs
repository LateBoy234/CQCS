using System.Windows.Controls;
using ThreeNH.ColorQualityControl.UI;
using ThreeNH.Controls;
using ThreeNH.DataModel;
using ThreeNH.Optical;

namespace ThreeNH.ColorQualityControl.Refactoring.Contracts
{
    /// <summary>
    /// SpecimenGridPresenter 访问标试样 DataGrid 与光谱/统计图（阶段2-T07）。
    /// </summary>
    internal interface ISpecimenGridHost
    {
        DataGrid StandardDataGrid { get; }

        DataGrid TestDataGrid { get; }

        SpectrumPlotControl SpectrumPlot { get; }

        AbsorbancePlotControl AbsorbancePlot { get; }

        StatisticsPlotControl StatisticsPlot { get; }

        bool IsShowAllStandards { get; }

        bool IsShowSingleStandard { get; }

        bool IsShowAllTests { get; }

        bool IsShowOrphanTests { get; }

        StandardSpecimen SingleStandardInShowing { get; }

        TestSpecimen CurrentTest { get; }

        SpecularComponent SpecularComponent { get; set; }

        bool IsShowStandard(StandardSpecimen standard);

        bool IsShowTest(TestSpecimen test);

        string SpectrumType { get; }

        SpecimenWrap CreateStandardWrap(StandardSpecimen standard);

        SpecimenWrap CreateTestWrap(TestSpecimen test);

        void NotifySingleStandardDiagramChanged();
    }
}