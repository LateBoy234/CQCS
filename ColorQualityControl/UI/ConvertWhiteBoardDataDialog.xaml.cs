using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ClosedXML.Excel;
using Microsoft.Win32;
using ThreeNH.ColorAlgorithm;
using ThreeNH.ColorQualityControl.Annotations;
using ThreeNH.DataModel;
using ThreeNH.Optical;
using Path = System.IO.Path;

namespace ThreeNH.ColorQualityControl.UI
{
    /// <summary>
    /// ConvertWhiteBoardDataDialog.xaml 的交互逻辑
    /// </summary>
    public partial class ConvertWhiteBoardDataDialog : Window, INotifyPropertyChanged
    {
        private XLWorkbook _workbook = null;

        public ConvertWhiteBoardDataDialog(string filePath)
        {
            InitializeComponent();
            DataContext = this;
            waveRangeCmbBox.ItemsSource = new string[]
            {
                "400~700",
                "360~740",
                "380~740",
                "360~780",
                "380~780",
                "300~780",
            };

            SetWorkbookPath(filePath);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public ObservableCollection<string> Columns { get; } = new ObservableCollection<string>();

        public ObservableCollection<int> Rows { get; } = new ObservableCollection<int>();


        private string _labStartColumn = null;

        public string LabStartColumn
        {
            get { return _labStartColumn; }
            set
            {
                if (value != _labStartColumn)
                {
                    _labStartColumn = value;
                    NotifyPropertyChanged(nameof(LabStartColumn));
                }
            }
        }


        private string _waveStartColumn = null;
        public string WaveStartColumn
        {
            get { return _waveStartColumn; }
            set
            {
                if (value != _waveStartColumn)
                {
                    _waveStartColumn = value;
                    NotifyPropertyChanged("WaveStartColumn");
                }
            }
        }

        private string _waveRange = null;
        public string WavelengthRange
        {
            get { return _waveRange; }
            set
            {
                if (value != _waveRange)
                {
                    _waveRange = value;
                    NotifyPropertyChanged("WavelengthRange");
                }
            }
        }

        private string _endRow;
        public string EndRow
        {
            get { return _endRow; }
            set
            {
                if (value != _endRow)
                {
                    _endRow = value;
                    NotifyPropertyChanged("EndRow");
                }
            }
        }


        private int _startRow = 0;
        public int StartRow
        {
            get { return _startRow; }
            set
            {
                if (value != _startRow)
                {
                    _startRow = value;
                    NotifyPropertyChanged("StartRow");
                }
            }
        }

        public void SetWorkbookPath(string filePath)
        {

            try
            {
                _workbook = new XLWorkbook(filePath);
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.Message);
                MessageBox.Show(ex.Message, Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (sheetNameComboBox != null)
            {
                sheetNameComboBox.ItemsSource = from sheet in _workbook.Worksheets select sheet.Name;
                sheetNameComboBox.SelectedIndex = 0;
            }

        }
        private void FindWavelengthRangeAndColumns(IXLColumns columns)
        {
            Regex pattern = new Regex(@"[^\d]*([3-7]\d\d)[^\d]*");
            List<int> waves = new List<int>();
            List<IXLColumn> waveColumns = new List<IXLColumn>();
            foreach (var column in columns)
            {
                var cell = column.FirstCell();
                if (cell.DataType == XLDataType.Text)
                {
                    string text = cell.GetString();
                    var match = pattern.Match(text);
                    if (match.Success)
                    {
                        waves.Add(int.Parse(match.Groups[1].Value));
                        waveColumns.Add(column);
                    }
                }
                else if (cell.DataType == XLDataType.Number)
                {
                    int number = cell.GetValue<int>();
                    if (number >= 360 && number <= 780)
                    {
                        waves.Add(number);
                        waveColumns.Add(column);
                    }
                }
            }
            if (waves.Count > 0)
            {
                WaveStartColumn = waveColumns[0].ColumnLetter();
                WavelengthRange = string.Format("{0}~{1}", waves.First(), waves.Last());
            }
        }

        private string FindLColumn(IXLColumns columns)
        {
            foreach (var column in columns)
            {
                string text = column.Cell(1).GetString();
                if (text != null && (text.Trim() == "L" || text.Trim() == "L*"))
                {
                    return column.ColumnLetter();
                }
            }
            return null;
        }


        private IXLWorksheet GetSheetByName(string sheetName)
        {
            foreach (var sheet in _workbook.Worksheets)
            {
                if (sheet.Name == sheetName)
                {
                    return sheet;
                }
            }
            return null;
        }

        private void sheetNameComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Columns.Clear();
            Rows.Clear();
            StartRow = 0;
            WavelengthRange = null;
            WaveStartColumn = null;

            if (_workbook == null)
            {
                return;
            }

            string sheetName = sheetNameComboBox.SelectedItem as string;
            IXLWorksheet sheet = GetSheetByName(sheetName);
            if (sheet == null)
            {
                Trace.TraceError("找不到表单{0}", sheetName);
                return;
            }

            IXLColumns columns = sheet.ColumnsUsed();
            foreach (var column in columns)
            {
                Columns.Add(column.ColumnLetter());
            }

            IXLRows rows = sheet.RowsUsed();
            foreach (var row in rows)
            {
                Rows.Add(row.RowNumber());
            }

            // 从第二行开始
            StartRow = 2;

            // 查找L、a、b所在列
            LabStartColumn = FindLColumn(columns);

            // 查找反射率起始列位置及范围
            FindWavelengthRangeAndColumns(columns);
        }


        public WavelengthRange GetWavelengthRange()
        {
            var tmp = WavelengthRange.Split('~');
            return new WavelengthRange(int.Parse(tmp[0]), int.Parse(tmp[1]), 10);
        }

        public bool ReplaceInLocal { get; set; } = false;


        private static int ColumnLetterToNumber(string letter)
        {
            int number = 0;
            foreach (var ch in letter)
            {
                number = number * 26 + ch - 'A';
            }
            return number + 1;
        }

        private bool ReadStandardFromSheet(IXLWorksheet sheet)
        {
            var formula = new Formula(StandardIlluminant.D65, StandardObserver.CIE1964);
            WavelengthRange waveRange = GetWavelengthRange();
            var lastRowNumber = sheet.LastRowUsed().RowNumber();
            for (int rowNumber = StartRow; rowNumber <= lastRowNumber; rowNumber++)
            {
                try
                {
                    IXLRow row = sheet.Row(rowNumber);
                    // 跳过空行
                    if (row.IsEmpty(false))
                    {
                        continue;
                    }

                    Spectrum s = new Spectrum(waveRange);
                    int startColumnNumber = ColumnLetterToNumber(WaveStartColumn);
                    for (int i = 0; i < waveRange.Count; i++)
                    {
                        try
                        {
                            s.Data[i] = row.Cell(i + startColumnNumber).GetDouble() / 100.0;
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"读取第{rowNumber}行第{startColumnNumber + i}列出错：{ex.Message}",
                                Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                            return false;
                        }
                    }

                    // 替换原来的Lab值
                    var lab = formula.CalculateLab(s);
                    var cell = row.Cell(LabStartColumn);
                    cell.Value = lab.L.ToString("F2");
                    cell = cell.CellRight();
                    cell.Value = lab.a.ToString("F2");
                    cell = cell.CellRight();
                    cell.Value = lab.b.ToString("F2");
                    // 将替换的Lab的颜色设为绿色
                    if (ReplaceInLocal)
                    {
                        var range = sheet.Range(row.Cell(LabStartColumn), row.Cell(LabStartColumn).CellRight(2));
                        range.Style.Font.FontColor = XLColor.Green;
                    }

                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
            }

            if (ReplaceInLocal)
            {
                _workbook.Save();
            }
            else
            {
                SaveFileDialog fileDlg = new SaveFileDialog();
                fileDlg.Title = S._("MenuItem_ConvertWhiteBoardData");
                fileDlg.Filter = S._("ExcelFileFilter");
                if (fileDlg.ShowDialog().GetValueOrDefault())
                {
                    _workbook.SaveAs(fileDlg.FileName);
                }
            }

            return true;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            string sheetName = sheetNameComboBox.SelectedItem as string;
            IXLWorksheet sheet = GetSheetByName(sheetName);

            if (sheet == null)
            {
                MessageBox.Show(S._("PleaseSelectASheet"));
                sheetNameComboBox.Focus();
                return;
            }

            if (StartRow < 1)
            {
                MessageBox.Show(S._("PleaseSelectStartRow"));
                return;
            }

            if (WaveStartColumn == null)
            {
                MessageBox.Show(S._("Please select start column of the spectrum"));
                return;
            }

            if (LabStartColumn == null)
            {
                MessageBox.Show(S._("Please select start column of the Lab"));
                return;
            }

            DialogResult = ReadStandardFromSheet(sheet);
        }

    }
}
