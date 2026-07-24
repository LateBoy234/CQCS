using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using ThreeNH.Optical;

namespace ThreeNH.DataModel
{
    public class ChromaticitySetting
    {
        private List<ChromaticityField> displayFields = new List<ChromaticityField>();  // 显示的字段


        public ChromaticitySetting()
        {
            DefaultFields(ColorSpace.CIE_LAB, ColorDiffFormula.CIE1976);
        }
        public ColorSpace CurrentColorSpace { get; set; }
        public ColorDiffFormula CurrentColorDiffFormula { get; set; }

        /// <summary>
        /// 从原有ChromaticitySetting构建一个一样的设置
        /// </summary>
        /// <param name="source">源</param>
        public ChromaticitySetting(ChromaticitySetting source)
        {
            SetDisplayFields(source.displayFields);
        }

        public virtual XmlElement ToXmlElement(XmlDocument doc, string nodeName)
        {
            XmlElement element = doc.CreateElement(nodeName);
            element.AppendChild(doc.CreateTextNode(
                string.Join(",", Array.ConvertAll(DisplayFields, field => field.ToString()))));
            return element;
        }


        public virtual bool LoadXmlElement(XmlElement gradnson)
        {
            displayFields.Clear();
            Regex re = new Regex("[,\\s]+");

            ChromaticityField field;
            foreach (var item in re.Split(gradnson.InnerText))
            {
                if (Enum.TryParse(item, out field))
                    displayFields.Add(field);
            }
            displayFields.EnsureFixedDisplayFields();
            UpdateNeeds();

            return true;
        }

        /// <summary>
        /// 添加要显示的字段；如果已存在则不再添加
        /// </summary>
        /// <param name="field">要显示的字段</param>
        public void AddDisplayField(ChromaticityField field)
        {
            if (!displayFields.Contains(field))
            {
                displayFields.Add(field);

                UpdateNeeds();
            }
        }

        public void AddDisplayFields(IEnumerable<ChromaticityField> fields)
        {
            displayFields.AddRange(fields.Except(displayFields));
            UpdateNeeds();
        }

        /// <summary>
        /// 插入一个新的显示字段在位置before前；如果已存在，则将其移动到指定的位置
        /// </summary>
        /// <param name="index"></param>
        /// <param name="field"></param>
        public void InsertField(int index, ChromaticityField field)
        {
            if (displayFields.Contains(field))
            {
                displayFields.Remove(field);
            }
            displayFields.Insert(index, field);

            UpdateNeeds();
        }

        /// <summary>
        /// 显示的颜色字段
        /// </summary>
        public ChromaticityField[] DisplayFields
        {
            get
            {
                //if (displayFields.Count > 0)
                return displayFields.ToArray();
                //return null;
            }
            set
            {
                SetDisplayFields(value);
            }
        }

        /// <summary>
        /// 设置显示的字段
        /// </summary>
        /// <param name="fields"></param>
        public void SetDisplayFields(IEnumerable<ChromaticityField> fields)
        {
            displayFields.Clear();
            if (fields != null)
            {
                displayFields.AddRange(fields);
            }
            displayFields.EnsureFixedDisplayFields();
            UpdateNeeds();
        }



        // 显示的色品字段
        public ChromaticityField[] ColorFields
        {
            get
            {
                if (DisplayFields == null)
                    return null;

                return (from field in DisplayFields
                        where field.IsColorField()
                        select field).ToArray();
            }
        }
        // 显示的色差字段
        public ChromaticityField[] ColorDiffFields
        {
            get
            {
                if (DisplayFields == null)
                    return null;

                return (from field in DisplayFields
                        where field.IsColorDiffField()
                        select field).ToArray();
            }
        }

        /// <summary>
        /// 色差和指数差字段
        /// </summary>
        public ChromaticityField[] DiffFields
        {
            get
            {
                if (DisplayFields == null)
                    return null;

                return (from field in DisplayFields
                        where field.IsColorDiffField()
                        select field)
                        .Union((from field in DisplayFields
                                where field.IsChromaticityIndexDiff()
                                select field)).ToArray();
            }
        }

