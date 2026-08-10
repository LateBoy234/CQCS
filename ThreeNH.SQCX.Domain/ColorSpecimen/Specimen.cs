using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.Serialization;
using System.Xml;
using ThreeNH.ColorAlgorithm;
using ThreeNH.Optical;


namespace ThreeNH.ColorSpecimen
{
    [DataContract]
    public class Specimen
    {
        public Specimen()
        {
            DateTime = DateTime.Now;
            UV = UVFlag.None;
        }
        public Specimen(Specimen source)
        {
            ID = source.ID;
            Name = source.Name;
            DateTime = source.DateTime;
            MeasurementCaliber = source.MeasurementCaliber;
            Instrument = source.Instrument;
            Material = source.Material;
            SCI = source.SCI;
            SCE = source.SCE;
            Clr = source.Clr;
            UV = source.UV;
            IsTransmission = source.IsTransmission;
            LensPosition = source.LensPosition;
            OpticalStruct = source.OpticalStruct;
            InstrumentSN = source.InstrumentSN;
            Tag = source.Tag;
            Gloss8 = source.Gloss8;
            HazeDatas = source.HazeDatas;
            Clarity = source.Clarity;
            ColorFormula = source.ColorFormula;
        }

        /// <summary>
        /// 将样品存储到XML元素中
        /// </summary>
        /// <param name="doc">XmlDocument对象</param>
        /// <param name="nodeName">生成的节点名</param>
        /// <param name="tagToXmlElem">
        /// 将Tag转换为节点的函数，第一个参数为将要附加到其上的XmlDocument对象，第二个参数为生成的元素名，第三个参数为Tag，返回值为存储Tag的XmlElement
        /// </param>
        /// <returns>存储数据的XML元素</returns>
        public XmlElement ToXmlElement(XmlDocument doc, string nodeName, Func<XmlDocument, string, object, XmlElement> tagToXmlElem)
        {
            var xmlElement = ToXmlElement(doc, nodeName);
            if (Tag != null)
            {
                xmlElement.AppendChild(tagToXmlElem(doc, "Tag", Tag));
            }

            return xmlElement;
        }

        public virtual XmlElement ToXmlElement(XmlDocument doc, string nodeName)
        {
            var node = doc.CreateElement(nodeName);
            node.SetAttribute("ID", ID.ToString());
            node.SetAttribute("Name", Name.ToString());
            node.SetAttribute("DateTime", DateTime.ToUniversalTime().ToString("u", CultureInfo.InvariantCulture));
            if (Material != null)
            {
                node.SetAttribute("Material", Material);
            }
            node.SetAttribute("UV", UV.ToString());
            node.SetAttribute("SpecularComponent", SpecularComponent.ToString());
            node.SetAttribute("MeasurementCaliber", MeasurementCaliber.ToString());
            node.SetAttribute(nameof(IsTransmission), IsTransmission.ToString());
            node.SetAttribute(nameof(LensPosition), LensPosition.ToString());
            node.SetAttribute(nameof(Temperature), Temperature.ToString("F2"));
            node.SetAttribute(nameof(Clarity), Clarity.ToString());
            if (OpticalStruct != null)
            {
                node.SetAttribute(nameof(OpticalStruct), OpticalStruct);
            }
            if (Instrument != null)
            {
                node.SetAttribute("Instrument", Instrument);
            }
            if (InstrumentSN != null)
            {
                node.SetAttribute("InstrumentSN", InstrumentSN);
            }
            if (SCI != null)
            {
                node.AppendChild(SCI.ToXmlElement(doc, "SCI"));
            }
            if (SCE != null)
            {
                node.AppendChild(SCE.ToXmlElement(doc, "SCE"));
            }
            if (Clr != null)
            {
                node.AppendChild(Clr.ToXmlElement(doc, "Clr"));
            }
            if (Gloss8 != null)
            {
                node.SetAttribute("Gloss8", Gloss8.ToString());
            }


            if (HazeDatas != null)
            {
                var hazeDatasNode = doc.CreateElement("HazeDataItems");
                foreach (var haze in HazeDatas)
                {
                    hazeDatasNode.AppendChild(haze.ToXmlElement(doc));
                }
                node.AppendChild(hazeDatasNode);
            }
            return node;
        }

