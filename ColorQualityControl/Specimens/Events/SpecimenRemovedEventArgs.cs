using System;
using ThreeNH.ColorSpecimen;
using ThreeNH.DataModel;

namespace ThreeNH.ColorQualityControl.Specimens.Events
{
    /// <summary>标样或试样从 Job 数据库移除后发布（阶段2-T03）。</summary>
    internal sealed class SpecimenRemovedEventArgs : EventArgs
    {
        public SpecimenRemovedEventArgs(StandardSpecimen standard, TestSpecimen test = null)
        {
            if (standard == null && test == null)
            {
                throw new ArgumentException("standard or test must be provided");
            }

            if (test != null)
            {
                Kind = SpecimenKind.Test;
                Test = test;
                Standard = standard ?? test.StandardSpecimen;
            }
            else
            {
                Kind = SpecimenKind.Standard;
                Standard = standard;
            }
        }

        public SpecimenKind Kind { get; }

        public StandardSpecimen Standard { get; }

        public TestSpecimen Test { get; }
    }
}
