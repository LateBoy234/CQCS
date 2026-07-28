using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using ThreeNH.ColorAlgorithm;
using ThreeNH.ColorimeterInterface;
using ThreeNH.ColorSpecimen;
using ThreeNH.DataModel;
using ThreeNH.Optical;
using Tolerance = ThreeNH.DataModel.Tolerance;

namespace ThreeNH.Spectrophotometer
{
    public static class DefineConveter
    {
        public static WavelengthRange WavelengthRange = new WavelengthRange(400, 700, 10);

        public static UVFlag ToUvFlag(this JToken token)
        {
            if (token == null)
            {
                return Optical.UVFlag.NoCut;
            }

            switch (token.ToString())
            {
                case "Cut None":
                    return Optical.UVFlag.NoCut;
                case "Cut 400nm":
                    return Optical.UVFlag.Cut400nm;
                case "Cut 420nm":
                    return Optical.UVFlag.Cut420nm;
                case "Cut 460nm":
                    return Optical.UVFlag.Cut460nm;
                case "On":
                    return Optical.UVFlag.On;
                case "Off":
                    return Optical.UVFlag.None;
                default:
                    return Optical.UVFlag.NoCut;
            }
        }

        public static MeasurementCaliber ToAperture(this JToken token)
        {
            if (token == null)
            {
                return Optical.MeasurementCaliber.Unknown;
            }

            switch (token.ToString())
            {
                case "4mm":
                    return Optical.MeasurementCaliber.Phi4;
                case "8mm":
                    return Optical.MeasurementCaliber.Phi8;
                case "15mm":
                    return Optical.MeasurementCaliber.Phi15;
                case "25.4mm":
                    return Optical.MeasurementCaliber.Phi1in;
                default:
                    return Optical.MeasurementCaliber.Unknown;
            }
        }

        public static LensPosition ToLensPosition(this JToken token)
        {
            if (token == null)
            {
                return Optical.LensPosition.Unknown;
            }

            switch (token.ToString())
            {
                case "4mm":
                    return Optical.LensPosition.Pos4;
                case "8mm":
                    return Optical.LensPosition.Pos8;
                case "15mm":
                    return Optical.LensPosition.Pos15;
                case "25.4mm":
                    return Optical.LensPosition.Pos1in;
                default:
                    return Optical.LensPosition.Unknown;
            }
        }

        public static WavelengthRange ToWavelengthRange(this JToken token)
        {
            int min = token["min"].ToObject<int>();
            int max = token["max"].ToObject<int>();
            int step = token["interval"].ToObject<int>();
            return new WavelengthRange(min, max, step);
        }

        public static MeasurementCaliber[] ToCalibers(this JToken token)
        {
            var calibers = new List<MeasurementCaliber>();
            var objs = JsonConvert.DeserializeObject<string[]>(token.ToString());
            foreach (var item in objs )
            {
                switch (item)
                {
                    case "4mm":
                        calibers.Add(Optical.MeasurementCaliber.Phi4);
                        break;
                    case "8mm":
                        calibers.Add(Optical.MeasurementCaliber.Phi8);
                        break;
                    case "15mm":
                        calibers.Add(Optical.MeasurementCaliber.Phi15);
                        break;
                    case "25.4mm":
                        calibers.Add(Optical.MeasurementCaliber.Phi1in);
                        break;
                }
            }

            return calibers.ToArray();
        }

        public static LensPosition[] ToLenPositions(this JToken token)
        {
            var positions = new List<LensPosition>();
            var objs = JsonConvert.DeserializeObject<string[]>(token.ToString());
            foreach (var item in objs)
            {
                switch (item)
                {

                    case "4mm":
                        positions.Add(Optical.LensPosition.Pos4);
                        break;
                    case "8mm":
                        positions.Add(Optical.LensPosition.Pos8);
                        break;
                    case "15mm":
                        positions.Add(Optical.LensPosition.Pos15);
                        break;
                    case "25.4mm":
                        positions.Add(Optical.LensPosition.Pos1in);
                        break;
                }
            }

            return positions.ToArray();
        }

