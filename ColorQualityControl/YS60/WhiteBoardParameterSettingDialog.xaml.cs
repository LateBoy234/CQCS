﻿﻿﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
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
using ThreeNH.SQCX.Infrastructure.Instruments.BTS;
using ClosedXML.Excel;
using Microsoft.Win32;
using ThreeNH.ColorAlgorithm;
using ThreeNH.ColorimeterInterface;
using ThreeNH.SQCX.Infrastructure.Instruments.Spectrophotometer;

namespace ThreeNH.ColorQualityControl.YS60
{
    /// <summary>
    /// WhiteBoardParameterSettingDialog.xaml 的交互逻辑
    /// </summary>
    public partial class WhiteBoardParameterSettingDialog : Window, INotifyPropertyChanged
    {
        int _wavelength_minimum = 360;
        int _wavelength_maximum = 780;

        public WhiteBoardParameterSettingDialog(ThreeNH.SQCX.Infrastructure.Instruments.BTS.Spectrometer spectrometer)
        {
            Spectrometer = spectrometer;
            _wavelength_minimum = 360;
            _wavelength_maximum = 780;
            InitializeComponent();
        }

        public WhiteBoardParameterSettingDialog(TS8450 spectrometer)
        {
            Spectrometer = spectrometer;
            _wavelength_minimum = spectrometer.WavelengthRange.Min;
            _wavelength_maximum = spectrometer.WavelengthRange.Max;
            InitializeComponent();
        }

        

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        public int WavelengthMinimum { 
            get=>_wavelength_minimum;
            set
            {
                if (_wavelength_minimum != value) {  
                    _wavelength_minimum = value;
                    NotifyPropertyChanged(nameof(WavelengthMinimum));
                }
            }
        }

        public int WavelengthMaximum
        {
            get => _wavelength_maximum;
            set
            {
                if (_wavelength_maximum != value)
                {
                    _wavelength_maximum = value;
                    NotifyPropertyChanged(nameof(WavelengthMaximum));
                }
            }
        }

        public IInstrument Spectrometer { get; set; }

        /// <summary>
        /// 验证用户输入是否有效
        /// </summary>
        /// <returns>如果输入有效返回真；否则返回假</returns>
        public bool Verify()
        {
            OuterNumber.Text = OuterNumber.Text.Trim();

            if (string.IsNullOrEmpty(OuterNumber.Text))
            {
                MessageBox.Show(S._("PleaseInputExternalNumber"));
                OuterNumber.Focus();
                return false;
            }

            if (!SCI.Verify() || !SCE.Verify())
            {
                return false;
            }


            return true;
        }

        public ThreeNH.SQCX.Infrastructure.Instruments.BTS.WhiteBoardParameter WhiteBoardParameter
        {
            get
            {
                 param($mt) $map[$mt.Value] .WhiteBoardParameter para = new  param($mt) $map[$mt.Value] .WhiteBoardParameter();
                para.Sn = OuterNumber.Text;
                // 由于YS60的白板戊数是百分比，所以转换回时需乘以100
                para.Sci = Array.ConvertAll(SCI.Spectrum.Data, _ => (float) (_ * 100.0));
                para.Sce = Array.ConvertAll(SCE.Spectrum.Data, _ => (float) (_ * 100.0));

                return para;
            }
            set
            {
                OuterNumber.Text = value.Sn;
                var range = new WavelengthRange( param($mt) $map[$mt.Value] .Constants.VALID_WAVE_BEGIN,  param($mt) $map[$mt.Value] .Constants.VALID_WAVE_END,
                    ThreeNH.SQCX.Infrastructure.Instruments.BTS.Constants.VALID_WAVE_STEP);
                // 由于YS60的白板参数是百分比，所以要除以100
                SCI.Spectrum = new Spectrum(range, Array.ConvertAll(value.Sci, _ => (double) _ / 100.0));
                SCE.Spectrum = new Spectrum(range, Array.ConvertAll(value.Sce, _ => (double) _ / 100.0));
            }
        }