        /// <summary>
        /// 所有的色差公式字段
        /// </summary>
        public ChromaticityField[] EquationFields
        {
            get
            {
                if (DisplayFields == null)
                    return null;

                return (from field in DisplayFields
                        where field.IsEquation()
                        select field).ToArray();
            }
        }

        /// <summary>
        /// 所有的颜色指数字段
        /// </summary>
        public ChromaticityField[] ColorIndexFields
        {
            get
            {
                if (DisplayFields == null)
                    return null;
                return (from field in DisplayFields
                        where field.IsChromaticityIndex()
                        select field).ToArray();
            }
        }

        /// <summary>
        /// 所有指数差字段
        /// </summary>
        public ChromaticityField[] ColorIndexDiffFields
        {
            get
            {
                if (DisplayFields == null)
                    return null;
                return (from field in DisplayFields
                        where field.IsChromaticityIndexDiff()
                        select field).ToArray();
            }
        }

        /// <summary>
        /// 样品的非数值属性
        /// </summary>
        public ChromaticityField[] SpecimenAttributeFields
        {
            get
            {
                if (DisplayFields == null)
                    return null;
                return (from field in DisplayFields where field.IsSpecimenAttribute() select field).ToArray();
            }
        }

        /// <summary>
        /// 如果字段需要 CIELAB返回true
        /// </summary>
        public bool NeedLab { get; private set; }
        /// <summary>
        /// 如果字段需要 CIELUV返回true
        /// </summary>
        public bool NeedLuv { get; private set; }
        /// <summary>
        /// 如果字段需要 CIEXYZ返回true
        /// </summary>
        public bool NeedXYZ { get; private set; }
        /// <summary>
        /// 如果字段需要 HunterLab返回true
        /// </summary>
        public bool NeedHunterLab { get; private set; }
        /// <summary>
        /// 如果字段值计算需要CIERGB返回true
        /// </summary>
        public bool NeedRGB { get; private set; }

        /// <summary>
        /// 如果需要DIN99颜色空间返回真
        /// </summary>
        public bool NeedDIN99Lab { get; private set; }

        //public bool NeedDeltaLab { get; private set; }
        public bool NeedCIEDE1976 { get; private set; }
        public bool NeedCIEDE1994 { get; private set; }
        public bool NeedCIEDE2000 { get; private set; }
        public bool NeedCMCDE { get; private set; }
        public bool NeedDeltaLuv { get; private set; }
        public bool NeedDeltaXyz { get; private set; }
        public bool NeedDeltaHunterLab { get; private set; }
        public bool NeedDeltaRGB { get; private set; }
        public bool NeedDIN99 { get; private set; }
        public bool NeedCMC11DE { get; private set; }
        public bool NeedCMC21DE { get; private set; }
        public bool NeedMunsell { get; private set; }

