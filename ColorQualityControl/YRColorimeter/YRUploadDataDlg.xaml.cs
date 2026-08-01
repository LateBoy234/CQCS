using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using ThreeNH.ColorAlgorithm;
using ThreeNH.CommunicationBase;
using ThreeNH.SQCX.Infrastructure.Instruments.YRColorimeter;

namespace ThreeNH.ColorQualityControl.YRColorimeter
{
    public class YRSpecimen
    {
        static Formula formula = new Formula();

        public YRSpecimen(SampleData sampleData)
        {
            formula.Observer = sampleData.Color[0].Observer;
            formula.Illuminant = sampleData.Color[0].Illuminant;

            XYZ = sampleData.Color[0].XYZ;
            Lab = formula.XyzToLab(XYZ);
            var rgb = Formula.LabToSrgb(Lab);
            PseudoColor = new SolidColorBrush(Color.FromRgb(rgb.R, rgb.G, rgb.B));
            RawData = sampleData.RawData;
        }

        public string Name { get; set; }
        public SolidColorBrush PseudoColor { get; private set; }
        public CIELAB Lab { get; private set; }
        public CIEXYZ XYZ { get; private set; }
        public YRSamplingData RawData { get; private set; }
    }

    public class YRStandard : YRSpecimen
    {
        public YRStandard(SampleData sampleData) : base(sampleData)
        {
        }
        public ObservableCollection<YRSpecimen> Tests { get; set; }
    }


    /// <summary>
    /// YRUploadDataDlg.xaml 的交互逻辑
    /// </summary>
    public partial class YRUploadDataDlg : Window
    {
        Colorimeter colorimeter;

        public YRUploadDataDlg(Colorimeter colorimeter)
        {
            this.colorimeter = colorimeter;
            SpecimenList = new ObservableCollection<YRSpecimen>();
            InitializeComponent();

            DataContext = this;
        }

        private void DataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.Header = e.Row.GetIndex() + 1;
        }

        public ObservableCollection<YRSpecimen> SpecimenList { get; private set; }


        private void HandleReceivedStandards(SampleData[] samples, bool hasMore, Exception ex)
        {
            if (ex != null)
            {
                MessageBox.Show(ex.Message);
            }
            else
            {
                foreach (var sample in samples)
                {
                    SpecimenList.Add(new YRStandard(sample));
                }
                processBar.Value = SpecimenList.Count;
                processTextBlock.Text = String.Format("{0}/{0}", processBar.Value, processBar.Maximum);
            }
        }
        private void OnReceivedStandard(SampleData[] samples, bool hasMore, Exception ex)
        {
            Dispatcher.Invoke(new Action<SampleData[], bool, Exception>(HandleReceivedStandards), samples, hasMore, ex);
        }

        private void BtnUploadStandard_Click(object sender, RoutedEventArgs e)
        {
            //dataGrid.ItemsSource = null;
            SpecimenList.Clear();

            try
            {
                int count = colorimeter.GetStandardCount();
                if (count > 0)
                {
                    processBar.Minimum = 0;
                    processBar.Maximum = count;
                    processBar.Value = 0;
                    processTextBlock.Text = String.Format("{0}/{0}", processBar.Value, processBar.Maximum);
                }
                colorimeter.YRUploadStandardAsync(OnReceivedStandard, 0, count);
            }
            catch (CommunicationException ex)
            {
                MessageBox.Show(S._(ex.Message));
            }
        }

        private void BtnUploadTests_Click(object sender, RoutedEventArgs e)
        {

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
                "仿真色",
                "L*", "a*", "b*", "X", "Y", "Z",
                "主灯亮-X", "主灯亮-Y", "主灯亮-Z",
                "辅灯亮-X", "辅灯亮-Y", "辅灯亮-Z",
                "主灯灭-X", "主灯灭-Y", "主灯灭-Z",
                "辅灯灭-X", "辅灯灭-Y", "辅灯灭-Z",
            };
            myWriter.WriteRow(headers);

            int rowIndex = 1;
            foreach (var item in SpecimenList)
            {
                List<String> rowItems = new List<string>();
                rowItems.Add(rowIndex.ToString());
                rowItems.Add(item.PseudoColor.Color.ToString());
                rowItems.Add(item.Lab.L.ToString("F2"));
                rowItems.Add(item.Lab.a.ToString("F2"));
                rowItems.Add(item.Lab.b.ToString("F2"));
                rowItems.Add(item.XYZ.X.ToString("F2"));
                rowItems.Add(item.XYZ.Y.ToString("F2"));
                rowItems.Add(item.XYZ.Z.ToString("F2"));
                for (int i = 0; i < 3; i++)
                {
                    rowItems.Add(item.RawData.MajorLightOn[i].ToString());
                }
                for (int i = 0; i < 3; i++)
                {
                    rowItems.Add(item.RawData.MinorLightOn[i].ToString());
                }
                for (int i = 0; i < 3; i++)
                {
                    rowItems.Add(item.RawData.MajorLightOff[i].ToString());
                }
                for (int i = 0; i < 3; i++)
                {
                    rowItems.Add(item.RawData.MinorLightOff[i].ToString());
                }

                myWriter.WriteRow(rowItems.ToArray());

                rowIndex += 1;
            }

            myWriter.Close();
        }
    }
}
