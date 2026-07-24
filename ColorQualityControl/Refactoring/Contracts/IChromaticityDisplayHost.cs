using System.Windows.Controls;
using ThreeNH.ColorAlgorithm;
using ThreeNH.ColorSpecimen;
using ThreeNH.DataModel;
using ThreeNH.Optical;

namespace ThreeNH.ColorQualityControl.Refactoring.Contracts
{
    /// <summary>
    /// ChromaticityDisplayService 访问标试样 DataGrid 与显色上下文（阶段2-T05）。
    /// </summary>
    internal interface IChromaticityDisplayHost
    {
        DataGrid TestDataGrid { get; }

        DataGrid StandardDataGrid { get; }

        DataGrid ToleranceDataGrid { get; }

        SpecimenDatabase Database { get; }

        Formula Formula { get; }

        StandardObserver Observer { get; }

        StandardIlluminant Illuminant { get; }

        ObserverIlluminantPair ObserverIlluminant { get; }

        StandardSpecimen CurrentStandard { get; }

        TestSpecimen CurrentTest { get; }

        StandardSpecimen SingleStandardInShowing { get; }
    }
}