        private void UpdateNeeds()
        {
            ChromaticityField[] fields = DisplayFields;
            if (fields == null)
            {
                NeedLab = NeedXYZ = NeedHunterLab = NeedLuv = NeedRGB =
                    NeedCIEDE1976 = NeedCIEDE1994 = NeedCIEDE2000 = NeedCMCDE =
                    NeedDeltaLuv = NeedDeltaXyz = NeedHunterLab = NeedDeltaRGB =
                    NeedCMC11DE = NeedCMC21DE = false;
                NeedMunsell = false;
                return;
            }

            NeedXYZ = true;  // XYZ值是必须的，以为获得其他值之前都需要XYZ值

            NeedLab = fields.Contains(ChromaticityField.L) ||
                fields.Contains(ChromaticityField.a) ||
                fields.Contains(ChromaticityField.b) ||
                fields.Contains(ChromaticityField.C) ||
                fields.Contains(ChromaticityField.h) ||
                fields.Contains(ChromaticityField.dL) ||
                fields.Contains(ChromaticityField.da) ||
                fields.Contains(ChromaticityField.db) ||
                fields.Contains(ChromaticityField.dC) ||
                //fields.Contains(ChromaticityField.dh) ||
                fields.Contains(ChromaticityField.dH) ||
                fields.Contains(ChromaticityField.dEab) ||
                fields.Contains(ChromaticityField.dE94) ||
                fields.Contains(ChromaticityField.dE00) ||
                fields.Contains(ChromaticityField.dEcmc) ||
                fields.Contains(ChromaticityField.dEcmc11) ||
                fields.Contains(ChromaticityField.dEcmc21) ||
                fields.Contains(ChromaticityField.dL00) ||
                fields.Contains(ChromaticityField.dC00) ||
                fields.Contains(ChromaticityField.dH00) ||
                fields.Contains(ChromaticityField.dL94) ||
                fields.Contains(ChromaticityField.dC94) ||
                fields.Contains(ChromaticityField.dH94) ||
                fields.Contains(ChromaticityField.dLcmc) ||
                fields.Contains(ChromaticityField.dCcmc) ||
                fields.Contains(ChromaticityField.dHcmc) ||
                fields.Contains(ChromaticityField.dLcmc11) ||
                fields.Contains(ChromaticityField.dCcmc11) ||
                fields.Contains(ChromaticityField.dHcmc11) ||
                fields.Contains(ChromaticityField.dLcmc21) ||
                fields.Contains(ChromaticityField.dCcmc21) ||
                fields.Contains(ChromaticityField.dHcmc21) ||
                fields.Contains(ChromaticityField.StainingFastness_dEgs) ||
                fields.Contains(ChromaticityField.StainingFastness_SSRLevel) ||
                fields.Contains(ChromaticityField.ColorFastness_dEf) ||
                fields.Contains(ChromaticityField.ColorFastness_GSRLevel) ||
                fields.Contains(ChromaticityField.ShadeBy555);

            NeedHunterLab = fields.Contains(ChromaticityField.HL) ||
                fields.Contains(ChromaticityField.Ha) ||
                fields.Contains(ChromaticityField.Hb) ||
                fields.Contains(ChromaticityField.dHL) ||
                fields.Contains(ChromaticityField.dHa) ||
                fields.Contains(ChromaticityField.dHb) ||
                fields.Contains(ChromaticityField.dHE) ||
                fields.Contains(ChromaticityField.WI_Hunter1942) ||
                fields.Contains(ChromaticityField.WI_Hunter1960);
            NeedLuv = fields.Contains(ChromaticityField.u) ||
                fields.Contains(ChromaticityField.v) ||
                fields.Contains(ChromaticityField.du) ||
                fields.Contains(ChromaticityField.dv) ||
                fields.Contains(ChromaticityField.dEuv);
            NeedRGB = fields.Contains(ChromaticityField.R) ||
                fields.Contains(ChromaticityField.G) ||
                fields.Contains(ChromaticityField.B) ||
                fields.Contains(ChromaticityField.dR) ||
                fields.Contains(ChromaticityField.dG) ||
                fields.Contains(ChromaticityField.dB);

            NeedDIN99Lab = fields.Contains(ChromaticityField.L99) ||
                        fields.Contains(ChromaticityField.a99) ||
                        fields.Contains(ChromaticityField.b99) ||
                        fields.Contains(ChromaticityField.C99) ||
                        fields.Contains(ChromaticityField.h99) ||
                        fields.Contains(ChromaticityField.dL99) ||
                        fields.Contains(ChromaticityField.da99) ||
                        fields.Contains(ChromaticityField.db99) ||
                        fields.Contains(ChromaticityField.dC99) ||
                        fields.Contains(ChromaticityField.dH99) ||
                        fields.Contains(ChromaticityField.dE99);
            NeedMunsell = fields.Contains(ChromaticityField.MH)
                || fields.Contains(ChromaticityField.MV)
                || fields.Contains(ChromaticityField.MC);
            if (fields.Length == 1 && fields[0] == ChromaticityField.L)
            {
                NeedLab = true;
            }

            // 色差
            fields = ColorDiffFields;
            NeedCIEDE1976 = fields.Contains(ChromaticityField.da)
                || fields.Contains(ChromaticityField.db)
                || fields.Contains(ChromaticityField.dC)
                || fields.Contains(ChromaticityField.dH)
                || fields.Contains(ChromaticityField.dEab);
            NeedCIEDE1994 = fields.Contains(ChromaticityField.dE94)
                            || fields.Contains(ChromaticityField.dL94)
                            || fields.Contains(ChromaticityField.dC94)
                            || fields.Contains(ChromaticityField.dH94);
            NeedCIEDE2000 = fields.Contains(ChromaticityField.dE00)
                            || fields.Contains(ChromaticityField.dL00)
                            || fields.Contains(ChromaticityField.dC00)
                            || fields.Contains(ChromaticityField.dH00);
            NeedCMCDE = fields.Contains(ChromaticityField.dEcmc)
                || fields.Contains(ChromaticityField.dLcmc)
                || fields.Contains(ChromaticityField.dCcmc)
                || fields.Contains(ChromaticityField.dHcmc);

            NeedCMC11DE = fields.Contains(ChromaticityField.dEcmc11)
                        || fields.Contains(ChromaticityField.dLcmc11)
                        || fields.Contains(ChromaticityField.dCcmc11)
                        || fields.Contains(ChromaticityField.dHcmc11);

            NeedCMC21DE = fields.Contains(ChromaticityField.dEcmc21)
                        || fields.Contains(ChromaticityField.dLcmc21)
                        || fields.Contains(ChromaticityField.dCcmc21)
                        || fields.Contains(ChromaticityField.dHcmc21);

            NeedDeltaXyz = fields.Contains(ChromaticityField.dX)
                || fields.Contains(ChromaticityField.dY)
                || fields.Contains(ChromaticityField.dZ)
                || fields.Contains(ChromaticityField.dx)
                || fields.Contains(ChromaticityField.dy);

            NeedDeltaHunterLab = fields.Contains(ChromaticityField.dHL)
                || fields.Contains(ChromaticityField.dHa)
                || fields.Contains(ChromaticityField.dHb)
                || fields.Contains(ChromaticityField.dHE);

            NeedDeltaLuv = fields.Contains(ChromaticityField.du)
                || fields.Contains(ChromaticityField.dv)
                || fields.Contains(ChromaticityField.dEuv);

            NeedDeltaRGB = fields.Contains(ChromaticityField.dR)
                || fields.Contains(ChromaticityField.dG)
                || fields.Contains(ChromaticityField.dB);

            NeedDIN99 = fields.Contains(ChromaticityField.da99)
                        || fields.Contains(ChromaticityField.db99)
                        || fields.Contains(ChromaticityField.dC99)
                        || fields.Contains(ChromaticityField.dH99)
                        || fields.Contains(ChromaticityField.dE99);

            if (fields.Length == 1 && fields[0] == ChromaticityField.dL)
                NeedCIEDE1976 = true;
        }

