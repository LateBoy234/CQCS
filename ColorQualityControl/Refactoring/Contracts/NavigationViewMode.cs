namespace ThreeNH.ColorQualityControl.Refactoring.Contracts
{
    /// <summary>导航树当前展示模式（阶段1-T12，替代 displayedCategory 的类型判断）。</summary>
    internal enum NavigationViewMode
    {
        None,
        AllStandards,
        AllTests,
        OrphanTests,
        SingleStandard,
        CurrentStandard
    }
}