        public static StandardIlluminant ToIlluminant(this JToken token)
        {
            switch (token.ToString())
            {
                case "D65": return StandardIlluminant.D65;
                case "D50": return StandardIlluminant.D50;
                case "A": return StandardIlluminant.A;
                case "C": return StandardIlluminant.C;
                case "D55": return StandardIlluminant.D55;
                case "D75": return StandardIlluminant.D75;
                case "F1": return StandardIlluminant.F1;
                case "F2": return StandardIlluminant.F2;
                case "F3": return StandardIlluminant.F3;
                case "F4": return StandardIlluminant.F4;
                case "F5": return StandardIlluminant.F5;
                case "F6": return StandardIlluminant.F6;
                case "F7": return StandardIlluminant.F7;
                case "F8": return StandardIlluminant.F8;
                case "F9": return StandardIlluminant.F9;
                case "F10": return StandardIlluminant.F10;
                case "F11": return StandardIlluminant.F11;
                case "F12": return StandardIlluminant.F12;
                default: return StandardIlluminant.D65;
            }
        }

        public static StandardObserver ToObserver(this JToken token)
        {
            return token.ToObject<int>() == 2 ? StandardObserver.CIE1931 : StandardObserver.CIE1964;
        }

        public static ColorSpace ToColorSpace(this JToken token)
        {
            switch (token.ToString())
            {
                case "Lab": return ColorSpace.CIE_LAB;
                case "Hunter Lab": return ColorSpace.HUNTER_LAB;
                case "XYZ": return ColorSpace.CIE_XYZ;
                case "sRGB": return ColorSpace.sRGB;
                case "Yxy": return ColorSpace.CIE_Yxy;
                case "Munsell":
                case "Munsell(C/2)": return ColorSpace.MUNSELL;
                case "LCh": return ColorSpace.CIE_LCH;
                case "Bxy": return ColorSpace.BETA_xy;
                case "Luv": return ColorSpace.CIE_LUV;
                case "Lab99": return ColorSpace.DIN99_LAB;
                default: return ColorSpace.Unknown;
            }
        }

        public static ColorDiffFormula ToColorDiffFormula(this JToken token)
        {
            switch (token.ToString())
            {
                case "DE": return ColorDiffFormula.CIE1976;
                case "DE94": return ColorDiffFormula.CIE1994;
                case "DE00": return ColorDiffFormula.CIE2000;
                case "CMC21": return ColorDiffFormula.CMC21;
                case "CMC11": return ColorDiffFormula.CMC11;
                case "CMC": return ColorDiffFormula.CMC;
                case "DEuv": return ColorDiffFormula.DEuv;
                case "DE99": return ColorDiffFormula.DIN99;
                case "HunterDE": return ColorDiffFormula.DHunterE;
                default: throw new ArgumentException();
            }
        }

        public static string ToString(ColorDiffFormula formula)
        {
            switch (formula)
            {
                case ColorDiffFormula.CIE1976:
                    return "DE";
                case ColorDiffFormula.CIE1994:
                    return "DE94";
                case ColorDiffFormula.CIE2000:
                    return "DE00";
                case ColorDiffFormula.CMC11:
                    return "CMC11";
                case ColorDiffFormula.CMC21:
                    return "CMC21";
                case ColorDiffFormula.CMC:
                    return "CMC";
                case ColorDiffFormula.DEuv:
                    return "DEuv";
                case ColorDiffFormula.DHunterE:
                    return "HunterDE";
                case ColorDiffFormula.DIN99:
                    return "DE99";
                default:
                    return "";
            }
        }

        public static string ToString(ColorSpace colorSpace)
        {
            switch (colorSpace)
            {
                case ColorSpace.CIE_XYZ:
                    return "XYZ";
                case ColorSpace.CIE_Yxy:
                    return "Yxy";
                case ColorSpace.CIE_LAB:
                    return "LAB";
                case ColorSpace.CIE_LCH:
                    return "LCh";
                case ColorSpace.CIE_LUV:
                    return "Luv";
                case ColorSpace.HUNTER_LAB:
                    return "Hunter Lab";
                case ColorSpace.sRGB:
                    return "sRGB";
                case ColorSpace.DIN99_LAB:
                    return "Lab99";
                case ColorSpace.MUNSELL:
                    return "Munsell";
                case ColorSpace.BETA_xy:
                    return "Bxy";
                default:
                    return "";
            }
        }

        public static CIELAB ToLab(this JObject jobj)
        {
            CIELAB lab = new CIELAB();
            lab.L = jobj["L"].ToObject<float>();
            lab.a = jobj["a"].ToObject<float>();
            lab.b = jobj["b"].ToObject<float>();
            return lab;
        }

