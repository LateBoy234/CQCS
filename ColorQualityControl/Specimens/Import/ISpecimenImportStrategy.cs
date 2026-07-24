namespace ThreeNH.ColorQualityControl.Specimens.Import
{
    /// <summary>
    /// 阶段 3-T04：按来源解析导入数据，写库仍由 MainWindow.ImportStandards/ImportTests 负责。
    /// </summary>
    internal interface ISpecimenImportStrategy
    {
        SpecimenImportSourceKind Kind { get; }

        /// <summary>是否可处理该请求（文件扩展名或预解析内容）。</summary>
        bool CanHandle(SpecimenImportRequest request);

        /// <summary>解析为统一结果；失败抛异常由调用方提示。</summary>
        SpecimenImportResult Load(SpecimenImportRequest request);
    }
}
