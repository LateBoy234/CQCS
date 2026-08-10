namespace ThreeNH.ColorimeterInterface
{
    /// <summary>
    /// 仪器维护类设置（出厂/语言/时间）。未实现完整 ISystemSetting 的机型可只实现本接口。
    /// </summary>
    public interface IInstrumentMaintenance
    {
        void RestoreFactorySettings();
        void SetInstrumentLanguage(InstrumentLanguage lang);
        void SetDateTime(System.DateTime datetime);
    }
}
