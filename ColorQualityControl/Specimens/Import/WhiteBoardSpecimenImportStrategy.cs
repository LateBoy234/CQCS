using System.Linq;

namespace ThreeNH.ColorQualityControl.Specimens.Import
{
    /// <summary>
    /// 白板 Excel 转换结果导入；对话框确认后若产出标样则走此策略打包。
    /// </summary>
    internal sealed class WhiteBoardSpecimenImportStrategy : ISpecimenImportStrategy
    {
        public SpecimenImportSourceKind Kind => SpecimenImportSourceKind.WhiteBoardExcel;

        public bool CanHandle(SpecimenImportRequest request)
        {
            return request?.PreParsedStandards != null && request.PreParsedStandards.Any();
        }

        public SpecimenImportResult Load(SpecimenImportRequest request)
        {
            return new SpecimenImportResult(
                Kind,
                request.PreParsedStandards,
                request.PreParsedTests,
                createNewId: request.CreateNewId ?? true,
                removeSame: request.RemoveSame ?? false);
        }
    }
}
