using System.Collections.Generic;
using System.Linq;
using ThreeNH.DataModel;

namespace ThreeNH.ColorQualityControl.Specimens.Import
{
    /// <summary>
    /// 一次导入解析结果：标样（可含子试样）与需按 Tag 匹配挂靠的试样。
    /// </summary>
    internal sealed class SpecimenImportResult
    {
        public SpecimenImportResult(
            SpecimenImportSourceKind sourceKind,
            IEnumerable<StandardSpecimen> standards = null,
            IEnumerable<TestSpecimen> tests = null,
            bool createNewId = true,
            bool removeSame = true)
        {
            SourceKind = sourceKind;
            Standards = (standards ?? Enumerable.Empty<StandardSpecimen>()).ToList();
            Tests = (tests ?? Enumerable.Empty<TestSpecimen>()).ToList();
            CreateNewId = createNewId;
            RemoveSame = removeSame;
        }

        public SpecimenImportSourceKind SourceKind { get; }

        public IReadOnlyList<StandardSpecimen> Standards { get; }

        /// <summary>Job 文件中的试样（含已挂标样的）；由 Apply 决定走 ImportStandards 还是 ImportTests。</summary>
        public IReadOnlyList<TestSpecimen> Tests { get; }

        public bool CreateNewId { get; }

        public bool RemoveSame { get; }

        public bool IsEmpty => Standards.Count == 0 && Tests.Count == 0;
    }
}
