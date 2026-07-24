using System;
using System.IO;

namespace ThreeNH.ColorQualityControl.Specimens.Import
{
    /// <summary>
    /// 按文件扩展名或显式来源解析导入策略。
    /// </summary>
    internal static class SpecimenImportStrategyRegistry
    {
        private static readonly ISpecimenImportStrategy[] FileStrategies =
        {
            new XmlJobFileImportStrategy(),
            new QtxJobFileImportStrategy(),
        };

        internal static ISpecimenImportStrategy ResolveForFile(string filePath)
        {
            var request = new SpecimenImportRequest { FilePath = filePath };
            foreach (var strategy in FileStrategies)
            {
                if (strategy.CanHandle(request))
                {
                    return strategy;
                }
            }

            return null;
        }

        internal static ISpecimenImportStrategy Resolve(SpecimenImportSourceKind kind)
        {
            switch (kind)
            {
                case SpecimenImportSourceKind.XmlJobFile:
                    return new XmlJobFileImportStrategy();
                case SpecimenImportSourceKind.QtxJobFile:
                    return new QtxJobFileImportStrategy();
                case SpecimenImportSourceKind.ExcelWorkbook:
                    return new ExcelSpecimenImportStrategy();
                case SpecimenImportSourceKind.InstrumentLibrary:
                    return new InstrumentLibraryImportStrategy();
                case SpecimenImportSourceKind.WhiteBoardExcel:
                    return new WhiteBoardSpecimenImportStrategy();
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
            }
        }

        internal static bool IsSupportedFileExtension(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return false;
            }

            var ext = Path.GetExtension(filePath);
            return string.Equals(ext, ".xml", StringComparison.OrdinalIgnoreCase)
                || string.Equals(ext, ".qtx", StringComparison.OrdinalIgnoreCase);
        }
    }
}
