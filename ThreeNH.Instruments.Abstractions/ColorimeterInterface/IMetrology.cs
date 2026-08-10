using System.Collections.Generic;

namespace ThreeNH.ColorimeterInterface
{
    /// <summary>
    /// YH/计量测量类型
    /// </summary>
    public enum MeasureType : byte
    {
        Compersation,
        OpticalTrap
    }

    /// <summary>
    /// 计量专用接口（SVD/OLSR）。勿挂到所有色差仪上，需要时实现本接口或 <see cref="IMetrologyColorimeter"/>。
    /// </summary>
    public interface IMetrology
    {
        List<float> GetMeasureSpectrumEight(int numbers = 8);

        bool SetSVD(List<List<float>> datas, MeasureType? measureType = null, byte caliber = 0);
        bool SetOLSR(List<List<float>> datas);
        List<List<float>> ReadSVDs(MeasureType? measureType = null, int rows = 31, int cols = 16);

        List<float> ReadSVDList(MeasureType? measureType = null, int rows = 31, int cols = 16);

        List<float> ReadOLSRList(MeasureType? measureType = null);
    }
}
