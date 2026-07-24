using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using ThreeNH.ColorAlgorithm;
using ThreeNH.DataModel;
using ThreeNH.Optical;
using ThreeNH.ColorSpecimen;
using Window = System.Windows.Window;

namespace ThreeNH.ColorQualityControl
{
    [ValueConversion(typeof(InstrumentType), typeof(int))]
    public class InstrumentTypeToInt : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            InstrumentType instrumentType = (InstrumentType) value;
            return (int) instrumentType;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int i = (int) value;
            if (i < 5)
            {
                return (InstrumentType) i;
            }

            return InstrumentType.Unknown;
        }
    }

    public class IlluminantObserverPair
    {
        private readonly StandardIlluminant _illuminant;
        private readonly StandardObserver _observer;

        public IlluminantObserverPair(StandardIlluminant illuminant, StandardObserver observer)
        {
            _illuminant = illuminant;
            _observer = observer;
        }

        public StandardIlluminant Illuminant => _illuminant;
        public StandardObserver Observer => _observer;

        public override int GetHashCode()
        {
            return (((int) _illuminant) << 8) | (int) _observer;
        }

        public override bool Equals(object obj)
        {
            if (obj == this) return true;
            if (obj == null) return false;
            if (obj is IlluminantObserverPair pair)
            {
                return GetHashCode() == pair.GetHashCode();
            }

            return false;
        }

        public override string ToString()
        {
            return string.Format("{0}/{1}\u00B0",
                _illuminant, _observer == StandardObserver.CIE1931 ? 2 : 10);
        }
    }

    /// <summary>
    /// ImportStandardsDialog.xaml 的交互逻辑
    /// </summary>
    public partial class ImportStandardsDialog : Window, INotifyPropertyChanged
    {
        XLWorkbook _workbook = null;
        ObservableCollection<string> _columns = new ObservableCollection<string>();
        ObservableCollection<int> _rows = new ObservableCollection<int>();

        public event PropertyChangedEventHandler PropertyChanged;

        public ImportStandardsDialog(string filePath)
        {
            var list = new List<IlluminantObserverPair>();
            foreach (StandardIlluminant illuminant in Enum.GetValues(typeof(StandardIlluminant)))
            {
                if (illuminant == StandardIlluminant.Unknown)
                    continue;
                list.Add(new IlluminantObserverPair(illuminant, StandardObserver.CIE1964));
                list.Add(new IlluminantObserverPair(illuminant, StandardObserver.CIE1931));
            }

            IlluminantObserverPairs = list.ToArray();
            SelectedIlluminantObserver = IlluminantObserverPairs[0];

            InitializeComponent();
            DataContext = this;
            waveRangeCmbBox.ItemsSource = new string[]
            {
                S._("None"),
                "400~700",
                "360~740",
                "380~740",
                "360~780",
                "380~780",
                "300~780",
            };

            SetWorkbookPath(filePath);
        }

        private void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public ObservableCollection<string> Columns
        {
            get { return _columns; }
        }

        public ObservableCollection<int> Rows
        {
            get { return _rows; }
        }

        public IlluminantObserverPair[] IlluminantObserverPairs { get; }

        public IlluminantObserverPair SelectedIlluminantObserver
        {
            get => _selectedIlluminantObserver;
            set
            {
                if (!Equals(_selectedIlluminantObserver, value))
                {
                    _selectedIlluminantObserver = value;
                    NotifyPropertyChanged(nameof(SelectedIlluminantObserver));
                }
            }
        }

        private string _xyzStartColumn = null;
        public string XYZStartColumn
        {
            get { return _xyzStartColumn; }
            set
            {
                if (value != _xyzStartColumn)
                {
                    _xyzStartColumn = value;
                    NotifyPropertyChanged("XYZStartColumn");
                }
            }
        }

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

        private string _nameColumn = null;
        public string NameColumn
        {
            get { return _nameColumn; }
            set
            {
                if (value != _nameColumn)
                {
                    _nameColumn = value;
                    NotifyPropertyChanged("NameColumn");
                }
            }
        }

        private string _specularComponentColumn = null;
        public string SpecularComponentColumn
        {
            get { return _specularComponentColumn; }
            set
            {
                if (value != _specularComponentColumn)
                {
                    _specularComponentColumn = value;
                    NotifyPropertyChanged(nameof(SpecularComponentColumn));
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
        private IlluminantObserverPair _selectedIlluminantObserver;

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
//                FileStream fileStream = File.OpenRead(filePath);
                FileStream fileStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                _workbook = new XLWorkbook(fileStream);
                fileStream.Close();

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

        private string FindXColumn(IXLColumns columns)
        {
            foreach (var column in columns)
            {
                string text = column.Cell(1).GetString();
                if (text != null && text.Trim() == "X")
                {
                    return column.ColumnLetter();
                }
            }
            return null;
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

        private string FindNameColumn(IXLWorksheet sheet)
        {
            IXLColumns columns = sheet.ColumnsUsed();
            int rowCount = sheet.RowsUsed().Count();

            foreach (var column in columns)
            {
                string text = column.Cell(1).GetString();
                if (text != null && (text.ToUpper().Contains("NAME") || text.Contains(S._("Name"))))
                {
                    return column.ColumnLetter();
                }
            }


            foreach (var column in columns)
            {
                var cells = column.CellsUsed();
                var count = (from cell in cells where cell.DataType == XLDataType.Text select cell).Distinct().Count();
                if (count >= (rowCount - 1) / 2)
                {
                    return column.ColumnLetter();
                }
            }

            return null;
        }

        /// <summary>识别 SQCX 导出表中的测光方式列（表头或 SCI/SCE 取值）。</summary>
        private string FindSpecularComponentColumn(IXLWorksheet sheet)
        {
            IXLColumns columns = sheet.ColumnsUsed();
            foreach (var column in columns)
            {
                string text = column.Cell(1).GetString();
                if (string.IsNullOrWhiteSpace(text))
                {
                    continue;
                }

                text = text.Trim();
                if (text.IndexOf(S._("SpecularComponent"), StringComparison.OrdinalIgnoreCase) >= 0
                    || text.IndexOf("SpecularComponent", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return column.ColumnLetter();
                }
            }

            int headerRow = sheet.FirstRowUsed()?.RowNumber() ?? 1;
            foreach (var column in columns)
            {
                int sciSceCount = 0;
                int dataCount = 0;
                foreach (var cell in column.CellsUsed())
                {
                    if (cell.Address.RowNumber <= headerRow)
                    {
                        continue;
                    }

                    dataCount++;
                    if (TryParseSpecularComponentCell(cell.GetString(), out _))
                    {
                        sciSceCount++;
                    }
                }

                if (dataCount > 0 && sciSceCount >= (dataCount + 1) / 2)
                {
                    return column.ColumnLetter();
                }
            }

            return null;
        }

        private static bool TryParseSpecularComponentCell(string text, out SpecularComponent sc)
        {
            sc = SpecularComponent.Unknown;
            if (string.IsNullOrWhiteSpace(text) || text.Trim() == "--")
            {
                return false;
            }

            text = text.Trim();
            if (string.Equals(text, SpecularComponent.SCI.ToString(), StringComparison.OrdinalIgnoreCase)
                || string.Equals(text, S._("SpecularComponent_SCI"), StringComparison.OrdinalIgnoreCase))
            {
                sc = SpecularComponent.SCI;
                return true;
            }

            if (string.Equals(text, SpecularComponent.SCE.ToString(), StringComparison.OrdinalIgnoreCase)
                || string.Equals(text, S._("SpecularComponent_SCE"), StringComparison.OrdinalIgnoreCase))
            {
                sc = SpecularComponent.SCE;
                return true;
            }

            if (text.IndexOf("SCI", StringComparison.OrdinalIgnoreCase) >= 0
                && text.IndexOf("SCE", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                sc = SpecularComponent.SCIAndSCE;
                return true;
            }

            return false;
        }

        private static void AssignImportedColorData(Specimen specimen, SpecularComponent sc, ColorData clr)
        {
            if (sc == SpecularComponent.SCI)
            {
                specimen.SCI = clr;
            }
            else if (sc == SpecularComponent.SCE)
            {
                specimen.SCE = clr;
            }
            else
            {
                specimen.Clr = clr;
            }
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
            _columns.Clear();
            _rows.Clear();
            StartRow = 0;
            XYZStartColumn = null;
            WavelengthRange = null;
            WaveStartColumn = null;
            NameColumn = null;
            SpecularComponentColumn = null;
    
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
                _columns.Add(column.ColumnLetter());
            }

            IXLRows rows = sheet.RowsUsed();
            foreach (var row in rows)
            {
                _rows.Add(row.RowNumber());
            }

            // 查找X、Y、Z所在列
            XYZStartColumn = FindXColumn(columns);
            // 查找L、a、b所在列
            LabStartColumn = FindLColumn(columns);

            // 查找名称列、测光方式列
            NameColumn = FindNameColumn(sheet);
            SpecularComponentColumn = FindSpecularComponentColumn(sheet);
            if (scCmbBox != null)
            {
                // 表内已有测光方式列时按行读取，避免与下拉框全局 SCI/SCE 设置冲突
                scCmbBox.IsEnabled = string.IsNullOrWhiteSpace(SpecularComponentColumn);
            }

            if (NameColumn != null)
            {
                StartRow = sheet.FirstRowUsed().RowNumber() + 1;
            }
            else
            {
                StartRow = 2;
            }
      
            // 查找反射率起始列位置及范围
            FindWavelengthRangeAndColumns(columns);
        }


        public WavelengthRange GetWavelengthRange()
        {
            if (WavelengthRange == null || WavelengthRange == S._("None"))
            {
                return ColorAlgorithm.WavelengthRange.InvalidRange;
            }
            var tmp = WavelengthRange.Split('~');
            return new WavelengthRange(int.Parse(tmp[0]), int.Parse(tmp[1]), 10);
        }


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
            bool isTransitive = SpectralTypeCmbBox.SelectedIndex == 1;

            int rowNumberMax;
            if (!int.TryParse(EndRow, out rowNumberMax) || rowNumberMax < StartRow)
            {
                rowNumberMax = sheet.LastRowUsed().RowNumber();
            }

            var formula = new Formula(SelectedIlluminantObserver.Illuminant, SelectedIlluminantObserver.Observer);
            WavelengthRange waveRange = GetWavelengthRange();

            string opticalStruct = null;
            if (opticalStructCmbBox.SelectedIndex != 0)
            {
                opticalStruct = ((ComboBoxItem)opticalStructCmbBox.SelectedItem).Content as string;
            }

            if (!string.IsNullOrWhiteSpace(SpecularComponentColumn))
            {
                return ReadStandardsWithSpecularColumn(
                    sheet, rowNumberMax, formula, waveRange, isTransitive, opticalStruct);
            }

            return ReadStandardsWithGlobalSpecularMode(
                sheet, rowNumberMax, formula, waveRange, isTransitive, opticalStruct);
        }

        private bool ReadStandardsWithSpecularColumn(
            IXLWorksheet sheet,
            int rowNumberMax,
            Formula formula,
            WavelengthRange waveRange,
            bool isTransitive,
            string opticalStruct)
        {
            var standardsByName = new Dictionary<string, StandardSpecimen>(StringComparer.Ordinal);
            for (int rowNumber = StartRow; rowNumber <= rowNumberMax; rowNumber++)
            {
                try
                {
                    IXLRow row = sheet.Row(rowNumber);
                    if (row.IsEmpty(false))
                    {
                        continue;
                    }

                    if (!TryParseSpecularComponentCell(
                            row.Cell(SpecularComponentColumn).GetString(), out var sc))
                    {
                        sc = SpecularComponent.Unknown;
                    }

                    string name = NameColumn != null ? row.Cell(NameColumn).GetString() : null;
                    string groupKey = string.IsNullOrWhiteSpace(name)
                        ? $"__row_{rowNumber}"
                        : name;

                    if (!standardsByName.TryGetValue(groupKey, out var standard))
                    {
                        standard = new StandardSpecimen
                        {
                            Name = name,
                            OpticalStruct = opticalStruct,
                            IsTransmission = isTransitive
                        };
                        standardsByName[groupKey] = standard;
                    }

                    var clr = ReadColorDataFromRow(row, formula, waveRange);
                    if (clr == null)
                    {
                        continue;
                    }

                    AssignImportedColorData(standard, sc, clr);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
            }

            StandardSpecimens = standardsByName.Values.ToList();
            return StandardSpecimens.Count > 0;
        }

        private bool ReadStandardsWithGlobalSpecularMode(
            IXLWorksheet sheet,
            int rowNumberMax,
            Formula formula,
            WavelengthRange waveRange,
            bool isTransitive,
            string opticalStruct)
        {
            List<ColorData> swatches = new List<ColorData>();
            List<string> swatchNames = new List<string>();

            for (int rowNumber = StartRow; rowNumber <= rowNumberMax; rowNumber++)
            {
                try
                {
                    IXLRow row = sheet.Row(rowNumber);
                    if (row.IsEmpty(false))
                    {
                        continue;
                    }

                    var clr = ReadColorDataFromRow(row, formula, waveRange);
                    if (clr == null)
                    {
                        continue;
                    }

                    if (NameColumn != null)
                    {
                        swatchNames.Add(row.Cell(NameColumn).GetString());
                    }

                    swatches.Add(clr);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
            }

            List<StandardSpecimen> list = new List<StandardSpecimen>();
            for (int i = 0; i < swatches.Count; )
            {
                StandardSpecimen standard = new StandardSpecimen();
                standard.Name = NameColumn != null ? swatchNames[i] : null;
                standard.OpticalStruct = opticalStruct;
                standard.IsTransmission = isTransitive;

                if (scCmbBox.SelectedIndex == 0)
                {
                    standard.Clr = swatches[i++];
                }
                else if (scCmbBox.SelectedIndex == 1)
                {
                    standard.SCI = swatches[i++];
                }
                else if (scCmbBox.SelectedIndex == 2)
                {
                    standard.SCE = swatches[i++];
                }
                else if (scCmbBox.SelectedIndex == 3)
                {
                    standard.SCI = swatches[i++];
                    if (i < swatches.Count)
                    {
                        standard.SCE = swatches[i++];
                    }
                }
                else if (scCmbBox.SelectedIndex == 4)
                {
                    standard.SCE = swatches[i++];
                    if (i < swatches.Count)
                    {
                        standard.SCI = swatches[i++];
                    }
                }

                list.Add(standard);
            }

            StandardSpecimens = list;
            return true;
        }

        private ColorData ReadColorDataFromRow(IXLRow row, Formula formula, WavelengthRange waveRange)
        {
            ColorData clr = new ColorData();

            if (!string.IsNullOrWhiteSpace(WaveStartColumn) && waveRange.IsValid)
            {
                Spectrum s = new Spectrum(waveRange);
                int startColumnNumber = ColumnLetterToNumber(WaveStartColumn);
                for (int i = 0; i < waveRange.Count; i++)
                {
                    s.Data[i] = row.Cell(i + startColumnNumber).GetDouble() / 100.0;
                }

                clr.Spectrum = s;
                return clr;
            }

            if (!string.IsNullOrWhiteSpace(XYZStartColumn))
            {
                var cell = row.Cell(XYZStartColumn);
                double X = cell.GetDouble();
                cell = cell.CellRight();
                double Y = cell.GetDouble();
                cell = cell.CellRight();
                double Z = cell.GetDouble();

                clr.Set(formula.Observer, formula.Illuminant, new CIEXYZ(X, Y, Z));
                return clr;
            }

            if (!string.IsNullOrWhiteSpace(LabStartColumn))
            {
                var cell = row.Cell(LabStartColumn);
                double L = cell.GetDouble();
                cell = cell.CellRight();
                double a = cell.GetDouble();
                cell = cell.CellRight();
                double b = cell.GetDouble();

                clr.Set(formula.Observer, formula.Illuminant,
                    formula.LabToXyz(new CIELAB(L, a, b)));
                return clr;
            }

            return null;
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

            if (XYZStartColumn == null && WaveStartColumn == null && LabStartColumn == null)
            {
                MessageBox.Show(S._("PleaseTheStartColumnOfXYZOrSpectrum"));
                return;
            }


            if (ReadStandardFromSheet(sheet))
            {
                DialogResult = true;
            }
        }

        public IList<StandardSpecimen> StandardSpecimens { get; private set; }

    }
}