        public static Tolerance ToTolerance(this JObject jobj)
        {
            try
            {
                Tolerance tolerance = new Tolerance();

                int use_de = jobj["use_de"]?.ToObject<int>() ?? 0;
                int use_items = jobj["use_items"]?.ToObject<int>() ?? 0;

                ToleranceItem de = new ToleranceItem
                {
                    Lower = null,
                    Upper = jobj["dE"].ToObject<float>(),
                    UseForJudgement = use_de != 0
                };
                ToleranceItem[] toleranceItems = new ToleranceItem[3];
                int index = 0;
                if(jobj["items"]!=null)
                {
                    foreach (var item in (JArray)jobj["items"])
                    {
                        toleranceItems[index].UseForJudgement = ((use_items >> index) & 1) != 0;
                        toleranceItems[index].Lower = item["lower"].ToObject<double>();
                        toleranceItems[index].Lower = item["upper"].ToObject<double>();
                    }
                }
                switch (jobj["color_space"].ToColorSpace())
                {
                    case ColorSpace.CIE_LAB:
                        tolerance[ChromaticityField.dL] = toleranceItems[0];
                        tolerance[ChromaticityField.da] = toleranceItems[1];
                        tolerance[ChromaticityField.db] = toleranceItems[2];
                        break;
                    case ColorSpace.HUNTER_LAB:
                        tolerance[ChromaticityField.dHL] = toleranceItems[0];
                        tolerance[ChromaticityField.dHa] = toleranceItems[1];
                        tolerance[ChromaticityField.dHb] = toleranceItems[2];
                        break;
                    case ColorSpace.CIE_XYZ:
                        tolerance[ChromaticityField.dX] = toleranceItems[0];
                        tolerance[ChromaticityField.dY] = toleranceItems[1];
                        tolerance[ChromaticityField.dZ] = toleranceItems[2];
                        break;
                    case ColorSpace.sRGB:
                        tolerance[ChromaticityField.dR] = toleranceItems[0];
                        tolerance[ChromaticityField.dG] = toleranceItems[1];
                        tolerance[ChromaticityField.dB] = toleranceItems[2];
                        break;
                    case ColorSpace.CIE_Yxy:
                        tolerance[ChromaticityField.dY] = toleranceItems[0];
                        tolerance[ChromaticityField.dx] = toleranceItems[1];
                        tolerance[ChromaticityField.dy] = toleranceItems[2];
                        break;
                    case ColorSpace.CIE_LCH:
                        tolerance[ChromaticityField.dL] = toleranceItems[0];
                        tolerance[ChromaticityField.dC] = toleranceItems[1];
                        tolerance[ChromaticityField.dH] = toleranceItems[2];
                        break;
                    case ColorSpace.BETA_xy:
                        tolerance[ChromaticityField.dLuminanceFactor] = toleranceItems[0];
                        tolerance[ChromaticityField.dx] = toleranceItems[1];
                        tolerance[ChromaticityField.dy] = toleranceItems[2];
                        break;
                    case ColorSpace.CIE_LUV:
                        tolerance[ChromaticityField.dL] = toleranceItems[0];
                        tolerance[ChromaticityField.du] = toleranceItems[1];
                        tolerance[ChromaticityField.dv] = toleranceItems[2];
                        break;
                    case ColorSpace.DIN99_LAB:
                        tolerance[ChromaticityField.dL99] = toleranceItems[0];
                        tolerance[ChromaticityField.da99] = toleranceItems[1];
                        tolerance[ChromaticityField.db99] = toleranceItems[2];
                        break;
                }

                switch (jobj["formula"].ToColorDiffFormula())
                {
                    case ColorDiffFormula.CIE1976:
                        tolerance[ChromaticityField.dEab] = de;
                        break;
                    case ColorDiffFormula.CIE1994:
                        tolerance[ChromaticityField.dE94] = de;
                        break;
                    case ColorDiffFormula.CIE2000:
                        tolerance[ChromaticityField.dE00] = de;
                        break;
                    case ColorDiffFormula.CMC21:
                        tolerance[ChromaticityField.dEcmc21] = de;
                        break;
                    case ColorDiffFormula.CMC11:
                        tolerance[ChromaticityField.dEcmc11] = de;
                        break;
                    case ColorDiffFormula.CMC:
                        tolerance[ChromaticityField.dEcmc] = de;
                        break;
                    case ColorDiffFormula.DEuv:
                        tolerance[ChromaticityField.dEuv] = de;
                        break;
                    case ColorDiffFormula.DIN99:
                        tolerance[ChromaticityField.dE99] = de;
                        break;
                    case ColorDiffFormula.DHunterE:
                        tolerance[ChromaticityField.dHE] = de;
                        break;
                }


                return tolerance;
            }
            catch (Exception ex)
            {

                throw;
            }
           
        }


