using System;
using System.ComponentModel;
using ThreeNH.ColorimeterInterface;
using ThreeNH.ColorQualityControl.Refactoring.Contracts;
using ThreeNH.ColorQualityControl.Services;
using ThreeNH.CommunicationBase;
using ThreeNH.YL30;
using static ThreeNH.CommunicationBase.EhMc17AtInterface;

namespace ThreeNH.ColorQualityControl.ConnectStrategies
{
    /// <summary>
    /// BackgroundWorker 连接线程上下文，策略通过它访问 Host/Service 而不直接依赖 MainWindow。
    /// </summary>
    internal sealed class InstrumentConnectContext
    {
        public InstrumentConnectContext(
            BackgroundWorker worker,
            DoWorkEventArgs args,
            IInstrumentConnectionHost host,
            InstrumentConnectionService service)
        {
            Worker = worker;
            Args = args;
            Host = host;
            Service = service;
            Progress = 1;
        }

        public BackgroundWorker Worker { get; }

        public DoWorkEventArgs Args { get; }

        public IInstrumentConnectionHost Host { get; }

        public InstrumentConnectionService Service { get; }

        public int Progress { get; set; }

        public object Address => Args.Argument;

        public void SetResult(Tuple<IInstrument, InstrumentType> result) => Args.Result = result;

        public void ReportProgress(string message) => Worker.ReportProgress(Progress++, message);

        public void ReportProgress(ConnectionProgressRequest request) => Worker.ReportProgress(Progress++, request);

        public void ReportProgress(int percent, string message) => Worker.ReportProgress(percent, message);

        public void ReportProgress(int percent, ConnectionProgressRequest request) => Worker.ReportProgress(percent, request);

        public object SelectedDevice
        {
            get => Service.SelectedDevice;
            set => Service.SelectedDevice = value;
        }

        public string SelectedPortName
        {
            get => Service.SelectedPortName;
            set => Service.SelectedPortName = value;
        }

        public YL30Communication SelectedSpecialDevice
        {
            get => Service.SelectedSpecialDevice;
            set => Service.SelectedSpecialDevice = value;
        }

        public void ResetDeviceSelectionWait() => Service.ResetDeviceSelectionWait();

        public void WaitForDeviceSelection() => Service.WaitForDeviceSelection();
    }
}
