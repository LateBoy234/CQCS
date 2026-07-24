using System.Collections.Generic;
using ThreeNH.Optical;
using ThreeNH.ColorAlgorithm;
using System.Xml;
using System;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;

namespace ThreeNH.ColorSpecimen
{
    [DataContract]
    public class ColorData
    {
        // 颜色值，在无反射率情况下
        [DataMember]
        private Dictionary<StandardObserver, Dictionary<StandardIlluminant, CIEXYZ>> values;

        public ColorData() { }
        public ColorData(Spectrum spectrum)
        {
            Spectrum = spectrum;
        }

        public ColorData(StandardIlluminant illuminant, StandardObserver observer,
            CIEXYZ xyz)
        {
            Set(observer, illuminant, xyz);
        }

        public ColorData(StandardIlluminant illuminant, StandardObserver observer, CIELAB lab)
        {
            var xyz = Formula.LabToXyz(lab, illuminant, observer);
            Set(observer, illuminant, xyz);
        }

        public virtual XmlElement ToXmlElement(XmlDocument doc, string nodeName)
        {
            var node = doc.CreateElement(nodeName);
            if (Spectrum != null)
            {
                node.SetAttribute("IsReflectance", true.ToString());
                var nodeReflectance = doc.CreateElement("Reflectances");
                node.AppendChild(nodeReflectance);

                nodeReflectance.SetAttribute("MinWavelength", Spectrum.Range.Min.ToString(CultureInfo.InvariantCulture));
                nodeReflectance.SetAttribute("MaxWavelength", Spectrum.Range.Max.ToString(CultureInfo.InvariantCulture));
                nodeReflectance.SetAttribute("WavelengthStep", Spectrum.Range.Step.ToString(CultureInfo.InvariantCulture));
                nodeReflectance.AppendChild(doc.CreateTextNode(
                    string.Join(";", Array.ConvertAll(Spectrum.Data, x => x.ToString(CultureInfo.InvariantCulture)))));
            }
            else
            {
                node.SetAttribute("IsReflectance", false.ToString());
                if (values != null)
                {
                    foreach (var observer in values.Keys)
                    {
                        foreach (var illuminant in values[observer].Keys)
                        {
                            var xyzNode = doc.CreateElement("XYZ");
                            node.AppendChild(xyzNode);
                            xyzNode.SetAttribute("Observer", observer.ToString());
                            xyzNode.SetAttribute("Illuminant", illuminant.ToString());
                            xyzNode.SetAttribute("X", values[observer][illuminant].X.ToString(CultureInfo.InvariantCulture));
                            xyzNode.SetAttribute("Y", values[observer][illuminant].Y.ToString(CultureInfo.InvariantCulture));
                            xyzNode.SetAttribute("Z", values[observer][illuminant].Z.ToString(CultureInfo.InvariantCulture));
                        }
                    }
                }
            }
            return node;
        }

        void Clear()
        {
            Spectrum = null;
            values = null;
        }

