using System;
using System.IO;
using ThreeNH.DataModel;

namespace ThreeNH.ColorQualityControl.Specimens.Import
{
    /// <summary>从 Datacolor QTX 加载标试样。</summary>
    internal sealed class QtxJobFileImportStrategy : ISpecimenImportStrategy
    {
        public SpecimenImportSourceKind Kind => SpecimenImportSourceKind.QtxJobFile;

        public bool CanHandle(SpecimenImportRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.FilePath))
            {
                return false;
            }

            return string.Equals(
                Path.GetExtension(request.FilePath),
                ".qtx",
                StringComparison.OrdinalIgnoreCase);
        }

        public SpecimenImportResult Load(SpecimenImportRequest request)
        {
            var database = new SpecimenDatabase();
            QTXHelper.Load(request.FilePath, database);
            return new SpecimenImportResult(
                Kind,
                database.StandardSpecimens,
                database.TestSpecimens,
                createNewId: request.CreateNewId ?? false,
                removeSame: request.RemoveSame ?? false);
        }
    }
}
