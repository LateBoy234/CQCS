using ThreeNH.DataModel;
using ThreeNH.Optical;

namespace ThreeNH.ColorQualityControl.Services
{
    /// <summary>Job 文件加载结果，供 MainWindow 刷新 UI（阶段1-T19）。</summary>
    internal sealed class JobDocumentLoadResult
    {
        public bool Success { get; set; }

        public string ErrorMessage { get; set; }

        public string FilePath { get; set; }

        public SpecularComponent SpecularComponent { get; set; }

        public ObserverAndIlluminantSetting ObserverAndIlluminantSetting { get; set; }

        public JobViewSettings ViewSettings { get; set; }
    }
}
