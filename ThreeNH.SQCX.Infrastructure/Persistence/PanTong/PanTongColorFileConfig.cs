
using System.Runtime.Serialization;
using ThreeNH.ColorAlgorithm;

namespace PanTongColorMatchSearch
{
    /// <summary>
    /// EXCEl导入时SCI设置
    /// </summary>
    public enum ScModeSetting
    {
        None, // 非SCI/SCE数据
        Sci,   // 只有SCI
        Sce,    // 只有SCE
        SciFollowWithSce,   // SCI后跟SCE
        SceFollowWithSci,   // SCE后跟SCI
    }

    /// <summary>
    /// 白板数据文件格式
    /// </summary>
    [DataContract]
    public class PanTongColorFileConfig
    {
        /// <summary>
        /// 是否不为45/0的判断 true 不是 false 是
        /// </summary>
        public bool IfNot45_0;
        /// <summary>
        /// 数据文件位置
        /// </summary>
        [DataMember]
        public string FilePath { get; set; }

        /// <summary>
        /// 所在表单页
        /// </summary>
        [DataMember]
        public string SheetName { get; set; }

        /// <summary>
        /// 数据起始行号
        /// </summary>
        [DataMember]
        public int StartRow { get; set; }

        /// <summary>
        /// 结束行号
        /// </summary>
        [DataMember]
        public int EndRow { get; set; }

        /// <summary>
        /// 样品名称所在列
        /// </summary>
        [DataMember]
        public string SampleNameColumn { get; set; }

        /// <summary>
        /// 样品备注所在列
        /// </summary>
        [DataMember]
        public string SampleRemarksColumn { get; set; }

        /// <summary>
        /// XYZ起始列
        /// </summary>
        [DataMember]
        public string YxyStartColumn { get; set; }

        /// <summary>
        /// Lab起始列
        /// </summary>
        [DataMember]
        public string LabStartColumn { get; set; }

        /// <summary>
        /// 光谱数据起始列
        /// </summary>
        [DataMember]
        public string SpectraStartColumn { get; set; }

        /// <summary>
        /// 光谱波长范围
        /// </summary>
        [DataMember]
        public WavelengthRange WavelengthRange { get; set; }

        /// <summary>
        /// SCI/SCE设置
        /// </summary>
        [DataMember]
        public ScModeSetting ScModeSetting { get; set; }

        /// <summary>
        /// 测试日期
        /// </summary>
        [DataMember]
        public string TestDateTimeColumn { get; set; }

        /// <summary>
        /// 注释
        /// </summary>
        public string IllumObserRemarks { get; set; }
    }
}
