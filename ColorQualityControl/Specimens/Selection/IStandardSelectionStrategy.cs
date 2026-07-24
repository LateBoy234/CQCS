using ThreeNH.ColorSpecimen;
using ThreeNH.DataModel;

namespace ThreeNH.ColorQualityControl.Specimens.Selection
{
    /// <summary>
    /// 阶段 3-T03：测样/匹配时如何选定父标样（手动当前 / 最近色 / 自动）。
    /// </summary>
    internal interface IStandardSelectionStrategy
    {
        /// <summary>策略标识，便于日志与单测。</summary>
        string Name { get; }

        /// <summary>为试样选出父标样；无合适标样时返回 null（孤儿试样）。</summary>
        StandardSpecimen Select(StandardSelectionContext context);
    }
}