        public static JObject ToJObject(Tolerance tolerance)
        {
            JObject jobj = new JObject();
            int use_de = 0;
            int use_items = 0;
            double de = 1.0;
            ColorDiffFormula formula = ColorDiffFormula.CIE1976;
            ColorSpace colorSpace = ColorSpace.CIE_LAB;

            ToleranceItem[] items = new ToleranceItem[3];

            if (tolerance.ItemKeys.Contains(ChromaticityField.dEab))
            {
                formula = ColorDiffFormula.CIE1976;
                de = tolerance[ChromaticityField.dEab].Upper.Value;
                use_de = tolerance[ChromaticityField.dEab].UseForJudgement ? 1 : 0;
            }
            else if (tolerance.ItemKeys.Contains(ChromaticityField.dE94))
            {
                formula = ColorDiffFormula.CIE1994;
                de = tolerance[ChromaticityField.dE94].Upper.Value;
                use_de = tolerance[ChromaticityField.dE94].UseForJudgement ? 1 : 0;
            }
            else if (tolerance.ItemKeys.Contains(ChromaticityField.dE00))
            {
                formula = ColorDiffFormula.CIE2000;
                de = tolerance[ChromaticityField.dE00].Upper.Value;
                use_de = tolerance[ChromaticityField.dE00].UseForJudgement ? 1 : 0;
            }
            else if (tolerance.ItemKeys.Contains(ChromaticityField.dEcmc11))
            {
                formula = ColorDiffFormula.CMC11;
                de = tolerance[ChromaticityField.dEcmc11].Upper.Value;
                use_de = tolerance[ChromaticityField.dEcmc11].UseForJudgement ? 1 : 0;
            }
            else if (tolerance.ItemKeys.Contains(ChromaticityField.dEcmc21))
            {
                formula = ColorDiffFormula.CMC21;
                de = tolerance[ChromaticityField.dEcmc21].Upper.Value;
                use_de = tolerance[ChromaticityField.dEcmc21].UseForJudgement ? 1 : 0;
            }
            else if (tolerance.ItemKeys.Contains(ChromaticityField.dEcmc))
            {
                formula = ColorDiffFormula.CMC;
                de = tolerance[ChromaticityField.dEcmc].Upper.Value;
                use_de = tolerance[ChromaticityField.dEcmc].UseForJudgement ? 1 : 0;
            }
            else if (tolerance.ItemKeys.Contains(ChromaticityField.dE99))
            {
                formula = ColorDiffFormula.DIN99;
                de = tolerance[ChromaticityField.dE99].Upper.Value;
                use_de = tolerance[ChromaticityField.dE99].UseForJudgement ? 1 : 0;
            }
            else if (tolerance.ItemKeys.Contains(ChromaticityField.dEuv))
            {
                formula = ColorDiffFormula.DEuv;
                de = tolerance[ChromaticityField.dEuv].Upper.Value;
                use_de = tolerance[ChromaticityField.dEuv].UseForJudgement ? 1 : 0;
            }
            else if (tolerance.ItemKeys.Contains(ChromaticityField.dHE))
            {
                formula = ColorDiffFormula.DHunterE;
                de = tolerance[ChromaticityField.dHE].Upper.Value;
                use_de = tolerance[ChromaticityField.dHE].UseForJudgement ? 1 : 0;
            }
            else
            {
                formula = ColorDiffFormula.CIE1976;
                de = 1.0;
                use_de = 1;
            }

            if (tolerance.ItemKeys.Contains(ChromaticityField.dL) &&
                tolerance.ItemKeys.Contains(ChromaticityField.da) &&
                tolerance.ItemKeys.Contains(ChromaticityField.db))
            {
                items[0] = tolerance[ChromaticityField.dL];
                items[1] = tolerance[ChromaticityField.da];
                items[2] = tolerance[ChromaticityField.db];
                colorSpace = ColorSpace.CIE_LAB;
            }
            else if (tolerance.ItemKeys.Contains(ChromaticityField.dX) &&
                tolerance.ItemKeys.Contains(ChromaticityField.dY) &&
                tolerance.ItemKeys.Contains(ChromaticityField.dZ))
            {
                items[0] = tolerance[ChromaticityField.dX];
                items[1] = tolerance[ChromaticityField.dY];
                items[2] = tolerance[ChromaticityField.dZ];
                colorSpace = ColorSpace.CIE_XYZ;
            }
            else if (tolerance.ItemKeys.Contains(ChromaticityField.dY) &&
                tolerance.ItemKeys.Contains(ChromaticityField.dx) &&
                tolerance.ItemKeys.Contains(ChromaticityField.dy))
            {
                items[0] = tolerance[ChromaticityField.dY];
                items[1] = tolerance[ChromaticityField.dx];
                items[2] = tolerance[ChromaticityField.dy];
                colorSpace = ColorSpace.CIE_Yxy;
            }
            else if (tolerance.ItemKeys.Contains(ChromaticityField.dHL) &&
                tolerance.ItemKeys.Contains(ChromaticityField.dHa) &&
                tolerance.ItemKeys.Contains(ChromaticityField.dHb))
            {
                items[0] = tolerance[ChromaticityField.dHL];
                items[1] = tolerance[ChromaticityField.dHa];
                items[2] = tolerance[ChromaticityField.dHb];
                colorSpace = ColorSpace.HUNTER_LAB;
            }
            else if (tolerance.ItemKeys.Contains(ChromaticityField.dL99) &&
                tolerance.ItemKeys.Contains(ChromaticityField.da99) &&
                tolerance.ItemKeys.Contains(ChromaticityField.db99))
            {
                items[0] = tolerance[ChromaticityField.dL99];
                items[1] = tolerance[ChromaticityField.da99];
                items[2] = tolerance[ChromaticityField.db99];
                colorSpace = ColorSpace.DIN99_LAB;
            }
            else if (tolerance.ItemKeys.Contains(ChromaticityField.dLuminanceFactor) &&
                tolerance.ItemKeys.Contains(ChromaticityField.dx) &&
                tolerance.ItemKeys.Contains(ChromaticityField.dy))
            {
                items[0] = tolerance[ChromaticityField.dLuminanceFactor];
                items[1] = tolerance[ChromaticityField.dx];
                items[2] = tolerance[ChromaticityField.dy];
                colorSpace = ColorSpace.BETA_xy;
            }
            else if (tolerance.ItemKeys.Contains(ChromaticityField.dL) &&
                tolerance.ItemKeys.Contains(ChromaticityField.dC) &&
                tolerance.ItemKeys.Contains(ChromaticityField.dH))
            {
                items[0] = tolerance[ChromaticityField.dL];
                items[1] = tolerance[ChromaticityField.dC];
                items[2] = tolerance[ChromaticityField.dH];
                colorSpace = ColorSpace.CIE_LCH;
            }
            else if (tolerance.ItemKeys.Contains(ChromaticityField.dL) &&
                tolerance.ItemKeys.Contains(ChromaticityField.du) &&
                tolerance.ItemKeys.Contains(ChromaticityField.dv))
            {
                items[0] = tolerance[ChromaticityField.dL];
                items[1] = tolerance[ChromaticityField.du];
                items[2] = tolerance[ChromaticityField.dv];
                colorSpace = ColorSpace.CIE_LUV;
            }
            else if (tolerance.ItemKeys.Contains(ChromaticityField.dR) &&
                tolerance.ItemKeys.Contains(ChromaticityField.dG) &&
                tolerance.ItemKeys.Contains(ChromaticityField.dB))
            {
                items[0] = tolerance[ChromaticityField.dR];
                items[1] = tolerance[ChromaticityField.dG];
                items[2] = tolerance[ChromaticityField.db];
                colorSpace = ColorSpace.sRGB;
            }

            jobj["formula"] = ToString(formula);
            jobj["use_de"] = use_de;
            jobj["de"] = de;
            jobj["color_space"] = ToString(colorSpace);

            JArray jarr = new JArray();
            for (int i = 0; i < 3; i++)
            {
                JObject item = new JObject();
                item["lower"] = items[i].Lower.GetValueOrDefault();
                item["upper"] = items[i].Upper.GetValueOrDefault();
                jarr.Add(item);
            }
            jobj["use_items"] = use_items;
            jobj["items"] = jarr;


            return jobj;
        }

