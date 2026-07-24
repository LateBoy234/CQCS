using System.Linq;

namespace ThreeNH.ColorQualityControl.Specimens.Import
{
    /// <summary>
    /// 仪器库导入：标样/试样已在 InstrumentDataManagementDialog 中组装。
    /// </summary>
    internal sealed class InstrumentLibraryImportStrategy : ISpecimenImportStrategy
    {
        public SpecimenImportSourceKind Kind => SpecimenImportSourceKind.InstrumentLibrary;

        public bool CanHandle(SpecimenImportRequest request)
        {
            return (request?.PreParsedStandards != null && request.PreParsedStandards.Any())
                || (request?.PreParsedTests != null && request.PreParsedTests.Any());
        }

        public SpecimenImportResult Load(SpecimenImportRequest request)
        {
            // 仪器库导入与 Excel 相同：合并进 Job，不按同名删除
            return new SpecimenImportResult(
                Kind,
                request.PreParsedStandards,
                request.PreParsedTests,
                createNewId: request.CreateNewId ?? true,
                removeSame: request.RemoveSame ?? false);
        }
    }
}