        private WhiteBoardParameter GetWhiteBoardParameter()
        {
            if (Spectrometer is ThreeNH.SQCX.Infrastructure.Instruments.BTS.Spectrometer ys60)
            {
                WhiteBoardParameter = ys60.GetWhiteBoardParameter();
            }
            else if (Spectrometer is TS8450 tS8450)
            {
                tS8450.GetWhiteboard(out var sn, out var number, out var sci, out var sce);
                return new WhiteBoardParameter
                {
                    Sn = sn,
                    Sci = Array.ConvertAll(sci.Data, x => (float)(x * 100)),
                    Sce = Array.ConvertAll(sce.Data, y => (float)(y * 100)),
                };

            }
            throw new NotImplementedException();
        }

        private void SetWhiteBoardParameter(WhiteBoardParameter para)
        {
            if (Spectrometer is ThreeNH.SQCX.Infrastructure.Instruments.BTS.Spectrometer ys60)
            {
                ys60.SetWhiteBoardParameter(WhiteBoardParameter);
                return;
            }
            else if (Spectrometer is TS8450 tS8450)
            {
                var sn = para.Sn;
                var wr = new ThreeNH.ColorAlgorithm.WavelengthRange(tS8450.WavelengthRange.Min, tS8450.WavelengthRange.Max, tS8450.WavelengthRange.Step);
                var sci = new Spectrum(wr, Array.ConvertAll(para.Sci, x => (double)x/100));
                var sce = new Spectrum(wr, Array.ConvertAll(para.Sce, x => (double)x/100));
                tS8450.SetWhitboard(sn, sn, sci, sce);
                return;
            }
            throw new NotImplementedException();
        }

