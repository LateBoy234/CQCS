using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml;
using ThreeNH.ColorAlgorithm;
using ThreeNH.ColorQualityControl.Adapter;
using ThreeNH.ColorSpecimen;
using ThreeNH.DataModel;
using ThreeNH.Optical;
using ThreeNH.Helper;
using ThreeNH.ColorQualityControl.Reports;

namespace ThreeNH.ColorQualityControl
{
    public class SpecimenDatabase
    {
        struct Key
        {
            public Key(SpecularComponent sc, StandardObserver observer, StandardIlluminant illuminant)
            {
                SpecularComponent = sc;
                Observer = observer;
                Illuminant = illuminant;
            }

            public SpecularComponent SpecularComponent { get; }
            public StandardObserver Observer { get; }
            public StandardIlluminant Illuminant { get; }
            public ObserverIlluminantPair ObserverIlluminant
            {
                get { return new ObserverIlluminantPair { Illuminant = Illuminant, Observer = Observer }; }
            }

            public static bool operator ==(Key left, Key right)
            {
                return left.SpecularComponent == right.SpecularComponent &&
                    left.Observer == right.Observer &&
                    left.Illuminant == right.Illuminant;
            }
            public static bool operator !=(Key left, Key right)
            {
                return left.SpecularComponent != right.SpecularComponent ||
                    left.Observer != right.Observer ||
                    left.Illuminant != right.Illuminant;
            }
            public override bool Equals(object obj)
            {
                if (obj is Key)
                {
                    Key other = (Key)obj;
                    return this == other;
                }
                return false;
            }
            public override int GetHashCode()
            {
                return ((int)SpecularComponent << 16) | ((int)Observer << 8) | (int)Illuminant;
            }
        }

        Dictionary<Key, Formula> _formulas = new Dictionary<Key, Formula>();
        Dictionary<Key, Dictionary<StandardSpecimen, StandardSpecimenAdapter>> _standardAdapters =
            new Dictionary<Key, Dictionary<StandardSpecimen, StandardSpecimenAdapter>>();
        Dictionary<Key, Dictionary<TestSpecimen, TestSpecimenAdapter>> _testsAdapters =
            new Dictionary<Key, Dictionary<TestSpecimen, TestSpecimenAdapter>>();
        List<StandardSpecimen> _standardList = new List<StandardSpecimen>();
        List<TestSpecimen> _testList = new List<TestSpecimen>();
        private bool _isModified;
        private ObserverAndIlluminantSetting _observerAndIlluminantSetting;
        private ChromaticitySetting _systemChromaticitySetting;


        public SpecimenDatabase()
        {
            ReportSetting = new ReportSetting();
            Init();
        }

        private void Init()
        {
            // 判定结果显示设置
            JudgementSetting = new JudgementSetting();

            // 标样名称生成规则
            StandardNamgeGenerator = new SpecimenNameGenerator("s{Index}");
            // 试样移称生成规则
            TestNameGenerator = new SpecimenNameGenerator("t{Index}");

            // 设置默认的光源观察者角度
            ObserverAndIlluminantSetting = new ObserverAndIlluminantSetting
            {
                PrimaryObserver = StandardObserver.CIE1964,
                PrimaryIlluminant = StandardIlluminant.D65,
                SecondaryObserver = StandardObserver.CIE1964,
                SecondaryIlluminant = StandardIlluminant.A,
                TertiaryObserver = StandardObserver.CIE1964,
                TertiaryIlluminant = StandardIlluminant.F2
            };

            // 设置默认显示的项
            SystemChromaticitySetting = new ChromaticitySetting();
            SystemChromaticitySetting.DefaultFields(ColorSpace.CIE_LAB, ColorDiffFormula.CIE1976);

            //设置默认容差设置
            SystemToleranceSetting = new SystemToleranceSetting()
            {
                PrimaryTolerance = new Tolerance(),
                SecondaryTolerance = new Tolerance(),
                TertiaryTolerance = new Tolerance(),
                IsFromSystemDefault = false
            };
            // 设置默认的容差项
            SystemToleranceSetting.ItemKeys = SystemChromaticitySetting.ColorDiffFields;

            IsModified = false;
            FilePath = null;

            IsAutoNaming = true;

        }

        public string FilePath { get; set; }

        // 最近一次更改的时间
        public DateTime LatestModificationTimestamp { get; set; } = DateTime.Now;

        public bool IsModified
        {
            get { return _isModified; }
            set
            {
                _isModified = value;
                LatestModificationTimestamp = DateTime.Now;
            }
        }

        public ReportSetting ReportSetting { get; set; }

        private static XmlElement SpecimenTagToXmlElement(XmlDocument doc, string nodeName, object tag)
        {
            var elem = doc.CreateElement(nodeName);
            elem.SetAttribute("Type", tag.GetType().ToString());
            if (tag is Tuple<ColorData, ColorData>)
            {
                var t = (Tuple<ColorData, ColorData>)tag;
                elem.AppendChild(t.Item1.ToXmlElement(doc, "Item1"));
                elem.AppendChild(t.Item2.ToXmlElement(doc, "Item2"));
            }
            else if (tag is double[] dValues)
            {
                elem.AppendChild(doc.CreateTextNode(string.Join(";",
                    Array.ConvertAll(dValues, x => x.ToString(CultureInfo.InvariantCulture)))));
            }
            else
            {
                elem.AppendChild(doc.CreateTextNode(tag.ToString()));
            }

            return elem;
        }

        private static object LoadSpecimenTagFromXmlElement(XmlElement element)
        {
            var typeName = element.GetAttribute("Type");
            if (string.IsNullOrWhiteSpace(typeName))
            {
                return null;
            }