        public static Specimen ToSpecimen(this JObject jobj)
        {
            if (jobj == null) return new Specimen(); // 容错：空JObject

            Specimen specimen = new Specimen();
            // 1. 基础字段解析（增加容错，避免null引用）
            specimen.ID = jobj["id"]?.ToObject<int>() ?? 0;
            specimen.Name = jobj["name"]?.ToString()?.Trim(); // 去除空格
            specimen.Temperature = jobj["temperature"]?.ToObject<double>() ?? 25.0;

            // 时间戳解析（增加容错）
            if (jobj.TryGetValue("timestamp", out JToken timestampToken) &&
                timestampToken.Type == JTokenType.Integer)
            {
                specimen.DateTime = new DateTime(1970, 1, 1).AddSeconds(timestampToken.ToObject<long>());
            }

            specimen.IsTransmission = jobj["is_trans"]?.ToObject<bool>() ?? false;
            bool is_lab = jobj["is_lab"]?.ToObject<bool>() ?? false;
            specimen.MeasurementCaliber = jobj["aperture"].ToAperture();
            specimen.UV = jobj["uv"].ToUvFlag();

            StandardIlluminant illuminan = jobj["illuminant"]?.ToIlluminant() ?? StandardIlluminant.D65;
            StandardObserver observer = jobj["observer"]?.ToObserver() ?? StandardObserver.CIE1964;

            // 2. 核心修复：兼容clr（对象/数组）+ 解决ContainsKey返回false的问题
            // 替代ContainsKey：兼容key带隐形字符、大小写问题
            Func<string, bool> HasKey = (key) => jobj.Properties()
                .Any(p => p.Name.Trim().Equals(key, StringComparison.OrdinalIgnoreCase));

            if (is_lab)
            {
                // 解析Lab类型（对象）
                if (HasKey("sci"))
                    specimen.SCI = new ColorData(illuminan, observer, ((JObject)jobj["sci"]).ToLab());
                if (HasKey("sce"))
                    specimen.SCE = new ColorData(illuminan, observer, ((JObject)jobj["sce"]).ToLab());
                if (HasKey("clr"))
                {
                    // 容错：确保clr是JObject类型
                    if (jobj["clr"].Type == JTokenType.Object)
                        specimen.Clr = new ColorData(illuminan, observer, ((JObject)jobj["clr"]).ToLab());
                    else
                        Trace.TraceWarning("clr字段类型不是Object，跳过Lab解析");
                }
                if (HasKey("wbg"))
                    specimen.SCI = new ColorData(illuminan, observer, ((JObject)jobj["wbg"]).ToLab());
                if (HasKey("bbg"))
                    specimen.SCE = new ColorData(illuminan, observer, ((JObject)jobj["bbg"]).ToLab());
            }
            else
            {
                // 解析光谱类型（数组）
                if (HasKey("sci"))
                    specimen.SCI = new ColorData(jobj["sci"].ToSpectrum());
                if (HasKey("sce"))
                    specimen.SCE = new ColorData(jobj["sce"].ToSpectrum());
                if (HasKey("clr"))
                {
                    // 容错：确保clr是JArray类型
                    if (jobj["clr"].Type == JTokenType.Array)
                        specimen.Clr = new ColorData(jobj["clr"].ToSpectrum());
                    else
                        Trace.TraceWarning("clr字段类型不是Array，跳过光谱解析");
                }
                if (HasKey("wbg"))
                    specimen.SCI = new ColorData(jobj["wbg"].ToSpectrum());
                if (HasKey("bbg"))
                    specimen.SCE = new ColorData(jobj["bbg"].ToSpectrum());
            }

            // 3. 兜底赋值
            if (specimen.SCI == null && specimen.Clr != null)
            {
                specimen.SCI = specimen.Clr;
            }

            // 4. Haze/Tt解析（增加核心容错：避免ts/hs为null）
            if (HasKey("Tt") || HasKey("Haze"))
            {
                var ts = (jobj["Tt"] as JArray)?.ToObject<IList<double>>() ?? new List<double>();
                var hs = (jobj["Haze"] as JArray)?.ToObject<IList<double>>() ?? new List<double>();

                // 修复：ts/hs可能为null，先判断Count
                if (ts.Count == 3 && hs.Count == 3) // 同时满足3个元素才解析
                {
                    var HazeDatas = new HazeData[3];
                    var illuminants = new[] { StandardIlluminant.C, StandardIlluminant.A, StandardIlluminant.D65 };
                    for (int i = 0; i < 3; i++)
                    {
                        HazeDatas[i] = new HazeData
                        {
                            Illuminant = illuminants[i],
                            Observer = StandardObserver.CIE1931,
                            Haze = !double.IsNaN(hs[i]) ? hs[i] * 0.01 : double.NaN,
                            Transmittance = !double.IsNaN(ts[i]) ? ts[i] * 0.01 : double.NaN,
                            ReferenceStandard = "ASTM D1003",
                        };
                    }
                    specimen.HazeDatas = HazeDatas;
                }
                else
                {
                    Trace.TraceError($"Invalid Tt or Haze format. Tt长度：{ts.Count}, Haze长度：{hs.Count}");
                }
            }

            return specimen;
        }

