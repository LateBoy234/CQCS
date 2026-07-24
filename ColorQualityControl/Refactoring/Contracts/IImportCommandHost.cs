using System.Windows;
using ThreeNH.ColorQualityControl.Specimens.Import;

namespace ThreeNH.ColorQualityControl.Refactoring.Contracts
{
    /// <summary>
    /// Import Command 对话框 Owner 与入库刷新（阶段4-T01；解析已在 Import Strategy）。
    /// </summary>
    internal interface IImportCommandHost
    {
        Window DialogOwner { get; }

        void ApplySpecimenImportResult(SpecimenImportResult result);
    }
}
