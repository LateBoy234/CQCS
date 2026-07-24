using ThreeNH.ColorSpecimen;
using ThreeNH.DataModel;

namespace ThreeNH.ColorQualityControl.Specimens.Selection
{
    /// <summary>
    /// 自动选标样：先最近色，越限或找不到则回退到手动当前标样。
    /// </summary>
    internal sealed class AutoStandardSelectionStrategy : IStandardSelectionStrategy
    {
        public static AutoStandardSelectionStrategy Instance { get; } = new AutoStandardSelectionStrategy();

        public string Name => "Auto";

        public StandardSpecimen Select(StandardSelectionContext context)
        {
            // 最近色失败时保留手动选中，避免测样变成孤儿
            return NearestColorStandardSelectionStrategy.Instance.Select(context)
                ?? ManualStandardSelectionStrategy.Instance.Select(context);
        }
    }
}
