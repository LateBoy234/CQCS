namespace PTS
{
    /// <summary>
    /// USB（ProductInfo*）与蓝牙（BluetoothDongleSpectrometer）共用的特殊机型判定。
    /// 新增需走特殊协议/数据处理的仪器时，只改本类。
    /// </summary>
    public static class SpecialInstrumentType
    {
        /// <summary>
        /// 是否为特殊机型（PTS21/45、PTS20 部分型号、TS7700 系列特定内部型号）。
        /// </summary>
        /// <param name="productType">产品大类（USB 为 ProductInfo.InternalModel；蓝牙为静态 ProductType）。</param>
        /// <param name="deviceType">内部型号字节（USB 为 ProductInfo.DeviceType；蓝牙为 InternalModel）。</param>
        public static bool IsSpecialType(ProductType productType, byte deviceType)
        {
            if (productType == ProductType.PTS21 || productType == ProductType.PTS45)
            {
                return true;
            }

            // DeviceType 在不同 ProductType 下复用同一字节，按枚举值匹配特殊型号
            var pts20 = (InternalModelTypeOfPTS20)deviceType;
            if (pts20 == InternalModelTypeOfPTS20.CM700Plus
                || pts20 == InternalModelTypeOfPTS20.ST700DPlus)
            {
                return true;
            }

            if (productType != ProductType.TS7700)
            {
                return false;
            }

            switch ((InternalModelTypeOf7700)deviceType)
            {
                case InternalModelTypeOf7700.TS7600:
                case InternalModelTypeOf7700.TS7700:
                case InternalModelTypeOf7700.TS7600B:
                case InternalModelTypeOf7700.TS7708:
                case InternalModelTypeOf7700.ST50:
                case InternalModelTypeOf7700.ST60:
                case InternalModelTypeOf7700.ST70:
                case InternalModelTypeOf7700.TS7708L:
                    return true;
                default:
                    return false;
            }
        }
    }
}
