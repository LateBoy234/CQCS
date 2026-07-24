using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Web.UI.HtmlControls;
using System.Xml;
using System.Xml.Serialization;
using ThreeNH.ColorQualityControl.Reports;
using ThreeNH.DataModel;
using ThreeNH.Optical;
using Tomlyn;
using Tomlyn.Model;

namespace ThreeNH.ColorQualityControl
{
    /// <summary>
    /// 连接方式
    /// </summary>
    enum ConnectionMethod
    {
        ByUSB,
        ByBluettooth_4,
        ByBluettooth_5,
        BySpecificalDevice,    // 通过指定的设备
        ByYL30Device
    }

    class AveragementConfig
    {
        public int Times { get; set; } = 3;   // 测量次数
        public int Interval { get; set; } = 1; // 自动测量间隔
        public bool IsAutoSave { get; set; } = true; // 是否自动保存测量结果
        public bool IsAutoMeasure { get; set; } = true; // 是否自动测量
    }

    class SystemDefaultConfig
    {
        public SystemDefaultConfig()
        {
            ReportSetting = new ReportSetting();
            JudgementSetting = new JudgementSetting();
            IsAutoNaming = true;
            StandardAutoNamingRule = "Standard{Index}";
            TestAudotNamingRule = "Sample{Index}";
            AppInfoId = "3a2011f5-3b6f-bcd7-7ebf-534ef71bda28";
            ServiceUrl = ReadTestUrl() ?? "https://cuali.3n88.com:7000";
            ObserverAndIlluminantSetting = new ObserverAndIlluminantSetting
            {
                Observer = StandardObserver.CIE1964,
                PrimaryIlluminant = StandardIlluminant.D65,
                SecondaryIlluminant = StandardIlluminant.A,
                TertiaryIlluminant = StandardIlluminant.F2
            };

            DisplaySetting = new ChromaticitySetting();
            DisplaySetting.DefaultFields(ColorSpace.CIE_LAB, ColorDiffFormula.CIE1976);


            InstrumentDataDisplaySetting = new ChromaticitySetting();
            InstrumentDataDisplaySetting.SetDisplayFields(new ChromaticityField[0]);
            InstrumentDataDisplaySetting.AddDisplayFields(new ChromaticityField[] {
                ChromaticityField.ID,
                ChromaticityField.Name,
                ChromaticityField.SpecularComponent,
                ChromaticityField.PseudoColor,
                ChromaticityField.UV,
            });
            InstrumentDataDisplaySetting.AddDisplayFields(ChromaticitySetting.GetFieldsBy(ColorSpace.CIE_LAB, false));
            InstrumentDataDisplaySetting.AddDisplayFields(ChromaticitySetting.GetReflectanceFields(400, 700));

            ToleranceSetting = new SystemToleranceSetting()
            {
                PrimaryTolerance = new Tolerance(),
                SecondaryTolerance = new Tolerance(),
                TertiaryTolerance = new Tolerance(),
                IsFromSystemDefault = false
            };
            ToleranceSetting.ItemKeys = DisplaySetting.ColorDiffFields;

            //ConnectionMethod = ConnectionMethod.BySpecificalDevice;
            //InstrumentType = InstrumentType.YS;

            // 测量试样时自动选择标样的配置
            IsAutoSelectStandard = false;
            AutoSelectStandardLimits = new Tolerance(new ChromaticityField[] {
                ChromaticityField.dL,
                ChromaticityField.da,
                ChromaticityField.db,
                ChromaticityField.dEab
            });
            AutoSelectStandardLimits[ChromaticityField.dL] = new ToleranceItem
            {
                Lower = null,
                Upper = 2.0,
                UseForJudgement = false
            };
            AutoSelectStandardLimits[ChromaticityField.da] = new ToleranceItem
            {
                Lower = null,
                Upper = 2.0,
                UseForJudgement = false
            };
            AutoSelectStandardLimits[ChromaticityField.db] = new ToleranceItem
            {
                Lower = null,
                Upper = 2.0,
                UseForJudgement = false
            };
            AutoSelectStandardLimits[ChromaticityField.dEab] = new ToleranceItem
            {
                Lower = null,
                Upper = 2.5,
                UseForJudgement = true
            };

            TestModelParameter = new TestModel();
        }


