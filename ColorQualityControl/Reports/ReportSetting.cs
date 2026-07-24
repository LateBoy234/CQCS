using System.Xml;

namespace ThreeNH.ColorQualityControl.Reports
{
    public class ReportSetting
    {
        public string Title { get; set; }
        public string Subtitle { get; set; }
        public string CompanyAddress { get; set; }
        public string CompanyEnglishAddress { get; set; }
        // 是否导出容差
        public bool IsExportTolerance { get; set; } = true;
        // 是否导出参考光源
        public bool IsExportReferences { get; set; } = true;

        public bool IsExportChromaticityChart { get; set; } = true;
        //色差累计报告导出试样详细LAB
        public bool IsExportTestLab { get; set; } = true;

        public XmlElement ToXmlElement(XmlDocument doc, string nodeName)
        {
            var node = doc.CreateElement(nodeName);
            if (!string.IsNullOrWhiteSpace(Title))
                node.SetAttribute("Title", Title);
            if (!string.IsNullOrWhiteSpace(Subtitle))
                node.SetAttribute("Subtitle", Subtitle);
            if (!string.IsNullOrWhiteSpace(CompanyAddress))
                node.SetAttribute("CompanyAddress", CompanyAddress);
            if (!string.IsNullOrWhiteSpace(CompanyEnglishAddress))
                node.SetAttribute("CompanyEnglishAddress", CompanyEnglishAddress);
            node.SetAttribute("IsExportTolerance", IsExportTolerance.ToString());
            // 修复误写：原先把 IsExportReferences 存成了 IsExportTolerance 的值
            node.SetAttribute("IsExportReferences", IsExportReferences.ToString());
            node.SetAttribute("IsExportTestLab", IsExportTestLab.ToString());

            return node;
        }

        public bool LoadXmlElement(XmlElement node)
        {
            if (node == null)
                return false;

            Title = node.GetAttribute("Title");
            Subtitle = node.GetAttribute("Subtitle");
            CompanyAddress = node.GetAttribute("CompanyAddress");
            CompanyEnglishAddress = node.GetAttribute("CompanyEnglishAddress");
            // 历史配置缺属性时回退 true，与属性默认值一致
            if (!bool.TryParse(node.GetAttribute("IsExportTolerance"), out bool isExportTolerance))
                isExportTolerance = true;
            IsExportTolerance = isExportTolerance;

            if (!bool.TryParse(node.GetAttribute("IsExportReferences"), out bool isExportReferences))
                isExportReferences = true;
            IsExportReferences = isExportReferences;

            if (!bool.TryParse(node.GetAttribute("IsExportTestLab"), out bool isExportTestLab))
                isExportTestLab = true;
            IsExportTestLab = isExportTestLab;

            return true;
        }

        public void Copy(ReportSetting source)
        {
            Title = source.Title;
            Subtitle = source.Subtitle;
            CompanyAddress = source.CompanyAddress;
            CompanyEnglishAddress = source.CompanyEnglishAddress;
            IsExportTolerance = source.IsExportTolerance;
            IsExportChromaticityChart = source.IsExportChromaticityChart;
            IsExportReferences = source.IsExportReferences;
            IsExportTestLab = source.IsExportTestLab;
        }
    }
}