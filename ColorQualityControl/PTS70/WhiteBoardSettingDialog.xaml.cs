﻿﻿﻿using ClosedXML.Excel;
using MahApps.Metro.Controls;
using Microsoft.Win32;
using ThreeNH.SQCX.Infrastructure.Instruments.PTS;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ThreeNH.ColorAlgorithm;
using ThreeNH.CommunicationBase;

namespace ThreeNH.ColorQualityControl.PTS70
{
    /// <summary>
    /// WhiteBoardSettingDialog.xaml 的交互逻辑
    /// </summary>
    public partial class WhiteBoardSettingDialog : MetroWindow, INotifyPropertyChanged
    {
        private ThreeNH.SQCX.Infrastructure.Instruments.PTS.Spectrometer _spectrometer;
        private ThreeNH.SQCX.Infrastructure.Instruments.PTS.ProductInfo _productInfo;
        public WhiteBoardSettingDialog(ThreeNH.SQCX.Infrastructure.Instruments.PTS.Spectrometer spectrometer)
        {
            InitializeComponent();
            _spectrometer = spectrometer;
            DataContext = this;
            Read();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void BrowseFile(object sender, RoutedEventArgs e)
        {

        }
        private string _whiteBoardNumber;

        public string WhiteBoardNumber
        {
            get => _whiteBoardNumber;
            set
            {
                _whiteBoardNumber = value;
                OnPropertyChanged(nameof(WhiteBoardNumber));
            }
        }

        private string _whiteBoardInnerNumber;
        public string WhiteBoardInnerNumber
        {
            get => _whiteBoardInnerNumber;
            set
            {
                _whiteBoardInnerNumber = value;
                OnPropertyChanged(nameof(WhiteBoardInnerNumber));
            }
        }

        private void OnReadClick(object sender, RoutedEventArgs e)
        {
            Read();
        }

        private void Read()
        {
            try
            {
                _productInfo = _spectrometer.GetProductInfo();

                var model = _productInfo.InternalModel;
                //InternalModelTypes = GetInternalModelType(model);
                //InternalModel = InternalModelTypes.FirstOrDefault(x => x.Key == productInfo.DeviceType);
                //InstrumentLanguage = productInfo.DefaultLanguage;
                //HardwareVersion = productInfo.HardwardVersion;
                //SN = productInfo.SN;
                //DeviceModel = productInfo.DeviceModelName;
                WhiteBoardNumber = _productInfo.WhitePlate.Sn;
                WhiteBoardInnerNumber = _productInfo.WhitePlate.InnerNumber;
                SCI.Spectrum = new Spectrum(new ThreeNH.ColorAlgorithm.WavelengthRange(_spectrometer.WavelengthRange.Min, _spectrometer.WavelengthRange.Max, _spectrometer.WavelengthRange.Step),
                    Array.ConvertAll(_productInfo.WhitePlate.Sci, f => (double)f));
                SCE.Spectrum = new Spectrum(new ThreeNH.ColorAlgorithm.WavelengthRange(_spectrometer.WavelengthRange.Min, _spectrometer.WavelengthRange.Max, _spectrometer.WavelengthRange.Step),
                    Array.ConvertAll(_productInfo.WhitePlate.Sce, f => (double)f));

                //SpectrographNumber = productInfo.Spectrograph.SN;
                //SpectrographCoefficients = productInfo.Spectrograph.Coefficients;
                //SpectrographOffset = productInfo.Spectrograph.Offset;

                //IoC.Get<Notifier>().ShowSuccess(S._("s_OperationSuccessful"));
            }
            catch (CommunicationException cex)
            {
                MessageBox.Show(S._(cex.ErrorCode.ToString()));
            }
        }

        public bool Verify()
        {
            if (string.IsNullOrEmpty(WhiteBoardNumber))
            {
                MessageBox.Show(S._("PleaseInputExternalNumber"));
                return false;
            }

            if (!SCI.Verify() || !SCE.Verify())
            {
                return false;
            }


            return true;
        }


        private void OnWriteClick(object sender, RoutedEventArgs e)
        {
            Write();
        }

        private void Write()
        {
            try
            {
                if (!Verify())
                    return;
                var sci = (from item in SCI.Spectrum.Data
                           select (float)item).ToArray();
                var sce = (from item in SCE.Spectrum.Data
                           select (float)item).ToArray();
                _spectrometer.SetProductInfo(ProductInfoField.WHITE_PLATE, new CalibrationBoard
                {
                    InnerNumber = WhiteBoardInnerNumber,
                    Sn = WhiteBoardNumber,
                    Sci = sci,
                    Sce = sce
                });
                _spectrometer.SetProductInfo(ProductInfoField.UPDATE,true);
                MessageBox.Show(S._("SuccessfullyWritten"));
            }
            catch (CommunicationException ex)
            {
                MessageBox.Show(S._(ex.ErrorCode.ToString()));
            }
        }

        private void OnSaveToFileClick(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Excel File(*.xlsx)|*.xlsx|Text File(*.txt)|*.txt|CSV File(*.csv)|*.csv";
            saveFileDialog.FileName = _spectrometer.Model + "_" + WhiteBoardNumber;
            if (saveFileDialog.ShowDialog().GetValueOrDefault())
            {
                FileInfo fileInfo = new FileInfo(saveFileDialog.FileName);
                switch (fileInfo.Extension.ToLower())
                {
                    case ".csv":
                        SaveAsCsvFile(fileInfo.FullName);
                        break;
                    case ".xlsx":
                        SaveAsXlsxFile(fileInfo.FullName);
                        break;
                    case ".txt":
                    default:
                        SaveAsTextFile(fileInfo.FullName);
                        break;
                }
            }
        }
        private void SaveAsXlsxFile(string fullName)
        {
            try
            {
                using (var wb = new XLWorkbook())
                {

                    var sheet = wb.AddWorksheet(string.IsNullOrEmpty(WhiteBoardNumber)
                        ? "Sheet 1"
                        : WhiteBoardNumber.Trim());


                    List<string> list = new List<string>();
                    list.Add("SCI");
                    list.AddRange(from x in SCI.Spectrum.Data select (x * 100).ToString("F2"));
                    var row = sheet.FirstRow();
                    row.Cell(1).Value = "Number";
                    row.Cell(2).Value = WhiteBoardNumber;

                    row = row.RowBelow();
                    var cell = row.FirstCell();
                    foreach (var item in list)
                    {
                        cell.Value = item;
                        cell = cell.CellRight();
                    }

                    list.Clear();
                    list.Add("SCE");
                    list.AddRange(from x in SCE.Spectrum.Data select (x * 100).ToString("F2"));
                    row = row.RowBelow();
                    cell = row.FirstCell();
                    foreach (var item in list)
                    {
                        cell.Value = item;
                        cell = cell.CellRight();
                    }

                    wb.SaveAs(fullName);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, S._("SaveToFile"),
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void SaveAsCsvFile(string fullName)
        {
            File.WriteAllText(fullName,
                string.Format("Number,{0}\r\nSCI,{1}\r\nSCE,{2}\r\n",
                    WhiteBoardNumber,
                    string.Join(",", Array.ConvertAll(SCI.Spectrum.Data, x => (x * 100).ToString("F2"))),
                    string.Join(",", Array.ConvertAll(SCE.Spectrum.Data, x => (x * 100).ToString("F2")))));
        }

        private void SaveAsTextFile(string path)
        {
            //var sci = string.Join(",", Array.ConvertAll(SCI.Spectrum.Data, _ => _*100.ToString("F2")));
            File.WriteAllText(path,
                string.Format("Number={0}\r\nSCI={1}\r\nSCE={2}\r\n",
                    WhiteBoardNumber,
                    string.Join(",", Array.ConvertAll(SCI.Spectrum.Data, x => (x * 100).ToString("F2"))),
                    string.Join(",", Array.ConvertAll(SCE.Spectrum.Data, x => (x * 100).ToString("F2")))));
        }

        private void OnLoadFromFileClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Excel File(*.xlsx)|*.xlsx|Text File(*.txt)|*.txt|CSV File(*.csv)|*.csv";
            if (openFileDialog.ShowDialog() != true)
                return;

            FileInfo fileInfo = new FileInfo(openFileDialog.FileName);
            Dictionary<string, object> dict = null;
            if (fileInfo.Extension.ToLower() == ".xlsx")
            {
                dict = LoadDataFromXlsx(fileInfo.FullName);
            }
            else
            {
                dict = LoadDataFromTxt(fileInfo.FullName);
            }

            if (dict == null)
            {
                MessageBox.Show(S._("DataFormatError"), S._("Label_LoadFromFile"),
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }


            WhiteBoardNumber = dict.ContainsKey("Number") ? dict["Number"] as string : "";

            float[] sci = null;
            float[] sce = null;
            try
            {
                if (dict.ContainsKey("SCI"))
                {
                    sci = ((List<string>)dict["SCI"]).ConvertAll(float.Parse).ToArray();
                }

                if (dict.ContainsKey("SCE"))
                {
                    sce = ((List<string>)dict["SCE"]).ConvertAll(float.Parse).ToArray();
                }

                if (sci == null || sce == null || sci.Length != ThreeNH.SQCX.Infrastructure.Instruments.PTS.Constants.VALID_WAVE_NUM ||
                    sce.Length != ThreeNH.SQCX.Infrastructure.Instruments.PTS.Constants.VALID_WAVE_NUM)
                {
                    MessageBox.Show(S._("DataFormatError"), S._("Label_LoadFromFile"),
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                SCI.Spectrum = new Spectrum(new ThreeNH.ColorAlgorithm.WavelengthRange(_spectrometer.WavelengthRange.Min, _spectrometer.WavelengthRange.Max, _spectrometer.WavelengthRange.Step),
                                    Array.ConvertAll(sci, x=>Convert.ToDouble(x/100)));
                SCE.Spectrum = new Spectrum(new ThreeNH.ColorAlgorithm.WavelengthRange(_spectrometer.WavelengthRange.Min, _spectrometer.WavelengthRange.Max, _spectrometer.WavelengthRange.Step),
                    Array.ConvertAll(sce, x => Convert.ToDouble(x/100)));
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, S._("Label_LoadFromFile"),
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

        }

        private Dictionary<string, object> LoadDataFromTxt(string path)
        {
            List<List<string>> list = new List<List<string>>();
            foreach (var line in File.ReadAllLines(path))
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var data = line.Split(new[] { ',', '=', ' ', '\t' }).ToList();
                list.Add(data);
            }

            var dict = new Dictionary<string, object>();
            foreach (var data in list)
            {
                if (data.Count < 1)
                    continue;

                switch (data[0].ToUpper())
                {
                    case "NUMBER":
                        dict["Number"] = data[1];
                        break;
                    case "SCI":
                        dict["SCI"] = data.GetRange(1, data.Count - 1);
                        break;
                    case "SCE":
                        dict["SCE"] = data.GetRange(1, data.Count - 1);
                        break;
                }
            }

            if (dict.Count == 0)
            {
                if (list.Count == 3)
                {
                    dict["Number"] = list[0][0];
                    dict["SCI"] = list[1];
                    dict["SCE"] = list[2];
                }
                else if (list.Count == 2)
                {
                    dict["SCI"] = list[0];
                    dict["SCE"] = list[1];
                }
                else if (list.Count == 1 && list[0].Count == ThreeNH.SQCX.Infrastructure.Instruments.PTS.Constants.VALID_WAVE_NUM * 2)
                {
                    dict["SCI"] = list[0].GetRange(0, ThreeNH.SQCX.Infrastructure.Instruments.PTS.Constants.VALID_WAVE_NUM);
                    dict["SCE"] = list[0].GetRange( param($mt) $map[$mt.Value] .Constants.VALID_WAVE_NUM,  param($mt) $map[$mt.Value] .Constants.VALID_WAVE_NUM);
                }
            }

            return dict.Count != 0 ? dict : null;
        }

        private Dictionary<string, object> LoadDataFromXlsx(string path)
        {
            try
            {
                using (var wb = new XLWorkbook(path))
                {
                    Dictionary<string, object> dict = new Dictionary<string, object>();
                    var sheet = wb.Worksheets.First();

                    var range = sheet.RangeUsed();
                    if (range.ColumnCount() >= ThreeNH.SQCX.Infrastructure.Instruments.PTS.Constants.VALID_WAVE_NUM) // 横向
                    {
                        var numberRow = range.FindRow(_ => _.FirstCell().Value.ToString().ToUpper() == "NUMBER");
                        if (numberRow != null)
                        {
                            dict["Number"] = numberRow.Cell(2).Value.ToString();
                        }

                        var sciRow = range.FindRow(_ => _.FirstCell().Value.ToString().ToUpper() == "SCI");
                        if (sciRow != null)
                        {
                            var values = from cell in sciRow.Cells(_ => _.DataType == XLDataType.Number)
                                         select cell.Value.ToString();
                            dict["SCI"] = values.ToList().GetRange(0, ThreeNH.SQCX.Infrastructure.Instruments.PTS.Constants.VALID_WAVE_NUM);
                        }

                        var sceRow = range.FindRow(_ => _.FirstCell().Value.ToString().ToUpper() == "SCE");
                        if (sceRow != null)
                        {
                            var values = from cell in sceRow.Cells(_ => _.DataType == XLDataType.Number)
                                         select cell.Value.ToString();
                            dict["SCE"] = values.ToList().GetRange(0, ThreeNH.SQCX.Infrastructure.Instruments.PTS.Constants.VALID_WAVE_NUM);
                        }

                        if (dict.Count == 0)
                        {
                            var rows = range.Rows(_ =>
                                _.CellCount() >= ThreeNH.SQCX.Infrastructure.Instruments.PTS.Constants.VALID_WAVE_NUM &&
                                _.Cells().All(cell => cell.DataType == XLDataType.Number));
                            if (rows != null && rows.Count() == 2)
                            {
                                dict["SCI"] = (from cell in rows.ElementAt(0).Cells() select cell.Value.ToString())
                                    .ToList();
                                dict["SCE"] = (from cell in rows.ElementAt(1).Cells() select cell.Value.ToString())
                                    .ToList();
                            }
                        }
                    }
                    else if (range.RowCount() >= ThreeNH.SQCX.Infrastructure.Instruments.PTS.Constants.VALID_WAVE_NUM)
                    {
                        // 第一行是名称

                        var numberColumn = range.FindColumn(_ => _.FirstCell().Value.ToString().ToUpper() == "NUMBER");
                        if (numberColumn != null)
                        {
                            dict["Number"] = numberColumn.Cell(2).Value.ToString();
                        }

                        var sciColumn = range.FindColumn(_ => _.FirstCell().Value.ToString().ToUpper() == "SCI");
                        if (sciColumn != null)
                        {
                            var values = from cell in sciColumn.Cells(_ => _.DataType == XLDataType.Number)
                                         select cell.Value.ToString();
                            dict["SCI"] = values.ToList().GetRange(0, ThreeNH.SQCX.Infrastructure.Instruments.PTS.Constants.VALID_WAVE_NUM);
                        }

                        var sceColumn = range.FindColumn(_ => _.FirstCell().Value.ToString().ToUpper() == "SCE");
                        if (sceColumn != null)
                        {
                            var values = from cell in sceColumn.Cells(_ => _.DataType == XLDataType.Number)
                                         select cell.Value.ToString();
                            dict["SCE"] = values.ToList().GetRange(0, ThreeNH.SQCX.Infrastructure.Instruments.PTS.Constants.VALID_WAVE_NUM);
                        }

                        if (dict.Count == 0)
                        {
                            var columns = range.Columns(_ =>
                                _.CellCount() >= ThreeNH.SQCX.Infrastructure.Instruments.PTS.Constants.VALID_WAVE_NUM &&
                                _.Cells().All(cell => cell.DataType == XLDataType.Number));
                            if (columns != null && columns.Count() == 2)
                            {
                                dict["SCI"] = (from cell in columns.ElementAt(0).Cells() select cell.Value.ToString())
                                    .ToList().GetRange(0, ThreeNH.SQCX.Infrastructure.Instruments.PTS.Constants.VALID_WAVE_NUM);
                                dict["SCE"] = (from cell in columns.ElementAt(1).Cells() select cell.Value.ToString())
                                    .ToList().GetRange(0, ThreeNH.SQCX.Infrastructure.Instruments.PTS.Constants.VALID_WAVE_NUM);
                            }
                        }
                    }

                    return dict.Count > 0 ? dict : null;
                }
            }
            catch (Exception ex)
            {
                return null;
            }

            return null;
        }
    }
}
