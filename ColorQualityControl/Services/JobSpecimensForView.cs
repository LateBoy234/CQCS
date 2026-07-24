using System.Collections.Generic;
using ThreeNH.ColorQualityControl.Adapter;

namespace ThreeNH.ColorQualityControl.Services
{
    /// <summary>标样及其下属试样的视图 adapter 集合（阶段1-T17）。</summary>
    internal sealed class JobStandardForView
    {
        public StandardSpecimenAdapter Standard { get; set; }

        public IReadOnlyList<TestSpecimenAdapter> Tests { get; set; }
    }

    /// <summary>重建 TreeView 所需的 Job 标试样快照（阶段1-T17）。</summary>
    internal sealed class JobSpecimensForView
    {
        public IReadOnlyList<JobStandardForView> Standards { get; set; }

        public IReadOnlyList<TestSpecimenAdapter> OrphanTests { get; set; }
    }
}
