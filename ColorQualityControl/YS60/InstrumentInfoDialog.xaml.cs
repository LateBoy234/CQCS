using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ThreeNH.CommunicationBase;

namespace ThreeNH.ColorQualityControl.YS60
{
    /// <summary>
    /// InstrumentInfoDialog.xaml 的交互逻辑
    /// </summary>
    public partial class InstrumentInfoDialog : Window
    {
        public InstrumentInfoDialog(BTS.Spectrometer spectrometer)
        {
            try
            {
                DeviceInfo = spectrometer.GetDeviceInfo();
                Status = spectrometer.GetInstrumentStatus();
            }
            catch (CommunicationException ex)
            {
                Trace.TraceError("Get device information failed: {0}", S._(ex.Message));
            }
            InitializeComponent();
            DataContext = this;
        }

        public BTS.DeviceInfo DeviceInfo { get; private set; }

        public BTS.InstrumentStatus Status { get; private set; }
    }
}