        /// <summary>
        /// 从XML元素中加载数据
        /// </summary>
        /// <param name="node">XML元素</param>
        /// <param name="loadTagFromXmlElem">加载Tag数据的函数</param>
        /// <returns>成功返回真，失败返回假</returns>
        public bool LoadXmlElement(XmlElement node, Func<XmlElement, object> loadTagFromXmlElem)
        {
            if (LoadXmlElement(node))
            {
                var list = node.GetElementsByTagName("Tag");
                if (list.Count == 1)
                {
                    Tag = loadTagFromXmlElem(list[0] as XmlElement);
                }

                return true;
            }

            return false;
        }

        public virtual bool LoadXmlElement(XmlElement node)
        {
            if (node.Attributes["ID"] == null || node.Attributes["Name"] == null)
            {
                return false;
            }

            int id;
            if (!int.TryParse(node.Attributes["ID"].Value, out id))
            {
                return false;
            }
            ID = id;

            foreach (XmlAttribute attr in node.Attributes)
            {
                switch (attr.Name)
                {
                    case "Name":
                        Name = attr.Value;
                        break;
                    case "DateTime":
                        {
                            DateTime datetime = new DateTime();
                            if (DateTime.TryParse(attr.Value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out datetime))
                            {
                                DateTime = datetime;
                            }
                        }
                        break;
                    case "Material":
                        Material = attr.Value;
                        break;
                    case "UV":
                        {
                            UVFlag flag;
                            if (Enum.TryParse(attr.Value, out flag))
                                UV = flag;
                        }
                        break;
                    case "Instrument":
                        Instrument = attr.Value;
                        break;
                    case "MeasurementCaliber":
                        {
                            MeasurementCaliber caliber;
                            if (Enum.TryParse(attr.Value, out caliber))
                                MeasurementCaliber = caliber;
                            else
                                MeasurementCaliber = MeasurementCaliber.Unknown;
                        }
                        break;
                    case "InstrumentSN":
                        InstrumentSN = attr.Value;
                        break;
                    case nameof(IsTransmission):
                        try
                        {
                            IsTransmission = bool.Parse(attr.Value);
                        }
                        catch
                        {
                            IsTransmission = false;
                        }

                        break;
                    case nameof(LensPosition):
                        try
                        {
                            LensPosition = (LensPosition)Enum.Parse(typeof(LensPosition), attr.Value);
                        }
                        catch
                        {
                            LensPosition = LensPosition.Unknown;
                        }

                        break;
                    case nameof(Temperature):
                        try
                        {
                            Temperature = double.Parse(attr.Value);
                        }
                        catch
                        {
                            Temperature = 25.0;
                        }
                        break;
                    case nameof(OpticalStruct):
                        OpticalStruct = attr.Value;
                        break;
                    case nameof(Gloss8):
                        if (int.TryParse(attr.Value, out int gloss))
                            Gloss8 = gloss;
                        break;
                    case nameof(Clarity):
                        if (double.TryParse(attr.Value, out double clarity))
                            Clarity = clarity;
                        else
                            Clarity = double.NaN;
                        break;
                }
            }

            foreach (XmlElement child in node.ChildNodes)
            {
                if (String.Compare(child.Name, "SCI", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    ColorData clrData = new ColorData();
                    if (clrData.LoadXmlElement(child))
                        SCI = clrData;
                }
                else if (String.Compare(child.Name, "SCE", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    ColorData clrData = new ColorData();
                    if (clrData.LoadXmlElement(child))
                        SCE = clrData;
                }
                else if (String.Compare(child.Name, "Clr", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    ColorData clrData = new ColorData();
                    if (clrData.LoadXmlElement(child))
                        Clr = clrData;
                }
                else if (String.Compare(child.Name, "HazeDataItems", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    var list = new List<HazeData>();
                    foreach (XmlElement grandson in child.ChildNodes)
                    {
                        var hazeData = new HazeData();
                        if (hazeData.LoadXmlElement(grandson))
                        {
                            list.Add(hazeData);
                        }
                    }

                    HazeDatas = list.ToArray();
                }
            }

            return true;
        }

        /// <summary>
        /// 样品ID
        /// </summary>
        [DataMember]
        public int ID { get; set; }
        /// <summary>
        /// 样品名称
        /// </summary>
        [DataMember]
        public string Name { get; set; }

        /// <summary>
        /// 测量时间
        /// </summary>
        [DataMember]
        public DateTime DateTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 料号
        /// </summary>
        [DataMember]
        public string Material { get; set; }

        /// <summary>
        /// 测量口径
        /// </summary>
        [DataMember]
        public MeasurementCaliber MeasurementCaliber { get; set; }

        /// <summary>
        /// 透镜位置
        /// </summary>
        [DataMember]
        public LensPosition LensPosition { get; set; }

        /// <summary>
        /// 镜面光测量方式
        /// </summary>
        public SpecularComponent SpecularComponent
        {
            get
            {
                if (SCI != null && SCE != null)
                    return SpecularComponent.SCIAndSCE;
                if (SCI != null)
                    return SpecularComponent.SCI;
                if (SCE != null)
                    return SpecularComponent.SCE;
                return SpecularComponent.Unknown;
            }
        }

        /// <summary>
        /// UV标志
        /// </summary>
        [DataMember]
        public UVFlag UV { get; set; }

        /// <summary>
        /// 用于测量样品的仪器
        /// </summary>
        [DataMember]
        public string Instrument { get; set; }

        /// <summary>
        /// SCI模式下测量的颜色值
        /// </summary>
        [DataMember]
        public ColorData SCI { get; set; }
        /// <summary>
        /// SCE模式下测量得到的颜色值
        /// </summary>
        [DataMember]
        public ColorData SCE { get; set; }
        /// <summary>
        /// 其他测光模式测量得到的颜色
        /// </summary>
        [DataMember]
        public ColorData Clr { get; set; }

        /// <summary>
        /// 8度光泽度
        /// </summary>
        [DataMember]
        public double ? Gloss8 { get; set; }


        /// <summary>
        /// 光学结构
        /// </summary>
        [DataMember]
        public string OpticalStruct { get; set; }
        /// <summary>
        /// 仪器的编号
        /// </summary>
        public string InstrumentSN { get; set; }

        /// <summary>
        /// 根据镜面光测量方式获取样品的光谱数据
        /// </summary>
        /// <param name="sc">镜面光测量方式，只能是SCI或SCE</param>
        /// <returns>返回相应的光谱数据；如果没有光谱返回null</returns>
        public Spectrum GetSpectrum(SpecularComponent sc)
        {
            if (sc == SpecularComponent.SCI && SCI != null)
                return SCI.Spectrum;
            else if (sc == SpecularComponent.SCE && SCE != null)
                return SCE.Spectrum;
            else if (sc == SpecularComponent.Unknown && Clr != null)
            {
                return Clr.Spectrum;
            }
            else if (sc == SpecularComponent.SCIAndSCE)
            {
                // SCIAndSCE 模式下优先 SCI 光谱，无 SCI 则回退 SCE
                if (SCI != null)
                    return SCI.Spectrum;
                if (SCE != null)
                    return SCE.Spectrum;
            }
            return null;
        }

        public ColorData GetColorData(SpecularComponent sc)
        {
            if (sc == SpecularComponent.SCI)
                return SCI;
            else if (sc == SpecularComponent.SCE)
                return SCE;
            else if (sc == SpecularComponent.Unknown)
                return Clr;
            else if (sc == SpecularComponent.SCIAndSCE)
                return SCI ?? SCE; // SCIAndSCE 模式下优先 SCI，无 SCI 则回退 SCE
            return null;
        }
        public ColorData GetColorDataEx(SpecularComponent sc)
        {
            if (sc == SpecularComponent.SCI)
                return SCI;
            else if (sc == SpecularComponent.SCE)
                return SCE;
            else if (sc == SpecularComponent.SCIAndSCE)
                return SCI ?? SCE;
            else if (sc == SpecularComponent.Unknown)
                return Clr;
            return null;
        }

        /// <summary>
        /// 是否是透射数据
        /// </summary>
        [DataMember]
        public bool IsTransmission { get; set; } = false;

        /// <summary>
        /// 附加数据
        /// </summary>
        public object Tag { get; set; }

        /// <summary>
        /// 测量时的环境温度
        /// </summary>
        public double Temperature { get; set; } = 25.0;

        public string ColorFormula { get; set; }

        public string Remark { get; set; }

        [DataMember]
        /// <summary>
        /// 雾度值
        /// </summary>
        public HazeData[] HazeDatas { get; set; }

        public HazeData GetHazeData(StandardIlluminant illuminant, StandardObserver observer)
        {
            if (HazeDatas == null)
            {
                return null;
            }

            foreach (var hazeData in HazeDatas)
            {
                if (hazeData.Illuminant == illuminant && hazeData.Observer == observer)
                {
                    return hazeData;
                }
            }

            return null;
        }

        [DataMember]

        public double Clarity { get; set; }

    }
}
