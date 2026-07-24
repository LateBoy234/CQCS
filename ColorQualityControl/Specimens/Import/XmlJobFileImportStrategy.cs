using System;
using System.IO;
using ThreeNH.DataModel;

namespace ThreeNH.ColorQualityControl.Specimens.Import
{
    /// <summary>从 SQCX Job XML 加载标试样。</summary>
    internal sealed class XmlJobFileImportStrategy : ISpecimenImportStrategy
    {
        public SpecimenImportSourceKind Kind => SpecimenImportSourceKind.XmlJobFile;

        public bool CanHandle(SpecimenImportRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.FilePath))
            {
                return false;
            }

            return string.Equals(
                Path.GetExtension(request.FilePath),
                ".xml",
                StringComparison.OrdinalIgnoreCase);
        }

        public SpecimenImportResult Load(SpecimenImportRequest request)
        {
            var database = new SpecimenDatabase();
            database.Load(request.FilePath);
            // Job 片段合并进当前 Job：不强制改 ID，也不按同名删除（与原 ImportFromFile 一致）
            return new SpecimenImportResult(
                Kind,
                database.StandardSpecimens,
                database.TestSpecimens,
                createNewId: request.CreateNewId ?? false,
                removeSame: request.RemoveSame ?? false);
        }
    }
}