        public bool LoadXmlElement(XmlElement child)
        {
            Clear();
            var culture = CultureInfo.InvariantCulture;
            var style = NumberStyles.Float | NumberStyles.AllowThousands;
            foreach (XmlElement node in child.ChildNodes)
            {
                if (string.Compare(node.Name, "Reflectances", true) == 0)
                {
                    if (node.Attributes["MinWavelength"] == null ||
                        node.Attributes["MaxWavelength"] == null ||
                        node.Attributes["WavelengthStep"] == null)
                    {
                        return false;
                    }
                    int min, max, step;
                    if (!int.TryParse(node.Attributes["MinWavelength"].Value, style, culture, out min) ||
                        !int.TryParse(node.Attributes["MaxWavelength"].Value, style, culture, out max) ||
                        !int.TryParse(node.Attributes["WavelengthStep"].Value, style, culture, out step))
                    {
                        return false;
                    }
                    WavelengthRange range = new WavelengthRange(min, max, step);

                    Regex re = new Regex("[;,\\s]+");
                    var strItems = re.Split(node.InnerText);
                    if (strItems.Length != range.Count)
                        return false;

                    try
                    {
                        double[] values = Array.ConvertAll(strItems, s => double.Parse(s, style, culture));
                        Spectrum = new Spectrum(range, values);
                        return true;
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                }
                else if (node.Name == "XYZ")
                {
                    if (node.Attributes["Illuminant"] == null || node.Attributes["Observer"] == null ||
                        node.Attributes["X"] == null || node.Attributes["Y"] == null || node.Attributes["Z"] == null)
                    {
                        Clear();
                        return false;
                    }

                    try
                    {
                        StandardIlluminant illuminant = (StandardIlluminant)Enum.Parse(
                            typeof(StandardIlluminant), node.Attributes["Illuminant"].Value);
                        StandardObserver observer = (StandardObserver)Enum.Parse(
                            typeof(StandardObserver), node.Attributes["Observer"].Value);
                        double X = double.Parse(node.Attributes["X"].Value, style, culture);
                        double Y = double.Parse(node.Attributes["Y"].Value, style, culture);
                        double Z = double.Parse(node.Attributes["Z"].Value, style, culture);

                        Set(observer, illuminant, new CIEXYZ(X, Y, Z));
                    }
                    catch (Exception)
                    {
                        Clear();
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// 设置指定观察者角度、光源下的XYZ值
        /// </summary>
        /// <param name="observer">xyz所属的观察者角度</param>
        /// <param name="illuminant">xyz所属的光源</param>
        /// <param name="xyz">颜色的XYZ值</param>
        public void Set(StandardObserver observer, StandardIlluminant illuminant, CIEXYZ xyz)
        {
            if (values == null)
                values = new Dictionary<StandardObserver, Dictionary<StandardIlluminant, CIEXYZ>>();
            if (!values.ContainsKey(observer))
                values[observer] = new Dictionary<StandardIlluminant, CIEXYZ>();
            values[observer][illuminant] = xyz;
        }

        /// <summary>
        /// 移除指定观察者角度和光源下的颜色值
        /// </summary>
        /// <param name="observer">观察者角度</param>
        /// <param name="illuminant">光源</param>
        public void Remove(StandardObserver observer, StandardIlluminant illuminant)
        {
            if (values.ContainsKey(observer) && values[observer].ContainsKey(illuminant))
                values[observer].Remove(illuminant);
        }

        /// <summary>
        /// 光谱
        /// </summary>
        [DataMember]
        public Spectrum Spectrum { get; set; }


        public CIELAB Lab(Formula formula)
        {
            CIEXYZ xyz = XYZ(formula);

            if (xyz != null)
                return formula.XyzToLab((CIEXYZ)xyz);
            return null;
        }

        public CIEXYZ XYZ(Formula formula)
        {
            if (Spectrum != null)
            {
                return formula.CalculateXyz(Spectrum);
            }
            else if (values != null && values.ContainsKey(formula.Observer))
            {
                if (values[formula.Observer].ContainsKey(formula.Illuminant))
                    return values[formula.Observer][formula.Illuminant];
            }
            return null;
        }
        public HunterLab HunterLab(Formula formula)
        {
            CIEXYZ xyz = XYZ(formula);

            if (xyz != null)
                return formula.XyzToHunterLab((CIEXYZ)xyz);
            return null;
        }

        /// <summary>
        /// 如果不是反射率返回支持的所有Formula；如果是反射率则返回null
        /// </summary>
        public Formula[] Formulas
        {
            get
            {
                if (Spectrum != null)
                    return null;
                if (values == null)
                    return new Formula[0];

                List<Formula> formulas = new List<Formula>();
                foreach (StandardObserver observer in values.Keys)
                {
                    foreach (StandardIlluminant illuminant in values[observer].Keys)
                    {
                        formulas.Add(new Formula(illuminant, observer));
                    }
                }
                return formulas.ToArray();
            }
        }
    }
}
