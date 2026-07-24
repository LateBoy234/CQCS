using System;
using ThreeNH.ColorSpecimen;
using ThreeNH.DataModel;

namespace ThreeNH.ColorQualityControl.Specimens.Events
{
    /// <summary>当前选中标样/试样变更（阶段2-T03，供 Presenter 订阅）。</summary>
    internal sealed class SelectionChangedEventArgs : EventArgs
    {
        public SelectionChangedEventArgs(StandardSpecimen currentStandard, TestSpecimen currentTest)
        {
            CurrentStandard = currentStandard;
            CurrentTest = currentTest;
        }

        public StandardSpecimen CurrentStandard { get; }

        public TestSpecimen CurrentTest { get; }
    }
}
