namespace ThreeNH.ColorQualityControl
{
    public class Statistics
    {
        /// <summary>
        /// 统计个数
        /// </summary>
        public int Total { get; set; }
        /// <summary>
        /// 有效个数
        /// </summary>
        public int Valid { get; set; }
        /// <summary>
        /// 合格数
        /// </summary>
        public int Pass { get; set; }
        /// <summary>
        /// 不合格数
        /// </summary>
        public int NotGood { get; set; }
        /// <summary>
        /// 警告的个数
        /// </summary>
        public int Warning { get; set; }
        /// <summary>
        /// 合格率
        /// </summary>
        public double PercentOfPass
        { 
            get { return 100.0 * Pass / Total; }
        }
        /// <summary>
        /// 不良率
        /// </summary>
        public double PercenOfNotGood
        {
            get { return 100.0 - PercentOfPass; }
        }
        /// <summary>
        /// 最小值
        /// </summary>
        public double Min { get; set; }
        /// <summary>
        /// 最大值
        /// </summary>
        public double Max { get; set; }
        /// <summary>
        /// 平均值
        /// </summary>
        public double Mean { get; set; }
        /// <summary>
        /// 标准差
        /// </summary>
        public double StdDev { get; set; }
        /// <summary>
        /// 最大与最小值之差
        /// </summary>
        public double Range
        {
            get { return Max - Min; }
        }
    }
}