        public static StandardSpecimen ToStandard(this JObject jobj)
        {
            StandardSpecimen standard = new StandardSpecimen(jobj.ToSpecimen());
            if((jobj["tol"])!=null)
            {
                Tolerance tolerance = ((JObject)jobj["tol"]).ToTolerance();
                standard.ToleranceSetting = new ToleranceSetting
                {
                    PrimaryTolerance = new Tolerance(tolerance),
                    SecondaryTolerance = new Tolerance(tolerance),
                    TertiaryTolerance = new Tolerance(tolerance)
                };
            }
            return standard;
        }

        public static Spectrum ToSpectrum(this JToken token)
        {
            if (token is JArray)
            {
                var arr = ((JArray)token).ToObject<IList<int>>();
                if (arr.Count == WavelengthRange.Count)
                {
                    Spectrum spectrum = new Spectrum(WavelengthRange);
                    for (int i = 0; i < WavelengthRange.Count; i++)
                    {
                        spectrum.Data[i] = arr[i] * 0.0001;
                    }
                    return spectrum;
                }
            }
            return null;
        }

        public static Spectrum WhiteBoardReflectanceToSpectrum(this JArray jarr)
        {
            var arr = jarr.ToObject<IList<double>>();
            if (arr.Count == WavelengthRange.Count)
            {
                Spectrum spectrum = new Spectrum(WavelengthRange);
                for (int i = 0; i < WavelengthRange.Count; i++)
                {
                    spectrum.Data[i] = arr[i] * 0.01;
                }
                return spectrum;
            }
            return null;
        }

