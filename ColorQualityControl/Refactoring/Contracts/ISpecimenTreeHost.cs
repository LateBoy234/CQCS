using System.Windows.Controls;
using ThreeNH.ColorSpecimen;
using ThreeNH.ColorQualityControl.Adapter;
using ThreeNH.DataModel;

namespace ThreeNH.ColorQualityControl.Refactoring.Contracts
{
    /// <summary>
    /// SpecimenTreePresenter 访问导航 TreeView 根节点与 adapter 包装（阶段2-T04）。
    /// </summary>
    internal interface ISpecimenTreeHost
    {
        TreeViewItem StandardGroupRoot { get; }

        TreeViewItem AllTestsRoot { get; }

        TreeViewItem OrphanTestsRoot { get; }

        SpecimenWrap CreateStandardWrap(StandardSpecimen standard);

        SpecimenWrap CreateTestWrap(TestSpecimen test);

        void ActivateStandard(StandardSpecimen standard);
    }
}
