using System.Collections.Generic;
using ThreeNH.DataModel;

namespace ThreeNH.ColorQualityControl.Specimens.Import
{
    /// <summary>
    /// 导入请求：文件路径与预解析列表二选一（Excel/仪器库/白板对话框产出标样后走预解析）。
    /// </summary>
    internal sealed class SpecimenImportRequest
    {
        public string FilePath { get; set; }

        public IEnumerable<StandardSpecimen> PreParsedStandards { get; set; }

        public IEnumerable<TestSpecimen> PreParsedTests { get; set; }

        public bool? CreateNewId { get; set; }

        public bool? RemoveSame { get; set; }
    }
}
