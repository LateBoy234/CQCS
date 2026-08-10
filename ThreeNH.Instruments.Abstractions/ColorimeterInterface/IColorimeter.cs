using System;
using ThreeNH.ColorSpecimen;
using ThreeNH.Optical;

namespace ThreeNH.ColorimeterInterface
{
    public class SpecimenReceivedEventArgs : EventArgs
    {
        public SpecimenReceivedEventArgs(Specimen[] specimens, bool hasMore = false)
        {
            Specimens = specimens;
            HasMore = hasMore;
        }
        public SpecimenReceivedEventArgs(Exception ex)
        {
            Exception = ex;
        }
        public SpecimenReceivedEventArgs(Specimen specimen)
        {
            Specimens = new[] { specimen };
        }

        /// <summary>
        /// 接收到的样品数据
        /// </summary>
        public Specimen[] Specimens { get; set; }
        /// <summary>
        /// 接收到的异常；当Specimens为null时可能会附带异常数据
        /// </summary>
        public Exception Exception { get; set; }
        /// <summary>
        /// 指示是否有更多数据
        /// </summary>
        public bool HasMore { get; set; }
    }

    public enum InstrumentLanguage
    {
        English,
        SimplifiedChinese,
        TraditionalChinese,
        Russian,
        Korea,
    }

    /// <summary>
    /// 色差仪接口：在 <see cref="IInstrument"/> 之上仅保留测色特有能力。
    /// 计量见 <see cref="IMetrology"/> / <see cref="IMetrologyColorimeter"/>；
    /// 样品读写见 <see cref="IRecordManagement"/>；系统设置见 <see cref="ISystemSetting"/>。
    /// </summary>
    public interface IColorimeter : IInstrument
    {
        /// <summary>
        /// 测光方式（SCI/SCE）
        /// </summary>
        SpecularComponent SpecularComponent { get; set; }

        /// <summary>
        /// 测量口径
        /// </summary>
        MeasurementCaliber Aperture { get; }
    }

    /// <summary>
    /// 具备计量（SVD/OLSR）能力的色差仪。普通测色场景勿依赖本接口。
    /// </summary>
    public interface IMetrologyColorimeter : IColorimeter, IMetrology
    {
    }
}
