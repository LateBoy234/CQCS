namespace ThreeNH.ColorQualityControl.Specimens.Selection
{
    /// <summary>
    /// 按「试样测量时自动选择标样」开关解析策略。
    /// </summary>
    internal static class StandardSelectionStrategyResolver
    {
        internal static IStandardSelectionStrategy ResolveForMeasure(bool isAutoSelectStandard)
        {
            return isAutoSelectStandard
                ? (IStandardSelectionStrategy)AutoStandardSelectionStrategy.Instance
                : ManualStandardSelectionStrategy.Instance;
        }

        /// <summary>编辑菜单「自动匹配标样」等：始终用最近色，不受开关影响。</summary>
        internal static IStandardSelectionStrategy ResolveNearestOnly()
            => NearestColorStandardSelectionStrategy.Instance;
    }
}
