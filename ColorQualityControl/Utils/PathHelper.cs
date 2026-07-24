using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using MahApps.Metro.Controls;

namespace ThreeNH.ColorQualityControl.Utils
{
    public  class PathHelper
    {
		
		public static string DefaultJsonFilePath=>
             Path.Combine(Directory.GetCurrentDirectory(), @"Data\Lab.json");
        // Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), @"SQCX\Lab.json");
        public static string CloudConfigFilePath => Path.Combine(Directory.GetCurrentDirectory(),@"Cloud\CloudConfig.json");
        public static string BthAutoConnectFilePath=> Path.Combine(Directory.GetCurrentDirectory(), @"appSetting\BluetoothConfig.json");
        public static string UrlSettingPath => Path.Combine(Directory.GetCurrentDirectory(), @"appSetting\UrlSetting.xml");

        public static bool CheckDirectoryAndFile(string path)
        {
            try
            {
                var directory = Path.GetDirectoryName(path);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                if (!File.Exists(path))
                {
                    //注意这里不能直接用FileCreate创建文件，如果这样第一次创建文件后，再写入内容时会出现当前文件正在使用，资源没有释放
                    FileStream fs = File.Create(path);
                    fs.Close();
                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
    }
}