            if (typeName == typeof(Tuple<ColorData, ColorData>).ToString())
            {
                ColorData item1 = new ColorData();
                ColorData item2 = new ColorData();
                try
                {
                    item1.LoadXmlElement(element.GetElementsByTagName("Item1")[0] as XmlElement);
                    item2.LoadXmlElement(element.GetElementsByTagName("Item2")[0] as XmlElement);
                    return new Tuple<ColorData, ColorData>(item1, item2);
                }
                catch
                {
                    return null;
                }
            }
            else if (typeName == typeof(double[]).ToString())
            {
                var culture = CultureInfo.InvariantCulture;
                var style = NumberStyles.Float | NumberStyles.AllowThousands;

                Regex re = new Regex("[;,\\s]+");
                var strItems = re.Split(element.InnerText);
                if (strItems.Length == 0)
                    return null;

                try
                {
                    return Array.ConvertAll(strItems, s => double.Parse(s, style, culture));
                }
                catch (Exception)
                {
                    return null;
                }
            }

            var text = element.InnerText;
            try
            {
                var type = Type.GetType(typeName);
                if (type.IsEnum)
                {
                    return Enum.Parse(type, text);
                }

                var method = type.GetMethod("Parse", BindingFlags.Static);
                if (method != null)
                    return method.Invoke(null, new Object[] { text });
                return null;
            }
            catch
            {
                return null;
            }
        }

        public XmlElement ToXmlElement(XmlDocument doc, string nodeName)
        {
            var root = doc.CreateElement(nodeName);
            root.SetAttribute("Version", "0.3");

            var configNode = doc.CreateElement("SystemConfig");
            root.AppendChild(configNode);


            // 光源
            //            var node = doc.CreateElement("Illuminant");
            //            configNode.AppendChild(node);
            //            node.SetAttribute("Primary", PrimaryIlluminant.ToString());
            //            node.SetAttribute("Secondary", SecondaryIlluminant.ToString());
            //            node.SetAttribute("Tertiary", TertiaryIlluminant.ToString());
            //
            //            // 观察者角度
            //            node = doc.CreateElement("Observer");
            //            configNode.AppendChild(node);
            //            node.SetAttribute("Value", Observer.ToString());
            var node = ObserverAndIlluminantSetting.ToXmlElement(doc, nameof(ObserverAndIlluminantSetting));
            configNode.AppendChild(node);

            // 容差设置
            configNode.AppendChild(SystemToleranceSetting.ToXmlElement(doc, "ToleranceSetting"));

            // 显示的色品坐标设置
            configNode.AppendChild(SystemChromaticitySetting.ToXmlElement(doc, "ChromaticitySetting"));

            // 容差判定设置
            configNode.AppendChild(JudgementSetting.ToXmlElement(doc, "JudgementSetting"));

            // 标样命名规则
            configNode.AppendChild(StandardNamgeGenerator.ToXmlElement(doc, "StandardNameRule"));
            configNode.AppendChild(TestNameGenerator.ToXmlElement(doc, "TestNameRule"));
            // 是否自动命名
            node = doc.CreateElement("IsAutoNaming");
            node.AppendChild(doc.CreateTextNode(IsAutoNaming.ToString()));
            configNode.AppendChild(node);
            // 默认料号
            node = doc.CreateElement("DefaultMaterial");
            node.AppendChild(doc.CreateTextNode(DefaultMaterial));
            configNode.AppendChild(node);

            // 报表设置
            configNode.AppendChild(ReportSetting.ToXmlElement(doc, "ReportSetting"));

            // 标样及相关联试样
            var standardsNode = doc.CreateElement("Standards");
            root.AppendChild(standardsNode);
            foreach (var standard in StandardSpecimens)
            {
                var standardNode = standard.ToXmlElement(doc, "Standard", SpecimenTagToXmlElement);
                standardsNode.AppendChild(standardNode);
                if (standard.TestSpecimens == null)
                    continue;

                var testsNode = doc.CreateElement("Tests");
                standardNode.AppendChild(testsNode);

                foreach (var test in standard.TestSpecimens)
                {
                    testsNode.AppendChild(test.ToXmlElement(doc, "Test", SpecimenTagToXmlElement));
                }
            }

            // 没有标样的试样
            var orphanTestsNode = doc.CreateElement("OrphanTests");
            root.AppendChild(orphanTestsNode);
            foreach (var test in OrphanTests)
            {
                orphanTestsNode.AppendChild(test.ToXmlElement(doc, "Test", SpecimenTagToXmlElement));
            }


            return root;
        }