        public static JArray WhiteBoardReflectanceToJArray(Spectrum spectrum)
        {
            var jarr = new JArray();
            for (int wavelen = WavelengthRange.Min; wavelen <= WavelengthRange.Max; wavelen += WavelengthRange.Step)
            {
                jarr.Add(spectrum[wavelen] * 100.0);
            }
            return jarr;
        }

        /// <summary>
        /// 将全局使用的标准光源代号转换回仪器中使用的
        /// </summary>
        /// <param name="illuminant">全局中使用的光源代号</param>
        /// <returns>返回仪器中使用的光源代号</returns>
        public static string ToString(UVFlag uv)
        {
            switch (uv)
            {
                case UVFlag.None:
                    return "Off";
                case UVFlag.On:
                    return "On";
                case UVFlag.NoCut:
                    return "Cut None";
                case UVFlag.Cut400nm:
                    return "Cut 400nm";
                case UVFlag.Cut420nm:
                    return "Cut 420nm";
                case UVFlag.Cut460nm:
                    return "Cut 460nm";
                default:
                    return "Cut None";
            }
        }

        public static string ToString(MeasurementCaliber caliber)
        {
            switch (caliber)
            {
                case MeasurementCaliber.Phi4:
                    return "4mm";
                case MeasurementCaliber.Phi8:
                    return "8mm";
                case MeasurementCaliber.Phi15:
                    return "15mm";
                case MeasurementCaliber.Phi1in:
                    return "25.4mm";
                default: return null;
            }
        }

        public static LensSwitchMode ToLensSwitchMode(this JToken token)
        {
            switch (token.ToString())
            {
                case "Auto": return LensSwitchMode.Auto;
                case "Manual": return LensSwitchMode.Manual;
                default: return LensSwitchMode.Manual;
            }
        }

        public static string ToString(LensSwitchMode mode)
        {
            switch (mode)
            {
                case LensSwitchMode.Manual: return "Manual";
                case LensSwitchMode.Auto: return "Auto";
                default: return "";
            }
        }

        public static string ToString(LensPosition lensPos)
        {
            switch (lensPos)
            {
                case LensPosition.Pos4:
                    return "4mm";
                case LensPosition.Pos8:
                    return "8mm";
                case LensPosition.Pos15:
                    return "15mm";
                case LensPosition.Pos1in:
                    return "25.4mm";
                default: return "";
            }
        }

        public static string ToString(SpecularComponent sc)
        {
            switch (sc)
            {
                case SpecularComponent.SCI: return "SCI";
                case SpecularComponent.SCE: return "SCE";
                case SpecularComponent.SCIAndSCE: return "I+E";
                default: return "";
            }
        }

        public static SpecularComponent ToSpecularComponent(this JToken token)
        {
            switch (token.ToString())
            {
                case "SCI": return SpecularComponent.SCI;
                case "SCE": return SpecularComponent.SCE;
                case "I+E": return SpecularComponent.SCIAndSCE;
                default: return SpecularComponent.Unknown;
            }
        }

