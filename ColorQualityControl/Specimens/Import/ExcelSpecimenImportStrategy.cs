using System.Linq;

namespace ThreeNH.ColorQualityControl.Specimens.Import
{
    /// <summary>
    /// Excel 导入：标样已由 ImportStandardsDialog 解析，此处只打包默认选项。
    /// </summary>
    internal sealed class ExcelSpecimenImportStrategy : ISpecimenImportStrategy
    {
        public SpecimenImportSourceKind Kind => SpecimenImportSourceKind.ExcelWorkbook;

        public bool CanHandle(SpecimenImportRequest request)
        {
            return request?.PreParsedStandards != null && request.PreParsedStandards.Any();
        }

        public SpecimenImportResult Load(SpecimenImportRequest request)
        {
            // Excel 菜单导入不删同名，避免覆盖用户已有 Job 数据
            return new SpecimenImportResult(
                Kind,
                request.PreParsedStandards,
                request.PreParsedTests,
                createNewId: request.CreateNewId ?? true,
                removeSame: request.RemoveSame ?? false);
        }
    }
}