        public void LoadXmlElement(XmlElement element)
        {
            Clear();
            string version = "0.0";
            if (element.Attributes["version"] != null)
            {
                version = element.Attributes["version"].Value;
            }

            foreach (XmlNode child in element.ChildNodes)
            {
                if (string.Compare(child.Name, "SystemConfig", true) == 0)
                {
                    foreach (XmlNode gradnson in child.ChildNodes)
                    {
                        switch (gradnson.Name)
                        {
                            case "Illuminant":
                                {
                                    StandardIlluminant illuminant;
                                    if (gradnson.Attributes["Primary"] != null
                                        && Enum.TryParse(gradnson.Attributes["Primary"].Value, out illuminant))
                                    {
                                        ObserverAndIlluminantSetting.PrimaryIlluminant = illuminant;
                                    }
                                    if (gradnson.Attributes["Secondary"] != null
                                        && Enum.TryParse(gradnson.Attributes["Secondary"].Value, out illuminant))
                                    {
                                        ObserverAndIlluminantSetting.SecondaryIlluminant = illuminant;
                                    }
                                    if (gradnson.Attributes["Tertiary"] != null
                                        && Enum.TryParse(gradnson.Attributes["Tertiary"].Value, out illuminant))
                                    {
                                        ObserverAndIlluminantSetting.TertiaryIlluminant = illuminant;
                                    }
                                }
                                break;

                            case "Observer":
                                {
                                    StandardObserver observer;
                                    if (gradnson.Attributes["Value"] != null
                                        && Enum.TryParse(gradnson.Attributes["Value"].Value, out observer))
                                    {
                                        ObserverAndIlluminantSetting.PrimaryObserver = observer;
                                        ObserverAndIlluminantSetting.SecondaryObserver = observer;
                                        ObserverAndIlluminantSetting.TertiaryObserver = observer;
                                    }
                                }
                                break;

                            case "ToleranceSetting":
                                SystemToleranceSetting.LoadXmlElement(gradnson as XmlElement);
                                break;

                            case "ChromaticitySetting":
                                SystemChromaticitySetting.LoadXmlElement(gradnson as XmlElement);
                                break;

                            case "JudgementSetting":
                                JudgementSetting.LoadXmlElement(gradnson as XmlElement);
                                break;

                            case "StandardNameRule":
                                StandardNamgeGenerator.LoadXmlElement(gradnson as XmlElement);
                                break;

                            case "TestNameRule":
                                TestNameGenerator.LoadXmlElement(gradnson as XmlElement);
                                break;
                            case "ReportSetting":
                                ReportSetting.LoadXmlElement(gradnson as XmlElement);
                                break;
                            case "IsAutoNaming":
                                {
                                    bool bVal;
                                    if (bool.TryParse((gradnson as XmlElement).InnerText, out bVal))
                                    {
                                        IsAutoNaming = bVal;
                                    }
                                    else
                                    {
                                        IsAutoNaming = true;
                                    }

                                }
                                break;
                            case "DefaultMaterial":
                                DefaultMaterial = (gradnson as XmlElement).InnerText;
                                break;

                            case nameof(ObserverAndIlluminantSetting):
                                ObserverAndIlluminantSetting.LoadXmlElement(gradnson as XmlElement);
                                break;
                            default:
                                break;
                        }
                    }
                }
                else if (string.Compare(child.Name, "Standards", true) == 0)
                {
                    foreach (XmlElement stdNode in child.ChildNodes)
                    {
                        StandardSpecimen standard = new StandardSpecimen();
                        if (!standard.LoadXmlElement(stdNode, LoadSpecimenTagFromXmlElement))
                            continue;
                        _standardList.Add(standard);

                        XmlNode testsNode = null;
                        foreach (XmlNode grandson in stdNode.ChildNodes)
                        {
                            if (string.Compare(grandson.Name, "Tests", true) == 0)
                            {
                                testsNode = grandson;
                                break;
                            }
                        }
                        if (testsNode == null)
                            continue;

                        foreach (XmlElement testNode in testsNode.ChildNodes)
                        {
                            if (string.Compare(testNode.Name, "Test", true) == 0)
                            {
                                TestSpecimen test = new TestSpecimen();
                                if (test.LoadXmlElement(testNode, LoadSpecimenTagFromXmlElement))
                                {
                                    test.StandardSpecimen = standard;
                                    _testList.Add(test);
                                }
                            }
                        }
                    }
                }
                else if (string.Compare(child.Name, "OrphanTests", true) == 0)
                {
                    foreach (XmlElement testNode in child.ChildNodes)
                    {
                        if (string.Compare(testNode.Name, "Test", true) == 0)
                        {
                            TestSpecimen test = new TestSpecimen();
                            if (test.LoadXmlElement(testNode, LoadSpecimenTagFromXmlElement))
                            {
                                _testList.Add(test);
                            }
                        }
                    }
                }
            }
        }


        public bool IsExists(StandardSpecimen standard)
        {
            return _standardList.IndexOf(standard) >= 0;
        }

        public bool IsExists(TestSpecimen test)
        {
            return _testList.IndexOf(test) >= 0;
        }

        /// <summary>
        /// 所有标样
        /// </summary>
        public StandardSpecimen[] StandardSpecimens
        {
            get { return _standardList.ToArray(); }
        }
        /// <summary>
        /// 所有试样
        /// </summary>
        public TestSpecimen[] TestSpecimens
        {
            get { return _testList.ToArray(); }
        }
        /// <summary>
        /// 所有没有标样的试样
        /// </summary>
        public TestSpecimen[] OrphanTests
        {
            get
            {
                return (from t in TestSpecimens
                        where t.StandardSpecimen == null
                        select t).ToArray();
            }
        }

        /// <summary>
        /// 标样名称生成器
        /// </summary>
        public SpecimenNameGenerator StandardNamgeGenerator { get; private set; }
        /// <summary>
        /// 试样名称生成器
        /// </summary>
        public SpecimenNameGenerator TestNameGenerator { get; private set; }

        /// <summary>
        /// 判定设置
        /// </summary>
        public JudgementSetting JudgementSetting { get; set; }

        /// <summary>
        /// 是否使用系统容差设置
        /// </summary>
        public bool IsUseSystemTolerance
        {
            get { return SystemToleranceSetting.IsFromSystemDefault; }
        }
        /// <summary>
        /// 是否为标样使用单独的容差设置
        /// </summary>
        public bool IsUseIndependentTolerance
        {
            get { return !SystemToleranceSetting.IsFromSystemDefault; }
        }

        /// <summary>
        /// 系统默认容差设置
        /// </summary>
        public SystemToleranceSetting SystemToleranceSetting { get; set; }

        /// <summary>
        /// 系统显示的字段设置
        /// </summary>
        public ChromaticitySetting SystemChromaticitySetting
        {
            get { return _systemChromaticitySetting; }
            set
            {
                _systemChromaticitySetting = value;
                IsModified = true;
            }
        }

        /// <summary>
        /// 光源和觀察者角度设置
        /// </summary>
        public ObserverAndIlluminantSetting ObserverAndIlluminantSetting
        {
            get { return _observerAndIlluminantSetting; }
            set
            {
                if (!Equals(value, _observerAndIlluminantSetting))
                {
                    _observerAndIlluminantSetting = value;
                    IsModified = true;
                }
            }
        }