        /// <summary>
        /// 测试环境下读取版本更新地址，正式环境下直接使用默认地址
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static  string ReadTestUrl()
        {
            string ConfigFileName = "AppConfig.json";
            try
            {
                string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigFileName);

                // 配置文件不存在直接返回 null
                if (!File.Exists(configPath))
                {
                    // Console.WriteLine($"⚠  {ConfigFileName} 不存在");
                    return null;
                }

                // 读取并解析 TOML 文件
                string jsonContent = File.ReadAllText(configPath);
                JObject jObject = JObject.Parse(jsonContent);
                string ServerUrl = jObject["ServerUrl"]?.ToString() ?? null;


                return ServerUrl;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"读取 {ConfigFileName} 失败：{ex.Message}", ex);
            }
        }
       static string ConfigFileName = "AppConfig.json";
        /// <summary>
        /// 生成 AppConfig.toml 配置文件到程序根目录
        /// </summary>
        private static void GenerateTomlConfig()
        {
            try
            {
                // 获取程序根目录 + AppConfig.toml 完整路径
                string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigFileName);

                // 避免覆盖已有配置（可选：如需覆盖可删除此判断）
                if (File.Exists(configPath))
                {
                    Console.WriteLine($"⚠  {ConfigFileName} 已存在，跳过生成（如需覆盖请注释此逻辑）");
                    return;
                }

                // 构建 TOML 配置内容（核心字段：ServerUrl）
                var configTable = new TomlTable
                {
                    ["ServerUrl"] = "http://192.168.3.87:7000", // 调试环境默认值
                    ["DebugMode"] = false,
                    ["LogLevel"] = "debug",
                    ["Timeout"] = 30
                };

                var tomlContent = TomlSerializer.Serialize(configTable);

                // 强制UTF8编码，避免中文乱码
                File.WriteAllText(configPath, tomlContent, System.Text.Encoding.UTF8);

                Console.WriteLine($"✅ {ConfigFileName} 已生成：{configPath}");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"生成 {ConfigFileName} 失败：{ex.Message}", ex);
            }
        }

        /// <summary>
        /// 从 AppConfig.toml 中读取 ServerUrl 字段
        /// </summary>
        /// <returns>ServerUrl 值（无则返回 null）</returns>
        public static string ReadServerUrlFromConfig()
        {
            try
            {
                string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigFileName);

                // 配置文件不存在直接返回 null
                if (!File.Exists(configPath))
                {
                    Console.WriteLine($"⚠  {ConfigFileName} 不存在");
                    return null;
                }

                // 读取并解析 TOML 文件
                string tomlContent = File.ReadAllText(configPath, System.Text.Encoding.UTF8);
                var parseResult = TomlSerializer.Deserialize<TomlTable>(tomlContent);

                // 检查解析错误
                if (!parseResult.ContainsKey("ServerUrl"))
                {
                    Console.WriteLine($"{parseResult["ServerUrl"].ToString()}");
                    return null;
                }
                // 读取根级 ServerUrl 字段
                string serverUrl = parseResult?["ServerUrl"]?.ToString()?.Trim();


                return serverUrl;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"读取 {ConfigFileName} 失败：{ex.Message}", ex);
            }
        }

        public void Save(string fileName)
        {
            XmlDocument doc = new XmlDocument();
            doc.CreateXmlDeclaration("1.0", "utf-8", "yes");
            var root = doc.CreateElement("SystemDefaultConfig");
            doc.AppendChild(root);

            if (ReportSetting != null)
                root.AppendChild(ReportSetting.ToXmlElement(doc, "ReportSetting"));
            if (DisplaySetting != null)
                root.AppendChild(DisplaySetting.ToXmlElement(doc, "DisplaySetting"));
            if (ToleranceSetting != null)
                root.AppendChild(ToleranceSetting.ToXmlElement(doc, "ToleranceSetting"));
            if (JudgementSetting != null)
                root.AppendChild(JudgementSetting.ToXmlElement(doc, "JudgementSetting"));
            root.AppendChild(ObserverAndIlluminantSetting.ToXmlElement(doc, "ObserverAndIlluminantSetting"));

            var connectionSetting = doc.CreateElement("ConnectionSetting");
            root.AppendChild(connectionSetting);
            connectionSetting.SetAttribute("ConnectionMethod", ConnectionMethod.ToString());
            connectionSetting.SetAttribute("InstrumentType", InstrumentType.ToString());
            connectionSetting.SetAttribute("LatestConnection", LatestConnection);

            var updateVersionSetting = doc.CreateElement("UpdateVersionSetting");
            root.AppendChild(updateVersionSetting);
            updateVersionSetting.SetAttribute("AppInfoId", AppInfoId);
            updateVersionSetting.SetAttribute("ServiceUrl", ServiceUrl);

            var namingSetting = doc.CreateElement("NamingSetting");
            root.AppendChild(namingSetting);
            namingSetting.SetAttribute("IsAutoNaming", IsAutoNaming.ToString());
            var node = namingSetting.AppendChild(doc.CreateElement("StandardAutoNamingRule"));
            node.AppendChild(doc.CreateTextNode(StandardAutoNamingRule));
            node = namingSetting.AppendChild(doc.CreateElement("TestAutoNamingRule"));
            node.AppendChild(doc.CreateTextNode(TestAudotNamingRule));
            namingSetting.SetAttribute("DefaultMaterial", DefaultMaterial);

            if (InstrumentDataDisplaySetting != null)
                root.AppendChild(InstrumentDataDisplaySetting.ToXmlElement(doc, "InstrumentDataDisplaySetting"));

            var autoSelectStandardSetting = doc.CreateElement("AutoSelectStandardSetting");
            root.AppendChild(autoSelectStandardSetting);
            autoSelectStandardSetting.SetAttribute("IsAutoSelectStandard", IsAutoSelectStandard.ToString());
            autoSelectStandardSetting.AppendChild(AutoSelectStandardLimits.ToXmlElement(doc, "StandardLimits"));

            var avgMeasureCfg = doc.CreateElement(nameof(AveragementConfig));
            root.AppendChild(avgMeasureCfg);
            avgMeasureCfg.SetAttribute("Times", AveragementConfig.Times.ToString());
            avgMeasureCfg.SetAttribute("Interval", AveragementConfig.Interval.ToString());
            avgMeasureCfg.SetAttribute("IsAutoSave", AveragementConfig.IsAutoSave.ToString());
            avgMeasureCfg.SetAttribute("IsAutoMeasure", AveragementConfig.IsAutoMeasure.ToString());

            var recentFilesNode = doc.CreateElement(nameof(RecentFiles));
            foreach (var recentFile in RecentFiles)
            {
                var fileNameNode = doc.CreateElement("FileName");
                fileNameNode.InnerText = recentFile;
                recentFilesNode.AppendChild(fileNameNode);
            }
            root.AppendChild(recentFilesNode);

            var showSpectrumPlot = doc.CreateElement("ShowSpectrumPlot");
            showSpectrumPlot.SetAttribute("Value", ShowSpectrumPlot.ToString());
            root.AppendChild(showSpectrumPlot);

            var showChromaticity = doc.CreateElement("ShowChromaticity");
            showChromaticity.SetAttribute("Value", ShowChromaticity.ToString());
            root.AppendChild(showChromaticity);

            var TestModelParameter = doc.CreateElement("TestModelParameter");
            root.AppendChild(TestModelParameter);
            TestModelParameter.SetAttribute("StdRepeatTimes", this.TestModelParameter.StdRepeatTimes.ToString());
            TestModelParameter.SetAttribute("StdDelayTimeMs", this.TestModelParameter.StdDelayTimeMs.ToString());
            TestModelParameter.SetAttribute("SampRepeatTimes", this.TestModelParameter.SampRepeatTimes.ToString());
            TestModelParameter.SetAttribute("SampDelayTimeMs", this.TestModelParameter.SampDelayTimeMs.ToString());
            TestModelParameter.SetAttribute("SampMeasurementMode", this.TestModelParameter.SampMeasurementMode.ToString());
            TestModelParameter.SetAttribute("StdMeasurementMode", this.TestModelParameter.StdMeasurementMode.ToString());


            var ICUMSAParameter = doc.CreateElement("ICUMSAParameter");
            root.AppendChild(ICUMSAParameter);
            ICUMSAParameter.SetAttribute("IUThickness", this.ICUMSAParameter.IUThickness.ToString());
            ICUMSAParameter.SetAttribute("IUConcentration", this.ICUMSAParameter.IUConcentration.ToString());
            ICUMSAParameter.SetAttribute("PathLengthEnum", this.ICUMSAParameter.PathLength.ToString());

            doc.Save(fileName);
        }

        public static SystemDefaultConfig Load(string fileName)
        {



            SystemDefaultConfig config = new SystemDefaultConfig();
            if (!File.Exists(fileName))
            {
                // Log.Warn($"XML配置文件不存在：{fileName}，返回默认配置");
                return config;
            }
            XmlDocument doc = new XmlDocument();
            doc.Load(fileName);
            XmlElement root = doc.DocumentElement;
            if (root == null || root.Name != "SystemDefaultConfig")
                return null;
            foreach (var child in root.ChildNodes)
            {
                var elementNode = child as XmlElement;
                switch (elementNode.Name)
                {
                    case "ShowSpectrumPlot":
                        if (!bool.TryParse(elementNode.GetAttribute("Value"), out bool showSpectrumPlot))
                        {
                            showSpectrumPlot = true;
                        }
                        config.ShowSpectrumPlot = showSpectrumPlot;
                        break;

                    case "ShowChromaticity":
                        if (!bool.TryParse(elementNode.GetAttribute("Value"), out bool showChromaticity))
                        {
                            showChromaticity = true;
                        }
                        config.ShowChromaticity = showChromaticity;
                        break;
                    case "ReportSetting":
                        config.ReportSetting.LoadXmlElement(elementNode);
                        break;

                    case "DisplaySetting":
                        config.DisplaySetting.LoadXmlElement(elementNode);
                        break;

                    case "ToleranceSetting":
                        config.ToleranceSetting.LoadXmlElement(elementNode);
                        break;

                    case "JudgementSetting":
                        config.JudgementSetting.LoadXmlElement(elementNode);
                        break;

                    case "ObserverAndIlluminantSetting":
                        config.ObserverAndIlluminantSetting.LoadXmlElement(elementNode);
                        break;

                    case "ConnectionSetting":
                        {
                            ConnectionMethod method = ConnectionMethod.BySpecificalDevice;
                            InstrumentType type = InstrumentType.YS;
                            string attr;
                            if ((attr = elementNode.GetAttribute("ConnectionMethod")) != null
                                && Enum.TryParse(attr, out method))
                            {
                                config.ConnectionMethod = method;
                            }
                            else
                            {
                                config.ConnectionMethod = ConnectionMethod.BySpecificalDevice;
                            }
                            if ((attr = elementNode.GetAttribute("InstrumentType")) != null
                                && Enum.TryParse(attr, out type))
                            {
                                config.InstrumentType = type;
                            }
                            else
                            {
                                config.InstrumentType = type;
                            }
                            config.LatestConnection = elementNode.GetAttribute("LatestConnection");
                        }
                        break;
                    case "UpdateVersionSetting":
                        {
                            config.AppInfoId = elementNode.GetAttribute("AppInfoId");
                            config.ServiceUrl = elementNode.GetAttribute("ServiceUrl");
                        }
                        break;

                    case "NamingSetting":
                        {
                            bool bVal = true;
                            string attr;
                            if ((attr = elementNode.GetAttribute("IsAutoNaming")) != null
                                && bool.TryParse(attr, out bVal))
                            {
                                config.IsAutoNaming = bVal;
                            }
                            else
                            {
                                config.IsAutoNaming = true;
                            }

                            var tmp = elementNode.SelectSingleNode("descendant::StandardAutoNamingRule");
                            if (tmp != null)
                            {
                                config.StandardAutoNamingRule = tmp.InnerText;
                            }
                            tmp = elementNode.SelectSingleNode("descendant::TestAutoNamingRule");
                            if (tmp != null)
                            {
                                config.TestAudotNamingRule = tmp.InnerText;
                            }

                            config.DefaultMaterial = elementNode.GetAttribute("DefaultMaterial");
                        }
                        break;

                    case "InstrumentDataDisplaySetting":
                        config.InstrumentDataDisplaySetting.LoadXmlElement(elementNode);
                        break;

                    case "AutoSelectStandardSetting":
                        {
                            bool bVal = true;
                            string attr;
                            if ((attr = elementNode.GetAttribute("IsAutoSelectStandard")) != null
                                && bool.TryParse(attr, out bVal))
                            {
                                config.IsAutoSelectStandard = bVal;
                            }
                            if (elementNode.FirstChild != null && elementNode.FirstChild.Name == "StandardLimits")
                            {
                                config.AutoSelectStandardLimits.LoadXmlElement(elementNode.FirstChild as XmlElement);
                            }
                        }
                        break;

                    case nameof(AveragementConfig):
                        try
                        {
                            config.AveragementConfig.Times = int.Parse(elementNode.GetAttribute("Times"));
                        }
                        catch
                        {
                            config.AveragementConfig.Times = 3;
                        }
                        try
                        {
                            config.AveragementConfig.Interval = int.Parse(elementNode.GetAttribute("Interval"));
                        }
                        catch
                        {
                            config.AveragementConfig.Interval = 1;
                        }
                        try
                        {
                            config.AveragementConfig.IsAutoMeasure = bool.Parse(elementNode.GetAttribute("IsAutoSave"));
                        }
                        catch
                        {
                            config.AveragementConfig.IsAutoMeasure = false;
                        }
                        try
                        {
                            config.AveragementConfig.IsAutoSave = bool.Parse(elementNode.GetAttribute("IsAutoMeasure"));
                        }
                        catch
                        {
                            config.AveragementConfig.IsAutoSave = true;
                        }
                        break;
                    case nameof(RecentFiles):
                        try
                        {
                            config._recentFiles.Clear();
                            foreach (XmlElement node in elementNode.GetElementsByTagName("FileName"))
                            {
                                config.AddRecentFile(node.InnerText);
                            }
                        }
                        catch (Exception)
                        {
                            // do nothing
                        }
                        break;
                    case "TestModelParameter":
                        string stdstr;
                        string samstr;
                        MeasurementMode measurementMode;
                        config.TestModelParameter.StdDelayTimeMs = Convert.ToDouble(elementNode.GetAttribute("StdDelayTimeMs"));
                        config.TestModelParameter.StdRepeatTimes = Convert.ToInt32(elementNode.GetAttribute("StdRepeatTimes"));
                        config.TestModelParameter.SampDelayTimeMs = Convert.ToDouble(elementNode.GetAttribute("SampDelayTimeMs"));
                        config.TestModelParameter.SampRepeatTimes = Convert.ToInt32(elementNode.GetAttribute("SampRepeatTimes"));
                        if ((stdstr = elementNode.GetAttribute("SampMeasurementMode")) != null
                               && Enum.TryParse(stdstr, out measurementMode))
                        {
                            config.TestModelParameter.SampMeasurementMode = measurementMode;
                        }
                        if ((samstr = elementNode.GetAttribute("StdMeasurementMode")) != null
                               && Enum.TryParse(samstr, out measurementMode))
                        {
                            config.TestModelParameter.StdMeasurementMode = measurementMode;
                        }
                        break;

                    case "ICUMSAParameter":
                        string pathLengthstr;
                        PathLengthEnum pathLengthEnum;
                        config.ICUMSAParameter.IUThickness = Convert.ToDouble(elementNode.GetAttribute("IUThickness"));
                        config.ICUMSAParameter.IUConcentration = Convert.ToInt32(elementNode.GetAttribute("IUConcentration"));
                        if ((pathLengthstr = elementNode.GetAttribute("PathLengthEnum")) != null
                               && Enum.TryParse(pathLengthstr, out pathLengthEnum))
                        {
                            config.ICUMSAParameter.PathLength = pathLengthEnum;
                        }
                        break;

                }
            }

            return config;
        }

        /// <summary>
        /// 版本更新地址
        /// </summary>
        public string ServiceUrl { get; set; }

        /// <summary>
        /// 版本更新用的appInfoId
        /// </summary>
        public string AppInfoId { get; set; }
        /// <summary>
        /// 报表设置
        /// </summary>
        public ReportSetting ReportSetting { get; set; }
        /// <summary>
        /// 显示设由
        /// </summary>
        public ChromaticitySetting DisplaySetting { get; set; }
        /// <summary>
        /// 仪器数据管理的显示设置
        /// </summary>
        public ChromaticitySetting InstrumentDataDisplaySetting { get; set; }
        /// <summary>
        /// 系统容差设置
        /// </summary>
        public SystemToleranceSetting ToleranceSetting { get; set; }
        /// <summary>
        /// 连接方式设置
        /// </summary>
        public ConnectionMethod ConnectionMethod { get; set; }

        /// <summary>
        /// 测量模式参数设置
        /// </summary>
        public TestModel TestModelParameter { get; set; }

        /// <summary>
        /// 液体参数设置
        /// </summary>
        public ICUMSAParameter ICUMSAParameter { get; set; } = new ICUMSAParameter();
        /// <summary>
        /// 最近的连接地址
        /// </summary>
        public string LatestConnection { get; set; }
        /// <summary>
        /// 连接的仪器类型
        /// </summary>
        public InstrumentType InstrumentType { get; set; }
        /// <summary>
        /// 是否自动命名
        /// </summary>
        public bool IsAutoNaming { get; set; }

        public bool AutoSaveStandardToLibrary { get; set; }
        /// <summary>
        /// 是否自动连接设备
        /// </summary>
        public bool AutoConnectToDevice { get; set; }

        /// <summary>
        /// 标样自动命名规则 
        /// </summary>
        public string StandardAutoNamingRule { get; set; }
        /// <summary>
        /// 试样自动命名规则
        /// </summary>
        public string TestAudotNamingRule { get; set; }
        /// <summary>
        /// 默认的料号
        /// </summary>
        public string DefaultMaterial { get; set; }
        /// <summary>
        /// 断定设置
        /// </summary>
        public JudgementSetting JudgementSetting { get; set; }
        /// <summary>
        /// 观察者角度和光源设置
        /// </summary>
        public ObserverAndIlluminantSetting ObserverAndIlluminantSetting { get; set; }
        /// <summary>
        /// 是否在测量试样时自动为其选择一个标样
        /// </summary>
        public bool IsAutoSelectStandard { get; set; }

        public Tolerance AutoSelectStandardLimits { get; set; }

        /// <summary>
        /// 平均测量配置
        /// </summary>
        public AveragementConfig AveragementConfig { get; set; } = new AveragementConfig();

        private List<string> _recentFiles = new List<string>();
        public string[] RecentFiles
        {
            get { return _recentFiles.ToArray(); }
        }

        public bool ShowSpectrumPlot { get; set; } = true;

        /// <summary>
        /// 显示色品图还是容差图
        /// </summary>
        public bool ShowChromaticity { get; set; } = true;

        public void AddRecentFile(string filePath)
        {
            // 如果已经包含该文件，将其移到最前
            if (_recentFiles.Contains(filePath))
            {
                if (_recentFiles[0] != filePath)
                {
                    _recentFiles.Remove(filePath);
                    _recentFiles.Insert(0, filePath);
                }
                return;
            }

            // 最多存储10个最近使用过的文件
            if (_recentFiles.Count >= 10)
            {
                _recentFiles.RemoveRange(9, _recentFiles.Count - 9);
            }
            _recentFiles.Insert(0, filePath);
        }

        public void RemoveFromRecentFileList(string filePath)
        {
            _recentFiles.Remove(filePath);
        }
    }
}
