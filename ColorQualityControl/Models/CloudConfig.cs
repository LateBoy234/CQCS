namespace ThreeNH.ColorQualityControl.Models
{
    /// <summary>客户 Web API 接口配置。</summary>
    public class CloudConfig
    {
        public bool Enabled { get; set; }

        /// <summary>例如 https://api.example.com</summary>
        public string ApiBaseUrl { get; set; } = string.Empty;

        /// <summary>相对上传路径，默认 /api/measure/upload</summary>
        public string UploadPath { get; set; } = "/api/measure/upload";

        public string ApiToken { get; set; } = string.Empty;

        public int TimeoutSeconds { get; set; } = 30;

        public string UploadUrl =>
            string.IsNullOrWhiteSpace(ApiBaseUrl)
                ? string.Empty
                : ApiBaseUrl.TrimEnd('/') + (UploadPath.StartsWith("/") ? UploadPath : "/" + UploadPath);
    }
}