        //        /// <summary>
        //        /// 当前观察者角度设置
        //        /// </summary>
        //        public StandardObserver Observer
        //        {
        //            get; set;
        //        }
        //        /// <summary>
        //        /// 主光源
        //        /// </summary>
        //        public StandardIlluminant PrimaryIlluminant
        //        {
        //            get; set;
        //        }
        //        /// <summary>
        //        /// 第二光源
        //        /// </summary>
        //        public StandardIlluminant SecondaryIlluminant
        //        {
        //            get; set;
        //        }
        //        /// <summary>
        //        /// 第三光源
        //        /// </summary>
        //        public StandardIlluminant TertiaryIlluminant
        //        {
        //            get; set;
        //        }
        /// <summary>
        /// 主光源觀察者角度對應的Formula
        /// </summary>
        public Formula PrimaryFormula
        {
            get { return GetFormula(ObserverAndIlluminantSetting.PrimaryObserver, ObserverAndIlluminantSetting.PrimaryIlluminant); }
        }
        /// <summary>
        /// 第二光源观察者角度对应的Formula
        /// </summary>
        public Formula SecondaryFormula
        {
            get
            {
                if (ObserverAndIlluminantSetting.SecondaryIlluminant != StandardIlluminant.Unknown)
                {
                    return GetFormula(ObserverAndIlluminantSetting.SecondaryObserver,
                        ObserverAndIlluminantSetting.SecondaryIlluminant);
                }
                return null;
            }
        }
        /// <summary>
        /// 第三光源和观察者角度对应的Formula
        /// </summary>
        public Formula TertiaryFormula
        {
            get
            {
                if (ObserverAndIlluminantSetting.TertiaryIlluminant != StandardIlluminant.Unknown)
                {
                    return GetFormula(ObserverAndIlluminantSetting.TertiaryObserver,
                        ObserverAndIlluminantSetting.TertiaryIlluminant);
                }
                return null;
            }
        }

        public string DefaultMaterial { get; set; }
        public bool IsAutoNaming { get; set; }

        /// <summary>
        /// 犾取给定光源和观察者角度对应的Formula
        /// </summary>
        /// <param name="observer">观察者角度</param>
        /// <param name="illuminant">光源</param>
        /// <returns>返回相应的Formual</returns>
        public Formula GetFormula(StandardObserver observer, StandardIlluminant illuminant)
        {
           Key key = new Key(SpecularComponent.Unknown, observer, illuminant);
            if (!_formulas.ContainsKey(key))
            {
                _formulas[key] = new Formula(illuminant, observer);
            }
            return _formulas[key];
        }
        /// <summary>
        /// 犾取key中的光源和观察者角度对应的Formula
        /// </summary>
        /// <param name="key">要犾取其光源和观察者角度对应的Formula的Key对象</param>
        /// <returns>返回Formula</returns>
        private Formula GetFormula(Key key)
        {
            return GetFormula(key.Observer, key.Illuminant);
        }

        /// <summary>
        /// 获取给SpecularComponent和光源、观察者角度下的标样
        /// </summary>
        /// <param name="sc"></param>
        /// <param name="illuminant"></param>
        /// <param name="observer"></param>
        /// <returns></returns>
        public StandardSpecimenAdapter[] GetStandards(SpecularComponent sc, StandardObserver observer, StandardIlluminant illuminant)
        {
            Key key = new Key(sc, observer, illuminant);
            if (!_standardAdapters.ContainsKey(key))
            {
                MakeStandardAdapters(key);
            }

            return _standardAdapters[key].Values.ToArray();
        }

        private void MakeStandardAdapters(Key key)
        {
            Formula formula = GetFormula(key.Observer, key.Illuminant);
            Dictionary<StandardSpecimen, StandardSpecimenAdapter> dict =
                new Dictionary<StandardSpecimen, StandardSpecimenAdapter>();
            foreach (var std in StandardSpecimens)
            {
                dict[std] = new StandardSpecimenAdapter(
                    std, formula, GetChromaticitySetting(std), key.SpecularComponent,
                    GetTolerance(std, new ObserverIlluminantPair(formula.Observer, formula.Illuminant)));
            }
            _standardAdapters[key] = dict;
        }

        /// <summary>
        /// 获取给SpecularComponent和光源、观察者角度下的试样
        /// </summary>
        /// <param name="sc"></param>
        /// <param name="illuminant"></param>
        /// <param name="observer"></param>
        /// <returns></returns>
        public TestSpecimenAdapter[] GetTests(SpecularComponent sc, StandardObserver observer, StandardIlluminant illuminant)
        {
            Key key = new Key(sc, observer, illuminant);
            if (!_testsAdapters.ContainsKey(key))
            {
                MakeTestAdapters(key);
            }
            return _testsAdapters[key].Values.ToArray();
        }

        private Dictionary<TestSpecimen, TestSpecimenAdapter> MakeTestAdapters(Key key)
        {
            Formula formula = GetFormula(key.Observer, key.Illuminant);
            Dictionary<TestSpecimen, TestSpecimenAdapter> dict =
                new Dictionary<TestSpecimen, TestSpecimenAdapter>();

            Formula refFormula1, refFormula2;
            GetReferenceFormula(formula, out refFormula1, out refFormula2);

            var stds = GetStandards(key.SpecularComponent, key.Observer, key.Illuminant);

            foreach (var std in stds)
            {
                if (std.StandardSpecimen.TestSpecimens != null)
                {
                    foreach (var test in std.StandardSpecimen.TestSpecimens)
                    {
                        dict[test] = new TestSpecimenAdapter(
                            test, formula, GetChromaticitySetting(std.StandardSpecimen),
                            key.SpecularComponent,
                            std, /*GetTolerance(std.StandardSpecimen, key.Illuminant),*/
                            refFormula1, refFormula2);
                        dict[test].Judge(std, JudgementSetting);

                    }
                }
            }
            foreach (var test in OrphanTests)
            {
                dict[test] = new TestSpecimenAdapter(
                    test, formula, SystemChromaticitySetting, key.SpecularComponent);
            }

            _testsAdapters[key] = dict;
            return dict;
        }

