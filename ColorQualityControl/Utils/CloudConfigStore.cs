using ThreeNH.ColorQualityControl.Models;

namespace ThreeNH.ColorQualityControl.Utils
{
    public static class CloudConfigStore
    {
        public static CloudConfig Load()
        {
            PathHelper.CheckDirectoryAndFile(PathHelper.CloudConfigFilePath);
            var config = JsonHelper.Load<CloudConfig>(PathHelper.CloudConfigFilePath);
            return config ?? new CloudConfig();
        }

        public static void Save(CloudConfig config)
        {
            PathHelper.CheckDirectoryAndFile(PathHelper.CloudConfigFilePath);
            JsonHelper.Save(config ?? new CloudConfig(), PathHelper.CloudConfigFilePath);
        }
    }
}
