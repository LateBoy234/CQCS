using System;
using System.Collections.Generic;
using System.Windows.Media;
using System.Xml;

namespace ThreeNH.ColorQualityControl
{
    public class JudgementSetting
    {
        private Dictionary<JudgementLevel, SolidColorBrush> _levelColors = 
            new Dictionary<JudgementLevel, SolidColorBrush>();
        private double? _warningLevel = null;

        public JudgementSetting()
        {
            _levelColors[JudgementLevel.Invalid] = Brushes.Black;
            _levelColors[JudgementLevel.NotGood] = Brushes.Red;
            _levelColors[JudgementLevel.Pass] = Brushes.Green;
            _levelColors[JudgementLevel.Warning] = Brushes.Orange;
        }
        
        public virtual XmlElement ToXmlElement(XmlDocument doc, string nodeName)
        {
            XmlElement elem = doc.CreateElement(nodeName);
            foreach(var item in Colors)
            {
                elem.SetAttribute(item.Key.ToString(), item.Value.ToString());
            }
            elem.SetAttribute("WarningLevel", _warningLevel.ToString());
            return elem;
        }


        public virtual bool LoadXmlElement(XmlElement node)
        {
            foreach(XmlAttribute attr in node.Attributes)
            {
                if (attr.Name == "WarningLevel")
                {
                    double value;
                    if (double.TryParse(attr.Value, out value))
                    {
                        _warningLevel = value;
                    }
                    continue;
                }

                JudgementLevel result;
                if (Enum.TryParse(attr.Name, out result))
                {
                    object clr = ColorConverter.ConvertFromString(attr.Value);
                    if (clr is Color)
                    {
                        _levelColors[result] = new SolidColorBrush((Color)clr);
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// 各个等级的顔色
        /// </summary>
        public Dictionary<JudgementLevel, SolidColorBrush> Colors
        {
            get { return _levelColors; }
        }

        /// <summary>
        /// 警告的百分比，不能超过一；如果不显示警戒，设为null
        /// </summary>
        public double? WarningLevel
        {
            get { return _warningLevel; }
            set
            {
                if (value != null)
                {
                    double d = (double)value;
                    if (d <= 0.0 || d >= 1.0)
                        throw new ArgumentOutOfRangeException();
                }
                _warningLevel = value;
            }
        }

    }
}