        /// <summary>
        /// 更新指定标样下的所有试样的判定结果
        /// </summary>
        /// <param name="standard">指定的标样</param>
        public void UpdateJudgements(StandardSpecimen standard)
        {
            if (standard == null)
                return;

            foreach (var key in _testsAdapters.Keys)
            {
                var tolance = GetTolerance(standard, key.ObserverIlluminant);
                foreach (var test in GetTests(standard, key.SpecularComponent, key.Observer, key.Illuminant))
                {
                    test.Judge(_standardAdapters[key][standard], JudgementSetting, tolance);
                }
            }
        }

        /// <summary>
        /// 更新所有标样的容差设置
        /// </summary>
        public void UpdateJudgements()
        {
            foreach (var key in _testsAdapters.Keys)
            {
                foreach (var test in _testsAdapters[key].Values)
                {
                    StandardSpecimen standard = test.TestSpecimen.StandardSpecimen;
                    if (standard == null)
                        continue;

                    test.Judge(_standardAdapters[key][standard], JudgementSetting, GetTolerance(standard, key.ObserverIlluminant));
                }
            }
        }

        public void UpdateCachesOf(TestSpecimen test)
        {
            if (test == null) return;

            ChromaticitySetting setting = GetChromaticitySetting(test.StandardSpecimen);
            foreach (var key in _testsAdapters.Keys)
            {
                var formula = GetFormula(key);
                Formula ref1, ref2;
                GetReferenceFormula(formula, out ref1, out ref2);
                Tolerance tolerance = GetTolerance(test.StandardSpecimen, key.ObserverIlluminant);
                StandardSpecimenAdapter stdAdapter = GetAdapter(test.StandardSpecimen, key.SpecularComponent, key.Observer, key.Illuminant);

                _testsAdapters[key][test] = new TestSpecimenAdapter(
                    test,
                    formula,
                    setting,
                    key.SpecularComponent,
                    stdAdapter,
                    ref1, ref2);
                _testsAdapters[key][test].Judge(stdAdapter, JudgementSetting);
            }
        }

        public void UpdateCachesOf(IEnumerable<TestSpecimen> tests)
        {
            if (tests == null) return;

            ChromaticitySetting setting = null;
            foreach (var key in _testsAdapters.Keys)
            {
                var formula = GetFormula(key);
                Formula ref1, ref2;
                GetReferenceFormula(formula, out ref1, out ref2);

                StandardSpecimen previousStandard = null;
                Tolerance tolerance = null;
                StandardSpecimenAdapter stdAdapter = null;
                foreach (var test in tests)
                {
                    if (previousStandard != test.StandardSpecimen)
                    {
                        previousStandard = test.StandardSpecimen;
                        tolerance = GetTolerance(test.StandardSpecimen, key.ObserverIlluminant);
                        stdAdapter = GetAdapter(test.StandardSpecimen, key.SpecularComponent, key.Observer, key.Illuminant);
                        setting = GetChromaticitySetting(test.StandardSpecimen);
                    }

                    _testsAdapters[key][test] = new TestSpecimenAdapter(
                        test,
                        formula,
                        setting,
                        key.SpecularComponent,
                        stdAdapter,
                        ref1, ref2);
                    _testsAdapters[key][test].Judge(stdAdapter, JudgementSetting);
                }
            }
        }

        public void OnIsUseSystemToleranceChanged()
        {
            foreach (var std in _standardList)
            {
                if (std.ToleranceSetting != null)
                {
                    UpdateJudgements(std);
                }
            }
        }

        /// <summary>
        /// 更新标样及其下的试样的缓存
        /// </summary>
        /// <param name="standard">要更新的标样</param>
        /// <param name="updateTests">是否同时更新其下的试样</param>
        public void UpdateCachesOf(StandardSpecimen standard, bool updateTests = true)
        {
            ChromaticitySetting setting = GetChromaticitySetting(standard);

            foreach (var key in _standardAdapters.Keys)
            {
                _standardAdapters[key][standard] = new StandardSpecimenAdapter(
                    standard,
                    GetFormula(key.Observer, key.Illuminant),
                    setting,
                    key.SpecularComponent,
                    GetTolerance(standard, key.ObserverIlluminant));
            }

            if (updateTests && standard.TestSpecimens != null)
            {
                UpdateCachesOf(standard.TestSpecimens);
            }
        }

        public Tolerance GetTolerance(StandardSpecimen standard, ObserverIlluminantPair observerIlluminant)
        {
            ToleranceSetting setting = null;
            if (standard == null || standard.ToleranceSetting == null ||
                SystemToleranceSetting.IsFromSystemDefault)
            {
                setting = SystemToleranceSetting;
            }
            else
            {
                setting = standard.ToleranceSetting;
            }

            if (observerIlluminant == ObserverAndIlluminantSetting.Primary)
                return setting.PrimaryTolerance;
            if (observerIlluminant == ObserverAndIlluminantSetting.Secondary)
            {

                if (setting.SecondaryTolerance != null)
                {
                    return setting.SecondaryTolerance;
                }
                else
                {
                    return setting.PrimaryTolerance;
                }
            }

            if (observerIlluminant == ObserverAndIlluminantSetting.Tertiary)
                if (setting.TertiaryTolerance != null)
                {
                    return setting.TertiaryTolerance;
                }
                else
                {
                    return setting.PrimaryTolerance;
                }
            return setting.PrimaryTolerance;
        }

        /// <summary>
        /// 获取标样的容差设置
        /// </summary>
        /// <param name="standard"></param>
        /// <returns>返回标样的容差设置</returns>
        public ToleranceSetting GetToleranceSetting(StandardSpecimen standard)
        {
            if (IsUseSystemTolerance || standard == null || standard.ToleranceSetting == null)
                return SystemToleranceSetting;
            return standard.ToleranceSetting;
        }

