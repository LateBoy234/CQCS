using System;
using System.Xml;
using ThreeNH.Optical;

namespace ThreeNH.ColorQualityControl.Services
{
    /// <summary>Job XML 内嵌的视图设置（阶段1-T19，原 ViewSettingToXmlElement / LoadViewSetting）。</summary>
    internal sealed class JobViewSettings
    {
        public SpecularComponent SpecularComponent { get; set; }

        public StandardIlluminant Illuminant { get; set; }

        public StandardObserver Observer { get; set; }

        public bool IsCanUseHaze { get; set; }

        public bool HasSpecularComponent { get; private set; }

        public bool HasObserverIlluminant { get; private set; }

        public bool HasIsCanUseHaze { get; private set; }

        public XmlElement ToXmlElement(XmlDocument doc)
        {
            XmlElement elem = doc.CreateElement("ViewSetting");
            elem.SetAttribute("SpecularComponent", SpecularComponent.ToString());
            elem.SetAttribute("Illuminant", Illuminant.ToString());
            elem.SetAttribute("Observer", Observer.ToString());
            elem.SetAttribute(nameof(IsCanUseHaze), IsCanUseHaze.ToString());
            return elem;
        }

        public static JobViewSettings TryFromXml(XmlElement elem)
        {
            if (elem == null)
            {
                return null;
            }

            var settings = new JobViewSettings();
            try
            {
                settings.SpecularComponent = (SpecularComponent)Enum.Parse(
                    typeof(SpecularComponent), elem.GetAttribute("SpecularComponent"));
                settings.HasSpecularComponent = true;
            }
            catch
            {
                // 与原先 LoadViewSetting 一致：解析失败则保留数据库侧已设置的值
            }

            try
            {
                settings.Illuminant = (StandardIlluminant)Enum.Parse(
                    typeof(StandardIlluminant), elem.GetAttribute("Illuminant"));
                settings.Observer = (StandardObserver)Enum.Parse(
                    typeof(StandardObserver), elem.GetAttribute("Observer"));
                settings.HasObserverIlluminant = true;
            }
            catch
            {
            }

            string strIsHazeEnable = elem.GetAttribute(nameof(IsCanUseHaze));
            if (!string.IsNullOrWhiteSpace(strIsHazeEnable))
            {
                try
                {
                    settings.IsCanUseHaze = bool.Parse(strIsHazeEnable);
                    settings.HasIsCanUseHaze = true;
                }
                catch
                {
                    settings.IsCanUseHaze = false;
                    settings.HasIsCanUseHaze = true;
                }
            }

            return settings;
        }
    }
}