        /// <summary>
        /// 将显示字段设为与指定的颜色空间和色差公式相匹配的默认字段
        /// </summary>
        /// <param name="diffFormula">使用的色差公式</param>
        /// <param name="colorSpace">使用的颜色空间</param>
        public void DefaultFields(ColorSpace colorSpace, ColorDiffFormula diffFormula)
        {
            CurrentColorSpace = colorSpace;
            CurrentColorDiffFormula = diffFormula;
            displayFields.Clear();
            displayFields.Add(ChromaticityField.PseudoColor);
            displayFields.Add(ChromaticityField.Name);
            displayFields.Add(ChromaticityField.SpecularComponent);
            displayFields.Add(ChromaticityField.DateTime);
            displayFields.Add(ChromaticityField.IlluminantAndObserver);

            switch (colorSpace)
            {
                case ColorSpace.CIE_LAB:
                    displayFields.Add(ChromaticityField.L);
                    displayFields.Add(ChromaticityField.a);
                    displayFields.Add(ChromaticityField.b);
                    displayFields.Add(ChromaticityField.dL);
                    displayFields.Add(ChromaticityField.da);
                    displayFields.Add(ChromaticityField.db);
                    displayFields.Add(ChromaticityField.ColorOffset);
                    break;
                case ColorSpace.CIE_LCH:
                    displayFields.Add(ChromaticityField.L);
                    displayFields.Add(ChromaticityField.C);
                    displayFields.Add(ChromaticityField.h);
                    displayFields.Add(ChromaticityField.dL);
                    displayFields.Add(ChromaticityField.dC);
                    displayFields.Add(ChromaticityField.dH);
                    break;
                case ColorSpace.CIE_XYZ:
                    displayFields.Add(ChromaticityField.X);
                    displayFields.Add(ChromaticityField.Y);
                    displayFields.Add(ChromaticityField.Z);
                    displayFields.Add(ChromaticityField.dX);
                    displayFields.Add(ChromaticityField.dY);
                    displayFields.Add(ChromaticityField.dZ);
                    break;
                case ColorSpace.CIE_Yxy:
                    displayFields.Add(ChromaticityField.Y);
                    displayFields.Add(ChromaticityField.x);
                    displayFields.Add(ChromaticityField.y);
                    displayFields.Add(ChromaticityField.dY);
                    displayFields.Add(ChromaticityField.dx);
                    displayFields.Add(ChromaticityField.dy);
                    break;
                case ColorSpace.HUNTER_LAB:
                    displayFields.Add(ChromaticityField.HL);
                    displayFields.Add(ChromaticityField.Ha);
                    displayFields.Add(ChromaticityField.Hb);
                    displayFields.Add(ChromaticityField.dHL);
                    displayFields.Add(ChromaticityField.dHa);
                    displayFields.Add(ChromaticityField.dHb);
                    break;
                case ColorSpace.CIE_LUV:
                    displayFields.Add(ChromaticityField.L);
                    displayFields.Add(ChromaticityField.u);
                    displayFields.Add(ChromaticityField.v);
                    displayFields.Add(ChromaticityField.dL);
                    displayFields.Add(ChromaticityField.du);
                    displayFields.Add(ChromaticityField.dv);
                    break;
                case ColorSpace.CIE_RGB:
                    displayFields.Add(ChromaticityField.R);
                    displayFields.Add(ChromaticityField.G);
                    displayFields.Add(ChromaticityField.B);
                    displayFields.Add(ChromaticityField.dR);
                    displayFields.Add(ChromaticityField.dG);
                    displayFields.Add(ChromaticityField.dB);
                    break;
                case ColorSpace.DIN99_LAB:
                    displayFields.Add(ChromaticityField.L99);
                    displayFields.Add(ChromaticityField.a99);
                    displayFields.Add(ChromaticityField.b99);
                    displayFields.Add(ChromaticityField.dL99);
                    displayFields.Add(ChromaticityField.da99);
                    displayFields.Add(ChromaticityField.db99);
                    break;
                case ColorSpace.DIN99_LCh:
                    displayFields.Add(ChromaticityField.L99);
                    displayFields.Add(ChromaticityField.C99);
                    displayFields.Add(ChromaticityField.h99);
                    displayFields.Add(ChromaticityField.dL99);
                    displayFields.Add(ChromaticityField.dC99);
                    displayFields.Add(ChromaticityField.dH99);
                    break;
                case ColorSpace.BETA_xy:
                    displayFields.Add(ChromaticityField.LuminanceFactor);
                    displayFields.Add(ChromaticityField.x);
                    displayFields.Add(ChromaticityField.y);
                    displayFields.Add(ChromaticityField.dLuminanceFactor);
                    displayFields.Add(ChromaticityField.dx);
                    displayFields.Add(ChromaticityField.dy);
                    break;
                //case ColorSpace.MUNSELL:
                //    break;
                default:
                    throw new ArgumentException(
                        string.Format("Unknown Color Space '{0}'", colorSpace));
            }

            DefaultEquation(diffFormula, false);

            displayFields.Add(ChromaticityField.Judgement);

            UpdateNeeds();
        }