        /// <summary>
        /// 根据当前设置获取标样显色设置
        /// </summary>
        /// <param name="standard"></param>
        /// <returns></returns>
        public ChromaticitySetting GetChromaticitySetting(StandardSpecimen standard)
        {
            if (IsUseSystemTolerance || standard == null || standard.ChromaticitySetting == null)
                return SystemChromaticitySetting;
            return standard.ChromaticitySetting;
        }

        /// <summary>
        /// 获取给定标样下且在SpecularComponent和光源、观察者角度下的试样
        /// </summary>
        /// <param name="standard"></param>
        /// <returns></returns>
        public TestSpecimenAdapter[] GetTests(StandardSpecimen standard, SpecularComponent sc,
            StandardObserver observer, StandardIlluminant illuminant)
        {
            return (from test in GetTests(sc, observer, illuminant)
                    where test.TestSpecimen.StandardSpecimen == standard
                    select test).ToArray();
        }


        /// <summary>
        /// 添加标样数据库中
        /// </summary>
        /// <param name="standard">要添加的标样</param>
        public void AddStandard(StandardSpecimen standard, bool createNewId = true)
        {
            if (createNewId)
                standard.ID = GetNewStandardIndex(); //StandardNamgeGenerator.NewIndex;
                                                     // 如果料号为空，使用默认料号设置其料号
            if (standard.Material == null)
            {
                standard.Material = DefaultMaterial;
            }

            // 将标样添加到Job中
            if (standard.Name == null)
            {
                standard.Name = StandardNamgeGenerator.GenerateName(standard, GetNewStandardIndex());
            }


            _standardList.Add(standard);


            foreach (var key in _standardAdapters.Keys)
            {
                _standardAdapters[key][standard] = new StandardSpecimenAdapter(
                    standard, GetFormula(key.Observer, key.Illuminant),
                    SystemChromaticitySetting, key.SpecularComponent,
                    GetTolerance(standard, key.ObserverIlluminant));
            }

            IsModified = true;
        }

        /// <summary>
        /// 将试样添加到数据库中并与指定的标样关联
        /// </summary>
        /// <param name="test">要添加的试样</param>
        /// <param name="standard">要关联的标样</param>
        public void AddTest(TestSpecimen test, StandardSpecimen standard = null)
        {
            test.StandardSpecimen = standard;
            if (test.Material == null)
            {
                if (standard != null)
                    test.Material = standard.Material;
                else
                    test.Material = DefaultMaterial;
            }
            test.ID = GetNewTestIndex(standard);

            // 如果没有名字，生成名字
            if (test.Name == null)
                test.Name = TestNameGenerator.GenerateName(test, GetNewTestIndex(standard));

            _testList.Add(test);


            foreach (var key in _testsAdapters.Keys)
            {
                var formula = GetFormula(key.Observer, key.Illuminant);
                Formula ref1, ref2;
                GetReferenceFormula(formula, out ref1, out ref2);

                if (standard != null)
                {
                    var standardAdapter = GetAdapter(standard, key.SpecularComponent, key.Observer, key.Illuminant);
                    _testsAdapters[key][test] = new TestSpecimenAdapter(
                        test, formula, GetChromaticitySetting(standard), key.SpecularComponent,
                        standardAdapter, /*GetTolerance(standard, key.Illuminant)*/
                        ref1, ref2);
                    _testsAdapters[key][test].Judge(standardAdapter, JudgementSetting);
                }
                else
                {
                    _testsAdapters[key][test] = new TestSpecimenAdapter(
                        test, formula, SystemChromaticitySetting, key.SpecularComponent);
                }
            }

            IsModified = true;
        }

        /// <summary>
        /// 获取标样的Adapter
        /// </summary>
        /// <param name="std"></param>
        /// <param name="sc"></param>
        /// <param name="observer"></param>
        /// <param name="illuminant"></param>
        /// <returns></returns>
        public StandardSpecimenAdapter GetAdapter(StandardSpecimen std, SpecularComponent sc,
            StandardObserver observer, StandardIlluminant illuminant)
        {
            if (std == null)
                return null;

            Key key = new Key(sc, observer, illuminant);
            if (!_standardAdapters.ContainsKey(key))
            {
                MakeStandardAdapters(key);
            }
            if (_standardAdapters[key].ContainsKey(std))
            {
                return _standardAdapters[key][std];
            }
            return new StandardSpecimenAdapter(
                std, GetFormula(observer, illuminant), GetChromaticitySetting(std), sc,
                GetTolerance(std, new ObserverIlluminantPair(observer, illuminant)));
        }
        /// <summary>
        /// 修改设置标样数据也发生变化
        /// </summary>
        /// <param name="std"></param>
        /// <param name="sc"></param>
        /// <param name="observer"></param>
        /// <param name="illuminant"></param>
        /// <returns></returns>
        public StandardSpecimenAdapter UpdataSpecimen(StandardSpecimen std, SpecularComponent sc, StandardObserver observer, StandardIlluminant illuminant)
        {
            Key key = new Key(sc, observer, illuminant);

            return new StandardSpecimenAdapter(
                std, GetFormula(observer, illuminant), GetChromaticitySetting(std), sc,
                GetTolerance(std, new ObserverIlluminantPair(observer, illuminant)));
        }

        /// <summary>
        /// 获取试样的Adapter
        /// </summary>
        /// <param name="test"></param>
        /// <param name="sc"></param>
        /// <param name="observer"></param>
        /// <param name="illuminant"></param>
        /// <returns></returns>
        public TestSpecimenAdapter GetAdapter(TestSpecimen test, SpecularComponent sc,
            StandardObserver observer, StandardIlluminant illuminant)
        {
            Key key = new Key(sc, observer, illuminant);
            if (!_testsAdapters.ContainsKey(key))
            {
                MakeTestAdapters(key);
            }

            if (_testsAdapters[key].ContainsKey(test))
            {
                return _testsAdapters[key][test];
            }
            return new TestSpecimenAdapter(
                test, GetFormula(key.Observer, key.Illuminant),
                GetChromaticitySetting(test.StandardSpecimen),
                key.SpecularComponent);
        }