        public static JToken ToJToken(ColorData colorData)
        {
            if (colorData.Spectrum != null)
            {
                JArray arr = new JArray();
                for (int w = WavelengthRange.Min; w <= WavelengthRange.Max; w += WavelengthRange.Step)
                {
                    arr.Add((short)Math.Round(colorData.Spectrum[w] * 10000));
                }
                return arr;
            }
            else
            {
                var lab = colorData.Lab(colorData.Formulas[0]);
                JObject obj = new JObject();
                obj["L"] = lab.L;
                obj["a"] = lab.a;
                obj["b"] = lab.b;
                return obj;
            }
        }


        public static JObject ToJObject(Specimen specimen)
        {
            JObject jobj = new JObject();
            jobj["id"] = specimen.ID;
            jobj["timestamp"] = (Int64)specimen.DateTime.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
            jobj["temperature"] = specimen.Temperature;
            jobj["name"] = specimen.Name;
            jobj["is_trans"] = specimen.IsTransmission;
            jobj["geometry"] = specimen.OpticalStruct;
            jobj["aperture"] = ToString(specimen.MeasurementCaliber);
            jobj["lens_pos"] = ToString(specimen.LensPosition);

            bool isSpectrum = (specimen.SCI?.Spectrum != null) ||
                (specimen.SCE?.Spectrum != null) || (specimen.Clr?.Spectrum != null);
            if (!isSpectrum)
            {
                jobj["is_lab"] = true;
                Formula formula;
                if (specimen.SCI != null && specimen.SCI.Spectrum == null)
                {
                    formula = specimen.SCI.Formulas[0];
                }
                else if (specimen.SCE != null && specimen.SCE.Spectrum == null)
                {
                    formula = specimen.SCE.Formulas[0];
                }
                else if (specimen.Clr != null && specimen.Clr.Spectrum == null)
                {
                    formula = specimen.Clr.Formulas[0];
                }
                else
                {
                    formula = new Formula { Illuminant = StandardIlluminant.D65, Observer = StandardObserver.CIE1964 };
                }
                jobj["illuminant"] = formula.Illuminant.ToString();
                jobj["observer"] = formula.Observer == StandardObserver.CIE1931 ? 2 : 10;
            }
            else
            {
                jobj["is_lab"] = false;
                jobj["illuminant"] = "D65";
                jobj["observer"] = 10;
            }

            if (specimen.SpecularComponent != SpecularComponent.Unknown)
            {
                jobj["sc_mode"] = ToString(specimen.SpecularComponent);
            }
            if (specimen.SCI != null)
            {
                jobj["sci"] = ToJToken(specimen.SCI);
            }
            if (specimen.SCE != null)
            {
                jobj["sce"] = ToJToken(specimen.SCE);
            }
            if (specimen.Clr != null)
            {
                jobj["clr"] = ToJToken(specimen.Clr);
            }

            if (specimen is StandardSpecimen std)
            {
                if (std.ToleranceSetting != null && std.ToleranceSetting.PrimaryTolerance != null)
                    jobj["tol"] = ToJObject(std.ToleranceSetting.PrimaryTolerance);
            }


            return jobj;
        }

        public static ProductInformation ToProductInformation(this JObject jobj)
        {
            ProductInformation info = new ProductInformation();
            info.InternalModel = jobj["model"].ToString();
            info.ModelName = jobj["model_name"].ToString();
            info.Language = jobj["language"].ToString();
            info.SN = jobj["sn"].ToString();
            info.HardwareVersion = jobj["hardware_version"].ToString();
            return info;
        }

        public static JObject ToJOjbect(this ProductInformation productInfo)
        {
            if (productInfo == null)
            {
                throw new ArgumentNullException();
            }

            JObject jobj = new JObject();
            jobj["model"] = productInfo.ModelName;
            jobj["model_name"] = productInfo.ModelName;
            jobj["language"] = productInfo.Language;
            jobj["sn"] = productInfo.SN;
            jobj["hardware_version"] = productInfo.HardwareVersion;
            return jobj;
        }

        public static SpectrometerParameter ToSpectrometerParameter(this JObject jobj)
        {
            var parameter = new SpectrometerParameter();
            parameter.SN = jobj["sn"].ToString();
            var jarr = (JArray)jobj["data"];
            parameter.Coefficients = (from item in jarr select item.ToObject<double>()).ToArray();
            return parameter;
        }

        public static JObject ToJObject(this SpectrometerParameter parameter)
        {
            JObject jobj = new JObject();
            jobj["sn"] = parameter.SN;
            jobj["data"] = new JArray(parameter.Coefficients);
            return jobj;
        }
    }
}