        public static ChromaticityField[] GetFieldsBy(ColorSpace colorSpace, bool withDiff = true)
        {
            List<ChromaticityField> fields = new List<ChromaticityField>();
            switch (colorSpace)
            {
                case ColorSpace.CIE_LAB:
                    fields.Add(ChromaticityField.L);
                    fields.Add(ChromaticityField.a);
                    fields.Add(ChromaticityField.b);
                    if (withDiff)
                    {
                        fields.Add(ChromaticityField.dL);
                        fields.Add(ChromaticityField.da);
                        fields.Add(ChromaticityField.db);
                    }
                    break;
                case ColorSpace.CIE_LCH:
                    fields.Add(ChromaticityField.L);
                    fields.Add(ChromaticityField.C);
                    fields.Add(ChromaticityField.h);
                    if (withDiff)
                    {
                        fields.Add(ChromaticityField.dL);
                        fields.Add(ChromaticityField.dC);
                        fields.Add(ChromaticityField.dH);
                    }
                    break;
                case ColorSpace.CIE_XYZ:
                    fields.Add(ChromaticityField.X);
                    fields.Add(ChromaticityField.Y);
                    fields.Add(ChromaticityField.Z);
                    if (withDiff)
                    {
                        fields.Add(ChromaticityField.dX);
                        fields.Add(ChromaticityField.dY);
                        fields.Add(ChromaticityField.dZ);
                    }
                    break;
                case ColorSpace.CIE_Yxy:
                    fields.Add(ChromaticityField.Y);
                    fields.Add(ChromaticityField.x);
                    fields.Add(ChromaticityField.y);
                    if (withDiff)
                    {
                        fields.Add(ChromaticityField.dY);
                        fields.Add(ChromaticityField.dx);
                        fields.Add(ChromaticityField.dy);
                    }
                    break;
                case ColorSpace.HUNTER_LAB:
                    fields.Add(ChromaticityField.HL);
                    fields.Add(ChromaticityField.Ha);
                    fields.Add(ChromaticityField.Hb);
                    if (withDiff)
                    {
                        fields.Add(ChromaticityField.dHL);
                        fields.Add(ChromaticityField.dHa);
                        fields.Add(ChromaticityField.dHb);
                    }
                    break;
                case ColorSpace.CIE_LUV:
                    fields.Add(ChromaticityField.L);
                    fields.Add(ChromaticityField.u);
                    fields.Add(ChromaticityField.v);
                    fields.Add(ChromaticityField.uq);
                    fields.Add(ChromaticityField.vq);
                    if (withDiff)
                    {
                        fields.Add(ChromaticityField.dL);
                        fields.Add(ChromaticityField.du);
                        fields.Add(ChromaticityField.dv);
                        fields.Add(ChromaticityField.duq);
                        fields.Add(ChromaticityField.dvq);
                    }
                    break;
                case ColorSpace.CIE_RGB:
                    fields.Add(ChromaticityField.R);
                    fields.Add(ChromaticityField.G);
                    fields.Add(ChromaticityField.B);
                    if (withDiff)
                    {
                        fields.Add(ChromaticityField.dR);
                        fields.Add(ChromaticityField.dG);
                        fields.Add(ChromaticityField.dB);
                    }
                    break;
                case ColorSpace.sRGB:
                    fields.Add(ChromaticityField.sR);
                    fields.Add(ChromaticityField.sG);
                    fields.Add(ChromaticityField.sB);
                    break;
                case ColorSpace.DIN99_LAB:
                    fields.Add(ChromaticityField.L99);
                    fields.Add(ChromaticityField.a99);
                    fields.Add(ChromaticityField.b99);
                    if (withDiff)
                    {
                        fields.Add(ChromaticityField.dL99);
                        fields.Add(ChromaticityField.da99);
                        fields.Add(ChromaticityField.db99);
                    }
                    break;
                case ColorSpace.DIN99_LCh:
                    fields.Add(ChromaticityField.L99);
                    fields.Add(ChromaticityField.C99);
                    fields.Add(ChromaticityField.h99);
                    if (withDiff)
                    {
                        fields.Add(ChromaticityField.dL99);
                        fields.Add(ChromaticityField.dC99);
                        fields.Add(ChromaticityField.dH99);
                    }
                    break;
                case ColorSpace.BETA_xy:
                    fields.Add(ChromaticityField.LuminanceFactor);
                    fields.Add(ChromaticityField.x);
                    fields.Add(ChromaticityField.y);
                    if (withDiff)
                    {
                        fields.Add(ChromaticityField.dLuminanceFactor);
                        fields.Add(ChromaticityField.dx);
                        fields.Add(ChromaticityField.dy);
                    }
                    break;
                case ColorSpace.MUNSELL:
                    fields.Add(ChromaticityField.MH);
                    fields.Add(ChromaticityField.MV);
                    fields.Add(ChromaticityField.MC);
                    if (withDiff)
                    {
                        fields.Add(ChromaticityField.dMH);
                        fields.Add(ChromaticityField.dMV);
                        fields.Add(ChromaticityField.dMC);
                    }
                    break;
                default:
                    break;
            }
            return fields.ToArray();
        }
        public static ChromaticityField[] GetReflectanceFields(int min, int max)
        {
            if (min < 360 || max > 740 || min > max || min % 10 != 0 || max % 10 != 0)
            {
                throw new ArgumentOutOfRangeException();
            }
            List<ChromaticityField> fields = new List<ChromaticityField>();
            for (int i = (min - 360) / 10; i <= (max - 360) / 10; i++)
            {
                fields.Add(ChromaticityField.R360 + i);
            }
            return fields.ToArray();
        }
        public static ChromaticityField[] GetReflectanceDiffFirleds(int min, int max)
        {
            if (min < 360 || max > 740 || min > max || min % 10 != 0 || max % 10 != 0)
            {
                throw new ArgumentOutOfRangeException();
            }
            List<ChromaticityField> fields = new List<ChromaticityField>();
            for (int i = (min - 360) / 10; i <= (max - 360) / 10; i++)
            {
                fields.Add(ChromaticityField.dR360 + i);
            }
            return fields.ToArray();
        }