        /// <summary>
        /// 获取容差的适配器对象ToleranceAdapter
        /// </summary>
        /// <param name="tolerance">要获取其容差的Tolerance对象</param>
        /// <param name="illuminant">Tolerance对应的光源，以便获取名称</param>
        /// <returns>返回相应的适配器对象</returns>
        public ToleranceAdapter GetAdapter(Tolerance tolerance, ObserverIlluminantPair observerIlluminant)
        {
            return new ToleranceAdapter(observerIlluminant, tolerance);
        }

        /// <summary>
        /// 但Job的ChromaticitySetting更改是调用，以更新值
        /// </summary>
        public void clearCatchs()
        {
            _standardAdapters.Clear();
            _testsAdapters.Clear();
        }

        /// <summary>
        /// 清空所有内容，回到初始化状态
        /// </summary>
        public void Clear()
        {
            _testList.Clear();
            _standardList.Clear();
            _standardAdapters.Clear();
            _testsAdapters.Clear();

            Init();
        }
        /// <summary>
        /// 移除指定的标样
        /// </summary>
        /// <param name="standard">要移除的标样</param>
        public void RemoveStandard(StandardSpecimen standard)
        {
            if (standard.TestSpecimens != null)
            {
                foreach (var test in standard.TestSpecimens.ToArray())
                {
                    RemoveTest(test);
                }
            }
            foreach (var value in _standardAdapters.Values)
            {
                value.Remove(standard);
            }
            _standardList.Remove(standard);

            IsModified = true;
        }
        /// <summary>
        /// 移除指定的试样
        /// </summary>
        /// <param name="test">要移除的试样</param>
        public void RemoveTest(TestSpecimen test)
        {
            foreach (var value in _testsAdapters.Values)
            {
                value.Remove(test);
            }
            _testList.Remove(test);
            test.StandardSpecimen = null;

            IsModified = true;
        }
        /// <summary>
        /// 根据给定的Formula对象获取相应的参考Formula对象
        /// </summary>
        /// <param name="formula">给定的Formula对象</param>
        /// <param name="referenceFormula1">返回参考一</param>
        /// <param name="referenceFormula2">返回参考二</param>
        public void GetReferenceFormula(Formula formula,
            out Formula referenceFormula1, out Formula referenceFormula2)
        {
            var t = new ObserverIlluminantPair(formula.Observer, formula.Illuminant);
            if (t == ObserverAndIlluminantSetting.Secondary)
            {
                referenceFormula1 = PrimaryFormula;
                referenceFormula2 = TertiaryFormula;

            }
            else if (t == ObserverAndIlluminantSetting.Tertiary)
            {
                referenceFormula1 = PrimaryFormula;
                referenceFormula2 = SecondaryFormula;
            }
            else
            {
                referenceFormula1 = SecondaryFormula;
                referenceFormula2 = TertiaryFormula;
            }
        }

        /// <summary>
        /// 获取标样下的所有试样的统计结果
        /// </summary>
        /// <param name="standard">要统计其试样的标样</param>
        /// <returns></returns>
        public Dictionary<ChromaticityField, Statistics> GetStatistics(StandardSpecimen standard,
            SpecularComponent sc, StandardObserver observer, StandardIlluminant illuminant)
        {
            if (standard == null)
                throw new ArgumentNullException();

            if (standard.TestSpecimens == null || standard.TestSpecimens.Count == 0)
                return null;

            ChromaticitySetting setting = GetChromaticitySetting(standard);
            Dictionary<ChromaticityField, Statistics> result =
                new Dictionary<ChromaticityField, Statistics>();
            //ToleranceSetting tolSetting = GetToleranceSetting(standard);
            var tolItemKeys = GetToleranceSetting(standard).ItemKeys;


            var stdAdapter = GetAdapter(standard, sc, observer, illuminant);
            foreach (var field in setting.DisplayFields)
            {
                var tests = GetTests(standard, sc, observer, illuminant);
                Statistics statistics = null;

                if (field.IsColorField())
                {
                    statistics = StatisticsHelper.GetChromaticityStatistics(tests, field);
                }
                else if (field.IsColorDiffField() && tolItemKeys.Contains(field))
                {
                    statistics = StatisticsHelper.GetColorDiffStatistics(tests, field);
                }
                else
                {
                    continue;
                }

                result[field] = statistics;
            }

            return result;
        }

        /// <summary>
        /// 获取一个新的标样索引号
        /// </summary>
        /// <returns>返回一个新的标样索引号</returns>
        public int GetNewStandardIndex()
        {
            var ids = from std in StandardSpecimens select std.ID;
            return ids.Count() > 0 ? ids.Max() + 1 : 1;
        }

        /// <summary>
        /// 获取新的试样索引
        /// </summary>
        /// <param name="standard">新试样所属标样，为null表示不属于任何标样</param>
        /// <returns></returns>
        public int GetNewTestIndex(StandardSpecimen standard)
        {
            var ids = from t in TestSpecimens
                      where t.StandardSpecimen == standard
                      select t.ID;
            return ids.Count() > 0 ? ids.Max() + 1 : 1;
        }