        private void OnReadClick(object sender, RoutedEventArgs e)
        {
            try
            {
                WhiteBoardParameter = GetWhiteBoardParameter();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void OnWriteClick(object sender, RoutedEventArgs e)
        {
            if (Verify())
            {
                try
                {
                    SetWhiteBoardParameter(WhiteBoardParameter);
                    MessageBox.Show(S._("WriteWhiteBoardParameterCompleted"),
                        S._("MenuItem_WhiteBoardParameterSetting"));
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void OnSaveToFileClick(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Excel File(*.xlsx)|*.xlsx|Text File(*.txt)|*.txt|CSV File(*.csv)|*.csv";
            saveFileDialog.FileName = Spectrometer.Model + "_" + WhiteBoardParameter.Sn;
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
                    var param = WhiteBoardParameter;
                    var sheet = wb.AddWorksheet(string.IsNullOrEmpty(OuterNumber.Text)
                        ? "Sheet 1"
                        : OuterNumber.Text.Trim());


                    List<string> list = new List<string>();
                    list.Add("SCI");
                    list.AddRange(from x in param.Sci select x.ToString("F2"));
                    var row = sheet.FirstRow();
                    row.Cell(1).Value = "Number";
                    row.Cell(2).Value = OuterNumber.Text.Trim();

                    row = row.RowBelow();
                    var cell = row.FirstCell();
                    foreach (var item in list)
                    {
                        cell.Value = item;
                        cell = cell.CellRight();
                    }

                    list.Clear();
                    list.Add("SCE");
                    list.AddRange(from x in param.Sce select x.ToString("F2"));
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
            var param = WhiteBoardParameter;
            File.WriteAllText(fullName,
                string.Format("Number,{0}\r\nSCI,{1}\r\nSCE,{2}\r\n",
                    OuterNumber.Text.Trim(),
                    string.Join(",", Array.ConvertAll(param.Sci, _ => _.ToString("F2"))),
                    string.Join(",", Array.ConvertAll(param.Sce, _ => _.ToString("F2")))));
        }

        private void SaveAsTextFile(string path)
        {
            var param = WhiteBoardParameter;
            File.WriteAllText(path,
                string.Format("Number={0}\r\nSCI={1}\r\nSCE={2}\r\n",
                    OuterNumber.Text.Trim(),
                    string.Join(",", Array.ConvertAll(param.Sci, _ => _.ToString("F2"))),
                    string.Join(",", Array.ConvertAll(param.Sce, _ => _.ToString("F2")))));
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

                var data = line.Split(new[] {',', '=', ' ', '\t'}).ToList();
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
                else if (list.Count == 1 && list[0].Count ==  param($mt) $map[$mt.Value] .Constants.VALID_WAVE_NUM * 2)
                {
                    dict["SCI"] = list[0].GetRange(0, ThreeNH.SQCX.Infrastructure.Instruments.BTS.Constants.VALID_WAVE_NUM);
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
                    if (range.ColumnCount() >=  param($mt) $map[$mt.Value] .Constants.VALID_WAVE_NUM) // 横向
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
                            dict["SCI"] = values.ToList().GetRange(0,  param($mt) $map[$mt.Value] .Constants.VALID_WAVE_NUM);
                        }

                        var sceRow = range.FindRow(_ => _.FirstCell().Value.ToString().ToUpper() == "SCE");
                        if (sceRow != null)
                        {
                            var values = from cell in sceRow.Cells(_ => _.DataType == XLDataType.Number)
                                select cell.Value.ToString();
                            dict["SCE"] = values.ToList().GetRange(0,  param($mt) $map[$mt.Value] .Constants.VALID_WAVE_NUM);
                        }

                        if (dict.Count == 0)
                        {
                            var rows = range.Rows(_ =>
                                _.CellCount() >=  param($mt) $map[$mt.Value] .Constants.VALID_WAVE_NUM &&
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
                    else if (range.RowCount() >=  param($mt) $map[$mt.Value] .Constants.VALID_WAVE_NUM)
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
                            dict["SCI"] = values.ToList().GetRange(0,  param($mt) $map[$mt.Value] .Constants.VALID_WAVE_NUM);
                        }

                        var sceColumn = range.FindColumn(_ => _.FirstCell().Value.ToString().ToUpper() == "SCE");
                        if (sceColumn != null)
                        {
                            var values = from cell in sceColumn.Cells(_ => _.DataType == XLDataType.Number)
                                         select cell.Value.ToString();
                            dict["SCE"] = values.ToList().GetRange(0,  param($mt) $map[$mt.Value] .Constants.VALID_WAVE_NUM);
                        }

                        if (dict.Count == 0)
                        {
                            var columns = range.Columns(_ =>
                                _.CellCount() >=  param($mt) $map[$mt.Value] .Constants.VALID_WAVE_NUM &&
                                _.Cells().All(cell => cell.DataType == XLDataType.Number));
                            if (columns != null && columns.Count() == 2)
                            {
                                dict["SCI"] = (from cell in columns.ElementAt(0).Cells() select cell.Value.ToString())
                                    .ToList().GetRange(0,  param($mt) $map[$mt.Value] .Constants.VALID_WAVE_NUM);
                                dict["SCE"] = (from cell in columns.ElementAt(1).Cells() select cell.Value.ToString())
                                    .ToList().GetRange(0,  param($mt) $map[$mt.Value] .Constants.VALID_WAVE_NUM);
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

        private void OnLoadFromFileClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Excel File(*.xlsx)|*.xlsx|Text File(*.txt)|*.txt|CSV File(*.csv)|*.csv";
            if (openFileDialog.ShowDialog() != true)
                return;

            FileInfo fileInfo = new FileInfo(openFileDialog.FileName);
            WhiteBoardParameter param = new WhiteBoardParameter();
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


            param.Sn = dict.ContainsKey("Number") ? dict["Number"] as string : "";

            float[] sci = null;
            float[] sce = null;
            try
            {
                if (dict.ContainsKey("SCI"))
                {
                    sci = ((List<string>) dict["SCI"]).ConvertAll(float.Parse).ToArray();
                }

                if (dict.ContainsKey("SCE"))
                {
                    sce = ((List<string>) dict["SCE"]).ConvertAll(float.Parse).ToArray();
                }

                if (sci == null || sce == null || sci.Length != ThreeNH.SQCX.Infrastructure.Instruments.BTS.Constants.VALID_WAVE_NUM ||
                    sce.Length !=  param($mt) $map[$mt.Value] .Constants.VALID_WAVE_NUM)
                {
                    MessageBox.Show(S._("DataFormatError"), S._("Label_LoadFromFile"),
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                param.Sci = sci;
                param.Sce = sce;
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, S._("Label_LoadFromFile"),
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }


            WhiteBoardParameter = param;
        }
    }
}