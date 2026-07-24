
using System;
using System.Globalization;
using System.Windows;
using ThreeNH.DataModel;
using ThreeNH.Optical;

namespace ThreeNH.ColorQualityControl.UI
{
    /// <summary>
    /// TestModeSettingView.xaml 的交互逻辑
    /// </summary>
    public partial class TestModeSettingView : Window
    {

        internal SystemDefaultConfig SystemDefaultConfig
        {
            get { return (Application.Current as App).SystemDefaultConfig; }
        }
        public TestModeSettingView()
        {
            InitializeComponent();

            tb_measureTimes.Text = SystemDefaultConfig.TestModelParameter.StdRepeatTimes.ToString();
            tb_measureInterval.Text = SystemDefaultConfig.TestModelParameter.StdDelayTimeMs == 0 ? "1" : SystemDefaultConfig.TestModelParameter.StdDelayTimeMs.ToString();


            tb_samMeasureTimes.Text = SystemDefaultConfig.TestModelParameter.SampRepeatTimes.ToString();
            tb_samMeasureInterval.Text = SystemDefaultConfig.TestModelParameter.SampDelayTimeMs == 0 ? "1" : SystemDefaultConfig.TestModelParameter.SampDelayTimeMs.ToString();

            OnStdTestModelChanged(SystemDefaultConfig.TestModelParameter);
        }


        private void OnStdTestModelChanged(TestModel newValue)
        {
            switch (newValue.StdMeasurementMode)
            {
                case MeasurementMode.SingleMeasure:
                    rd_stdSinggle.IsChecked = true;
                    break;
                case MeasurementMode.ContinuousMeasure:
                    re_stdContinu.IsChecked = true;
                    break;
                default:
                    rd_stdSinggle.IsChecked = true;
                    break;
            }
            switch (newValue.SampMeasurementMode)
            {
                case MeasurementMode.SingleMeasure:
                    rd_samSinggle.IsChecked = true;
                    break;
                case MeasurementMode.ContinuousMeasure:
                    re_samContinu.IsChecked = true;
                    break;
                default:
                    rd_samSinggle.IsChecked = true;
                    break;
            }
        }
        private void OkClick(object sender, RoutedEventArgs e)
        {
            TestModel testModelChanged = new TestModel();

            testModelChanged.StdMeasurementMode = rd_stdSinggle.IsChecked == true ? MeasurementMode.SingleMeasure : MeasurementMode.ContinuousMeasure;
            testModelChanged.SampMeasurementMode = rd_samSinggle.IsChecked == true ? MeasurementMode.SingleMeasure : MeasurementMode.ContinuousMeasure;

            int st, sd, spt, spd;

            int.TryParse(tb_measureTimes.Text, out st);
            testModelChanged.StdRepeatTimes = st;

            int.TryParse(tb_measureInterval.Text, out sd);
            testModelChanged.StdDelayTimeMs = sd;

            int.TryParse(tb_samMeasureTimes.Text, out spt);
            testModelChanged.SampRepeatTimes = spt;

            int.TryParse(tb_samMeasureInterval.Text, out spd);
            testModelChanged.SampDelayTimeMs = spd;

            SystemDefaultConfig.TestModelParameter= testModelChanged;

            this.Close();
        }

        private void CancleClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void OnModeChanged(object sender, RoutedEventArgs e)
        {
            // 单选时隐藏，连续时显示
            panelTestContinuous.Visibility = rd_samSinggle.IsChecked == true
                                         ? Visibility.Collapsed
                                         : Visibility.Visible;
            panelStandardContinuous.Visibility = rd_stdSinggle.IsChecked == true
                                         ? Visibility.Collapsed
                                         : Visibility.Visible;
        }
    }
}
