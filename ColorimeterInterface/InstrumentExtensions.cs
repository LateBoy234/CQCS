using System;

namespace ThreeNH.ColorimeterInterface
{
    /// <summary>
    /// 仪器接口兼容扩展：统一连接生命周期命名。
    /// </summary>
    public static class InstrumentExtensions
    {
        /// <summary>
        /// 历史 API 名为 Disconnect，与基接口 Close 同义，避免调用方继续分叉。
        /// </summary>
        public static void Disconnect(this IInstrument instrument)
        {
            instrument?.Close();
        }
    }

    /// <summary>
    /// 色差仪能力扩展：把已下沉到可选接口的操作，继续以扩展方法暴露给旧调用方。
    /// </summary>
    public static class ColorimeterExtensions
    {
        public static bool GetYTContain400(this IColorimeter instrument)
        {
            return (instrument as IYtWavelengthCapability)?.YTContain400 ?? false;
        }

        public static void DeleteAllSpecimens(this IColorimeter instrument)
        {
            if (instrument is ISpecimenClearing clearing)
            {
                clearing.DeleteAllSpecimens();
                return;
            }
            if (instrument is IRecordManagement records)
            {
                records.RemoveAllRecords();
                return;
            }
            throw new NotSupportedException("当前仪器不支持删除全部样品");
        }

        public static void RestoreFactorySettings(this IColorimeter instrument)
        {
            if (instrument is ISystemSetting settings)
            {
                settings.RestoreFactorySettings();
                return;
            }
            if (instrument is IInstrumentMaintenance maintenance)
            {
                maintenance.RestoreFactorySettings();
                return;
            }
            throw new NotSupportedException("当前仪器不支持恢复出厂设置");
        }

        public static void SetInstrumentLanguage(this IColorimeter instrument, InstrumentLanguage lang)
        {
            if (instrument is ISystemSetting settings)
            {
                settings.SetInstrumentLanguage(lang);
                return;
            }
            if (instrument is IInstrumentMaintenance maintenance)
            {
                maintenance.SetInstrumentLanguage(lang);
                return;
            }
            throw new NotSupportedException("当前仪器不支持设置语言");
        }

        public static void SetDateTime(this IColorimeter instrument, DateTime datetime)
        {
            if (instrument is ISystemSetting settings)
            {
                settings.SetDateTime(datetime);
                return;
            }
            if (instrument is IInstrumentMaintenance maintenance)
            {
                maintenance.SetDateTime(datetime);
                return;
            }
            throw new NotSupportedException("当前仪器不支持设置时间");
        }
    }
}
