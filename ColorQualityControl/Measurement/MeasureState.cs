namespace ThreeNH.ColorQualityControl.Measurement
{
    /// <summary>
    /// 测量生命周期状态（阶段 3-T06），替代分散的 UpdateMeasureUi(true/false) 布尔判断。
    /// </summary>
    internal enum MeasureState
    {
        /// <summary>空闲，菜单/工具栏可用。</summary>
        Idle = 0,

        /// <summary>测量进行中（含命名对话框），菜单/工具栏禁用。</summary>
        Measuring = 1,
    }
}
