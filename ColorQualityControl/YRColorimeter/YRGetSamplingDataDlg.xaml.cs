using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Win32;
using ThreeNH.CommunicationBase;
using ThreeNH.SQCX.Infrastructure.Instruments.YRColorimeter;

namespace ThreeNH.ColorQualityControl.YRColorimeter
{
    /// <summary>
    /// YRGetSamplingDataDlg.xaml 的交互逻辑
    /// </summary>
    public partial class YRGetSamplingDataDlg : Window, INotifyPropertyChanged
    {
        private Colorimeter colorimeter;
        private uint _times = 1;
        private uint _interval = 5;
        private DispatcherTimer _timer;
        private uint _measured_times = 0;

        public event PropertyChangedEventHandler PropertyChanged;


        public YRGetSamplingDataDlg(Colorimeter colorimeter)
        {
            this.colorimeter = colorimeter;
            SamplingDataList = new ObservableCollection<YRSamplingData>();

            _timer = new DispatcherTimer();
            _timer.Tick += OnTimerTick;

            DataContext = this;
            InitializeComponent();
        }

        private void TakeSamples()
        {

            try
            {
                YRSamplingData samplingData;
                colorimeter.YRGetSamplingData(out samplingData);
                SamplingDataList.Add(samplingData);
            }
            catch (CommunicationException ex)
            {
                MessageBox.Show(S._(ex.ErrorCode.ToString()));
                if (_timer.IsEnabled)
                {
                    _timer.Stop();
                    measureBtn.Content = "测量";
                }
            }
        }

        private void OnTimerTick(object sender, EventArgs e)
        {
            if (++_measured_times == _times)
            {
                _timer.Stop();
                measureBtn.Content = "测量";
            }
            TakeSamples();
        }

        private void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public ObservableCollection<YRSamplingData> SamplingDataList
        {
            get;
            private set;
        }

        public uint Times
        {
            get { return _times; }
            set
            {
                if (_times != value)
                {
                    _times = value;
                    NotifyPropertyChanged("Times");
                }
            }
        }

        public uint Interval
        {
            get { return _interval; }
            set
            {
                if (_interval != value)
                {
                    _interval = value;
                    NotifyPropertyChanged("Interval");
                }
            }
        }

        private void DataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.Header = e.Row.GetIndex() + 1;
        }

        private void BtnMeasure_Click(object sender, RoutedEventArgs e)
        {
            if (_timer.IsEnabled)
            {
                _timer.Stop();
                measureBtn.Content = "测量";
            }
            else
            {
                TakeSamples();
                _measured_times = 1;
                if (Times > 1)
                {
                    measureBtn.Content = "停止";
                    _timer.Interval = new TimeSpan(0, 0, (int)Interval);
                    _timer.Start();
                }
            }
        }

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Filter = S._("ExportFileFilter"); // "Excel Workbook(*.xlsx)|*.xlsx|Text(*.txt)|*.txt";
            if (dlg.ShowDialog() != true)
                return;


            IMyWriter myWriter = new MyExcelWriter();

            try
            {
                myWriter.Create(dlg.FileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return;
            }

            var headers = new string[]
            {
                "序号",
                "主灯亮-X", "主灯亮-Y", "主灯亮-Z",
                "辅灯亮-X", "辅灯亮-Y", "辅灯亮-Z",
                "主灯灭-X", "主灯灭-Y", "主灯灭-Z",
                "辅灯灭-X", "辅灯灭-Y", "辅灯灭-Z",
            };
            myWriter.WriteRow(headers);

            int rowIndex = 1;
            foreach(var item in SamplingDataList)
            {
                List<String> rowItems = new List<string>();
                rowItems.Add(rowIndex.ToString());
                for (int i = 0; i < 3; i++)
                {
                    rowItems.Add(item.MajorLightOn[i].ToString());
                }
                for (int i = 0; i < 3; i++)
                {
                    rowItems.Add(item.MinorLightOn[i].ToString());
                }
                for (int i = 0; i < 3; i++)
                {
                    rowItems.Add(item.MajorLightOff[i].ToString());
                }
                for (int i = 0; i < 3; i++)
                {
                    rowItems.Add(item.MinorLightOff[i].ToString());
                }

                myWriter.WriteRow(rowItems.ToArray());

                rowIndex += 1;
            }

            myWriter.Close();
        }

        private void ClearBtn_Click(object sender, RoutedEventArgs e)
        {
            SamplingDataList.Clear();
        }
    }
}
