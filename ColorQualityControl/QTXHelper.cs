using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Configuration;
using System.Text;
using System.Text.RegularExpressions;
using ThreeNH.ColorAlgorithm;
using ThreeNH.ColorSpecimen;
using ThreeNH.DataModel;
using ThreeNH.Optical;

namespace ThreeNH.ColorQualityControl
{
    public static class DateHelper
    {
        public static double ToUnixTimestamp(this DateTime dateTime)
        {
            return (TimeZoneInfo.ConvertTimeToUtc(dateTime) -
                     new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc)).TotalSeconds;
        }

        public static DateTime ConvertUinxTimeStampToDateTime(this long time_t)
        {
            return new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc) + new TimeSpan(time_t * 10000000);
        }
    }

    /// <summary>
    /// 导出QTX文件的相关函数
    /// </summary>
    public class QTXHelper
    {
        public class Section
        {
            public Section(string name = null)
            {
                Name = name;
                Items = new Dictionary<string, string>();
            }

            public string Name { get; set; }
            public Dictionary<string, string> Items { get; set; }

            public string this[string key]
            {
                get { return Items[key]; }
                set { Items[key] = value; }
            }

            public bool Contains(string key)
            {
                return Items.ContainsKey(key);
            }
        }

        public class StandardSection : Section
        {
            public List<Section> TestSections { get; set; }
        }


        static void ParseViewingString(string viewString,
            out SpecularComponent sc,
            out MeasurementCaliber caliber,
            out string opticalStructure,
            out UVFlag uv)
        {
            Dictionary<string, SpecularComponent> SCs = new Dictionary<string, SpecularComponent>
            {
                {"SCI", SpecularComponent.SCI }, {"SCE", SpecularComponent.SCE }
            };
            Dictionary<string, MeasurementCaliber> calibers = new Dictionary<string, MeasurementCaliber>
            {
                {"MAV", MeasurementCaliber.Phi8 }, {"SAV", MeasurementCaliber.Phi4 }
            };
            Dictionary<string, string> opticalStructures = new Dictionary<string, string>
            {
                {"D/8", "d/8" }, {"8/D", "8/d" }, { "45/0", "45/0" }, {"0/45", "0/45" }
            };
            Dictionary<string, UVFlag> UVs = new Dictionary<string, UVFlag>
            {
                {"UV INC", UVFlag.On },
                { "UVINC", UVFlag.On },
                { "UV 400", UVFlag.On },
                { "UV 420", UVFlag.On },
                { "UV 440", UVFlag.On },
                { "UV 460", UVFlag.On },
                { "UV CAL", UVFlag.On },
                { "UV Exc", UVFlag.None },
                { "NONE", UVFlag.None }
            };

            viewString = viewString.TrimEnd(',').ToUpper();

            sc = SpecularComponent.Unknown;
            foreach (var key in SCs.Keys)
            {
                if (viewString.Contains(key))
                    sc = SCs[key];
            }

            caliber = MeasurementCaliber.Unknown;
            foreach (var key in calibers.Keys)
            {
                if (viewString.Contains(key))
                    caliber = calibers[key];
            }

            opticalStructure = null;
            foreach (var key in opticalStructures.Keys)
            {
                if (viewString.Contains(key))
                    opticalStructure = opticalStructures[key];
            }

            uv = UVFlag.None;
            foreach (var key in UVs.Keys)
            {
                if (viewString.Contains(key))
                    uv = UVs[key];
            }
        }

        /// <summary>
        /// QTX 导出写的是视角度数 "2"/"10"，不能用 Enum.TryParse：数字串会被当成枚举底层整型，
        /// "10" 会变成非法枚举 10 而非 CIE1964，导致 ColorData.XYZ(formula) 按 CIE1964 查不到已存 XYZ。
        /// </summary>
        static StandardObserver ParseQtxObserver(string observerText, StandardObserver defaultObserver)
        {
            if (string.IsNullOrWhiteSpace(observerText))
                return defaultObserver;

            observerText = observerText.TrimEnd(',').Trim();
            if (observerText == "2" || observerText == "2\u00B0")
                return StandardObserver.CIE1931;
            if (observerText == "10" || observerText == "10\u00B0")
                return StandardObserver.CIE1964;

            StandardObserver observer;
            if (Enum.TryParse(observerText, true, out observer)
                && Enum.IsDefined(typeof(StandardObserver), observer)
                && observer != StandardObserver.Unknown)
            {
                return observer;
            }
            return defaultObserver;
        }

        static SpecularComponent GetSectionViewing(Section section, string viewingKey, SpecularComponent defaultSc = SpecularComponent.SCI)
        {
            if (!section.Items.ContainsKey(viewingKey))
                return defaultSc;

            ParseViewingString(section[viewingKey], out var sc, out _, out _, out _);
            return sc == SpecularComponent.Unknown ? defaultSc : sc;
        }

        static void AssignColorData(Specimen specimen, SpecularComponent sc, ColorData clr)
        {
            if (sc == SpecularComponent.SCI)
                specimen.SCI = clr;
            else if (sc == SpecularComponent.SCE)
                specimen.SCE = clr;
            else
                specimen.Clr = clr;
        }

        static void MergeStandardSpecimen(StandardSpecimen target, StandardSpecimen source)
        {
            if (source.SCI != null)
                target.SCI = source.SCI;
            if (source.SCE != null)
                target.SCE = source.SCE;
            if (source.Clr != null)
                target.Clr = source.Clr;

            if (source.TestSpecimens == null || source.TestSpecimens.Count == 0)
                return;

            if (target.TestSpecimens == null)
                target.TestSpecimens = new List<TestSpecimen>();

            var tests = new Dictionary<string, TestSpecimen>();
            foreach (var test in target.TestSpecimens)
            {
                if (test != null)
                    tests[test.Name] = test;
            }

            foreach (var test in source.TestSpecimens)
            {
                if (test == null)
                    continue;

                if (tests.TryGetValue(test.Name, out var existing))
                {
                    if (test.SCI != null)
                        existing.SCI = test.SCI;
                    if (test.SCE != null)
                        existing.SCE = test.SCE;
                    if (test.Clr != null)
                        existing.Clr = test.Clr;
                }
                else
                {
                    tests[test.Name] = test;
                }
            }

            target.TestSpecimens = tests.Values.ToList();
        }

        static StandardSpecimen ToStandard(StandardSection section)
        {
            StandardSpecimen std = new StandardSpecimen();
            try
            {
                std.Name = section["STD_NAME"];
                long lval;
                if (long.TryParse(section["STD_DATETIME"].TrimEnd(','), out lval))
                {
                    std.DateTime = lval.ConvertUinxTimeStampToDateTime();
                }
                //原有默认设置为 SpecularComponent sc = SpecularComponent.Unknown,
                //如果按照Unknown,导入数据后，如果将此样品作为标样，仪器上的为sci或sce
                //此标样数据就会变为null，所以把此光源默认设置为sci __2025.2.28
                SpecularComponent sc = SpecularComponent.SCI;
                MeasurementCaliber caliber = MeasurementCaliber.Unknown;
                UVFlag uv = UVFlag.None;
                string opticalStructure = null;
                if (section.Items.ContainsKey("STD_VIEWING"))
                    ParseViewingString(section["STD_VIEWING"], out sc, out caliber, out opticalStructure, out uv);
                std.UV = uv;
                std.OpticalStruct = opticalStructure;
                std.MeasurementCaliber = caliber;

                ColorData clr = new ColorData();

                int reflpint;
                bool ival = int.TryParse(section["STD_REFLPOINTS"].TrimEnd(','),out reflpint);
                // 如果为-1表示是XYZ值
                if (!ival|| reflpint == -1)
                {
                    double X = double.Parse(section["STD_X"].TrimEnd(','));
                    double Y = double.Parse(section["STD_Y"].TrimEnd(','));
                    double Z = double.Parse(section["STD_Z"].TrimEnd(','));
                    StandardIlluminant illuminant = StandardIlluminant.D65;
                    StandardObserver observer = StandardObserver.CIE1964;

                    if (section.Items.ContainsKey("STD_ILLUMINANT"))
                        Enum.TryParse(section["STD_ILLUMINANT"].TrimEnd(','), out illuminant);
                    if (section.Items.ContainsKey("STD_OBSERVER"))
                        observer = ParseQtxObserver(section["STD_OBSERVER"], observer);
                    clr.Set(observer, illuminant, new CIEXYZ(X, Y, Z));
                }
                else
                {
                    int interval = int.Parse(section["STD_REFLINTERVAL"].TrimEnd(','));
                    int low = int.Parse(section["STD_REFLLOW"].TrimEnd(','));
                    WavelengthRange range = new WavelengthRange(low, low + interval * (reflpint - 1), interval);
                    List<double> R = new List<double>();
                    foreach (var sVal in section["STD_R"].TrimEnd(',').Split(','))
                    {
                        R.Add(double.Parse(sVal) / 100.0);
                    }
                    clr.Spectrum = new Spectrum(range, R.ToArray());
                }
                // 与 ToTestSpecimen 一致：按 VIEWING 写入对应通道，避免 SCI/SCE 段互相覆盖
                AssignColorData(std, sc, clr);


                if (section.Contains("STD_INST_TYPE"))
                    std.Instrument = section["STD_INST_TYPE"].TrimEnd(',');
                if (section.Contains("STD_INSTRUMENT_SERIAL_NO"))
                    std.InstrumentSN = section["STD_INSTRUMENT_SERIAL_NO"].TrimEnd(',');
            }
            catch
            {
                return null;
            }

            return std;
        }

        static TestSpecimen ToTestSpecimen(Section section, out string stdName)
        {
            TestSpecimen bat = new TestSpecimen();
            stdName = null;

            try
            {
                bat.Name = section["BAT_NAME"];
                stdName = section["STD_NAME"];

                long lval;
                if (long.TryParse(section["BAT_DATETIME"].TrimEnd(','), out lval))
                {
                    bat.DateTime = lval.ConvertUinxTimeStampToDateTime();
                }

                SpecularComponent sc = SpecularComponent.Unknown;
                MeasurementCaliber caliber = MeasurementCaliber.Unknown;
                UVFlag uv = UVFlag.None;
                string opticalStructure = null;
                if (section.Items.ContainsKey("BAT_VIEWING"))
                    ParseViewingString(section["BAT_VIEWING"], out sc, out caliber, out opticalStructure, out uv);
                bat.UV = uv;
                bat.OpticalStruct = opticalStructure;
                bat.MeasurementCaliber = caliber;

                ColorData clr = new ColorData();
                int batreflpoints;
                bool ival = int.TryParse(section["BAT_REFLPOINTS"].TrimEnd(','),out batreflpoints);
                // 如果为-1表示是XYZ值
                if (!ival|| batreflpoints==-1)
                {
                    double X = double.Parse(section["BAT_X"].TrimEnd(','));
                    double Y = double.Parse(section["BAT_Y"].TrimEnd(','));
                    double Z = double.Parse(section["BAT_Z"].TrimEnd(','));
                    StandardIlluminant illuminant = StandardIlluminant.D65;
                    StandardObserver observer = StandardObserver.CIE1964;

                    if (section.Items.ContainsKey("BAT_ILLUMINANT"))
                        Enum.TryParse(section["BAT_ILLUMINANT"].TrimEnd(','), out illuminant);
                    if (section.Items.ContainsKey("BAT_OBSERVER"))
                        observer = ParseQtxObserver(section["BAT_OBSERVER"], observer);

                    clr.Set(observer, illuminant, new CIEXYZ(X, Y, Z));
                }
                else
                {
                    int batreflinterval;
                    int bat_reflow;
                    bool interval = int.TryParse(section["BAT_REFLINTERVAL"].TrimEnd(','),out batreflinterval);
                    bool low = int.TryParse(section["BAT_REFLLOW"].TrimEnd(','), out bat_reflow);
                    WavelengthRange range = new WavelengthRange(bat_reflow, bat_reflow + batreflinterval * (batreflpoints - 1), batreflinterval);
                    List<double> R = new List<double>();
                    var bat_r = section["BAT_R"].TrimEnd(',');
                    if(bat_r!="")
                    {
                        foreach (var sVal in section["BAT_R"].TrimEnd(',').Split(','))
                        {
                            R.Add(double.Parse(sVal) / 100.0);
                        }

                        clr.Spectrum = new Spectrum(range, R.ToArray());
                    }
                }

                if (sc == SpecularComponent.SCI)
                    bat.SCI = clr;
                else if (sc == SpecularComponent.SCE)
                    bat.SCE = clr;
                else
                    bat.Clr = clr;

                if (section.Contains("BAT_INST_TYPE"))
                    bat.Instrument = section["BAT_INST_TYPE"].TrimEnd(',');
                if (section.Contains("BAT_INSTRUMENT_SERIAL_NO"))
                    bat.InstrumentSN = section["BAT_INSTRUMENT_SERIAL_NO"].TrimEnd(',');
            }
            catch
            {
                return null;
            }

            return bat;
        }

        static string ReflToString(double[] refl)
        {
            return string.Join(",", from x in refl select (x * 100.0).ToString("F6"));
        }

        static string GetViewingString(Specimen specimen, SpecularComponent sc)
        {
            List<string> list = new List<string>();
            list.Add("%R");     // 反射数据

            // 口径
            switch (specimen.MeasurementCaliber)
            {
                case MeasurementCaliber.Phi4: list.Add("MAV"); break;
                case MeasurementCaliber.Phi8: list.Add("LAV"); break;
                default: list.Add("NORMAL"); break;
            }

            // SCI和SCE
            switch (sc)
            {
                case SpecularComponent.SCI: list.Add("SCI"); break;
                case SpecularComponent.SCE: list.Add("SCE"); break;
                default: break;
            }

            // 几何结构
            list.Add(specimen.OpticalStruct != null ? specimen.OpticalStruct : " ");

            // UV成分
            list.Add(specimen.UV == UVFlag.On ? "UV Inc" : "None");

            return string.Join(" ", list);
        }

        /// <summary>
        /// 写标样数据
        /// </summary>
        /// <param name="standard">标样</param>
        /// <param name="withSample">是否同时写试样数据</param>
        public static StandardSection ToSection(StandardSpecimen standard, SpecularComponent sc, string nameSuffix = null)
        {
            if (standard.GetColorData(sc) == null)
                return null;

            StandardSection section = new StandardSection();

            string stdName = standard.Name + (string.IsNullOrEmpty(nameSuffix) ? "" : nameSuffix);
            section["STD_NAME"] = stdName;
            section["STD_DATETIME"] = ((long)standard.DateTime.ToUnixTimestamp()).ToString() + ",";

            ColorData clr = standard.GetColorData(sc);
            if (clr.Spectrum != null)
            {
                section["STD_REFLPOINTS"] = clr.Spectrum.Range.Count.ToString() + ",";
                section["STD_REFLINTERVAL"] = clr.Spectrum.Range.Step.ToString() + ",";
                section["STD_REFLLOW"] = clr.Spectrum.Range.Min.ToString() + ",";
                section["STD_R"] = ReflToString(clr.Spectrum.Data) + ",";
            }
            else
            {
                section["STD_REFLPOINTS"] = "-1";
                section["STD_REFLINTERVAL"] = "";
                section["STD_REFLLOW"] = "";
                section["STD_R"] = "";

                var formula = clr.Formulas[0];
                var xyz = clr.XYZ(formula);
                section["STD_X"] = xyz.X.ToString();
                section["STD_Y"] = xyz.Y.ToString();
                section["STD_Z"] = xyz.Z.ToString();
                section["STD_ILLUMINANT"] = formula.Illuminant.ToString() + ",";
                section["STD_OBSERVER"] = formula.Observer == StandardObserver.CIE1931 ? "2" : "10";
            }
            section["STD_VIEWING"] = GetViewingString(standard, sc).ToString();
            if (!string.IsNullOrWhiteSpace(standard.Instrument))
            {
                section["STD_INST_TYPE"] = standard.Instrument.ToString() + ",";
            }

            if (!string.IsNullOrWhiteSpace(standard.InstrumentSN))
            {
                section["STD_INSTRUMENT_SERIAL_NO"] = standard.InstrumentSN.ToString() + ",";
            }

            if (standard.TestSpecimens != null)
            {
                section.TestSections = new List<Section>();

                foreach (var test in standard.TestSpecimens)
                {
                    var testSection = ToSection(stdName, test, sc, nameSuffix);
                    if (testSection != null)
                    {
                        section.TestSections.Add(testSection);
                    }
                }
            }

            return section;
        }

        public static Section ToSection(string stdName, TestSpecimen test, SpecularComponent sc, string nameSuffix = null)
        {
            if (test.GetColorData(sc) == null)
                return null;

            Section section = new Section();

            section["STD_NAME"] = stdName + (string.IsNullOrEmpty(nameSuffix) ? "" : nameSuffix);
            section["BAT_NAME"] = test.Name;
            section["BAT_DATETIME"] = ((long)test.DateTime.ToUnixTimestamp()).ToString() + ",";

            ColorData clr = test.GetColorData(sc);
            if (clr.Spectrum != null)
            {
                section["BAT_REFLPOINTS"] = clr.Spectrum.Range.Count.ToString() + ",";
                section["BAT_REFLINTERVAL"] = clr.Spectrum.Range.Step.ToString() + ",";
                section["BAT_REFLLOW"] = clr.Spectrum.Range.Min.ToString() + ",";
                section["BAT_R"] = ReflToString(clr.Spectrum.Data) + ",";
            }
            else
            {
                section["BAT_REFLPOINTS"] = "";
                section["BAT_REFLINTERVAL"] = "";
                section["BAT_REFLLOW"] = "";
                section["BAT_R"] = "";

                var formula = clr.Formulas[0];
                var xyz = clr.XYZ(formula);
                section["BAT_X"] = xyz.X.ToString();
                section["BAT_Y"] = xyz.Y.ToString();
                section["BAT_Z"] = xyz.Z.ToString();
                section["BAT_ILLUMINANT"] = formula.Illuminant.ToString() + ",";
                section["BAT_OBSERVER"] = formula.Observer == StandardObserver.CIE1931 ? "2" : "10";
            }
            section["BAT_VIEWING"] = GetViewingString(test, sc).ToString();
            if (!string.IsNullOrWhiteSpace(test.Instrument))
            {
                section["BAT_INST_TYPE"] = test.Instrument.ToString() + ",";
            }

            if (!string.IsNullOrWhiteSpace(test.InstrumentSN))
            {
                section["BAT_INSTRUMENT_SERIAL_NO"] = test.InstrumentSN.ToString() + ",";
            }

            return section;
        }

        public static void Save(string fileName, SpecimenDatabase database,
            SpecularComponent sc = SpecularComponent.SCIAndSCE)
        {
            Save(new StreamWriter(fileName), database, sc);
        }

        public static void Save(StreamWriter writer, SpecimenDatabase database, SpecularComponent sc = SpecularComponent.SCIAndSCE)
        {
            List<Section> sections = new List<Section>();
            foreach (var std in database.StandardSpecimens)
            {
                if(std.SCE!=null)
                {
                    var stdSection = ToSection(std, SpecularComponent.SCE);
                    if (stdSection != null)
                        sections.Add(stdSection);
                }
                if(std.SCI!=null)
                {
                    var stdSection = ToSection(std, SpecularComponent.SCI );
                    if (stdSection != null)
                        sections.Add(stdSection);
                }
                else
                {
                    var stdSection = ToSection(std, std.SpecularComponent);
                    if (stdSection != null)
                        sections.Add(stdSection);
                }
            }
            Save(writer, sections);
        }

        public static void Save(StreamWriter writer, List<Section> sections)
        {
            int stdIndex = 0;
            foreach (var section in sections)
            {
                if (section is StandardSection)
                {
                    var stdSection = section as StandardSection;

                    writer.WriteLine("[STANDARD_DATA {0}]", stdIndex++);
                    foreach (var item in section.Items)
                    {
                        writer.WriteLine("{0}={1}", item.Key, item.Value);
                    }

                    if (stdSection.TestSections == null || stdSection.TestSections.Count == 0)
                        continue;

                    int testIndex = 0;
                    foreach (var testSection in stdSection.TestSections)
                    {
                        StringBuilder buffer = new StringBuilder();
                        buffer.AppendLine($"[BATCH_DATA {testIndex++}]");
                        foreach (var item in testSection.Items)
                        {
                            buffer.AppendLine($"{item.Key}={item.Value}");
                        }
                        writer.WriteLine(buffer.ToString());
                    }
                }
                else
                {
                    writer.WriteLine("[{0}]", section.Name);
                    foreach (var item in section.Items)
                    {
                        writer.WriteLine("{0}={1}", item.Key, item.Value);
                    }
                }
            }
            writer.Flush();
        }

        public static List<Section> Load(string qtxFileName)
        {
            return Load(File.OpenText(qtxFileName));
        }

        public static void Load(string qtxFileName, SpecimenDatabase database)
        {
            Load(File.OpenText(qtxFileName), database);
        }

        /// <summary>
        /// 加库QTX文件
        /// </summary>
        /// <param name="reader">QTX文件流</param>
        /// <returns>QTX文件的所有段</returns>
        public static List<Section> Load(StreamReader reader)
        {
            Regex rxSectionName = new Regex(@"^\[(.+?)\]$");
            Regex rxStdSectionName = new Regex(@"^\[STANDARD_DATA(\s+\d+)?\]$");
            Regex rxTestSectionName = new Regex(@"^\[BATCH_DATA(\s+\d+)?\]$");
            Regex rxKeyValue = new Regex(@"^([\w_-]+)\s*=(.*)$");


            Section curSection = null;
            StandardSection stdSection = null;

            List<Section> sections = new List<Section>();
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                line = Regex.Replace(line, @"[\x00-\x08\x0B\x0C\x0E-\x1F]", "");
                line = line.Trim();
                if (string.IsNullOrEmpty(line))
                    continue;
                var m = rxKeyValue.Match(line);
                if (m.Success && curSection != null)
                {
                    curSection[m.Groups[1].Value] = m.Groups[2].Value;
                }
                else if (rxStdSectionName.IsMatch(line))
                {
                    curSection = stdSection = new StandardSection();
                    sections.Add(curSection);
                }
                else if (rxTestSectionName.IsMatch(line))
                {
                    curSection = new Section();
                    if (stdSection != null)
                    {
                        if (stdSection.TestSections == null)
                            stdSection.TestSections = new List<Section>();
                        stdSection.TestSections.Add(curSection);
                    }
                    else
                    {
                        curSection.Name = "BATCH_DATA";
                        sections.Add(curSection);
                    }
                }
                else
                {
                    var _ = rxSectionName.Match(line);
                    if (_.Success)
                    {
                        curSection = new Section();
                        curSection.Name = _.Groups[1].Value;
                        sections.Add(curSection);
                    }
                }
            }

            return sections;
        }

        /// <summary>
        /// 将QTX文件加载到database中
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="database"></param>
        public static void Load(StreamReader reader, SpecimenDatabase database)
        {
            Dictionary<string, StandardSpecimen> standards = new Dictionary<string, StandardSpecimen>();
            Dictionary<string, StandardSpecimen> sceStandards = new Dictionary<string, StandardSpecimen>();

            foreach (var section in Load(reader))
            {
                if (section is StandardSection)
                {
                    var stdSection = section as StandardSection;
                    var std = ToStandard(stdSection);
                    if (std != null)
                    {
                        if (stdSection.TestSections != null)
                        {
                            std.TestSpecimens = new List<TestSpecimen>();
                            foreach (var testSection in stdSection.TestSections)
                            {
                                string stdName;
                                var test = ToTestSpecimen(testSection, out stdName);
                                std.TestSpecimens.Add(test);
                            }
                        }

                        // 导出时 SCI/SCE 同名无 .SCE 后缀，须按 VIEWING 或后缀归并而非覆盖
                        var viewingSc = GetSectionViewing(stdSection, "STD_VIEWING");
                        string baseName;
                        bool isLegacySceName;
                        if (std.Name.EndsWith(".SCE"))
                        {
                            baseName = std.Name.Substring(0, std.Name.Length - 4);
                            isLegacySceName = true;
                            std.Name = baseName;
                        }
                        else
                        {
                            baseName = std.Name;
                            isLegacySceName = false;
                        }

                        if (isLegacySceName || viewingSc == SpecularComponent.SCE)
                        {
                            if (standards.ContainsKey(baseName))
                                MergeStandardSpecimen(standards[baseName], std);
                            else
                                sceStandards[baseName] = std;
                        }
                        else if (standards.ContainsKey(baseName))
                        {
                            MergeStandardSpecimen(standards[baseName], std);
                        }
                        else
                        {
                            standards[baseName] = std;
                        }
                    }
                }
            }

            // 合并仅含 SCE 段、尚无同名 SCI 段的标样（兼容旧版 .SCE 后缀命名）
            foreach (var item in sceStandards)
            {
                if (standards.ContainsKey(item.Key))
                {
                    MergeStandardSpecimen(standards[item.Key], item.Value);
                }
                else
                {
                    standards[item.Key] = item.Value;
                }
            }

            foreach (var std in standards.Values)
            {
                var tests = std.TestSpecimens;
                std.TestSpecimens = null;

                database.AddStandard(std);
                if (tests != null)
                {
                    foreach (var test in tests)
                    {
                        if (test != null)
                            database.AddTest(test, std);
                    }
                }
            }
        }
    }
}