        /// <summary>
        /// 根据颜色空间设置默认的色品坐标及色差属性（不包括色差公式）
        /// </summary>
        /// <param name="colorSpace">颜色空间</param>
        public void DefaultChromaticity(ColorSpace colorSpace)
        {
            foreach (var field in displayFields.ToArray())
            {
                if (field.IsChromaticity())
                    displayFields.Remove(field);
            }

            displayFields.AddRange(GetFieldsBy(colorSpace));

            UpdateNeeds();
        }

        /// <summary>
        /// 根据指定的色差公式设定色差空式字段
        /// </summary>
        /// <param name="diffFormula">色差公式</param>
        /// <param name="updateNeeds">是否更新Need..属性</param>
        public void DefaultEquation(ColorDiffFormula diffFormula, bool updateNeeds = true)
        {
            foreach (var field in displayFields.ToArray())
            {
                if (field.IsEquation())
                    displayFields.Remove(field);
            }

            switch (diffFormula)
            {
                case ColorDiffFormula.CIE1976:
                    displayFields.Add(ChromaticityField.dEab);
                    break;
                case ColorDiffFormula.CIE1994:
                    displayFields.Add(ChromaticityField.dE94);
                    break;
                case ColorDiffFormula.CIE2000:
                    displayFields.Add(ChromaticityField.dE00);
                    break;
                case ColorDiffFormula.CMC:
                    displayFields.Add(ChromaticityField.dEcmc);
                    break;
                case ColorDiffFormula.CMC11:
                    displayFields.Add(ChromaticityField.dEcmc11);
                    break;
                case ColorDiffFormula.CMC21:
                    displayFields.Add(ChromaticityField.dEcmc21);
                    break;
                case ColorDiffFormula.DEuv:
                    displayFields.Add(ChromaticityField.dEuv);
                    break;
                case ColorDiffFormula.DHunterE:
                    displayFields.Add(ChromaticityField.dHE);
                    break;
                case ColorDiffFormula.DIN99:
                    displayFields.Add(ChromaticityField.dE99);
                    break;
                default:
                    break;
            }

            if (updateNeeds)
            {
                UpdateNeeds();
            }
        }

