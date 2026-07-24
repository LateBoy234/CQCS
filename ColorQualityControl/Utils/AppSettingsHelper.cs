using System;
using System.Configuration;
using System.IO;
using System.Xml;

namespace ColorQualityControl.Utils
{
    /// <summary>
    /// AppSettings配置节读写类
    /// </summary>
    public class AppSettingsHelper
    {
        #region dir method

        /// <summary>
        /// 获取AppSettings指定键对应的值
        /// </summary>
        /// <param name="key"></param>
        public static string Get(string key)
        {
            return ConfigurationManager.AppSettings[key] ?? string.Empty;
        }

        /// <summary>
        /// 设置AppSettings指定键对应的值
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static void Set(string key, string value)
        {
            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            config.AppSettings.Settings.Remove(key);
            config.AppSettings.Settings.Add(key, value);
            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
        }

        /// <summary>
        ///  取Config文件AppSettings中的键值
        /// </summary>
        /// <param name="path"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static string Get(string path, string key)
        {
            var value = string.Empty;
            try
            {
                var xDoc = new XmlDocument();
                if (!File.Exists(path))
                {
                    Console.WriteLine("文件不存在!");
                    return string.Empty;
                }
                xDoc.Load(path);
                var xNode = xDoc.SelectSingleNode("//appSettings");
                if (xNode != null)
                {
                    var xElem = (XmlElement)xNode.SelectSingleNode("//add[@key='" + key + "']");
                    //0为key的值,1为value的值
                    if (xElem != null)
                    {
                        value = xElem.Attributes[1].Value;
                    }
                }
            }
            catch
            {
                //Console.WriteLine(ex.Message);
            }
            return value;
        }

        /// <summary>
        /// 写AppSettings
        /// </summary>
        /// <param name="path"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static bool Set(string path, string key, string value)
        {
            try
            {
                var xDoc = new XmlDocument();
                xDoc.Load(path);
                var xNode = xDoc.SelectSingleNode("//appSettings");
                if (xNode != null)
                {
                    var xElem = (XmlElement)xNode.SelectSingleNode("//add[@key='" + key + "']");
                    if (xElem != null)
                    {
                        xElem.SetAttribute("value", value);
                    }
                    else
                    {
                        xElem = xDoc.CreateElement("add");
                        xElem.SetAttribute("key", key);
                        xElem.SetAttribute("value", value);
                        xNode.AppendChild(xElem);
                    }
                    xDoc.Save(path);
                    return true;
                }

            }
            catch
            {
            }
            return false;
        }

        #endregion
    }
}