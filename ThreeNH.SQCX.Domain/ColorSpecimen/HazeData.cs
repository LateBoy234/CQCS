using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml;
using ThreeNH.Optical;

namespace ThreeNH.ColorSpecimen
{
    public class HazeData
    {
        /// <summary>
        /// 参考光源
        /// </summary>
        public StandardIlluminant Illuminant { get; set; }
        /// <summary>
        /// 参考观察者角度
        /// </summary>
        public StandardObserver Observer { get; set; }
        /// <summary>
        /// 参考标准
        /// </summary>
        public String ReferenceStandard { get; set; }
        /// <summary>
        /// 雾度值
        /// </summary>
        public double Haze { get; set; }
        /// <summary>
        /// 透过率
        /// </summary>
        public double Transmittance { get; set; }

        public XmlElement ToXmlElement(XmlDocument doc, string nodeName = null)
        {
            if (nodeName == null)
            {
                nodeName = String.Format("Haze-{0}-{1}", Illuminant, Observer);
            }

            var node = doc.CreateElement(nodeName);
            node.SetAttribute("Illuminant", Illuminant.ToString());
            node.SetAttribute("Observer", Observer.ToString());
            node.SetAttribute("ReferenceStandard", ReferenceStandard);
            node.SetAttribute("Haze", Haze.ToString(CultureInfo.InvariantCulture));
            node.SetAttribute("Transmittance", Transmittance.ToString(CultureInfo.InvariantCulture));
            return node;
        }

        public bool LoadXmlElement(XmlElement node)
        {
            var culture = CultureInfo.InvariantCulture;
            var style = NumberStyles.Float | NumberStyles.AllowThousands;

            try
            {
                if (node.Attributes["Illuminant"] != null)
                {
                    Illuminant = (StandardIlluminant) Enum.Parse(
                        typeof(StandardIlluminant), node.Attributes["Illuminant"].Value);
                }

                if (node.Attributes["Observer"] != null)
                {
                    Observer = (StandardObserver) Enum.Parse(
                        typeof(StandardObserver), node.Attributes["Observer"].Value);
                }

                if (node.Attributes["ReferenceStandard"] != null)
                {
                    ReferenceStandard = node.Attributes["ReferenceStandard"].Value;
                }

                if (node.Attributes["Haze"] != null) 
                {
                    Haze = double.Parse(node.Attributes["Haze"].Value, style, culture);
                }

                if (node.Attributes["Transmittance"] != null)
                {
                    Transmittance = double.Parse(node.Attributes["Transmittance"].Value, style, culture);
                }

                return true;
            }
            catch(Exception)
            {
                return false;
            }
        }
    }
}