        /// <summary>
        /// 拷贝source的设置
        /// </summary>
        /// <param name="source">要拷贝其设置的源</param>
        public void Copy(ChromaticitySetting source)
        {
            SetDisplayFields(source.DisplayFields);
        }

        public ColorSpace ColorSpace
        {
            get
            {
                var displayFields = DisplayFields;
                if (displayFields == null)
                    return ColorSpace.Unknown;

                SortedSet<ChromaticityField> fields = new SortedSet<ChromaticityField>(displayFields);
                fields.ExceptWith(EquationFields);
                fields.ExceptWith(ColorIndexFields);
                fields.ExceptWith(ColorIndexDiffFields);
                fields.ExceptWith(SpecimenAttributeFields);


                if (fields.Count != 6 && fields.Count != 3)
                    return ColorSpace.Unknown;

                if (fields.IsSupersetOf(new ChromaticityField[] {
                ChromaticityField.L, ChromaticityField.a, ChromaticityField.b,
                ChromaticityField.dL, ChromaticityField.da, ChromaticityField.db}))
                {
                    return ColorSpace.CIE_LAB;
                }

                if (fields.IsSupersetOf(new ChromaticityField[] {
                ChromaticityField.L, ChromaticityField.C, ChromaticityField.h,
                ChromaticityField.dL, ChromaticityField.dC, ChromaticityField.dH}))
                {
                    return ColorSpace.CIE_LCH;
                }

                if (fields.IsSupersetOf(new ChromaticityField[] {
                ChromaticityField.L, ChromaticityField.u, ChromaticityField.v,
                ChromaticityField.dL, ChromaticityField.du, ChromaticityField.dv}))
                {
                    return ColorSpace.CIE_LUV;
                }

                if (fields.IsSupersetOf(new ChromaticityField[] {
                ChromaticityField.HL, ChromaticityField.Ha, ChromaticityField.Hb,
                ChromaticityField.dHL, ChromaticityField.dHa, ChromaticityField.dHb}))
                {
                    return ColorSpace.HUNTER_LAB;
                }

                if (fields.IsSupersetOf(new ChromaticityField[] {
                ChromaticityField.X, ChromaticityField.Y, ChromaticityField.Z,
                ChromaticityField.dX, ChromaticityField.dY, ChromaticityField.dZ}))
                {
                    return ColorSpace.CIE_XYZ;
                }

                if (fields.IsSupersetOf(new ChromaticityField[] {
                ChromaticityField.Y, ChromaticityField.x, ChromaticityField.y,
                ChromaticityField.dY, ChromaticityField.dx, ChromaticityField.dy}))
                {
                    return ColorSpace.CIE_Yxy;
                }

                if (fields.IsSupersetOf(new ChromaticityField[] {
                ChromaticityField.R, ChromaticityField.G, ChromaticityField.B,
                ChromaticityField.dR, ChromaticityField.dG, ChromaticityField.dB}))
                {
                    return ColorSpace.CIE_RGB;
                }

                if (fields.IsSupersetOf(new ChromaticityField[] {
                    ChromaticityField.sR, ChromaticityField.sG, ChromaticityField.sB }))
                {
                    return ColorSpace.sRGB;
                }

                if (fields.IsSupersetOf(new ChromaticityField[] {
                    ChromaticityField.L99, ChromaticityField.a99, ChromaticityField.b99,
                    ChromaticityField.dL99, ChromaticityField.da99, ChromaticityField.db99}))
                {
                    return ColorSpace.DIN99_LAB;
                }

                if (fields.IsSupersetOf(new ChromaticityField[] {
                    ChromaticityField.L99, ChromaticityField.C99, ChromaticityField.h99,
                    ChromaticityField.dL99, ChromaticityField.dC99, ChromaticityField.dH99}))
                {
                    return ColorSpace.DIN99_LCh;
                }

                if (fields.IsSubsetOf(new ChromaticityField[] {
                    ChromaticityField.LuminanceFactor, ChromaticityField.x, ChromaticityField.y,
                    ChromaticityField.dLuminanceFactor, ChromaticityField.dx, ChromaticityField.dy}))
                {
                    return ColorSpace.BETA_xy;
                }

                return ColorSpace.Unknown;
            }
        }

        public ColorDiffFormula ColorDiffFormula
        {
            get
            {
                var fields = EquationFields;
                if (fields == null || fields.Length != 1)
                    return ColorDiffFormula.Unknown;
                switch (fields[0])
                {
                    case ChromaticityField.dEab: return ColorDiffFormula.CIE1976;
                    case ChromaticityField.dE94: return ColorDiffFormula.CIE1994;
                    case ChromaticityField.dE00: return ColorDiffFormula.CIE2000;
                    case ChromaticityField.dEcmc: return ColorDiffFormula.CMC;
                    case ChromaticityField.dHE: return ColorDiffFormula.DHunterE;
                    case ChromaticityField.dEuv: return ColorDiffFormula.DEuv;
                    case ChromaticityField.dE99: return ColorDiffFormula.DIN99;
                    case ChromaticityField.dEcmc11: return ColorDiffFormula.CMC11;
                    case ChromaticityField.dEcmc21: return ColorDiffFormula.CMC21;
                    default: return ColorDiffFormula.Unknown;
                }
            }
        }
    }
}