        /// <summary>
        /// 查找与给定Lab最接近的标样
        /// </summary>
        /// <param name="lab">要查找的Lab</param>
        /// <returns>返回找到的标样；如果没找到，返回null</returns>
        public StandardSpecimen FindNearestStandard(CIELAB lab, SpecularComponent sc, StandardObserver observer, StandardIlluminant illuminant)
        {
            var standards = GetStandards(sc, observer, illuminant);
            if (standards.Length == 0)
                return null;

            var diffs = from std in standards
                        where std.Lab != null
                        let dL = Math.Abs(std.L - lab.L)
                        let da = Math.Abs(std.a - lab.a)
                        let db = Math.Abs(std.b - lab.b)
                        let dE = Math.Sqrt(dL * dL + da * da + db * db)
                        select new { dL = dL, da = da, db = db, dE = dE };
            if (!diffs.Any()) return null;
            double min = diffs.ElementAt(0).dE;
            int indexOfMin = 0;
            for (int i = 1; i < diffs.Count(); i++)
            {
                if (diffs.ElementAt(i).dE < min)
                {
                    min = diffs.ElementAt(i).dE;
                    indexOfMin = i;
                }
            }

            return standards[indexOfMin].StandardSpecimen;
        }

        /// <summary>
        /// 获取样品中使用到的测光方式
        /// </summary>
        public SpecularComponent GetSpecularComponent()
        {
            SpecularComponent sc = SpecularComponent.Unknown;

            Func<SpecularComponent, SpecularComponent> get_sc = (sample_sc) =>
            {
                if ((sample_sc == SpecularComponent.SCI && sc == SpecularComponent.SCE) || (sample_sc == SpecularComponent.SCE && sc == SpecularComponent.SCI))
                {
                    return SpecularComponent.SCIAndSCE;
                }
                return sample_sc;
            };

            if (_standardList != null)
            {
                foreach (var std in _standardList)
                {
                    sc = get_sc(std.SpecularComponent);
                    if (sc == SpecularComponent.SCIAndSCE)
                    {
                        return sc;
                    }
                }
            }
            if (OrphanTests != null)
            {
                foreach (var test in OrphanTests)
                {
                    sc = get_sc(test.SpecularComponent);
                    if (sc == SpecularComponent.SCIAndSCE)
                    {
                        return sc;
                    }
                }
            }

            return sc;
        }

        public void Save(string fileName)
        {
            XmlDocument doc = new XmlDocument();
            doc.AppendChild(doc.CreateXmlDeclaration("1.0", "utf-8", null));
            var root = ToXmlElement(doc, "ThreeNHJob");
            doc.AppendChild(root);

            doc.Save(fileName);
        }

        public void Load(string fileName)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(fileName);
            if (doc.DocumentElement.Name.Contains("SpectraMagicNX_data"))
                LoadXmlElementFromSpectraMagicNXData(doc.DocumentElement);
            else
                LoadXmlElement(doc.DocumentElement);
        }

        private void LoadXmlElementFromSpectraMagicNXData(XmlElement element)
        {

            Clear();

            foreach (XmlNode child in element.ChildNodes)
            {
                if (string.Compare(child.Name, "data_set", true) == 0)
                {
                    foreach (XmlElement targetNode in child.ChildNodes)
                    {
                        if (string.Compare(targetNode.Name, "target", true) == 0)
                        {
                            foreach (XmlElement groupNode in targetNode.ChildNodes)
                            {
                                StandardSpecimen standard = new StandardSpecimen();
                                var range = new WavelengthRange(360, 740, 10);
                                var data = new Dictionary<int, double>();
                                foreach (XmlElement itemNode in groupNode.ChildNodes)
                                {
                                    var name = itemNode.GetAttribute("name");
                                    var id = itemNode.GetAttribute("id");
                                    if (string.Compare(name, $"数据名", true) == 0)
                                    {
                                        standard.Name = itemNode.InnerText;
                                        continue;
                                    }
                                    if (string.Compare(name, $"标样号", true) == 0)
                                    {
                                        standard.ID = int.Parse(id);
                                        continue;
                                    }
                                    for (var waveLength = range.Min; waveLength <= range.Max; waveLength += range.Step)
                                    {

                                        if (string.Compare(name, $"{waveLength}nm", true) == 0)
                                        {
                                            if (!data.ContainsKey(waveLength))
                                            {
                                                if (double.TryParse(itemNode.InnerText, out var result))
                                                {
                                                    data.Add(waveLength, result);
                                                }
                                                else
                                                {
                                                    data.Add(waveLength, 0);
                                                }
                                            }
                                        }
                                    }
                                }

                                if (data.Count != 0)
                                {
                                    var specimen = new Spectrum(range, data.Select(o => o.Value / 100.0).ToArray());
                                    standard.SCI = new ColorData(specimen);
                                    _standardList.Add(standard);
                                }

                            }
                        }
                        else if (string.Compare(targetNode.Name, "sample", true) == 0)
                        {
                            foreach (XmlElement groupNode in targetNode.ChildNodes)
                            {
                                TestSpecimen test = new TestSpecimen();
                                var range = new WavelengthRange(360, 740, 10);
                                var data = new Dictionary<int, double>();
                                foreach (XmlElement itemNode in groupNode.ChildNodes)
                                {
                                    var name = itemNode.GetAttribute("name");
                                    var id = itemNode.GetAttribute("id");
                                    if (string.Compare(name, $"数据名", true) == 0)
                                    {
                                        test.Name = itemNode.InnerText;
                                        continue;
                                    }
                                    if (string.Compare(name, $"标样名", true) == 0)
                                    {
                                        test.Tag = itemNode.InnerText;
                                        continue;
                                    }
                                    for (var waveLength = range.Min; waveLength <= range.Max; waveLength += range.Step)
                                    {

                                        if (string.Compare(name, $"{waveLength}nm", true) == 0)
                                        {
                                            if (!data.ContainsKey(waveLength))
                                            {
                                                if (double.TryParse(itemNode.InnerText, out var result))
                                                {
                                                    data.Add(waveLength, result);
                                                }
                                            }
                                        }
                                    }
                                }

                                if (data.Count != 0)
                                {
                                    var specimen = new Spectrum(range, data.Select(o => o.Value / 100.0).ToArray());
                                    test.SCI = new ColorData(specimen);
                                    _testList.Add(test);
                                }

                            }
                        }
                    }
                }
            }
        }
    }
}
