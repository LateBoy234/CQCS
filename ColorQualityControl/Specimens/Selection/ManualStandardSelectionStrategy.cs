using ThreeNH.ColorSpecimen;
using ThreeNH.DataModel;

namespace ThreeNH.ColorQualityControl.Specimens.Selection
{
    /// <summary>
    /// 手动选标样：优先单标样视图正在显示的标样，否则用 CurrentStandard。
    /// </summary>
    internal sealed class ManualStandardSelectionStrategy : IStandardSelectionStrategy
    {
        public static ManualStandardSelectionStrategy Instance { get; } = new ManualStandardSelectionStrategy();

        public string Name => "Manual";

        public StandardSpecimen Select(StandardSelectionContext context)
        {
            if (context?.Host == null)
            {
                return null;
            }

            // 单标样视图下用户正在看的标样优先，避免树选中未同步到 CurrentStandard 时挂错父标样
            return context.SingleStandardInShowing ?? context.CurrentStandard;
        }
    }
}
