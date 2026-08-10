namespace ThreeNH.ColorimeterInterface
{
    /// <summary>
    /// 批量清空仪器内样品。从 IColorimeter 拆出，避免与完整 IRecordManagement 强绑定。
    /// </summary>
    public interface ISpecimenClearing
    {
        void DeleteAllSpecimens();

        /// <param name="standard">标样索引；-1 表示全部试样</param>
        void DeleteTests(int standard);
    }
}
