using ThreeNH.ColorSpecimen;
using ThreeNH.DataModel;

namespace ThreeNH.ColorQualityControl.Refactoring.Contracts
{
    /// <summary>
    /// 阶段 1：当前选中状态由 <see cref="Services.SpecimenSelectionState"/> 持有；Showing 模式仍留 Navigation（T12）。
    /// </summary>
    internal interface ISpecimenSelectionState
    {
        StandardSpecimen CurrentStandard { get; set; }

        TestSpecimen CurrentTest { get; set; }

        StandardSpecimen SingleStandardInShowing { get; }
    }
}
